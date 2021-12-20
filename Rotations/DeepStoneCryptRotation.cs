using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DestinyUtility.Rotations
{
    public class DeepStoneCryptRotation
    {
        public static readonly int DeepStoneCryptEncounterCount = 4;

        [JsonProperty("CurrentChallengeEncounter")]
        public static DeepStoneCryptEncounter CurrentChallengeEncounter = DeepStoneCryptEncounter.Security;

        [JsonProperty("DeepStoneCryptLinks")]
        public static List<DeepStoneCryptLink> DeepStoneCryptLinks { get; set; } = new List<DeepStoneCryptLink>();

        public class DeepStoneCryptLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("Encounter")]
            public DeepStoneCryptEncounter Encounter { get; set; } = DeepStoneCryptEncounter.Security;
        }

        public enum DeepStoneCryptEncounter
        {
            Security, // Red Rover
            Atraks1, // Copies of Copies
            Descent, // Of All Trades
            Taniks, // The Core Four
        }
    }
}
