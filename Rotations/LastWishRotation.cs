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
    public class LastWishRotation
    {
        public static readonly int LastWishEncounterCount = 5;

        

        [JsonProperty("LastWishLinks")]
        public static List<LastWishLink> LastWishLinks { get; set; } = new List<LastWishLink>();

        public class LastWishLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("Encounter")]
            public LastWishEncounter Encounter { get; set; } = LastWishEncounter.Kalli;
        }
    }

    public enum LastWishEncounter
    {
        Kalli, // Summoning Ritual
        ShuroChi, // Which Witch
        Morgeth, // Forever Fight
        Vault, // Keep Out
        Riven, // Strength of Memory
    }
}
