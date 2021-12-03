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
using DestinyUtility.Data;

namespace DestinyUtility.Commands
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        private CommandService _service;

        public Commands(CommandService service)
        {
            _service = service;
        }

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

        [Command("info", RunMode = RunMode.Async)]
        [Summary("Replies with the info of the bot.")]
        public async Task InfoAsync()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B));

            embed.ThumbnailUrl = app.IconUrl;

            embed.Title = "Bot Information";
            embed.Description =
                "Destiny Utility is an [popen-source](https://github.com/oatsfx/DestinyUtility) Discord bot using Discord.Net-Labs for various Destiny 2 Needs. " +
                "This bot is actively developed by [@OatsFX](https://twitter.com/OatsFX). It pulls most of its information from the Bungie API.";

            await ReplyAsync("", false, embed.Build());
        }

        [Command("help", RunMode = RunMode.Async)]
        public async Task Help()
        {
            var dmChannel = await Context.User.CreateDMChannelAsync();

            var serverId = Context.Guild.Id;

            string prefix = BotConfig.DefaultCommandPrefix;

            var auth = new EmbedAuthorBuilder()
            {
                Name = "Command List"
            };

            var builder = new EmbedBuilder()
            {
                Color = new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
            };

            foreach (var module in _service.Modules)
            {
                string description = "";
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess && !description.Contains(cmd.Name)) // Check for Duplicates and Permissions.
                        description += $"{prefix}{cmd.Name}\n";
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = true;
                    });
                }
            }

            await dmChannel.SendMessageAsync("", false, builder.Build());

            await ReplyAsync($"{Context.User.Mention}, check your DMs. <a:verified:690374136526012506>");
        }

        [Command("invite", RunMode = RunMode.Async)]
        [Summary("Provides links that support this bot.")]
        public async Task Invite()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B));

            embed.ThumbnailUrl = app.IconUrl;

            embed.Title = "Invite Link";
            embed.Description =
                "__**Invite me to your server!**__" +
                "\n[Invite](https://discord.com/oauth2/authorize?client_id=882303133643047005&scope=bot&permissions=8)" +
                "\n__**Let me use Slash Commands in your server!**__" +
                "\n[Slash Commands](https://discord.com/oauth2/authorize?client_id=882303133643047005&scope=applications.commands)";

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
                    $"{aau.UniqueBungieName} (<@{aau.DiscordID}>): Level {aau.LastLoggedLevel}\n";
            }

            await ReplyAsync($"", false, embed.Build());
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
                    $"Progress to Next Level: **{progress}/100000**";

                await ReplyAsync($"", false, embed.Build());
            }
            catch (Exception x)
            {
                await ReplyAsync($"{x}");
            }
        }

        [Command("rank", RunMode = RunMode.Async)]
        [Alias("leaderboard")]
        [Summary("Gets a leaderboard of all registered users.")]
        public async Task Rank()
        {
            await ReplyAsync($"Depreciated to a Slash Command.");
        }

        [Command("createHub", RunMode = RunMode.Async)]
        [Summary("Creates the post with buttons so people can start their logs.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task CreateHub()
        {
            var app = await Context.Client.GetApplicationInfoAsync();

            ICategoryChannel cc = null;
            foreach (var categoryChan in Context.Guild.CategoryChannels)
                if (categoryChan.Name.Equals($"Thrallway Logger"))
                {
                    cc = categoryChan;
                    // Remove an existing hub.
                    foreach (var channel in categoryChan.Channels)
                        if (channel.Name.ToLower().Contains("thrallway") && channel.Name.ToLower().Contains("hub"))
                            await channel.DeleteAsync();
                }

            // Create a category channel if there isn't one.
            if (cc == null)
            {
                cc = await Context.Guild.CreateCategoryChannelAsync($"Thrallway Logger");

                await cc.AddPermissionOverwriteAsync(Context.Guild.GetRole(Context.Guild.Id), new OverwritePermissions(sendMessages: PermValue.Deny));
                await cc.AddPermissionOverwriteAsync(Context.Client.GetUser(app.Id), new OverwritePermissions(sendMessages: PermValue.Allow, attachFiles: PermValue.Allow, embedLinks: PermValue.Allow, manageChannel: PermValue.Allow));
            }

            var hubChannel = Context.Guild.CreateTextChannelAsync($"thrallway-hub").Result;
            await hubChannel.ModifyAsync(x =>
            {
                x.CategoryId = cc.Id;
                x.Topic = $"Thrallway Hub: Start your logging here.";
                x.PermissionOverwrites = new[]
                {
                    new Overwrite(Context.Guild.Id, PermissionTarget.Role, new OverwritePermissions(sendMessages: PermValue.Deny, viewChannel: PermValue.Allow)),
                    new Overwrite(Context.Client.CurrentUser.Id, PermissionTarget.User, new OverwritePermissions(sendMessages: PermValue.Allow, attachFiles: PermValue.Allow, embedLinks: PermValue.Allow, manageChannel: PermValue.Allow))
                };
            });

            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Thrallway Hub",
                IconUrl = app.IconUrl,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Make sure you have your AFK setup running before clicking one of the buttons."
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };
            embed.Description =
                $"Are you getting ready to go AFK in Shattered Throne soon?\n" +
                $"Click the \"Ready\" button and I'll start logging your progress and even DM you if I think you wiped!\n" +
                $"When you are done, click the \"Stop\" button and I'll shut your logging down.";

            Emoji sleepyEmote = new Emoji("😴");
            Emoji helpEmote = new Emoji("❔");
            Emoji stopEmote = new Emoji("🛑");

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Ready", customId: $"startAFK", ButtonStyle.Secondary, sleepyEmote, row: 0)
                .WithButton("Stop", customId: $"stopAFK", ButtonStyle.Secondary, stopEmote, row: 0)
                .WithButton("Help", customId: $"viewHelp", ButtonStyle.Secondary, helpEmote, row: 0);

            await hubChannel.SendMessageAsync($"", false, embed.Build(), component: buttonBuilder.Build());

            await ReplyAsync($"{Context.User.Mention}: Hub created at {hubChannel.Mention}. Feel free to move that Category anywhere!");
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
