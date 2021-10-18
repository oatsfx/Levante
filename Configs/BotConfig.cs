using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DestinyUtility.Configs
{
    public sealed class BotConfig
    {
        [JsonProperty("DiscordToken")]
        public static string DiscordToken { get; set; } = "[YOUR TOKEN HERE]";

        [JsonProperty("BungieApiKey")]
        public static string BungieApiKey { get; set; } = "[YOUR API KEY HERE]";

        [JsonProperty("TimeBetweenRefresh")]
        public static int TimeBetweenRefresh { get; set; } = 5;

        [JsonProperty("Version")]
        public static double Version { get; set; } = 1.0;

        [JsonProperty("DefaultCommandPrefix")]
        public static string DefaultCommandPrefix { get; set; } = "t!";

        [JsonProperty("Note")]
        public static string Note { get; set; } = "Hello World";

        [JsonProperty("EmbedColor")]
        public static EmbedColorGroup EmbedColor { get; set; } = new EmbedColorGroup();

        public partial class EmbedColorGroup
        {
            [JsonProperty("R")]
            public static int R { get; set; } = 0;

            [JsonProperty("G")]
            public static int G { get; set; } = 0;

            [JsonProperty("B")]
            public static int B { get; set; } = 0;
        }
    }
}
