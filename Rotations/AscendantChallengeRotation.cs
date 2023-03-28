using Levante.Rotations.Interfaces;
using Levante.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Levante.Rotations.Abstracts;

namespace Levante.Rotations
{
    public class AscendantChallengeRotation : Rotation<AscendantChallenge, AscendantChallengeLink, AscendantChallengePrediction>
    {
        public AscendantChallengeRotation()
        {
            FilePath = @"Trackers/ascendantChallenge.json";
            RotationFilePath = @"Rotations/ascendantChallenge.json";

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override AscendantChallengePrediction DatePrediction(int Challenge, int Skip)
        {
            int iteration = CurrentRotations.Actives.AscendantChallenge;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Challenge)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new AscendantChallengePrediction { AscendantChallenge = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(AscendantChallengeLink Tracker) => Tracker.AscendantChallenge == CurrentRotations.Actives.AscendantChallenge;
    }

    /*public enum AscendantChallenge
    {
        AgonarchAbyss, // Bay of Drowned Wishes
        CimmerianGarrison, // Chamber of Starlight
        Ouroborea, // Aphelion's Rest
        ForfeitShrine, // Gardens of Esila
        ShatteredRuins, // Spine of Keres
        KeepOfHonedEdges, // Harbinger's Seclude
    }*/

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
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("AscendantChallenge")]
        public int AscendantChallenge { get; set; } = 0;
    }

    public class AscendantChallengePrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public AscendantChallenge AscendantChallenge { get; set; }
    }
}
