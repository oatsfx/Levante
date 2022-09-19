using Discord;
using Levante.Configs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Levante.Rotations.VowOfTheDiscipleRotation;

namespace Levante.Rotations
{
    public class KingsFallRotation
    {
        public static readonly int KingsFallEncounterCount = 5;
        public static readonly string FilePath = @"Trackers/kingsFall.json";

        [JsonProperty("KingsFallLinks")]
        public static List<KingsFallLink> KingsFallLinks { get; set; } = new List<KingsFallLink>();

        public class KingsFallLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("Encounter")]
            public KingsFallEncounter Encounter { get; set; } = KingsFallEncounter.Basilica;
        }
        public static string GetChallengeString(KingsFallEncounter Encounter)
        {
            switch (Encounter)
            {
                case KingsFallEncounter.Basilica: return "The Grass Is Always Greener";
                case KingsFallEncounter.Warpriest: return "Devious Thievery";
                case KingsFallEncounter.Golgoroth: return "Gaze Amaze";
                case KingsFallEncounter.Daughters: return "Under Construction";
                case KingsFallEncounter.Oryx: return "Hands Off";
                default: return "King's Fall";
            }
        }

        public static void AddUserTracking(ulong DiscordID, KingsFallEncounter Encounter)
        {
            KingsFallLinks.Add(new KingsFallLink() { DiscordID = DiscordID, Encounter = Encounter });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            KingsFallLinks.Remove(GetUserTracking(DiscordID, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static KingsFallLink GetUserTracking(ulong DiscordID, out KingsFallEncounter Encounter)
        {
            foreach (var Link in KingsFallLinks)
                if (Link.DiscordID == DiscordID)
                {
                    Encounter = Link.Encounter;
                    return Link;
                }
            Encounter = KingsFallEncounter.Basilica;
            return null;
        }

        public static void CreateJSON()
        {
            KingsFallRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<KingsFallRotation>(json);
            }
            else
            {
                obj = new KingsFallRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new KingsFallRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(KingsFallEncounter Encounter)
        {
            KingsFallEncounter iterationEncounter = CurrentRotations.KFChallengeEncounter;
            int WeeksUntil = 0;
            do
            {
                iterationEncounter = iterationEncounter == KingsFallEncounter.Oryx ? KingsFallEncounter.Basilica : iterationEncounter + 1;
                WeeksUntil++;
            } while (iterationEncounter != Encounter);
            return CurrentRotations.WeeklyResetTimestamp.AddDays(WeeksUntil * 7); // Because there is no .AddWeeks().
        }
    }

    public enum KingsFallEncounter
    {
        Basilica, // The Grass Is Always Greener
        Warpriest, // Devious Thievery
        Golgoroth, // Gaze Amaze
        Daughters, // Under Construction
        Oryx, // Hands Off
    }
}
