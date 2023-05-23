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
    public class LightfallMissionRotation : SetRotation<StoryMission, LightfallMissionLink, LightfallMissionPrediction>
    {
        public LightfallMissionRotation()
        {
            FilePath = @"Trackers/lightfallMission.json";
            RotationFilePath = @"Rotations/lightfallMission.json";

            IsDaily = false;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override LightfallMissionPrediction DatePrediction(int Mission, int Skip)
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
            return new LightfallMissionPrediction { LightfallMission = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(LightfallMissionLink Tracker) => Tracker.Mission == CurrentRotations.Actives.LightfallMission;

        public override string ToString() => "Lightfall Featured Mission";
    }

    public class LightfallMissionLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Mission")]
        public int Mission { get; set; } = 0;

        public override string ToString() => $"{CurrentRotations.LightfallMission.Rotations[Mission]}";
    }

    public class LightfallMissionPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public StoryMission LightfallMission { get; set; }
    }
}
