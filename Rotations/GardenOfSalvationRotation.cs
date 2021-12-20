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
    public class GardenOfSalvationRotation
    {
        public static readonly int GardenOfSalvationEncounterCount = 4;

        [JsonProperty("CurrentChallengeEncounter")]
        public static GardenOfSalvationEncounter CurrentChallengeEncounter = GardenOfSalvationEncounter.Evade;

        [JsonProperty("GardenOfSalvationLinks")]
        public static List<GardenOfSalvationLink> GardenOfSalvationLinks { get; set; } = new List<GardenOfSalvationLink>();

        public class GardenOfSalvationLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("Encounter")]
            public GardenOfSalvationEncounter Encounter { get; set; } = GardenOfSalvationEncounter.Evade;
        }

        public enum GardenOfSalvationEncounter
        {
            Evade, // Staying Alive
            Summon, // A Link to the Chain
            ConsecratedMind, // To the Top
            SanctifiedMind, // Zero to One Hundred
        }
    }
}
