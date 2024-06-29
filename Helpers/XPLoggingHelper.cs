﻿using Discord;
using Levante.Configs;
using System;

namespace Levante.Helpers
{
    public class XPLoggingHelper
    {
        public static ComponentBuilder GenerateChannelButtons(ulong DiscordID)
        {
            Emoji deleteEmote = new Emoji("⛔");
            Emoji restartEmote = new Emoji("😴");

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Delete Log Channel", customId: $"deleteChannel", ButtonStyle.Secondary, deleteEmote, row: 0)
                .WithButton("Restart Logging", customId: $"restartLogging:{DiscordID}", ButtonStyle.Secondary, restartEmote, row: 0);

            return buttonBuilder;
        }

        public static EmbedBuilder GenerateSessionSummary(ActiveConfig.ActiveAFKUser aau)
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Session Summary: {aau.UniqueBungieName}",
                IconUrl = BotConfig.BotAvatarUrl,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"XP Logging Session Summary"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = auth,
                Footer = foot,
            };
            int powerBonusGained = aau.LastPowerBonus - aau.StartPowerBonus;
            var levelCap = ManifestHelper.CurrentLevelCap;
            var nextLevelAt = aau.LastNextLevelAt;

            int levelsGained = (aau.LastLevel - aau.StartLevel) + (aau.LastExtraLevel - aau.StartExtraLevel);
            long xpGained = 0;
            // If the user hit the cap during the session, calculate gained XP differently.
            if (aau.StartLevel < levelCap && aau.LastLevel >= levelCap)
            {
                int levelsToCap = levelCap - aau.StartLevel;
                int levelsPastCap = levelsGained - levelsToCap;
                // StartNextLevelAt should be the 100,000 or whatever Bungie decides to change it to later on.
                xpGained = ((levelsToCap) * aau.StartNextLevelAt) + (levelsPastCap * nextLevelAt) - aau.StartLevelProgress + aau.LastLevelProgress;
            }
            else // The nextLevelAt stays the same because it did not change mid-logging session.
            {
                xpGained = (levelsGained * nextLevelAt) - aau.StartLevelProgress + aau.LastLevelProgress;
            }
            
            var timeSpan = DateTime.Now - aau.TimeStarted;
            string timeString = $"{(Math.Floor(timeSpan.TotalHours) > 0 ? $"{Math.Floor(timeSpan.TotalHours)}h " : "")}" +
                    $"{(timeSpan.Minutes > 0 ? $"{timeSpan.Minutes:00}m " : "")}" +
                    $"{timeSpan.Seconds:00}s";
            int xpPerHour = (int)Math.Floor(xpGained / (DateTime.Now - aau.TimeStarted).TotalHours);
            embed.WithCurrentTimestamp();
            embed.Description = $"Time Logged: {timeString}\n";

            embed.AddField(x =>
            {
                x.Name = "Level Information";
                x.Value = $"Start: {aau.StartLevel} ({aau.StartLevelProgress:n0}/{aau.StartNextLevelAt})\n" +
                    $"Now: {aau.LastLevel} ({aau.LastLevelProgress:n0}/{aau.LastNextLevelAt})\n" +
                    $"Gained: {levelsGained}\n";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "XP Information";
                x.Value = $"Gained: {xpGained:n0}\n" +
                    $"XP Per Hour: {xpPerHour:n0}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Power Bonus Information";
                x.Value = $"Start: {aau.StartPowerBonus}\n" +
                    $"Now: {aau.LastPowerBonus}\n" +
                    $"Gained: {powerBonusGained}\n";
                x.IsInline = true;
            });

            return embed;
        }
    }
}
