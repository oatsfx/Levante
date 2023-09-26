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

    public class XPLoggingConfig : IConfig
    {
        // Supporter AFK Users. Used in order to maintain without a limit.
        [JsonProperty("PriorityActiveAFKUsers")]
        public List<XPLoggingUser> PriorityXPLoggingUsers { get; set; } = new();

        [JsonProperty("ActiveAFKUsers")]
        public List<XPLoggingUser> XPLoggingUsers { get; set; } = new();

        [JsonProperty("MaximumLoggingUsers")]
        public int MaximumLoggingUsers { get; set; } = 80;

        [JsonProperty("RefreshesBeforeKick")]
        public int RefreshesBeforeKick { get; } = 2;

        [JsonProperty("TimeBetweenRefresh")]
        public int TimeBetweenRefresh { get; } = 2;

        [JsonProperty("RefreshScaling")]
        public double RefreshScaling { get; set; } = 0.025;
    }

    public enum LoggingType
    {
        Basic,
        Priority,
    }

    public class XPLoggingUser
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; }

        [JsonProperty("UniqueBungieName")]
        public string UniqueBungieName { get; set; } = "Guardian#0000";

        [JsonProperty("DiscordChannelID")]
        public ulong DiscordChannelID { get; set; }

        [JsonProperty("StartLevel")]
        public int StartLevel { get; set; }

        [JsonProperty("StartLevelProgress")]
        public int StartLevelProgress { get; set; }

        [JsonProperty("StartPowerBonus")]
        public int StartPowerBonus { get; set; }

        [JsonProperty("TimeStarted")]
        public DateTime TimeStarted { get; set; } = DateTime.Now;

        [JsonProperty("LastLevel")]
        public int LastLevel { get; set; }

        [JsonProperty("LastLevelProgress")]
        public int LastLevelProgress { get; set; }

        [JsonProperty("LastPowerBonus")]
        public int LastPowerBonus { get; set; }

        [JsonProperty("NoXPGainRefreshes")]
        public int NoXPGainRefreshes { get; set; }

        [JsonProperty("ActivityHash")]
        public long ActivityHash { get; set; }

        [JsonProperty("FireteamMembers")]
        public List<string> FireteamMembers { get; set; } = new();
    }
}
