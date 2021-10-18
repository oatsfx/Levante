using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DestinyUtility.Configs
{
    public partial class LostSectorTrackingConfig
    {
        [JsonProperty("LostSectorLinks")]
        public static List<LostSectorLink> LostSectorLinks { get; set; } = new List<LostSectorLink>();

        public partial class LostSectorLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("LostSector")]
            public LostSector LostSector { get; set; } = LostSector.AphelionsRest;

            [JsonProperty("Difficulty")]
            public LostSectorDifficulty Difficulty { get; set; } = LostSectorDifficulty.Legend;

            [JsonProperty("ArmorDrop")]
            public ExoticArmorType ArmorDrop { get; set; } = ExoticArmorType.Helmet;
        }

        public enum LostSector
        {
            // Dreaming City
            BayOfDrownedWishes,
            ChamberOfStarlight,
            AphelionsRest,
            // Tangled Shore
            TheEmptyTank,
            // Luna
            K1Logistics,
            K1Communion,
            K1CrewsQuarters,
            K1Revelation,
            // Europa
            ConcealedVoid,
            BunkerE15,
            Perdition,
        }

        public enum LostSectorDifficulty
        {
            Legend,
            Master
        }

        public enum ExoticArmorType
        {
            Helmet,
            Arms,
            Chest,
            Boots
        }
    }
}
