using Levante.Rotations.Abstracts;
using Levante.Rotations.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Rotations
{
    public class EmpireHuntRotation : Rotation<EmpireHunt, EmpireHuntLink, EmpireHuntPrediction>
    {
        public EmpireHuntRotation()
        {
            FilePath = @"Trackers/empireHunt.json";
            RotationFilePath = @"Rotations/empireHunt.json";

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override EmpireHuntPrediction DatePrediction(int Hunt, int Skip)
        {
            int iteration = CurrentRotations.Actives.EmpireHunt;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Hunt)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new EmpireHuntPrediction { EmpireHunt = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(EmpireHuntLink Tracker) => Tracker.EmpireHunt == CurrentRotations.Actives.EmpireHunt;
    }

    /*public enum EmpireHunt
    {
        Warrior, // Phylaks
        Technocrat, // Praksis
        DarkPriestess, // Kridis
    }*/

    public class EmpireHunt
    {
        [JsonProperty("Name")]
        public readonly string Name;

        [JsonProperty("BossName")]
        public readonly string BossName;
    }

    public class EmpireHuntLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("EmpireHunt")]
        public int EmpireHunt { get; set; } = 0;
    }

    public class EmpireHuntPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public EmpireHunt EmpireHunt { get; set; }
    }
}
