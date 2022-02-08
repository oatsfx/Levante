using Levante.Configs;
using Levante.Leaderboards;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Helpers
{
    public class LeaderboardHelper
    {
        public static string GetLeaderboardString(Leaderboard LB)
        {
            switch (LB)
            {
                case Leaderboard.Level: return "Season Pass Level";
                case Leaderboard.LongestSession: return "Longest Thrallway Session";
                case Leaderboard.XPPerHour: return "Most Thrallway XP Per Hour";
                case Leaderboard.MostThrallwayTime: return "Total Thrallway Time";
                case Leaderboard.PowerLevel: return "Equipped Power Level";
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
                    if (DataConfig.IsExistingLinkedUser(User.Id) && SortedList[i].UniqueBungieName.Equals(DataConfig.GetLinkedUser(User.Id).UniqueBungieName))
                    {
                        embedDesc += $"{i + 1}) **{SortedList[i].UniqueBungieName}** ({GetValueString(SortedList[i])})\n";
                        isTop10 = true;
                    }
                    else
                        embedDesc += $"{i + 1}) {SortedList[i].UniqueBungieName} ({GetValueString(SortedList[i])})\n";
                }

                if (!isTop10 && DataConfig.IsExistingLinkedUser(User.Id))
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
                    if (DataConfig.IsExistingLinkedUser(User.Id) && SortedList[i].UniqueBungieName.Equals(DataConfig.GetLinkedUser(User.Id).UniqueBungieName))
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
            else if (LE.GetType() == typeof(PowerLevelData.PowerLevelDataEntry))
                return $"Power: {(LE as PowerLevelData.PowerLevelDataEntry).PowerLevel}";
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
            else if (T == typeof(PowerLevelData.PowerLevelDataEntry))
                return Leaderboard.PowerLevel;
            else
                return 0;
        }

        public static bool CheckAndLoadDataFiles()
        {
            LevelData ld;
            XPPerHourData xph;
            LongestSessionData ls;
            MostThrallwayTimeData mtt;
            PowerLevelData pld;

            bool closeProgram = false;
            if (File.Exists(LevelData.FilePath))
            {
                string json = File.ReadAllText(LevelData.FilePath);
                ld = JsonConvert.DeserializeObject<LevelData>(json);
            }
            else
            {
                ld = new LevelData();
                File.WriteAllText(LevelData.FilePath, JsonConvert.SerializeObject(ld, Formatting.Indented));
                Console.WriteLine($"No levelData.json file detected. A new one has been created and the program has stopped.");
                closeProgram = true;
            }

            if (File.Exists(XPPerHourData.FilePath))
            {
                string json = File.ReadAllText(XPPerHourData.FilePath);
                xph = JsonConvert.DeserializeObject<XPPerHourData>(json);
            }
            else
            {
                xph = new XPPerHourData();
                File.WriteAllText(XPPerHourData.FilePath, JsonConvert.SerializeObject(xph, Formatting.Indented));
                Console.WriteLine($"No xpPerHourData.json file detected. A new one has been created and the program has stopped.");
                closeProgram = true;
            }

            if (File.Exists(LongestSessionData.FilePath))
            {
                string json = File.ReadAllText(LongestSessionData.FilePath);
                ls = JsonConvert.DeserializeObject<LongestSessionData>(json);
            }
            else
            {
                ls = new LongestSessionData();
                File.WriteAllText(LongestSessionData.FilePath, JsonConvert.SerializeObject(ls, Formatting.Indented));
                Console.WriteLine($"No longestSessionData.json file detected. A new one has been created and the program has stopped.");
                closeProgram = true;
            }

            if (File.Exists(MostThrallwayTimeData.FilePath))
            {
                string json = File.ReadAllText(MostThrallwayTimeData.FilePath);
                mtt = JsonConvert.DeserializeObject<MostThrallwayTimeData>(json);
            }
            else
            {
                mtt = new MostThrallwayTimeData();
                File.WriteAllText(MostThrallwayTimeData.FilePath, JsonConvert.SerializeObject(mtt, Formatting.Indented));
                Console.WriteLine($"No mostThrallwayTimeData.json file detected. A new one has been created and the program has stopped.");
                closeProgram = true;
            }

            if (File.Exists(PowerLevelData.FilePath))
            {
                string json = File.ReadAllText(PowerLevelData.FilePath);
                pld = JsonConvert.DeserializeObject<PowerLevelData>(json);
            }
            else
            {
                pld = new PowerLevelData();
                File.WriteAllText(PowerLevelData.FilePath, JsonConvert.SerializeObject(mtt, Formatting.Indented));
                Console.WriteLine($"No powerLevelData.json file detected. A new one has been created and the program has stopped.");
                closeProgram = true;
            }

            if (closeProgram == true) return false;
            return true;
        }

        public static void CheckLeaderboardData(ActiveConfig.ActiveAFKUser AAU)
        {
            // Generate a Leaderboard entry, and overwrite if the existing one is worse.
            if (XPPerHourData.IsExistingLinkedEntry(AAU.UniqueBungieName))
            {
                var entry = XPPerHourData.GetExistingLinkedEntry(AAU.UniqueBungieName);

                int xpPerHour = 0;
                if ((DateTime.Now - AAU.TimeStarted).TotalHours >= 1)
                    xpPerHour = (int)Math.Floor((((AAU.LastLoggedLevel - AAU.StartLevel) * 100000) - AAU.StartLevelProgress + AAU.LastLevelProgress) / (DateTime.Now - AAU.TimeStarted).TotalHours);

                // Only add back if the entry is better than their previous.
                if (xpPerHour > entry.XPPerHour)
                {
                    XPPerHourData.DeleteEntryFromConfig(AAU.UniqueBungieName);
                    XPPerHourData.AddEntryToConfig(new XPPerHourData.XPPerHourEntry()
                    {
                        XPPerHour = xpPerHour,
                        UniqueBungieName = AAU.UniqueBungieName
                    });
                }
            }
            else
            {
                int xpPerHour = 0;
                if ((DateTime.Now - AAU.TimeStarted).TotalHours >= 1)
                    xpPerHour = (int)Math.Floor((((AAU.LastLoggedLevel - AAU.StartLevel) * 100000) - AAU.StartLevelProgress + AAU.LastLevelProgress) / (DateTime.Now - AAU.TimeStarted).TotalHours);

                XPPerHourData.AddEntryToConfig(new XPPerHourData.XPPerHourEntry()
                {
                    XPPerHour = xpPerHour,
                    UniqueBungieName = AAU.UniqueBungieName
                });
            }

            if (LongestSessionData.IsExistingLinkedEntry(AAU.UniqueBungieName))
            {
                var entry = LongestSessionData.GetExistingLinkedEntry(AAU.UniqueBungieName);

                var sessionTime = DateTime.Now - AAU.TimeStarted;

                // Only add back if the entry is better than their previous.
                if (sessionTime > entry.Time)
                {
                    LongestSessionData.DeleteEntryFromConfig(AAU.UniqueBungieName);
                    LongestSessionData.AddEntryToConfig(new LongestSessionData.LongestSessionEntry()
                    {
                        Time = sessionTime,
                        UniqueBungieName = AAU.UniqueBungieName
                    });
                }
            }
            else
            {
                LongestSessionData.AddEntryToConfig(new LongestSessionData.LongestSessionEntry()
                {
                    Time = DateTime.Now - AAU.TimeStarted,
                    UniqueBungieName = AAU.UniqueBungieName
                });
            }

            if (MostThrallwayTimeData.IsExistingLinkedEntry(AAU.UniqueBungieName))
            {
                var entry = MostThrallwayTimeData.GetExistingLinkedEntry(AAU.UniqueBungieName);

                var newTotalTime = (DateTime.Now - AAU.TimeStarted) + entry.Time;

                // Overwrite the existing entry with new data.
                MostThrallwayTimeData.DeleteEntryFromConfig(AAU.UniqueBungieName);
                MostThrallwayTimeData.AddEntryToConfig(new MostThrallwayTimeData.MostThrallwayTimeEntry()
                {
                    Time = newTotalTime,
                    UniqueBungieName = AAU.UniqueBungieName
                });
            }
            else
            {
                MostThrallwayTimeData.AddEntryToConfig(new MostThrallwayTimeData.MostThrallwayTimeEntry()
                {
                    Time = DateTime.Now - AAU.TimeStarted,
                    UniqueBungieName = AAU.UniqueBungieName
                });
            }
        }
    }
}
