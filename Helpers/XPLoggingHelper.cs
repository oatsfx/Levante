using Discord;
using Levante.Configs;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using APIHelper;
using APIHelper.Structs;
using Newtonsoft.Json;

namespace Levante.Helpers
{
    public class XPLoggingHelper
    {
        public static ComponentBuilder GenerateDeleteChannelButton()
        {
            Emoji deleteEmote = new Emoji("⛔");

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Delete Log Channel", customId: $"deleteChannel", ButtonStyle.Secondary, deleteEmote, row: 0);

            return buttonBuilder;
        }

        public static EmbedBuilder GenerateSessionSummary(ActiveConfig.ActiveAFKUser aau, string AuthorAvatarURL)
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Session Summary: {aau.UniqueBungieName}",
                IconUrl = AuthorAvatarURL,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"XP Logging Session Summary"
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

        public static int GetBrightEngrams(ActiveConfig.ActiveAFKUser user)
        {
            var memType = DataConfig.GetLinkedUser(user.DiscordID).BungieMembershipType;

            var profile = API.GetProfile(long.Parse(user.BungieMembershipID),
                (BungieMembershipType) int.Parse(memType), new[]
                {
                    Components.QueryComponents.Characters
                });

            if (profile.Response.Characters == null)
                return 0;

            var lastPlayed = new DateTime();
            var goodChar = "";

            foreach (var (key, value) in profile.Response.Characters.Data)
            {
                var charLastPlayed = DateTime.Parse(value.DateLastPlayed.ToString());
                if (charLastPlayed <= lastPlayed)
                {
                    continue;
                }

                lastPlayed = charLastPlayed;
                goodChar = key;
            }

            // lazy method because characterInventories struct is broken
            var url = $"/Platform/Destiny2/{memType}/Profile/{long.Parse(user.BungieMembershipID)}/?components=201";
            dynamic resp = JsonConvert.DeserializeObject(RemoteAPI.Query(url));

            var i = 0;
            if (resp != null)
                foreach (var responseItem in resp.Response.characterInventories[goodChar].items)
                    if (responseItem.itemHash == 1968811824)
                        i += 1;

            return i;
        }
    }
}
