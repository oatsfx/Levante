using BungieSharper.Entities.Destiny;
using Serilog;
using System.Linq;
using Levante.Services;
using Levante.Helpers;
using Levante.Configs;

namespace Levante.Util
{
    // This class contains all of the Discord Emote strings to use for UI elements in Destiny.
    public static class DestinyEmote
    {
        // Needs to be char for char.
        public static string MatchEmote(string Query)
        {
            Query = Query.Replace(" ", "").Replace("-", "").Replace("'", "");
            if (typeof(DestinyEmote).GetField(Query) != null)
                return (string)typeof(DestinyEmote).GetField(Query).GetValue(null);
            else
                return $"{EmoteConfig.GetEmote("Classified")}";
        }

        // Parses Bungie's "Emotes" ("[Glaive]") into our app's Discord emotes.
        public static string ParseBungieText(string Text)
        {
            if (!Text.Contains('[')) return Text;
            int replacements = Text.Count(x => x == '[');
            for (int i = 0; i < replacements; i++)
            {
                int leftIndex = Text.IndexOf('[') + 1;
                int rightIndex = Text.IndexOf(']');

                string inBetween = Text[leftIndex..rightIndex];
                Text = Text.Replace($"[{inBetween}]", MatchEmote(inBetween));
            }
            return Text;
        }

        public static string ParseBungieVars(string Text)
        {
            if (!Text.Contains('{')) return Text;
            int replacements = Text.Count(x => x == '{');
            for (int i = 0; i < replacements; i++)
            {
                int leftIndex = Text.IndexOf('{') + 1;
                int rightIndex = Text.IndexOf('}');

                string inBetween = Text[leftIndex..rightIndex];
                int colonIndex = inBetween.IndexOf(':') + 1;
                string variableHash = Text[(leftIndex + colonIndex)..rightIndex];
                Text = Text.Replace($"{{{inBetween}}}", $"{ManifestHelper.StringVariables[variableHash]}");
            }
            return Text;
        }

        public static string ClarityClean(string input)
        {
            string output = input.Replace("🡅", $"{EmoteConfig.GetEmote("Enhanced")}");
            // Add more cleans.
            return output;
        }
    }
}
