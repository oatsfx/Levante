using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Configs
{
    public sealed class BotConfig : IConfig
    {
        public static string FilePath { get; } = @"Configs/botConfig.json";

        [JsonProperty("AppName")]
        public static string AppName { get; set; } = "Levante";

        [JsonProperty("Website")]
        public static string Website { get; set; } = "oatsfx.com";

        // Include the @ in this field as the string this is used in, does not include the @.         
        [JsonProperty("Twitter")]
        public static string Twitter { get; set; } = "@OatsFX";

        // Exclude the https://discord.gg/ and only include the ending.
        [JsonProperty("SupportServer")]
        public static string SupportServer { get; set; } = "Levante";

        [JsonProperty("DiscordToken")]
        public static string DiscordToken { get; set; } = "[YOUR TOKEN HERE]";

        [JsonProperty("BungieApiKey")]
        public static string BungieApiKey { get; set; } = "[YOUR API KEY HERE]";

        [JsonProperty("Version")]
        public static double Version { get; set; } = 1.0;

        [JsonProperty("DefaultCommandPrefix")]
        public static string DefaultCommandPrefix { get; set; } = "l!";

        [JsonProperty("Note")]
        public static string Note { get; set; } = "Hello World";

        [JsonProperty("DurationToWaitForNextMessage")]
        public static int DurationToWaitForNextMessage { get; set; } = 20;

        [JsonProperty("BotStaff")]
        public static List<ulong> BotStaffDiscordIDs { get; set; } = new List<ulong>();

        [JsonProperty("BotSupporters")]
        public static List<ulong> BotSupportersDiscordIDs { get; set; } = new List<ulong>();

        [JsonProperty("EmbedColor")]
        public static EmbedColorGroup EmbedColor { get; set; } = new EmbedColorGroup();

        [JsonProperty("UniversalCodes")]
        public static List<UniversalCode> UniversalCodes { get; set; } = new List<UniversalCode>();

        public class EmbedColorGroup
        {
            [JsonProperty("R")]
            public static int R { get; set; } = 0;

            [JsonProperty("G")]
            public static int G { get; set; } = 0;

            [JsonProperty("B")]
            public static int B { get; set; } = 0;
        }

        public class UniversalCode
        {
            [JsonProperty("Name")]
            public string Name { get; set; } = "[EMBLEM NAME]";

            [JsonProperty("ImageUrl")]
            public string ImageUrl { get; set; } = "[IMAGE URL]";

            [JsonProperty("Code")]
            public string Code { get; set; } = "[EMBLEM CODE]";
        }
    }
}
