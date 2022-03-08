using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Configs
{
    public sealed class BotConfig : IConfig
    {
        public static string FilePath { get; } = @"Configs/botConfig.json";

        [JsonProperty("DiscordToken")]
        public static string DiscordToken { get; set; } = "[YOUR TOKEN HERE]";

        [JsonProperty("BungieApiKey")]
        public static string BungieApiKey { get; set; } = "[YOUR API KEY HERE]";

        [JsonProperty("TimeBetweenRefresh")]
        public static int TimeBetweenRefresh { get; set; } = 4;

        [JsonProperty("Version")]
        public static double Version { get; set; } = 1.0;

        [JsonProperty("DefaultCommandPrefix")]
        public static string DefaultCommandPrefix { get; set; } = "f!";

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

        public partial class EmbedColorGroup
        {
            [JsonProperty("R")]
            public static int R { get; set; } = 0;

            [JsonProperty("G")]
            public static int G { get; set; } = 0;

            [JsonProperty("B")]
            public static int B { get; set; } = 0;
        }

        public bool CheckAndLoadConfig()
        {
            BotConfig config;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                config = JsonConvert.DeserializeObject<BotConfig>(json);
                return false;
            }
            else
            {
                config = new BotConfig();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. A new one has been created and the program has stopped. Go and change API tokens and other items.");
                return true;
            }
        }
    }
}
