using BungieSharper.Entities.Trending;
using Discord;
using Levante.Configs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BungieSharper.Entities.Forum;

namespace Levante.Helpers
{
    public class CreationsHelper
    {
        private Timer CommunityCreationsTimer;

        public static readonly string FilePath = @"Configs/creationsConfig.json";

        [JsonProperty("CreationsPosts")]
        private List<string> CreationsPosts = new();

        public CreationsHelper()
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                CreationsPosts = JsonConvert.DeserializeObject<List<string>>(json);
            }
            else
            {
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(CreationsPosts, Formatting.Indented));
                Console.WriteLine($"No creationsConfig.json file detected. A new one has been created, no action needed.");
            }
            CommunityCreationsTimer = new Timer(CheckCommunityCreationsCallback, null, 5000, 60000*2);
        }
        private async void CheckCommunityCreationsCallback(Object o) => await CheckCreations().ConfigureAwait(false);

        private async Task CheckCreations()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/Platform/CommunityContent/Get/1/0/0/").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                bool isNewCreations = false;
                foreach (var creation in item.Response.results)
                {
                    // Find Author
                    string authorName = "";
                    string authorAvatarUrl = "https://bungie.net";
                    foreach (var author in item.Response.authors)
                    {
                        if ($"{author.membershipId}".Equals($"{creation.authorMembershipId}"))
                        {
                            authorAvatarUrl += $"{author.profilePicturePath}";
                            authorName = $"{author.displayName}";
                            break;
                        }
                    }
                    // Find Editor
                    string editorName = "";
                    foreach (var author in item.Response.authors)
                    {
                        if ($"{author.membershipId}".Equals($"{creation.editorMembershipId}"))
                        {
                            editorName = $"{author.displayName}";
                            break;
                        }
                    }
                    if (!CreationsPosts.Contains($"{creation.postId}"))
                    {
                        Console.WriteLine($"New Community Creation Found: {creation.subject} ({creation.postId})");
                        CreationsPosts.Add($"{creation.postId}");

                        var auth = new EmbedAuthorBuilder()
                        {
                            Name = $"A new Community Creation has been approved!",
                            IconUrl = authorAvatarUrl,
                            Url = $"https://www.bungie.net/en/Community/Detail?itemId={creation.postId}"
                        };
                        var foot = new EmbedFooterBuilder()
                        {
                            Text = $"Powered by the Bungie API"
                        };
                        var embed = new EmbedBuilder()
                        {
                            Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                            Author = auth,
                            Footer = foot,
                        };
                        embed.Description = $"[{System.Web.HttpUtility.HtmlDecode($"{creation.subject}")}](https://www.bungie.net/en/Community/Detail?itemId={creation.postId})";
                        if (creation.urlMediaType == ForumMediaType.Image)
                            embed.ImageUrl = $"{creation.urlLinkOrImage}";
                        else if (creation.urlMediaType == ForumMediaType.Video || creation.urlMediaType == ForumMediaType.Youtube)
                            embed.Description += $"\n> Video Link: {creation.urlLinkOrImage}";

                        embed.ThumbnailUrl = authorAvatarUrl;
                        embed.AddField(x =>
                        {
                            x.Name = "Submitted";
                            x.Value = $"{TimestampTag.FromDateTime(DateTime.Parse($"{creation.creationDate}"))}\n" +
                                $"by {authorName}";
                            x.IsInline = true;
                        }).AddField(x =>
                        {
                            x.Name = "Approved";
                            x.Value = $"{TimestampTag.FromDateTime(DateTime.Parse($"{creation.lastModified}"))}\n" +
                                $"by {editorName}";
                            x.IsInline = true;
                        }).AddField(x =>
                        {
                            x.Name = "Upvotes";
                            x.Value = $"{creation.upvotes}";
                            x.IsInline = false;
                        });
                        await BotConfig.CreationsLogChannel.SendMessageAsync(embed: embed.Build());

                        isNewCreations = true;
                    }
                }

                if (!isNewCreations)
                {
                    Console.WriteLine($"No new Community Creations Found. :(");
                }
                else
                {
                    File.WriteAllText(FilePath, JsonConvert.SerializeObject(CreationsPosts, Formatting.Indented));
                }
            }
        }
    }
}
