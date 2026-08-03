using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BungieSharper.Entities.Forum;
using Newtonsoft.Json.Linq;
using Serilog;
using Discord.WebSocket;
using Levante.Configs;
using Levante.Util;

namespace Levante.Services
{
    public class NewsService
    {
        private readonly Timer _timer;
        private readonly DiscordShardedClient _client;
        private readonly GuildService _guilds;

        public readonly string FilePath = @"/Data/newsData.json";

        private List<string> NewsArticles = new();

        public NewsService(DiscordShardedClient client, GuildService guilds)
        {
            if (!File.Exists(FilePath))
            {
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(NewsArticles, Formatting.Indented));
                Log.Information("[{Type}] No newsConfig.json file detected. A new one has been created, no action needed.", "News");
            }

            string json = File.ReadAllText(FilePath);
            NewsArticles = JsonConvert.DeserializeObject<List<string>>(json);

            _timer = new Timer(CheckNewsCallback, null, 5000, 60000 * 2);
            _client = client;
            _guilds = guilds;

            Log.Information("[{Type}] News Service Loaded.", "News");
        }

        public async void CheckNewsCallback(object o) => await CheckNews().ConfigureAwait(false);

        private async Task CheckNews()
        {
            Log.Information("[{Type}] Pulling News...", "News");
            try
            {
                var content = await BungieApi.Query($"/Platform/Content/Rss/NewsArticles/0/?includebody=false");
                dynamic item = JsonConvert.DeserializeObject(content);

                bool isNewNews = false;
                List<string> newNews = new();
                foreach (var news in item.Response.NewsArticles)
                {
                    newNews.Add($"{news.UniqueIdentifier}");
                    var title = $"{news.Title}";
                    var link = $"{BungieApi.API_BASE_URL}{news.Link}";
                    // Build Embed
                    if (!NewsArticles.Contains($"{news.UniqueIdentifier}"))
                    {
                        // Build Embed
                        var auth = new EmbedAuthorBuilder()
                        {
                            Name = $"{news.Title}",
                            Url = link
                        };
                        var foot = new EmbedFooterBuilder()
                        {
                            Text = $"Powered by the Bungie API"
                        };
                        var embed = new EmbedBuilder
                        {
                            Color = new Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                            //Author = auth,
                            Footer = foot,
                            Description = $"### {news.Title}\n{news.Description}",
                            ImageUrl = $"{news.ImagePath}",
                            ThumbnailUrl = $"{news.OptionalMobileImagePath}"
                        };
                        embed.AddField(x =>
                        {
                            x.Name = "Published at";
                            x.Value = $"{TimestampTag.FromDateTime(DateTime.SpecifyKind(DateTime.Parse($"{news.PubDate}"), DateTimeKind.Utc))}";
                            x.IsInline = false;
                        }).AddField(x =>
                        {
                            x.Name = "Link";
                            x.Value = link;
                            x.IsInline = false;
                        });

                        await _guilds.PostToNewsChannels(embed);
                        //if (BotConfig.CreationsLogChannel is SocketNewsChannel)
                        //await msg.CrosspostAsync();

                        isNewNews = true;
                    }
                }

                if (isNewNews)
                {
                    NewsArticles = newNews;
                    File.WriteAllText(FilePath, JsonConvert.SerializeObject(NewsArticles, Formatting.Indented));
                    Log.Information("[{Type}] News successfully handled.", "News");
                }
                else
                {
                    Log.Information("[{Type}] No new News found.", "News");
                }
            }
            catch (Exception x)
            {
                Log.Warning("[{Type}] News request failed. {Exception}", "News", x);
            }
        }

        public Embed GetRecentNewsEmbed()
        {
            var content = Task.Run(() => BungieApi.Query("/Platform/Content/Rss/NewsArticles/0/?includebody=false")).Result;
            dynamic item = JsonConvert.DeserializeObject(content);
            var news = item.Response.NewsArticles[0];
            var title = $"{news.Title}";
            var link = $"{BungieApi.API_BASE_URL}{news.Link}";
            // Build Embed
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"{news.Title}",
                Url = link
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by the Bungie API"
            };
            var embed = new EmbedBuilder
            {
                Color = new Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                //Author = auth,
                Footer = foot,
                Description = $"### {news.Title}\n{news.Description}",
                ImageUrl = $"{news.ImagePath}",
                ThumbnailUrl = $"{news.OptionalMobileImagePath}"
            };
            embed.AddField(x =>
            {
                x.Name = "Published at";
                x.Value = $"{TimestampTag.FromDateTime(DateTime.SpecifyKind(DateTime.Parse($"{news.PubDate}"), DateTimeKind.Utc))}";
                x.IsInline = false;
            }).AddField(x =>
            {
                x.Name = "Link";
                x.Value = link;
                x.IsInline = false;
            });

            return embed.Build();
        }
    }
}
