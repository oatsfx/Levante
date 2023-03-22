using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Rotations
{
    public class AscendantChallengeRotation
    {
        public static readonly string FilePath = @"Trackers/ascendantChallenge.json";
        public static readonly string RotationsFilePath = @"Rotations/ascendantChallenge.json";

        public static List<AscendantChallenge> AscendantChallenges = new();

        [JsonProperty("AscendantChallengeLinks")]
        public static List<AscendantChallengeLink> AscendantChallengeLinks { get; set; } = new List<AscendantChallengeLink>();

        public class AscendantChallengeLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("AscendantChallenge")]
            public AscendantChallenge AscendantChallenge { get; set; } = 0;
        }

        public static string GetChallengeNameString(AscendantChallenge Encounter)
        {
            switch (Encounter)
            {
                case AscendantChallenge.AgonarchAbyss: return "Agonarch Abyss";
                case AscendantChallenge.CimmerianGarrison: return "Cimmerian Garrison";
                case AscendantChallenge.Ouroborea: return "Ouroborea";
                case AscendantChallenge.ForfeitShrine: return "Forfeit Shrine";
                case AscendantChallenge.ShatteredRuins: return "Shattered Ruins";
                case AscendantChallenge.KeepOfHonedEdges: return "Keep of Honed Edges";
                default: return "Ascendant Challenge";
            }
        }

        public static string GetChallengeLocationString(AscendantChallenge Encounter)
        {
            switch (Encounter)
            {
                case AscendantChallenge.AgonarchAbyss: return "Bay of Drowned Wishes";
                case AscendantChallenge.CimmerianGarrison: return "Chamber of Starlight";
                case AscendantChallenge.Ouroborea: return "Aphelion's Rest";
                case AscendantChallenge.ForfeitShrine: return "Gardens of Esila";
                case AscendantChallenge.ShatteredRuins: return "Spine of Keres";
                case AscendantChallenge.KeepOfHonedEdges: return "Harbinger's Seclude";
                default: return "Ascendant Challenge Location";
            }
        }

        public static void AddUserTracking(ulong DiscordID, AscendantChallenge AscendantChallenge)
        {
            AscendantChallengeLinks.Add(new AscendantChallengeLink() { DiscordID = DiscordID, AscendantChallenge = AscendantChallenge });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            AscendantChallengeLinks.RemoveAll(x => x.DiscordID == DiscordID);
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static AscendantChallengeLink GetUserTracking(ulong DiscordID, out AscendantChallenge AscendantChallenge)
        {
            foreach (var Link in AscendantChallengeLinks)
                if (Link.DiscordID == DiscordID)
                {
                    AscendantChallenge = Link.AscendantChallenge;
                    return Link;
                }
            AscendantChallenge = AscendantChallenge.AgonarchAbyss;
            return null;
        }

        public static void CreateJSON()
        {
            AscendantChallengeRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<AscendantChallengeRotation>(json);
            }
            else
            {
                obj = new AscendantChallengeRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new AscendantChallengeRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(AscendantChallenge AscendantChallenge)
        {
            AscendantChallenge iterationChal = CurrentRotations.AscendantChallenge;
            int WeeksUntil = 0;
            do
            {
                iterationChal = iterationChal == AscendantChallenge.KeepOfHonedEdges ? AscendantChallenge.AgonarchAbyss : iterationChal + 1;
                WeeksUntil++;
            } while (iterationChal != AscendantChallenge);
            return CurrentRotations.WeeklyResetTimestamp.AddDays(WeeksUntil * 7); // Because there is no .AddWeeks().
        }
    }

    public enum AscendantChallenge
    {
        AgonarchAbyss, // Bay of Drowned Wishes
        CimmerianGarrison, // Chamber of Starlight
        Ouroborea, // Aphelion's Rest
        ForfeitShrine, // Gardens of Esila
        ShatteredRuins, // Spine of Keres
        KeepOfHonedEdges, // Harbinger's Seclude
    }

    /*public class AscendantChallenge
    {
        [JsonProperty("Name")]
        public readonly string Name;
        [JsonProperty("Location")]
        public readonly string Location;
    }*/
}
