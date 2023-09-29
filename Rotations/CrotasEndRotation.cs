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
    public class CrotasEndRotation : SetRotation<CrotasEnd, CrotasEndLink, CrotasEndPrediction>
    {
        public CrotasEndRotation()
        {
            FilePath = @"Trackers/crotasEnd.json";
            RotationFilePath = @"Rotations/crotasEnd.json";

            IsDaily = false;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override CrotasEndPrediction DatePrediction(int Encounter, int Skip)
        {
            int iteration = CurrentRotations.Actives.CEChallenge;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Encounter)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new CrotasEndPrediction { CrotasEnd = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(CrotasEndLink Tracker) => Tracker.Encounter == CurrentRotations.Actives.CEChallenge;

        public override string ToString() => "Crota's End Challenge";
    }

    public class CrotasEnd
    {
        [JsonProperty("Encounter")]
        public readonly string Encounter;

        [JsonProperty("ChallengeName")]
        public readonly string ChallengeName;

        public override string ToString() => $"{Encounter} ({ChallengeName})";
    }

    public class CrotasEndLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Encounter")]
        public int Encounter { get; set; } = 0;

        public override string ToString() => $"{CurrentRotations.CrotasEnd.Rotations[Encounter]}";
    }

    public class CrotasEndPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public CrotasEnd CrotasEnd { get; set; }
    }
}
