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
    public class FeaturedRaidRotation : SetRotation<FeaturedRaid, FeaturedRaidLink, FeaturedRaidPrediction>
    {
        public FeaturedRaidRotation()
        {
            FilePath = @"Trackers/featuredRaid.json";
            RotationFilePath = @"Rotations/featuredRaid.json";

            IsDaily = false;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override FeaturedRaidPrediction DatePrediction(int Encounter, int Skip)
        {
            int iteration = CurrentRotations.Actives.FeaturedRaid;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Encounter)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new FeaturedRaidPrediction { FeaturedRaid = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(FeaturedRaidLink Tracker) => Tracker.Raid == CurrentRotations.Actives.FeaturedRaid;

        public override string ToString() => "Featured Raid";
    }

    public class FeaturedRaid
    {
        [JsonProperty("Raid")]
        public readonly string Raid;

        public override string ToString() => Raid;
    }

    public class FeaturedRaidLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Raid")]
        public int Raid { get; set; } = 0;

        public override string ToString() => $"{CurrentRotations.FeaturedRaid.Rotations[Raid]}";
    }

    public class FeaturedRaidPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public FeaturedRaid FeaturedRaid { get; set; }
    }
}
