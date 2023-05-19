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
    public class VaultOfGlassRotation : SetRotation<VaultOfGlass, VaultOfGlassLink, VaultOfGlassPrediction>
    {
        public VaultOfGlassRotation()
        {
            FilePath = @"Trackers/vaultOfGlass.json";
            RotationFilePath = @"Rotations/vaultOfGlass.json";

            IsDaily = false;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override VaultOfGlassPrediction DatePrediction(int Encounter, int Skip)
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
            return new VaultOfGlassPrediction { VaultOfGlass = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(VaultOfGlassLink Tracker) => Tracker.Encounter == CurrentRotations.Actives.VoGChallenge || CurrentRotations.FeaturedRaid.Rotations[CurrentRotations.Actives.FeaturedRaid].Raid.Equals("Vault of Glass");

        public override string ToString() => "Vault of Glass Challenge";
    }

    /*public enum VaultOfGlassEncounter
    {
        Confluxes, // Vision of Confluence, Wait for It...
        Oracles, // Praedyth's Revenge, The Only Oracle for You
        Templar, // Fatebringer, Out of Its Way
        Gatekeepers, // Hezen Vengeance, Strangers in Time
        Atheon, // Corrective Measure, Ensemble's Refrain
    }*/

    public class VaultOfGlass
    {
        [JsonProperty("Encounter")]
        public readonly string Encounter;

        [JsonProperty("ChallengeName")]
        public readonly string ChallengeName;

        public override string ToString() => $"{Encounter} ({ChallengeName})";
    }

    public class VaultOfGlassLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Encounter")]
        public int Encounter { get; set; } = 0;

        public override string ToString() => $"{CurrentRotations.VaultOfGlass.Rotations[Encounter]}";
    }

    public class VaultOfGlassPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public VaultOfGlass VaultOfGlass { get; set; }
    }
}
