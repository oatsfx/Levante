using Levante.Rotations.Abstracts;
using Levante.Rotations.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Rotations
{
    public class CurseWeekRotation : Rotation<CurseWeek, CurseWeekLink, CurseWeekPrediction>
    {
        public CurseWeekRotation()
        {
            FilePath = @"Trackers/curseWeek.json";
            RotationFilePath = @"Rotations/curseWeek.json";

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override CurseWeekPrediction DatePrediction(int Strength, int Skip)
        {
            int iteration = CurrentRotations.Actives.CurseWeek;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Strength)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new CurseWeekPrediction { CurseWeek = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(CurseWeekLink Tracker) => Tracker.Strength == CurrentRotations.Actives.CurseWeek;
    }

    /*public enum CurseWeek
    {
        Weak,
        Medium,
        Strong,
    }*/

    public class CurseWeek
    {
        [JsonProperty("Name")]
        public readonly string Name;

        [JsonProperty("PetraLocation")]
        public readonly string PetraLocation;
    }

    public class CurseWeekLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Strength")]
        public int Strength { get; set; } = 0;
    }

    public class CurseWeekPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public CurseWeek CurseWeek { get; set; }
    }
}
