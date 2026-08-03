using Discord;
using Discord.WebSocket;
using Levante.Configs;
using Levante.Structs;
using Levante.Util;
using Newtonsoft.Json;
using Serilog;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Dapper.SqlMapper;

namespace Levante.Services
{
    public class UserService
    {
        public readonly string FilePath = @"Data/userData.json";

        private HttpListener _listener;
        private Dictionary<ulong, User> users = new();

        public UserService()
        {
            if (!Directory.Exists(FilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                Log.Information("[{Type}] Created a directory.", "Users");
            }

            if (!File.Exists(FilePath))
            {
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(users, Formatting.Indented));
                Log.Information("[{Type}] No {FilePath} file detected. A new one has been created, no action needed.", "Users", FilePath);
            }

            string json = File.ReadAllText(FilePath);
            users = JsonConvert.DeserializeObject<Dictionary<ulong, User>>(json);
            Log.Information("[{Type}] Loaded {Count} Users from data.", "Users", users.Count);

            // Don't continue with OAuth if we are debugging.
            if (AppConfig.IsDebug) return;

            _listener = new()
            {
                AuthenticationSchemes = AuthenticationSchemes.Anonymous
            };
            _listener.Prefixes.Add($"http://+:{AppConfig.App.OauthPort}/");
            _listener.Start();
            _listener.BeginGetContext(new AsyncCallback(GetToken), _listener);
            Log.Information("[{Type}] Listening on port: {Port}...", "Users", AppConfig.App.OauthPort);
        }

        public Dictionary<ulong, User> GetUsers() => users;

        public void AddUser(User newUser)
        {
            if (IsExistingUser(newUser.GetUserId())) return;
            users.Add(newUser.GetUserId(), newUser);
            WriteUsersToFile();
        }

        public bool IsExistingUser(ulong userId) => users.ContainsKey(userId);

        public User GetUser(ulong userId) => users[userId].RefreshCode();

        public void RemoveUser(ulong userId)
        {
            users.Remove(userId);
            WriteUsersToFile();
        }

        public void RemoveUsers(IEnumerable<ulong> usersToRemove)
        {
            foreach (var user in usersToRemove) users.Remove(user);
            WriteUsersToFile();
        }

        public void AddSupporter(ulong userId)
        {
            if (!IsExistingUser(userId))
            {
                var newUser = new User(userId, settings: new UserSettings() { IsSupporter = true });
                AddUser(newUser);
                return;
            }
            GetUser(userId).MakeSupporter();
            WriteUsersToFile();
        }

        public void RemoveSupporter(ulong userId)
        {
            GetUser(userId).RemoveSupporter();
            WriteUsersToFile();
        }

        public Dictionary<ulong, User> GetSupporters() => users
            .Where(x => x.Value.IsSupporter())
            .ToDictionary(x => x.Key, x => x.Value);

        public void AddStaff(ulong userId)
        {
            if (!IsExistingUser(userId))
            {
                var newUser = new User(userId, settings: new UserSettings() { IsStaff = true });
                AddUser(newUser);
                return;
            }
            GetUser(userId).MakeStaff();
            WriteUsersToFile();
        }

        public void RemoveStaff(ulong userId)
        {
            GetUser(userId).RemoveStaff();
            WriteUsersToFile();
        }

        public User GetDeveloper() => users.FirstOrDefault(x => x.Value.IsDeveloper()).Value;

        private void WriteUsersToFile()
        {
            string output = JsonConvert.SerializeObject(users, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        private async void GetToken(IAsyncResult ar)
        {
            try
            {
                if (!HttpListener.IsSupported)
                {
                    Log.Error("[{Type}] HttpListener is not supported.", "OAuth");
                    return;
                }

                HttpListenerContext context = _listener.EndGetContext(ar);

                var query = context.Request.QueryString;

                var result = new BungieOauthResult();

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
                        Log.Error("[{Type}] Error occurred: {Error}.", "OAuth", query["error_description"]);
                        _listener.BeginGetContext(new AsyncCallback(GetToken), _listener);
                        return;
                    }
                }
                else
                {
                    result.SetReason(BungieOauthErrorReason.MissingParameters);
                }

                _listener.BeginGetContext(new AsyncCallback(GetToken), _listener);
                //LogHelper.ConsoleLog("[OAUTH] Sending Request.");

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                string responseString = "You are going to be redirected.";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                response.ContentLength64 = buffer.Length;
                if (result.GetReason() != BungieOauthErrorReason.None)
                {
                    //LogHelper.ConsoleLog($"[OAUTH] Redirecting to Link Fail with reason {result.Reason}.");
                    response.Redirect($"https://www.{AppConfig.App.Website}/link-fail/?error={Convert.ToInt32(result.GetReason())}");
                }
                else
                {
                    //LogHelper.ConsoleLog("[OAUTH] Redirecting to Link Success.");
                    response.Redirect($"https://www.{AppConfig.App.Website}/link-success/?discDisp={Uri.EscapeDataString(result.GetDiscordUsername())}");
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
                    //LogHelper.ConsoleLog("[OAUTH] Unable to send response write data.");
                }
                Log.Information("[{Type}] Flow completed. Listening on port: {Port}...", "OAuth", AppConfig.App.OauthPort);
            }
            catch (Exception x)
            {
                _listener.BeginGetContext(new AsyncCallback(GetToken), _listener);
            }
        }

        private async Task<BungieOauthResult> ProcessCode(string Code, ulong discordId)
        {
            var result = new BungieOauthResult(AppConfig.App.Name);

            var headers = new Dictionary<string, string>
                {
                    { "client_id", $"{AppConfig.Credentials.BungieClientId}" },
                    { "client_secret", $"{AppConfig.Credentials.BungieClientSecret}" },
                    { "Authorization",  $"Basic {Code}" },
                    { "Content-Type", "application/x-www-form-urlencoded" },
                    { "grant_type", "authorization_code" },
                    { "code", Code },
                };

            var content = await BungieApi.Post("/Platform/App/OAuth/Token/", headers);
            dynamic item = JsonConvert.DeserializeObject(content);

            var accessExpiration = new TimeSpan();
            var refreshExpiration = new TimeSpan();

            try
            {
                accessExpiration = TimeSpan.FromSeconds(double.Parse($"{item.expires_in}"));
                refreshExpiration = TimeSpan.FromSeconds(double.Parse($"{item.refresh_expires_in}"));
            }
            catch
            {
                result.SetReason(BungieOauthErrorReason.Unknown);
                return result;
            }

            var bungieTag = "";
            var memId = "";
            var memType = "";

            try
            {
                var memContent = await BungieApi.Query($"/Platform/Destiny2/-1/Profile/{item.membership_id}/LinkedProfiles/?getAllMemberships=true");
                dynamic memItem = JsonConvert.DeserializeObject(memContent);

                var lastPlayed = new DateTime();
                var goodProfile = -1;

                if (memItem == null || memItem.ErrorCode != 1)
                {
                    return null;
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
                    return null;
                }

                memType = memItem.Response.profiles[goodProfile].membershipType;
                memId = memItem.Response.profiles[goodProfile].membershipId;

                var bungieTagCode = $"{memItem.Response.bnetMembership.bungieGlobalDisplayNameCode}".PadLeft(4, '0');
                bungieTag = $"{memItem.Response.bnetMembership.bungieGlobalDisplayName}#{bungieTagCode}";

                Log.Information("[{Type}] Received tokens for {BungieTag} on platform {Platform}.", "OAuth", memItem.Response.bnetMembership.supplementalDisplayName, memType);
            }
            catch
            {
                result.SetReason(BungieOauthErrorReason.OldCode);
                return result;
            }

            if (memType == null)
            {
                result.SetReason(BungieOauthErrorReason.NoProfileDataFound);
                return result;
            }

            IUser user = LevanteCordInstance.Client.GetUser(discordId);
            if (user == null)
                user = LevanteCordInstance.Client.Rest.GetUserAsync(discordId).Result;

            if (user == null)
            {
                result.SetReason(BungieOauthErrorReason.DiscordUserNotFound);
                return result;
            }

            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Account Linking",
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {AppConfig.App.Name} v{AppConfig.App.Version}",
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                Author = auth,
                Footer = foot,
            };
            embed.Description =
                $"Linking Successful.\n" +
                $"Your Discord account ({user.Mention}) is now linked to **{bungieTag}**.";

            embed.AddField(x =>
            {
                x.Name = $"> Default Platform";
                x.Value = $"{(Guardian.Platform)int.Parse(memType)}";
                x.IsInline = false;
            });
            try
            {
                await user.SendMessageAsync(embed: embed.Build());
            }
            catch
            {
                result.SetReason(BungieOauthErrorReason.NoDiscordMessageSent);
                return result;
            }

            // Don't make users have to unlink to do this.
            var newCreds = new BungieUserCredentials()
            {
                MembershipId = memId,
                MembershipType = memType,
                BungieName = bungieTag,
                AccessToken = item.access_token,
                RefreshToken = item.refresh_token,
                AccessExpiration = DateTime.Now + accessExpiration,
                RefreshExpiration = DateTime.Now + refreshExpiration,
            };
            if (IsExistingUser(discordId))
            {
                GetUser(discordId).UpdateBungieCreds(newCreds);
                WriteUsersToFile();
            }
            else
            {
                var newUser = new User(discordId, newCreds);
                AddUser(newUser);
            }

            result.SetDiscordUsername(user.Username);
            result.SetReason(BungieOauthErrorReason.Unknown);
            return result;
        }
    }

    public class User
    {
        [JsonProperty(nameof(UserId))]
        private ulong UserId { get; set; } = 0;

        [JsonProperty(nameof(BungieCredentials), DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(typeof(BungieUserCredentials))]
        public BungieUserCredentials BungieCredentials { get; internal set; }

        [JsonProperty(nameof(Settings), DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(typeof(UserSettings))]
        public UserSettings Settings { get; internal set; }

        public User(ulong userId, BungieUserCredentials bungieCredentials = null, UserSettings settings = null)
        {
            UserId = userId;
            BungieCredentials = bungieCredentials;
            Settings = settings;
        }

        public ulong GetUserId() => UserId;

        public User RefreshCode()
        {
            // Don't bother doing this if the user doesn't even have Bungie Credentials.
            // This is because we still store user flags regardless.
            if (BungieCredentials == null)
                return this;

            // Don't refresh the code if we don't need to.
            if (BungieCredentials.AccessExpiration > DateTime.Now)
                return this;

            // We don't have permission to refresh auth tokens.
            if (BungieCredentials.RefreshExpiration < DateTime.Now)
                return this;

            using var client = new HttpClient();
            var values = new Dictionary<string, string>
                {
                    { "client_id", $"{AppConfig.Credentials.BungieClientId}" },
                    { "client_secret", $"{AppConfig.Credentials.BungieClientSecret}" },
                    { "Content-Type", "application/x-www-form-urlencoded" },
                    { "grant_type", "refresh_token" },
                    { "refresh_token", $"{BungieCredentials.RefreshToken}" }
                };
            var postContent = new FormUrlEncodedContent(values);

            var response = client.PostAsync("https://www.bungie.net/Platform/App/OAuth/Token/", postContent).Result;
            var content = response.Content.ReadAsStringAsync().Result;
            dynamic item = JsonConvert.DeserializeObject(content);

            if (item.refresh_token == null || item.access_token == null)
            {
                Log.Information("[{Type}] Received null tokens from refresh; keep all tokens the same as before.", "Users");
                return this;
            }
            BungieCredentials.RefreshToken = item.refresh_token;
            BungieCredentials.AccessToken = item.access_token;

            BungieCredentials.AccessExpiration = DateTime.Now.Add(TimeSpan.FromSeconds(double.Parse($"{item.expires_in}")));
            BungieCredentials.RefreshExpiration = DateTime.Now.Add(TimeSpan.FromSeconds(double.Parse($"{item.refresh_expires_in}")));

            return this;
        }

        public void UpdateBungieCreds(BungieUserCredentials userCredentials) => BungieCredentials = userCredentials;

        public bool IsRefreshAccessExpired() => BungieCredentials.RefreshExpiration < DateTime.Now;

        public void MakeSupporter() => (Settings ??= new UserSettings()).IsSupporter = true;

        public bool IsSupporter() => Settings?.IsSupporter ?? false;

        public void RemoveSupporter() => (Settings ??= new UserSettings()).IsSupporter = false;

        public void MakeStaff() => (Settings ??= new UserSettings()).IsStaff = true;

        public bool IsStaff() => Settings?.IsStaff ?? false;

        public bool RemoveStaff() => (Settings ??= new UserSettings()).IsStaff = false;

        public bool IsDeveloper() => Settings?.IsDeveloper ?? false;

        public bool ShouldSerializeBungieCredentials() => BungieCredentials?.ShouldSerialize() ?? false;

        public bool ShouldSerializeSettings() => Settings?.ShouldSerialize() ?? false;
    }

    public class BungieUserCredentials
    {
        [JsonProperty(nameof(MembershipId))]
        public string MembershipId { get; set; }

        [JsonProperty(nameof(MembershipType))]
        public string MembershipType { get; set; }

        [JsonProperty(nameof(BungieName))]
        public string BungieName { get; set; }

        [JsonProperty(nameof(AccessToken))]
        public string AccessToken { get; set; }

        [JsonProperty(nameof(RefreshToken))]
        public string RefreshToken { get; set; }

        [JsonProperty(nameof(AccessExpiration))]
        public DateTime AccessExpiration { get; set; } = DateTime.Now;

        [JsonProperty(nameof(RefreshExpiration))]
        public DateTime RefreshExpiration { get; set; } = DateTime.Now;

        public bool ShouldSerializeMembershipId() => !string.IsNullOrEmpty(MembershipId);
        public bool ShouldSerializeMembershipType() => !string.IsNullOrEmpty(MembershipType);
        public bool ShouldSerializeBungieName() => !string.IsNullOrEmpty(BungieName);
        public bool ShouldSerializeAccessToken() => !string.IsNullOrEmpty(AccessToken);
        public bool ShouldSerializeRefreshToken() => !string.IsNullOrEmpty(RefreshToken);
        public bool ShouldSerializeAccessExpiration() => AccessExpiration != default;
        public bool ShouldSerializeRefreshExpiration() => RefreshExpiration != default;

        public bool ShouldSerialize() =>
            ShouldSerializeMembershipId() ||
            ShouldSerializeMembershipType() ||
            ShouldSerializeBungieName() ||
            ShouldSerializeAccessToken() ||
            ShouldSerializeRefreshToken() ||
            ShouldSerializeAccessExpiration() ||
            ShouldSerializeRefreshExpiration();
    }

    public class UserSettings
    {
        [JsonProperty(nameof(IsSupporter), DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool IsSupporter { get; set; } = false;

        [JsonProperty(nameof(IsStaff), DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool IsStaff { get; set; } = false;

        [JsonProperty(nameof(IsDeveloper), DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool IsDeveloper { get; set; } = false;

        public bool ShouldSerializeIsSupporter() => IsSupporter;
        public bool ShouldSerializeIsStaff() => IsStaff;
        public bool ShouldSerializeIsDeveloper() => IsDeveloper;

        public bool ShouldSerialize() =>
            ShouldSerializeIsSupporter() ||
            ShouldSerializeIsStaff() ||
            ShouldSerializeIsDeveloper();
    }
}
