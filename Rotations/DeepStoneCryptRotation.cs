using Levante.Configs;
using Levante.Util;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Levante.Rotations.Interfaces;
using Levante.Rotations.Abstracts;

namespace Levante.Rotations
{
    public class DeepStoneCryptRotation : SetRotation<DeepStoneCrypt, DeepStoneCryptLink, DeepStoneCryptPrediction>
    {
        public DeepStoneCryptRotation()
        {
            FilePath = @"Trackers/deepStoneCrypt.json";
            RotationFilePath = @"Rotations/deepStoneCrypt.json";

            IsDaily = false;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override DeepStoneCryptPrediction DatePrediction(int Encounter, int Skip)
        {
            int iteration = CurrentRotations.Actives.DSCChallenge;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Encounter)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new DeepStoneCryptPrediction { DeepStoneCrypt = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(DeepStoneCryptLink Tracker) => Tracker.Encounter == CurrentRotations.Actives.DSCChallenge || CurrentRotations.FeaturedRaid.Rotations[CurrentRotations.Actives.FeaturedRaid].Raid.Equals("Deep Stone Crypt");

        public override string ToString() => "Deep Stone Crypt Challenge";
    }

    /*public enum DeepStoneCryptEncounter
    {
        Security, // Red Rover
        Atraks1, // Copies of Copies
        Descent, // Of All Trades
        Taniks, // The Core Four
    }*/

    public class DeepStoneCrypt
    {
        [JsonProperty("Encounter")]
        public readonly string Encounter;

        [JsonProperty("ChallengeName")]
        public readonly string ChallengeName;

        public override string ToString() => $"{Encounter} ({ChallengeName})";
    }

    public class DeepStoneCryptLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Encounter")]
        public int Encounter { get; set; } = 0;

        public override string ToString() => $"{CurrentRotations.DeepStoneCrypt.Rotations[Encounter]}";
    }

    public class DeepStoneCryptPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public DeepStoneCrypt DeepStoneCrypt { get; set; }
    }
}
