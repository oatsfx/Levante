using Levante.Configs;
using Levante.Services;
using System.Collections.Generic;
using System.Linq;

namespace Levante.Structs
{
    // This class contains some of the Destiny 2 RGB color arrays.
    public static class DestinyColors
    {
        // Rarities
        public static readonly int[] Common = { 195, 188, 180 };
        public static readonly int[] Uncommon = { 54, 112, 66 };
        public static readonly int[] Rare = { 80, 118, 164 };
        public static readonly int[] Legendary = { 82, 47, 100 };
        public static readonly int[] Exotic = { 206, 174, 51 };

        public static int[] GetColorFromString(string query)
        {
            var colorMap = new Dictionary<string, int[]>
            {
                { "Common", Common },
                { "Uncommon", Uncommon },
                { "Rare", Rare },
                { "Legendary", Legendary },
                { "Exotic", Exotic }
            };

            return colorMap.FirstOrDefault(kvp => query.Contains(kvp.Key)).Value
                ?? new[] { AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B };
        }

    }
}
