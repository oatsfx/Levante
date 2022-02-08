using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Levante.Configs;
using System.Drawing;
using System.IO;
using Levante.Util;
using System.Net;
using System.Collections.Generic;
using Fergun.Interactive;

namespace Levante.Commands
{
    public class Destiny : ModuleBase<SocketCommandContext>
    {
        public InteractiveService Interactive { get; set; }

        [Command("currentOffers", RunMode = RunMode.Async)]
        [Alias("limitedEmblems", "offers", "offer")]
        [Summary("Gives a list of current emblem offers. If a hash code is provided, command will return with offer of that emblem (if it exists).")]
        public async Task CurrentOffers(long EmblemHashCode = -1)
        {
            if (EmblemHashCode == -1)
            {
                await ReplyAsync($"", false, EmblemOffer.GetOfferListEmbed().Build());
                return;
            }

            if (!EmblemOffer.HasExistingOffer(EmblemHashCode))
            {
                await ReplyAsync("Are you sure you entered the correct hash code? Run this command without any arguments and try again.");
            }
            else
            {
                EmblemOffer eo = EmblemOffer.GetSpecificOffer(EmblemHashCode);
                await ReplyAsync("", false, eo.BuildEmbed().Build());
            }
        }

        [Command("level", RunMode = RunMode.Async)]
        [Summary("Gets your Destiny 2 Season Pass Rank.")]
        public async Task GetLevel([Remainder] SocketGuildUser user = null)
        {
            if (user == null)
            {
                user = Context.User as SocketGuildUser;
            }

            if (!DataConfig.IsExistingLinkedUser(user.Id))
            {
                await ReplyAsync("No account linked.");
                return;
            }
            try
            {
                string season = GetCurrentDestiny2Season(out int seasonNum);

                var app = await Context.Client.GetApplicationInfoAsync();
                var auth = new EmbedAuthorBuilder()
                {
                    Name = $"Season {seasonNum}: {season}",
                    IconUrl = user.GetAvatarUrl(),
                };
                var foot = new EmbedFooterBuilder()
                {
                    Text = $"Powered by Bungie API"
                };
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                    Author = auth,
                    Footer = foot,
                };
                embed.Description =
                    $"Player: **{DataConfig.GetLinkedUser(user.Id).UniqueBungieName}**\n" +
                    $"Level: **{DataConfig.GetUserSeasonPassLevel(user.Id, out int progress)}**\n" +
                    $"Progress to Next Level: **{String.Format("{0:n0}", progress)}/100,000**";

                await ReplyAsync($"", false, embed.Build());
            }
            catch (Exception x)
            {
                await ReplyAsync($"{x}");
            }
        }

        [Command("tryOut", RunMode = RunMode.Async)]
        [Alias("tryOn")]
        [Summary("Try on any emblem in the Bungie API.")]
        public async Task TryOut(long HashCode = -1, [Remainder] string name = null)
        {
            if (name == null)
            {
                var linkedUser = DataConfig.GetLinkedUser(Context.User.Id);
                if (linkedUser != null)
                    name = linkedUser.UniqueBungieName.Substring(0, linkedUser.UniqueBungieName.Length - 5);
                else
                    name = Context.User.Username;
            }

            Emblem emblem;
            Bitmap bitmap;
            try
            {
                emblem = new Emblem(HashCode);
                bitmap = (Bitmap)GetImageFromPicPath(emblem.GetBackgroundUrl()); //load the image file
            }
            catch (Exception)
            {
                await ReplyAsync($"No emblem found for Hash Code: {HashCode}.");
                return;
            }

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                using (Font font = new Font("Neue Haas Grotesk Display Pro", 18, FontStyle.Bold))
                {
                    graphics.DrawString(name, font, Brushes.White, new PointF(83f, 7f));
                }

                using (Font font = new Font("Neue Haas Grotesk Display Pro", 14))
                {
                    graphics.DrawString($"Levante Bot{(RequireBotStaff.IsBotStaff(Context.User.Id) ? " Staff" : "")}", font, new SolidBrush(System.Drawing.Color.FromArgb(128, System.Drawing.Color.White)), new PointF(84f, 37f));
                }
            }

            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                byte[] bytes = stream.ToArray();
                using (var fs = new FileStream(@"temp.png", FileMode.Create))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Close();
                }
            }

            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Emblem Testing",
                IconUrl = emblem.GetIconUrl(),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"This is not a perfect replica."
            };
            int[] emblemRGB = emblem.GetRGBAsIntArray();
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(emblemRGB[0], emblemRGB[1], emblemRGB[2]),
                Author = auth,
                Footer = foot
            };
            embed.Description =
                    $"This is probably what you'd look like in-game if you had **{emblem.GetName()}**.";
            embed.ImageUrl = @"attachment://temp.png";

            await Context.Channel.SendFileAsync("temp.png", null, false, embed.Build());
        }

        [Command("view", RunMode = RunMode.Async)]
        [Alias("details")]
        [Summary("Get details on an emblem via its Hash Code found via Bungie's API.")]
        public async Task ViewEmblem([Remainder] string SearchQuery = null)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/Armory/Search/DestinyInventoryItemDefinition/" + SearchQuery + "/").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                if (DataConfig.IsBungieAPIDown(content))
                {
                    await ReplyAsync($"Bungie API is temporary down, try again later.");
                    return;
                }

                if (item.Response.results.totalResults <= 0)
                {
                    await ReplyAsync($"No emblem found by the name of: {SearchQuery}");
                    return;
                }

                bool hasMultipleResults = false;
                List<EmblemSearch> emblemList = new List<EmblemSearch>();
                int resultNum = int.Parse($"{item.Response.results.totalResults}");
                int currentPage = -1;
                if (resultNum > 1)
                {
                    var waitMsg = await ReplyAsync($"Searching through {resultNum} entries, *estimated {(resultNum > 20 ? Math.Ceiling(resultNum / (double)25) * 4 + 2 : Math.Ceiling(resultNum / (double)25) + 2)} seconds*... <a:loading:872886173075378197>");
                    for (int i = 0; i < resultNum; i++)
                    {
                        if (i % 25 == 0)
                        {
                            currentPage++;
                            await Task.Delay(400);
                            response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/Armory/Search/DestinyInventoryItemDefinition/" + SearchQuery + "/?page=" + currentPage).Result;
                            content = response.Content.ReadAsStringAsync().Result;
                            item = JsonConvert.DeserializeObject(content);
                        }
                        if (Emblem.HashIsAnEmblem(long.Parse($"{item.Response.results.results[i - (currentPage * 25)].hash}")))
                        {
                            emblemList.Add(new EmblemSearch(long.Parse($"{item.Response.results.results[i - (currentPage * 25)].hash}"), $"{item.Response.results.results[i - (currentPage * 25)].displayProperties.name}"));
                        }
                    }
                    await waitMsg.DeleteAsync();
                }
                else if (resultNum == 1)
                {
                    if (Emblem.HashIsAnEmblem(long.Parse($"{item.Response.results.results[0].hash}")))
                    {
                        emblemList.Add(new EmblemSearch(long.Parse($"{item.Response.results.results[0].hash}"), $"{item.Response.results.results[0].displayProperties.name}"));
                    }
                }

                if (emblemList.Count > 1)
                    hasMultipleResults = true;

                int responseNum = 1;
                if (hasMultipleResults)
                {
                    string result = "";
                    for (int i = 0; i < emblemList.Count; i++)
                    {
                        result += $"**{i + 1})** {emblemList[i].GetName()}\n";
                    }
                    var auth = new EmbedAuthorBuilder()
                    {
                        Name = $"Multiple Emblems Found for: {SearchQuery}"
                    };
                    var foot = new EmbedFooterBuilder()
                    {
                        Text = $"Don't see the emblem you are looking for? Try doing a more specific search."
                    };
                    var embed = new EmbedBuilder()
                    {
                        Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                        Author = auth,
                        Footer = foot
                    };
                    embed.Title = $"Respond with the number of the emblem you want to view.";
                    embed.Description = result;
                    await ReplyAsync("", false, embed.Build());

                    var responseMsg = await Interactive.NextMessageAsync(x => x.Channel.Id == Context.Channel.Id, timeout: TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage));

                    if (responseMsg == null || responseMsg.Value.Author != Context.User || !int.TryParse(responseMsg.Value.ToString(), out responseNum))
                    {
                        await ReplyAsync($"Closed command. Invalid input: {responseMsg.Value}.");
                        return;
                    }
                }

                if (emblemList.Count == 0)
                {
                    await ReplyAsync($"{SearchQuery} did not bring results of the Emblem type.");
                    return;
                }
                await ViewEmblemLong(emblemList[responseNum - 1].GetEmblemHash());
            }
        }

        [Command("view", RunMode = RunMode.Async)]
        [Alias("details")]
        [Summary("Get details on an emblem via its Hash Code found via Bungie's API.")]
        public async Task ViewEmblemLong(long HashCode = -1)
        {
            if (HashCode == -1)
            {
                await ReplyAsync("No Hash Code provided.");
                return;
            }

            Emblem emblem;
            try
            {
                emblem = new Emblem(HashCode);
            }
            catch (Exception)
            {
                await ReplyAsync($"No emblem found for Hash Code: {HashCode}.");
                return;
            }

            if (!Emblem.HashIsAnEmblem(HashCode))
            {
                await ReplyAsync($"{emblem.GetName()} ({emblem.GetHashCode()}) is not an Emblem type.");
                return;
            }

            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Emblem Details: {emblem.GetName()}",
                IconUrl = emblem.GetIconUrl(),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by Bungie API"
            };
            int[] emblemRGB = emblem.GetRGBAsIntArray();
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(emblemRGB[0], emblemRGB[1], emblemRGB[2]),
                Author = auth,
                Footer = foot
            };
            try
            {
                embed.Description =
                    (emblem.GetSourceString().Equals("") ? "No source data provided." : emblem.GetSourceString()) + "\n" +
                    $"Hash Code: {emblem.GetItemHash()}\n";
                embed.ImageUrl = emblem.GetBackgroundUrl();
            }
            catch
            {
                await ReplyAsync("There seems to be an API issue with that emblem, sorry about that!");
                return;
            }

            embed.ThumbnailUrl = emblem.GetIconUrl();

            await ReplyAsync("", false, embed.Build());
        }

        private string GetCurrentDestiny2Season(out int SeasonNumber)
        {
            ulong seasonHash = 0;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/3/Profile/4611686018471482002/?components=100").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                seasonHash = item.Response.profile.data.currentSeasonHash;

                var response1 = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/Manifest/DestinySeasonDefinition/" + seasonHash + "/").Result;
                var content1 = response1.Content.ReadAsStringAsync().Result;
                dynamic item1 = JsonConvert.DeserializeObject(content1);

                SeasonNumber = item1.Response.seasonNumber;
                return $"{item1.Response.displayProperties.name}";
            }
        }

        public static System.Drawing.Image GetImageFromPicPath(string strUrl)
        {
            using (WebResponse wrFileResponse = WebRequest.Create(strUrl).GetResponse())
            {
                using (Stream objWebStream = wrFileResponse.GetResponseStream())
                {
                    MemoryStream ms = new MemoryStream();
                    objWebStream.CopyTo(ms, 8192);
                    return System.Drawing.Image.FromStream(ms);
                }
            }
        }
    }
}
