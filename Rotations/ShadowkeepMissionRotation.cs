using Levante.Rotations.Abstracts;
using Levante.Rotations.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levante.Rotations
{
    public class ShadowkeepMissionRotation : SetRotation<StoryMission, ShadowkeepMissionLink, ShadowkeepMissionPrediction>
    {
        public ShadowkeepMissionRotation()
        {
            FilePath = @"Trackers/shadowkeepMission.json";
            RotationFilePath = @"Rotations/shadowkeepMission.json";

            IsDaily = false;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override ShadowkeepMissionPrediction DatePrediction(int Mission, int Skip)
        {
            int iteration = CurrentRotations.Actives.ShadowkeepMission;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Mission)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new ShadowkeepMissionPrediction { ShadowkeepMission = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(ShadowkeepMissionLink Tracker) => Tracker.Mission == CurrentRotations.Actives.ShadowkeepMission;

        public override string ToString() => "Shadowkeep Featured Mission";
    }

    public class ShadowkeepMissionLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Mission")]
        public int Mission { get; set; } = 0;

        public override string ToString() => $"{CurrentRotations.ShadowkeepMission.Rotations[Mission]}";
    }

    public class ShadowkeepMissionPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public StoryMission ShadowkeepMission { get; set; }
    }
}
