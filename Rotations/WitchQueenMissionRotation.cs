using Levante.Rotations.Abstracts;
using Levante.Rotations.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levante.Rotations
{
    public class WitchQueenMissionRotation : SetRotation<StoryMission, WitchQueenMissionLink, WitchQueenMissionPrediction>
    {
        public WitchQueenMissionRotation()
        {
            FilePath = @"Trackers/witchQueenMission.json";
            RotationFilePath = @"Rotations/witchQueenMission.json";

            IsDaily = false;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override WitchQueenMissionPrediction DatePrediction(int Mission, int Skip)
        {
            int iteration = CurrentRotations.Actives.WitchQueenMission;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Mission)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new WitchQueenMissionPrediction { WitchQueenMission = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(WitchQueenMissionLink Tracker) => Tracker.Mission == CurrentRotations.Actives.WitchQueenMission;

        public override string ToString() => "Witch Queen Featured Mission";
    }

    public class StoryMission
    {
        [JsonProperty("Name")]
        public readonly string Name;

        public override string ToString() => $"{Name}";
    }

    public class WitchQueenMissionLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Mission")]
        public int Mission { get; set; } = 0;

        public override string ToString() => $"{CurrentRotations.WitchQueenMission.Rotations[Mission]}";
    }

    public class WitchQueenMissionPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public StoryMission WitchQueenMission { get; set; }
    }
}
