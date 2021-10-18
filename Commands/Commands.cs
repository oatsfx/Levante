using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DestinyUtility.Configs;

namespace DestinyUtility.Commands
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("ping", RunMode = RunMode.Async)]
        [Summary("Replies with latency in milliseconds.")]
        public async Task PingAsync()
        {
            int[] colors = new int[3];
            int latency = Context.Client.Latency;

            if (latency >= 0 && latency < 200)
            {
                colors[0] = 123;
                colors[1] = 232;
                colors[2] = 98;
            }
            else if (latency >= 200 && latency < 450)
            {
                colors[0] = 251;
                colors[1] = 254;
                colors[2] = 50;
            }
            else
            {
                colors[0] = 237;
                colors[1] = 69;
                colors[2] = 69;
            }

            var embed = new EmbedBuilder()
            {
                Color = new Color(colors[0], colors[1], colors[2]),
            };
            embed.Description =
                $"Pong! ({latency} ms)";

            await ReplyAsync("", false, embed.Build());
        }

        [Command("activeAFK", RunMode = RunMode.Async)]
        [Alias("actives", "activeAFKers")]
        [Summary("Gets a list of the users that are AFKing.")]
        public async Task ActiveAFK()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Active AFK Users",
                IconUrl = app.IconUrl
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"{ActiveConfig.ActiveAFKUsers.Count} people are AFKing"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };
            embed.Description = $"__AFK List:__\n";

            foreach (var aau in ActiveConfig.ActiveAFKUsers)
            {
                embed.Description +=
                    $"{DataConfig.GetUniqueBungieName(aau.DiscordID)} (<@{aau.DiscordID}>): Level {aau.LastLoggedLevel}\n";
            }

            await ReplyAsync($"", false, embed.Build());
        }

        [Command("level", RunMode = RunMode.Async)]
        [Alias("rank")]
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
                    $"Player: **{DataConfig.GetUniqueBungieName(user.Id)}**\n" +
                    $"Level: **{DataConfig.GetUserSeasonPassLevel(user.Id, out int progress)}**\n" +
                    $"Progress to Next Level: **{progress}/100000**";

                await ReplyAsync($"", false, embed.Build());
            }
            catch (Exception x)
            {
                await ReplyAsync($"{x}");
            }
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
    }
}
