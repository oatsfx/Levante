using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Levante.Configs;

// ReSharper disable UnusedMember.Global

namespace Levante.Commands
{
    public class XPLogging : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("active-logging", "Gets a list of the users that are using my XP logging feature.")]
        public async Task ActiveAFK()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder
            {
                Name = "Active XP Logging Users",
                IconUrl = app.IconUrl
            };
            var foot = new EmbedFooterBuilder
            {
                Text = $"{ActiveConfig.ActiveAFKUsers.Count} people are logging their XP."
            };
            var embed = new EmbedBuilder
            {
                Color =
                    new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot
            };

            if (ActiveConfig.ActiveAFKUsers.Count >= 1)
            {
                embed.Description = "__XP Logging List:__\n";
                foreach (var aau in ActiveConfig.ActiveAFKUsers)
                    embed.Description +=
                        $"{aau.UniqueBungieName}: Level {aau.LastLoggedLevel}\n";
            }
            else
            {
                embed.Description = "No users are using my XP logging feature.";
            }

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("current-session", "Pulls stats of a current XP session.")]
        public async Task SessionStats(
            [Summary("user", "User you want the current XP Session stats for. Leave empty for your own.")] IUser User =
                null)
        {
            User ??= Context.User as SocketGuildUser;

            if (User == null)
            {
                await RespondAsync("Something went wrong...", ephemeral: true);
                return;
            }

            if (!ActiveConfig.IsExistingActiveUser(User.Id))
            {
                if (Context.User.Id == User.Id)
                {
                    await RespondAsync("You are not using my logging feature.", ephemeral: true);
                    return;
                }

                await RespondAsync($"{User.Username} is not using my logging feature.", ephemeral: true);
                return;
            }

            var aau = ActiveConfig.GetActiveAFKUser(User.Id);

            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder
            {
                Name = $"Current Session Stats for {aau.UniqueBungieName}",
                IconUrl = app.IconUrl
            };
            var foot = new EmbedFooterBuilder
            {
                Text = "XP Logging Session Summary"
            };
            var embed = new EmbedBuilder
            {
                Color =
                    new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot
            };
            var levelsGained = aau.LastLoggedLevel - aau.StartLevel;
            long xpGained = levelsGained * 100000 - aau.StartLevelProgress + aau.LastLevelProgress;
            var timeSpan = DateTime.Now - aau.TimeStarted;
            var timeString = $"{(Math.Floor(timeSpan.TotalHours) > 0 ? $"{Math.Floor(timeSpan.TotalHours)}h " : "")}" +
                             $"{(timeSpan.Minutes > 0 ? $"{timeSpan.Minutes:00}m " : "")}" +
                             $"{timeSpan.Seconds:00}s";
            var xpPerHour = 0;
            if ((DateTime.Now - aau.TimeStarted).TotalHours >= 1)
                xpPerHour = (int) Math.Floor(xpGained / (DateTime.Now - aau.TimeStarted).TotalHours);
            embed.WithCurrentTimestamp();
            embed.Description =
                $"Levels Gained: {levelsGained}\n" +
                $"XP Gained: {xpGained:n0}\n" +
                $"Time: {timeString}\n" +
                $"XP Per Hour: {xpPerHour:n0}";

            await RespondAsync(embed: embed.Build());
        }
    }
}