using Levante.Configs;

namespace Levante.Util
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

        public static int[] GetColorFromString(string Query)
        {
            if (Query.Contains("Common"))
                return Common;
            else if (Query.Contains("Uncommon"))
                return Uncommon;
            else if (Query.Contains("Rare"))
                return Rare;
            else if (Query.Contains("Legendary"))
                return Legendary;
            else if (Query.Contains("Exotic"))
                return Exotic;
            else
                return new int[] { BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B };
        }
    }
}
