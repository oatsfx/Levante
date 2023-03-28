using Levante.Rotations.Abstracts;
using Levante.Rotations.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;

namespace Levante.Rotations
{
    public class NightmareHuntRotation : Rotation<NightmareHunt, NightmareHuntLink, NightmareHuntPrediction>
    {
        public NightmareHuntRotation()
        {
            FilePath = @"Trackers/nightmareHunt.json";
            RotationFilePath = @"Rotations/altarsOfSorrow.json";

            GetRotationJSON();
            GetTrackerJSON();
        }

        /*public static string GetHuntNameString(NightmareHunt Encounter)
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
        }*/

        public override NightmareHuntPrediction DatePrediction(int Hunt, int Skip)
        {
            int[] iterations = CurrentRotations.Actives.NightmareHunts;
            int WeeksUntil = 0;
            do
            {
                iterations[0] = iterations[0] >= Rotations.Count - 3 ? iterations[0] - 5 : iterations[0] + 3;
                iterations[1] = iterations[1] >= Rotations.Count - 3 ? iterations[1] - 5 : iterations[1] + 3;
                iterations[2] = iterations[2] >= Rotations.Count - 3 ? iterations[2] - 5 : iterations[2] + 3;
                WeeksUntil++;
            } while (iterations[0] != Hunt && iterations[1] != Hunt && iterations[2] != Hunt);
            return new NightmareHuntPrediction { NightmareHunt = Rotations[Hunt], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) }; // Because there is no .AddWeeks().
        }

        public override bool IsTrackerInRotation(NightmareHuntLink Tracker) => CurrentRotations.Actives.NightmareHunts.Contains(Tracker.Hunt);
    }

    /*public enum NightmareHunt
    {
        Crota,
        Phogoth,
        Ghaul,
        Taniks,
        Zydron,
        Skolas,
        Omnigul,
        Fanatic,
    }*/

    public class NightmareHunt
    {
        [JsonProperty("Name")]
        public string Name;
        [JsonProperty("Boss")]
        public string Boss;
    }

    public class NightmareHuntLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Hunt")]
        public int Hunt { get; set; } = 0;
    }

    public class NightmareHuntPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public NightmareHunt NightmareHunt { get; set; }
    }
}
