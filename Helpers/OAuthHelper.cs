using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Levante.Configs;
using Levante.Util;

namespace Levante.Helpers
{
    public class OAuthHelper
    {
        private HttpListener _listener;

        public OAuthHelper()
        {
            _listener = new HttpListener();
            _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            _listener.Prefixes.Add("http://*:8080/");
            _listener.Start();
            _listener.BeginGetContext(new AsyncCallback(GetToken), _listener);
            LogHelper.ConsoleLog("[OAUTH] Listening.");
        }

        public async void GetToken(IAsyncResult ar)
        {
            if (!HttpListener.IsSupported)
            {
                LogHelper.ConsoleLog("[OAUTH] HttpListener is not supported.");
                return;
            }

            HttpListenerContext context = _listener.EndGetContext(ar);
            LogHelper.ConsoleLog("[OAUTH] Connection Received.");

            var query = context.Request.QueryString;

            CodeResult result = new()
            {
                DiscordDisplayName = $"Levante#3845",
                Reason = ErrorReason.None
            };

            if (query != null && query.Count > 0)
            {
                if (!string.IsNullOrEmpty(query["code"]))
                {
                    var base64EncodedBytes = Convert.FromBase64String($"{query["state"]}");
                    ulong discordId = ulong.Parse(Encoding.UTF8.GetString(base64EncodedBytes));
                    result = await ProcessCode($"{query["code"]}", discordId).ConfigureAwait(false);
                }
                else if (!string.IsNullOrEmpty(query["error"]))
                {
                    LogHelper.ConsoleLog($"[OAUTH] Error occurred: {query["error_description"]}.");
                    return;
                }
            }
            else
            {
                result.Reason = ErrorReason.MissingParameters;
            }

            _listener.BeginGetContext(new AsyncCallback(GetToken), _listener);
            LogHelper.ConsoleLog("[OAUTH] Sending Request.");

            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string responseString = "You are going to be redirected.";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

            response.ContentLength64 = buffer.Length;
            if (result.Reason != ErrorReason.None)
            {
                LogHelper.ConsoleLog("[OAUTH] Redirecting to Link Fail.");
                response.Redirect($"https://www.levante.dev/link-fail/?error={Convert.ToInt32(result.Reason)}");
            }
            else
            {
                LogHelper.ConsoleLog("[OAUTH] Redirecting to Link Success.");
                response.Redirect($"https://www.levante.dev/link-success/?discDisp={Uri.EscapeDataString(result.DiscordDisplayName)}");
            }

            // simulate work
            //await Task.Delay(500);

            try
            {
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // You must close the output stream.
                output.Close();
            }
            catch (Exception x)
            {
                LogHelper.ConsoleLog("[OAUTH] Unable to send response write data.");
            }
            LogHelper.ConsoleLog("[OAUTH] Flow completed. Listening...");
        }

        private async Task<CodeResult> ProcessCode(string Code, ulong DiscordID)
        {
            var result = new CodeResult()
            {
                DiscordDisplayName = $"Levante#3845",
                Reason = ErrorReason.None
            };

            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
                {
                    { "client_id", $"{BotConfig.BungieClientID}" },
                    { "client_secret", $"{BotConfig.BungieClientSecret}" },
                    { "Authorization",  $"Basic {Code}" },
                    { "Content-Type", "application/x-www-form-urlencoded" },
                    { "grant_type", "authorization_code" },
                    { "code", Code },
                };
                var postContent = new FormUrlEncodedContent(values);

                var response = client.PostAsync("https://www.bungie.net/Platform/App/OAuth/Token/", postContent).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                result.Access = item.access_token;
                result.Refresh = item.refresh_token;
                try
                {
                    result.AccessExpiration = TimeSpan.FromSeconds(double.Parse($"{item.expires_in}"));
                    result.RefreshExpiration = TimeSpan.FromSeconds(double.Parse($"{item.refresh_expires_in}"));
                }
                catch
                {
                    result.Reason = ErrorReason.Unknown;
                    return result;
                }

                string bungieTag = "";
                string memId = "";
                int memType = -2;
                try
                {
                    memType = GetMembershipDataFromBungieId($"{item.membership_id}", out memId, out bungieTag);
                }
                catch
                {
                    result.Reason = ErrorReason.OldCode;
                    return result;
                }

                if (memType <= -2)
                {
                    result.Reason = ErrorReason.NoProfileDataFound;
                    return result;
                }

                IUser user;
                if (LevanteCordInstance.Client.GetUser(DiscordID) == null)
                    user = LevanteCordInstance.Client.Rest.GetUserAsync(DiscordID).Result;
                else
                    user = LevanteCordInstance.Client.GetUser(DiscordID);

                if (user == null)
                {
                    result.Reason = ErrorReason.DiscordUserNotFound;
                    return result;
                }

                var auth = new EmbedAuthorBuilder()
                {
                    Name = $"Account Linking"
                };
                var foot = new EmbedFooterBuilder()
                {
                    Text = $"Powered by {BotConfig.AppName} v{BotConfig.Version}",
                };
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                    Author = auth,
                    Footer = foot,
                };
                embed.Description =
                    $"Linking Successful.\n" +
                    $"Your Discord account ({user.Mention}) is now linked to **{bungieTag}**.";

                embed.AddField(x =>
                {
                    x.Name = $"> Default Platform";
                    x.Value = $"{(Guardian.Platform)memType}";
                    x.IsInline = false;
                });
                try
                {
                    await user.SendMessageAsync(embed: embed.Build());
                }
                catch
                {
                    result.Reason = ErrorReason.NoDiscordMessageSent;
                    return result;
                }

                // Don't make users have to unlink to do this.
                if (DataConfig.IsExistingLinkedUser(user.Id))
                    DataConfig.DeleteUserFromConfig(user.Id);

                DataConfig.AddUserToConfig(user.Id, memId, $"{memType}", bungieTag, result);

                result.DiscordDisplayName = $"{user.Username}#{user.Discriminator}";
                return result;
            }
        }

        private int GetMembershipDataFromBungieId(string BungieID, out string MembershipID, out string BungieTag)
        {
            ///Platform/Destiny2/254/Profile/17125100/LinkedProfiles/
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                string memId = "";
                string memType = "";

                var memResponse = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/-1/Profile/{BungieID}/LinkedProfiles/?getAllMemberships=true").Result;
                var memContent = memResponse.Content.ReadAsStringAsync().Result;
                dynamic memItem = JsonConvert.DeserializeObject(memContent);

                var lastPlayed = new DateTime();
                var goodProfile = -1;

                if (memItem == null || memItem.ErrorCode != 1)
                {
                    BungieTag = null;
                    MembershipID = null;
                    return -2;
                }

                for (var j = 0; j < memItem.Response.profiles.Count; j++)
                {
                    if (memItem.Response.profiles[j].isCrossSavePrimary == true)
                    {
                        memType = memItem.Response.profiles[j].membershipType;
                        memId = memItem.Response.profiles[j].membershipId;
                        goodProfile = j;
                        break;
                    }

                    if (DateTime.Parse(memItem.Response.profiles[j].dateLastPlayed.ToString()) <= lastPlayed) continue;

                    lastPlayed = DateTime.Parse(memItem.Response.profiles[j].dateLastPlayed.ToString());
                    goodProfile = j;
                }

                if (goodProfile == -1)
                {
                    BungieTag = null;
                    MembershipID = null;
                    return -2;
                }

                memType = memItem.Response.profiles[goodProfile].membershipType;
                memId = memItem.Response.profiles[goodProfile].membershipId;

                LogHelper.ConsoleLog($"[OAUTH] Received tokens for {memItem.Response.bnetMembership.supplementalDisplayName} on platform {memType}.");

                MembershipID = $"{memId}";
                string bungieTagCode = $"{memItem.Response.bnetMembership.bungieGlobalDisplayNameCode}".PadLeft(4, '0');
                BungieTag = $"{memItem.Response.bnetMembership.bungieGlobalDisplayName}#{bungieTagCode}";
                return int.Parse($"{memType}");
            }
        }

        public class CodeResult
        {
            public string DiscordDisplayName;
            public ErrorReason Reason;
            public string Access;
            public string Refresh;
            public TimeSpan AccessExpiration;
            public TimeSpan RefreshExpiration;
        }

        public enum ErrorReason
        {
            None,
            MissingParameters,
            OldCode,
            NoProfileDataFound,
            DiscordUserNotFound,
            NoDiscordMessageSent,
            Unknown,
        }
    }
}
