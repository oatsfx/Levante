using Discord;
using Levante.Configs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Helpers
{
    public class ThrallwayHelper
    {
        public static ComponentBuilder GenerateDeleteChannelButton()
        {
            Emoji deleteEmote = new Emoji("⛔");

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Delete Log Channel", customId: $"deleteChannel", ButtonStyle.Secondary, deleteEmote, row: 0);

            return buttonBuilder;
        }

        public static EmbedBuilder GenerateSessionSummary(ActiveConfig.ActiveAFKUser aau)
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Session Summary: {aau.UniqueBungieName}",
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
            embed.Description =
                $"Levels Gained: {levelsGained}\n" +
                $"XP Gained: {String.Format("{0:n0}", xpGained)}\n" +
                $"Time: {timeString}\n" +
                $"XP Per Hour: {String.Format("{0:n0}", xpPerHour)}";

            return embed;
        }
    }
}
