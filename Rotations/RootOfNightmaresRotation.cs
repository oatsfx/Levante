using Discord;
using Levante.Configs;
using Levante.Rotations.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Levante.Rotations.Abstracts;

namespace Levante.Rotations
{
    public class RootOfNightmaresRotation : SetRotation<RootOfNightmares, RootOfNightmaresLink, RootOfNightmaresPrediction>
    {
        public RootOfNightmaresRotation()
        {
            FilePath = @"Trackers/rootOfNightmares.json";
            RotationFilePath = @"Rotations/rootOfNightmares.json";

            IsDaily = false;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override RootOfNightmaresPrediction DatePrediction(int Encounter, int Skip)
        {
            int iteration = CurrentRotations.Actives.RoNChallenge;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Encounter)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new RootOfNightmaresPrediction { RootOfNightmares = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(RootOfNightmaresLink Tracker) => Tracker.Encounter == CurrentRotations.Actives.RoNChallenge;

        public override string ToString() => "Root of Nightmares Challenge";
    }

    public class RootOfNightmares
    {
        [JsonProperty("Encounter")]
        public readonly string Encounter;

        [JsonProperty("ChallengeName")]
        public readonly string ChallengeName;

        public override string ToString() => $"{Encounter} ({ChallengeName})";
    }

    public class RootOfNightmaresLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Encounter")]
        public int Encounter { get; set; } = 0;

        public override string ToString() => $"{CurrentRotations.RootOfNightmares.Rotations[Encounter]}";
    }

    public class RootOfNightmaresPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public RootOfNightmares RootOfNightmares { get; set; }
    }
}
