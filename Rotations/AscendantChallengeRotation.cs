using Levante.Rotations.Interfaces;
using Levante.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Levante.Rotations.Abstracts;

namespace Levante.Rotations
{
    public class AscendantChallengeRotation : SetRotation<AscendantChallenge, AscendantChallengeLink, AscendantChallengePrediction>
    {
        public AscendantChallengeRotation()
        {
            FilePath = @"Trackers/ascendantChallenge.json";
            RotationFilePath = @"Rotations/ascendantChallenge.json";

            IsDaily = false;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override AscendantChallengePrediction DatePrediction(int Encounter, int Skip)
        {
            int iteration = CurrentRotations.Actives.AscendantChallenge;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Encounter)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new AscendantChallengePrediction { AscendantChallenge = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(AscendantChallengeLink Tracker) => Tracker.AscendantChallenge == CurrentRotations.Actives.AscendantChallenge;

        public override string ToString() => "Ascendant Challenge";
    }

    public class AscendantChallenge
    {
        [JsonProperty("Name")]
        public readonly string Name;
        [JsonProperty("Location")]
        public readonly string Location;

        public override string ToString() => $"{Name} ({Location})";
    }

    public class AscendantChallengeLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; }

        [JsonProperty("AscendantChallenge")]
        public int AscendantChallenge { get; set; }

        public override string ToString() => $"{CurrentRotations.AscendantChallenge.Rotations[AscendantChallenge]}";
    }

    public class AscendantChallengePrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public AscendantChallenge AscendantChallenge { get; set; }
    }
}
