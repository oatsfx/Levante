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
    public class LastWishRotation : Rotation<LastWish, LastWishLink, LastWishPrediction>
    {
        public LastWishRotation()
        {
            FilePath = @"Trackers/lastWish.json";
            RotationFilePath = @"Rotations/lastWish.json";

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override LastWishPrediction DatePrediction(int Encounter, int Skip)
        {
            int iteration = CurrentRotations.Actives.LWChallenge;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Encounter)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new LastWishPrediction { LastWish = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(LastWishLink Tracker) => Tracker.Encounter == CurrentRotations.Actives.LWChallenge;
    }

    /*public enum LastWishEncounter
    {
        Kalli, // Summoning Ritual
        ShuroChi, // Which Witch
        Morgeth, // Forever Fight
        Vault, // Keep Out
        Riven, // Strength of Memory
    }*/

    public class LastWish
    {
        [JsonProperty("Encounter")]
        public readonly string Encounter;

        [JsonProperty("ChallengeName")]
        public readonly string ChallengeName;
    }

    public class LastWishLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Encounter")]
        public int Encounter { get; set; } = 0;
    }

    public class LastWishPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public LastWish LastWish { get; set; }
    }
}
