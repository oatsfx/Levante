using Levante.Rotations.Abstracts;
using Levante.Rotations.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Rotations
{
    public class DaresOfEternityRotation : SetRotation<DaresOfEternity, DaresOfEternityLink, DaresOfEternityPrediction>
    {
        public DaresOfEternityRotation()
        {
            FilePath = @"Trackers/witchQueenMission.json";
            RotationFilePath = @"Rotations/witchQueenMission.json";

            IsDaily = false;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override DaresOfEternityPrediction DatePrediction(int Week, int Skip)
        {
            int iteration = CurrentRotations.Actives.DaresOfEternity;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Week)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new DaresOfEternityPrediction { DaresOfEternity = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(DaresOfEternityLink Tracker) => Tracker.Week == CurrentRotations.Actives.DaresOfEternity;

        public override string ToString() => "Dares of Eternity";
    }

    public class DaresOfEternity
    {
        [JsonProperty("Name")]
        public readonly string Name;

        public override string ToString() => $"{Name}";
    }

    public class DaresOfEternityLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Week")]
        public int Week { get; set; } = 0;

        public override string ToString() => $"{CurrentRotations.DaresOfEternity.Rotations[Week]}";
    }

    public class DaresOfEternityPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public DaresOfEternity DaresOfEternity { get; set; }
    }
}
