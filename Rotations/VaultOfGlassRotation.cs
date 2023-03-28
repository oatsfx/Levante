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
    public class VaultOfGlassRotation : Rotation<VaultOfGlass, VaultOfGlassLink, VaultOfGlassPrediction>
    {
        public VaultOfGlassRotation()
        {
            FilePath = @"Trackers/vaultOfGlass.json";
            RotationFilePath = @"Rotations/vaultOfGlass.json";

            GetRotationJSON();
            GetTrackerJSON();
        }

        /*public static string GetEncounterString(VaultOfGlassEncounter Encounter)
        {
            switch (Encounter)
            {
                case VaultOfGlassEncounter.Confluxes: return "Confluxes";
                case VaultOfGlassEncounter.Oracles: return "Oracles";
                case VaultOfGlassEncounter.Templar: return "Templar";
                case VaultOfGlassEncounter.Gatekeepers: return "Gatekeepers";
                case VaultOfGlassEncounter.Atheon: return "Atheon";
                default: return "Vault of Glass";
            }
        }

        public static string GetChallengeString(VaultOfGlassEncounter Encounter)
        {
            switch (Encounter)
            {
                case VaultOfGlassEncounter.Confluxes: return "Wait for It...";
                case VaultOfGlassEncounter.Oracles: return "The Only Oracle for You";
                case VaultOfGlassEncounter.Templar: return "Out of Its Way";
                case VaultOfGlassEncounter.Gatekeepers: return "Strangers in Time";
                case VaultOfGlassEncounter.Atheon: return "Ensemble's Refrain";
                default: return "Vault of Glass";
            }
        }

        public static string GetChallengeRewardString(VaultOfGlassEncounter Encounter)
        {
            switch (Encounter)
            {
                case VaultOfGlassEncounter.Confluxes: return "Vision of Confluence (Timelost)";
                case VaultOfGlassEncounter.Oracles: return "Praedyth's Revenge (Timelost)";
                case VaultOfGlassEncounter.Templar: return "Fatebringer (Timelost)";
                case VaultOfGlassEncounter.Gatekeepers: return "Hezen Vengeance (Timelost)";
                case VaultOfGlassEncounter.Atheon: return "Corrective Measure (Timelost)";
                default: return "Vault of Glass Weapon";
            }
        }*/

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

        public override bool IsTrackerInRotation(VaultOfGlassLink Tracker) => Tracker.Encounter == CurrentRotations.Actives.VoGChallenge;
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
    }

    public class VaultOfGlassLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Encounter")]
        public int Encounter { get; set; } = 0;
    }

    public class VaultOfGlassPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public VaultOfGlass VaultOfGlass { get; set; }
    }
}
