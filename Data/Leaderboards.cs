using DestinyUtility.Configs;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DestinyUtility.Data
{
    public class Leaderboards
    {
        public static string GetLeaderboardString(Leaderboard LB)
        {
            switch (LB)
            {
                case Leaderboard.Level: return "Season Pass Level";
                case Leaderboard.LongestSession: return "Longest Thrallway Session";
                case Leaderboard.XPPerHour: return "Most Thrallway XP Per Hour";
                case Leaderboard.MostThrallwayTime: return "Total Thrallway Time";
                default: return "Leaderboard";
            }
        }

        public static EmbedBuilder GetLeaderboardEmbed<T>(List<T> SortedList, IUser User) where T : LeaderboardEntry
        {
            bool isTop10 = false;
            string embedDesc = "";

            if (SortedList.Count <= 0)
            {
                embedDesc = "No data, try again later.";
            }
            else if (SortedList.Count >= 10)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (SortedList[i].UniqueBungieName.Equals(DataConfig.GetLinkedUser(User.Id).UniqueBungieName))
                    {
                        embedDesc += $"{i + 1}) **{SortedList[i].UniqueBungieName}** ({GetValueString(SortedList[i])})\n";
                        isTop10 = true;
                    }
                    else
                        embedDesc += $"{i + 1}) {SortedList[i].UniqueBungieName} ({GetValueString(SortedList[i])})\n";
                }

                if (!isTop10)
                {
                    embedDesc += "...\n";
                    for (int i = 10; i < SortedList.Count; i++)
                    {
                        if (SortedList[i].UniqueBungieName.Equals(DataConfig.GetLinkedUser(User.Id).UniqueBungieName))
                            embedDesc += $"{i + 1}) **{SortedList[i].UniqueBungieName}** ({GetValueString(SortedList[i])})";
                    }
                }
            }
            else
            {
                for (int i = 0; i < SortedList.Count; i++)
                {
                    if (SortedList[i].UniqueBungieName.Equals(DataConfig.GetLinkedUser(User.Id).UniqueBungieName))
                        embedDesc += $"{i + 1}) **{SortedList[i].UniqueBungieName}** ({GetValueString(SortedList[i])})\n";
                    else
                        embedDesc += $"{i + 1}) {SortedList[i].UniqueBungieName} ({GetValueString(SortedList[i])})\n";
                }
            }

            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Top Linked Guardians by {GetLeaderboardString(GetLeaderboardEnumValue(typeof(T)))}",
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by Destiny Utility"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };
            embed.Description = embedDesc;

            return embed;
        }

        private static string GetValueString(LeaderboardEntry LE)
        {
            if (LE.GetType() == typeof(LevelData.LevelDataEntry))
                return $"Level: {(LE as LevelData.LevelDataEntry).LastLoggedLevel}";
            else if (LE.GetType() == typeof(LongestSessionData.LongestSessionEntry))
                return $"{(Math.Floor((LE as LongestSessionData.LongestSessionEntry).Time.TotalHours) > 0 ? $"{Math.Floor((LE as LongestSessionData.LongestSessionEntry).Time.TotalHours)}h " : "")}" +
                    $"{((LE as LongestSessionData.LongestSessionEntry).Time.Minutes > 0 ? $"{(LE as LongestSessionData.LongestSessionEntry).Time.Minutes:00}m " : "")}" +
                    $"{(LE as LongestSessionData.LongestSessionEntry).Time.Seconds:00}s";
            else if (LE.GetType() == typeof(XPPerHourData.XPPerHourEntry))
                return $"XP Per Hour: {String.Format("{0:n0}", (LE as XPPerHourData.XPPerHourEntry).XPPerHour)}";
            else if (LE.GetType() == typeof(MostThrallwayTimeData.MostThrallwayTimeEntry))
                return $"Hours: {Math.Floor((LE as MostThrallwayTimeData.MostThrallwayTimeEntry).Time.TotalHours)}";
            else
                return $"{LE.UniqueBungieName}";
        }

        private static Leaderboard GetLeaderboardEnumValue(Type T)
        {
            if (T == typeof(LevelData.LevelDataEntry))
                return Leaderboard.Level;
            else if (T == typeof(LongestSessionData.LongestSessionEntry))
                return Leaderboard.LongestSession;
            else if (T == typeof(XPPerHourData.XPPerHourEntry))
                return Leaderboard.XPPerHour;
            else if (T == typeof(MostThrallwayTimeData.MostThrallwayTimeEntry))
                return Leaderboard.MostThrallwayTime;
            else
                return 0;
        }
    }
    public interface LeaderboardEntry
    {
        public string UniqueBungieName { get; set; }
    }

    public enum Leaderboard
    {
        Level,
        LongestSession,
        XPPerHour,
        MostThrallwayTime,
    }
}
