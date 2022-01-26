using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DestinyUtility.Rotations
{
    public class NightmareHuntRotation
    {
        public static readonly int NightmareHuntCount = 8;
        public static readonly string FilePath = @"Trackers/nightmareHunt.json";

        [JsonProperty("NightmareHuntLinks")]
        public static List<NightmareHuntLink> NightmareHuntLinks { get; set; } = new List<NightmareHuntLink>();

        public class NightmareHuntLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("NightmareHunt")]
            public NightmareHunt NightmareHunt { get; set; } = NightmareHunt.Crota;
        }

        public static string GetHuntNameString(NightmareHunt Encounter)
        {
            switch (Encounter)
            {
                case NightmareHunt.Crota: return "Despair";
                case NightmareHunt.Phogoth: return "Fear";
                case NightmareHunt.Ghaul: return "Rage";
                case NightmareHunt.Taniks: return "Isolation";
                case NightmareHunt.Zydron: return "Servitude";
                case NightmareHunt.Skolas: return "Pride";
                case NightmareHunt.Omnigul: return "Anguish";
                case NightmareHunt.Fanatic: return "Insanity";
                default: return "Nightmare Hunt";
            }
        }

        public static string GetHuntBossString(NightmareHunt Encounter)
        {
            switch (Encounter)
            {
                case NightmareHunt.Crota: return "Crota, Son of Oryx";
                case NightmareHunt.Phogoth: return "Phogoth, the Untamed";
                case NightmareHunt.Ghaul: return "Dominus Ghaul";
                case NightmareHunt.Taniks: return "Taniks, the Scarred";
                case NightmareHunt.Zydron: return "Zydron, Gate Lord";
                case NightmareHunt.Skolas: return "Skolas, Kell of Kells";
                case NightmareHunt.Omnigul: return "Omnigul, Will of Crota";
                case NightmareHunt.Fanatic: return "Fikrul, the Fanatic";
                default: return "Nightmare Hunt Boss";
            }
        }

        public static void AddUserTracking(ulong DiscordID, NightmareHunt NightmareHunt)
        {
            NightmareHuntLinks.Add(new NightmareHuntLink() { DiscordID = DiscordID, NightmareHunt = NightmareHunt });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            NightmareHuntLinks.Remove(GetUserTracking(DiscordID, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static NightmareHuntLink GetUserTracking(ulong DiscordID, out NightmareHunt NightmareHunt)
        {
            foreach (var Link in NightmareHuntLinks)
                if (Link.DiscordID == DiscordID)
                {
                    NightmareHunt = Link.NightmareHunt;
                    return Link;
                }
            NightmareHunt = NightmareHunt.Crota;
            return null;
        }

        public static void CreateJSON()
        {
            NightmareHuntRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<NightmareHuntRotation>(json);
            }
            else
            {
                obj = new NightmareHuntRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new NightmareHuntRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(NightmareHunt NightmareHunt)
        {
            NightmareHunt[] iterationHunts = CurrentRotations.NightmareHunts;
            int WeeksUntil = 0;
            do
            {
                iterationHunts[0] = iterationHunts[0] >= NightmareHunt.Skolas ? iterationHunts[0] - 5 : iterationHunts[0] + 3;
                iterationHunts[1] = iterationHunts[1] >= NightmareHunt.Skolas ? iterationHunts[1] - 5 : iterationHunts[1] + 3;
                iterationHunts[2] = iterationHunts[2] >= NightmareHunt.Skolas ? iterationHunts[2] - 5 : iterationHunts[2] + 3;
                WeeksUntil++;
            } while (iterationHunts[0] != NightmareHunt && iterationHunts[1] != NightmareHunt && iterationHunts[2] != NightmareHunt);
            return CurrentRotations.WeeklyResetTimestamp.AddDays(WeeksUntil * 7); // Because there is no .AddWeeks().
        }
    }

    public enum NightmareHunt
    {
        Crota,
        Phogoth,
        Ghaul,
        Taniks,
        Zydron,
        Skolas,
        Omnigul,
        Fanatic,
    }
}
