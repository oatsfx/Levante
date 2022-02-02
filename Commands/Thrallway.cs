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
    public class Thrallway : ModuleBase<SocketCommandContext>
    {
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

            if (ActiveConfig.ActiveAFKUsers.Count >= 1)
            {
                embed.Description = $"__AFK List:__\n";
                foreach (var aau in ActiveConfig.ActiveAFKUsers)
                {
                    embed.Description +=
                        $"{aau.UniqueBungieName} (<@{aau.DiscordID}>): Level {aau.LastLoggedLevel}\n";
                }
            }
            else
            {
                embed.Description = "No users are using my logging feature.";
            }

            await ReplyAsync($"", false, embed.Build());
        }

        [Command("currentSession", RunMode = RunMode.Async)]
        [Alias("session", "cs", "sessionStats")]
        [Summary("Pulls stats of current Thrallway session.")]
        public async Task SessionStats([Remainder] SocketGuildUser user = null)
        {
            if (user == null)
                user = Context.User as SocketGuildUser;

            if (!ActiveConfig.IsExistingActiveUser(user.Id))
            {
                if (Context.User.Id == user.Id)
                {
                    await ReplyAsync("You are not using my logging feature.");
                    return;
                }
                else
                {
                    await ReplyAsync($"{user.Username} is not using my logging feature.");
                    return;
                }
            }

            var aau = ActiveConfig.GetActiveAFKUser(user.Id);

            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Current Session Stats for {aau.UniqueBungieName}",
                IconUrl = app.IconUrl,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Thrallway Session Summary"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };
            int levelsGained = aau.LastLoggedLevel - aau.StartLevel;
            long xpGained = (levelsGained * 100000) - aau.StartLevelProgress + aau.LastLevelProgress;
            var timeSpan = DateTime.Now - aau.TimeStarted;
            string timeString = $"{(Math.Floor(timeSpan.TotalHours) > 0 ? $"{Math.Floor(timeSpan.TotalHours)}h " : "")}" +
                    $"{(timeSpan.Minutes > 0 ? $"{timeSpan.Minutes:00}m " : "")}" +
                    $"{timeSpan.Seconds:00}s";
            int xpPerHour = 0;
            if ((DateTime.Now - aau.TimeStarted).TotalHours >= 1)
                xpPerHour = (int)Math.Floor(xpGained / (DateTime.Now - aau.TimeStarted).TotalHours);
            embed.WithCurrentTimestamp();
            embed.Description =
                $"Levels Gained: {levelsGained}\n" +
                $"XP Gained: {String.Format("{0:n0}", xpGained)}\n" +
                $"Time: {timeString}\n" +
                $"XP Per Hour: {String.Format("{0:n0}", xpPerHour)}";

            await ReplyAsync($"", embed: embed.Build());
        }
    }
}
