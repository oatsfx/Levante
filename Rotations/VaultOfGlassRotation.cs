using DestinyUtility.Configs;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DestinyUtility.Rotations
{
    public class VaultOfGlassRotation
    {
        public static readonly int VaultOfGlassEncounterCount = 5;

        [JsonProperty("CurrentChallengeEncounter")]
        public static VaultOfGlassEncounter CurrentChallengeEncounter = VaultOfGlassEncounter.Confluxes;

        [JsonProperty("VaultOfGlassLinks")]
        public static List<VaultOfGlassLink> VaultOfGlassLinks { get; set; } = new List<VaultOfGlassLink>();

        public class VaultOfGlassLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("Encounter")]
            public VaultOfGlassEncounter Encounter { get; set; } = VaultOfGlassEncounter.Confluxes;
        }

        public enum VaultOfGlassEncounter
        {
            Confluxes, // Vision of Confluence, Wait for It...
            Oracles, // Praedyth's Revenge, The Only Oracle for You
            Templar, // Fatebringer, Out of Its Way
            Gatekeepers, // Hezen Vengeance, Strangers in Time
            Atheon, // Corrective Measure, Ensemble's Refrain
        }
    }
}
