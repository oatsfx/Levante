using Levante.Configs;
using Levante.Util;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Levante.Rotations.Abstracts;
using Levante.Rotations.Interfaces;

namespace Levante.Rotations
{
    public class FeaturedDungeonRotation : SetRotation<FeaturedDungeon, FeaturedDungeonLink, FeaturedDungeonPrediction>
    {
        public FeaturedDungeonRotation()
        {
            FilePath = @"Trackers/featuredDungeon.json";
            RotationFilePath = @"Rotations/featuredDungeon.json";

            IsDaily = false;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override FeaturedDungeonPrediction DatePrediction(int Encounter, int Skip)
        {
            int iteration = CurrentRotations.Actives.FeaturedDungeon;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Encounter)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new FeaturedDungeonPrediction { FeaturedDungeon = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(FeaturedDungeonLink Tracker) => Tracker.Dungeon == CurrentRotations.Actives.FeaturedDungeon;

        public override string ToString() => "Featured Dungeon";
    }

    public class FeaturedDungeon
    {
        [JsonProperty("Dungeon")]
        public readonly string Dungeon;

        public override string ToString() => Dungeon;
    }

    public class FeaturedDungeonLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Dungeon")]
        public int Dungeon { get; set; } = 0;

        public override string ToString() => $"{CurrentRotations.FeaturedDungeon.Rotations[Dungeon]}";
    }

    public class FeaturedDungeonPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public FeaturedDungeon FeaturedDungeon { get; set; }
    }
}
