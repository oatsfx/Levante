using Discord;
using Fergun.Interactive;
using Levante.Helpers;
using Levante.Services;
using Levante.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace Levante.Configs
{
    public enum PrivacySetting
    {
        Open,
        ClanAndFriendsOnly,
        FriendsOnly,
        InvitationOnly,
        Closed,
    }

    public class LoggingConfig : IConfig
    {
        public static string FilePath { get; } = @"Configs/loggingConfig.json";

        // Supporter AFK Users. Used in order to maintain without a limit.
        [JsonProperty("PriorityActiveAFKUsers")]
        public List<LoggingUser> PriorityUsers { get; set; } = new();

        [JsonProperty("ActiveAFKUsers")]
        public List<LoggingUser> Users { get; set; } = new();
    }

    public enum LoggingType
    {
        Basic,
        Priority,
    }

    public class LoggingUser
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("UniqueBungieName")]
        public string UniqueBungieName { get; set; } = "Guardian#0000";

        [JsonProperty("DiscordChannelID")]
        public ulong DiscordChannelID { get; set; } = 0;

        [JsonProperty("NoGainRefreshes")]
        public int NoGainRefreshes { get; set; } = 0;

        [JsonProperty("ActivityHash")]
        public long ActivityHash { get; set; } = 0;

        [JsonProperty("Start")]
        public LoggingValuesGroup Start { get; set; }

        [JsonProperty("Last")]
        public LoggingValuesGroup Last { get; set; }

        [JsonProperty("OverrideHash")]
        public long OverrideHash { get; set; } = 0;

        public class LoggingValuesGroup
        {
            [JsonProperty("Level")]
            public int Level { get; set; } = 0;

            [JsonProperty("ExtraLevel")]
            public int ExtraLevel { get; set; } = 0;

            [JsonProperty("LevelProgress")]
            public int LevelProgress { get; set; } = 0;

            [JsonProperty("PowerBonus")]
            public int PowerBonus { get; set; } = 0;

            [JsonProperty("NextLevelAt")]
            public int NextLevelAt { get; set; } = 0;

            [JsonProperty("Timestamp")]
            public DateTime Timestamp { get; set; } = DateTime.Now;

            // These are dependent on the type of LoggingOverride the user selects.
            [JsonProperty("OverrideValue")]
            public int OverrideValue { get; set; } = -1;

            [JsonProperty("OverrideInventoryList")]
            public List<string> OverrideInventoryList { get; set; } = new();
        }
    }
}
