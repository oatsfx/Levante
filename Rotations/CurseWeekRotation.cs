using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Rotations
{
    public class CurseWeekRotation
    {
        public static readonly int CurseWeekCount = 3;
        public static readonly string FilePath = @"Trackers/curseWeek.json";

        [JsonProperty("CurseWeekLinks")]
        public static List<CurseWeekLink> CurseWeekLinks { get; set; } = new List<CurseWeekLink>();

        public class CurseWeekLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("Week")]
            public CurseWeek Strength { get; set; } = CurseWeek.Weak;
        }

        public static void AddUserTracking(ulong DiscordID, CurseWeek CurseWeek)
        {
            CurseWeekLinks.Add(new CurseWeekLink() { DiscordID = DiscordID, Strength = CurseWeek });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            CurseWeekLinks.Remove(GetUserTracking(DiscordID, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static CurseWeekLink GetUserTracking(ulong DiscordID, out CurseWeek CurseWeek)
        {
            foreach (var Link in CurseWeekLinks)
                if (Link.DiscordID == DiscordID)
                {
                    CurseWeek = Link.Strength;
                    return Link;
                }
            CurseWeek = CurseWeek.Weak;
            return null;
        }

        public static void CreateJSON()
        {
            CurseWeekRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<CurseWeekRotation>(json);
            }
            else
            {
                obj = new CurseWeekRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new CurseWeekRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(CurseWeek CurseWeek)
        {
            CurseWeek iterationWeek = CurrentRotations.CurseWeek;
            int WeeksUntil = 0;
            do
            {
                iterationWeek = iterationWeek == CurseWeek.Strong ? CurseWeek.Weak : iterationWeek + 1;
                WeeksUntil++;
            } while (iterationWeek != CurseWeek);
            return CurrentRotations.WeeklyResetTimestamp.AddDays(WeeksUntil * 7); // Because there is no .AddWeeks().
        }
    }

    public enum CurseWeek
    {
        Weak,
        Medium,
        Strong,
    }
}
