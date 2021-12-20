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
    public class AltarsOfSorrowRotation
    {
        public static readonly int AltarWeaponCount = 3;

        [JsonProperty("CurrentAlterWeapon")]
        public static AltarsOfSorrow CurrentAlterWeapon = AltarsOfSorrow.Shotgun;

        [JsonProperty("AltarsOfSorrowLinks")]
        public static List<AltarsOfSorrowLink> AltarsOfSorrowLinks { get; set; } = new List<AltarsOfSorrowLink>();

        public class AltarsOfSorrowLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("WeaponDrop")]
            public AltarsOfSorrow WeaponDrop { get; set; } = AltarsOfSorrow.Shotgun;
        }

        public enum AltarsOfSorrow
        {
            Shotgun, // Blasphemer
            Sniper, // Apostate
            Rocket, // Heretic
        }
    }
}
