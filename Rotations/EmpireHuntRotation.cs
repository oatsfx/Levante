using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Rotations
{
    public class EmpireHuntRotation
    {
        public static readonly int EmpireHuntCount = 3;
        public static readonly string FilePath = @"Trackers/empireHunt.json";

        [JsonProperty("EmpireHuntLinks")]
        public static List<EmpireHuntLink> EmpireHuntLinks { get; set; } = new List<EmpireHuntLink>();

        public class EmpireHuntLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("EmpireHunt")]
            public EmpireHunt EmpireHunt { get; set; } = EmpireHunt.Technocrat;
        }

        public static string GetHuntNameString(EmpireHunt Encounter)
        {
            switch (Encounter)
            {
                case EmpireHunt.Warrior: return "The Warrior";
                case EmpireHunt.Technocrat: return "The Technocrat";
                case EmpireHunt.DarkPriestess: return "The Dark Priestess";
                default: return "Empire Hunt";
            }
        }

        public static string GetHuntBossString(EmpireHunt Encounter)
        {
            switch (Encounter)
            {
                case EmpireHunt.Warrior: return "Phylaks, the Warrior";
                case EmpireHunt.Technocrat: return "Praksis, the Technocrat";
                case EmpireHunt.DarkPriestess: return "Kridis, the Dark Priestess";
                default: return "Empire Hunt Boss";
            }
        }

        public static void AddUserTracking(ulong DiscordID, EmpireHunt EmpireHunt)
        {
            EmpireHuntLinks.Add(new EmpireHuntLink() { DiscordID = DiscordID, EmpireHunt = EmpireHunt });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            EmpireHuntLinks.Remove(GetUserTracking(DiscordID, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static EmpireHuntLink GetUserTracking(ulong DiscordID, out EmpireHunt EmpireHunt)
        {
            foreach (var Link in EmpireHuntLinks)
                if (Link.DiscordID == DiscordID)
                {
                    EmpireHunt = Link.EmpireHunt;
                    return Link;
                }
            EmpireHunt = EmpireHunt.Warrior;
            return null;
        }

        public static void CreateJSON()
        {
            EmpireHuntRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<EmpireHuntRotation>(json);
            }
            else
            {
                obj = new EmpireHuntRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new EmpireHuntRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(EmpireHunt EmpireHunt)
        {
            EmpireHunt iterationHunt = CurrentRotations.EmpireHunt;
            int WeeksUntil = 0;
            do
            {
                iterationHunt = iterationHunt == EmpireHunt.DarkPriestess ? EmpireHunt.Warrior : iterationHunt + 1;
                WeeksUntil++;
            } while (iterationHunt != EmpireHunt);
            return CurrentRotations.WeeklyResetTimestamp.AddDays(WeeksUntil * 7); // Because there is no .AddWeeks().
        }
    }

    public enum EmpireHunt
    {
        Warrior, // Phylaks
        Technocrat, // Praksis
        DarkPriestess, // Kridis
    }
}
