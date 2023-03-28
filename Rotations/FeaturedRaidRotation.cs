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
    public class FeaturedRaidRotation : Rotation<FeaturedRaid, FeaturedRaidLink, FeaturedRaidPrediction>
    {
        public FeaturedRaidRotation()
        {
            FilePath = @"Trackers/featuredRaid.json";
            RotationFilePath = @"Rotations/featuredRaid.json";

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override FeaturedRaidPrediction DatePrediction(int Raid, int Skip)
        {
            int iteration = CurrentRotations.Actives.FeaturedRaid;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Raid)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new FeaturedRaidPrediction { FeaturedRaid = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(FeaturedRaidLink Tracker) => Tracker.Raid == CurrentRotations.Actives.FeaturedRaid;
    }

    /*public enum Raid
    {
        LastWish,
        GardenOfSalvation,
        DeepStoneCrypt,
        VaultOfGlass,
        VowOfTheDisciple,
        KingsFall,
    }*/

    public class FeaturedRaid
    {
        [JsonProperty("Raid")]
        public readonly string Raid;
    }

    public class FeaturedRaidLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Raid")]
        public int Raid { get; set; } = 0;
    }

    public class FeaturedRaidPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public FeaturedRaid FeaturedRaid { get; set; }
    }
}
