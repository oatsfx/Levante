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
    public class GardenOfSalvationRotation : SetRotation<RaidEncounter, GardenOfSalvationLink, GardenOfSalvationPrediction>
    {
        public GardenOfSalvationRotation()
        {
            FilePath = @"Trackers/gardenOfSalvation.json";
            RotationFilePath = @"Rotations/gardenOfSalvation.json";

            IsDaily = false;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override GardenOfSalvationPrediction DatePrediction(int Encounter, int Skip)
        {
            int iteration = CurrentRotations.Actives.GoSChallenge;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Encounter)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new GardenOfSalvationPrediction { GardenOfSalvation = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(GardenOfSalvationLink Tracker) => Tracker.Encounter == CurrentRotations.Actives.GoSChallenge || CurrentRotations.FeaturedRaid.Rotations[CurrentRotations.Actives.FeaturedRaid].Raid.Equals("Garden of Salvation");

        public override string ToString() => "Garden of Salvation Challenge";
    }

    /*public enum GardenOfSalvationEncounter
    {
        Evade, // Staying Alive
        Summon, // A Link to the Chain
        ConsecratedMind, // To the Top
        SanctifiedMind, // Zero to One Hundred
    }*/

    public class RaidEncounter
    {
        [JsonProperty("Encounter")]
        public readonly string Encounter;

        [JsonProperty("ChallengeName")]
        public readonly string ChallengeName;

        public override string ToString() => $"{Encounter} ({ChallengeName})";
    }

    public class GardenOfSalvationLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Encounter")]
        public int Encounter { get; set; } = 0;

        public override string ToString() => $"{CurrentRotations.GardenOfSalvation.Rotations[Encounter]}";
    }

    public class GardenOfSalvationPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public RaidEncounter GardenOfSalvation { get; set; }
    }
}
