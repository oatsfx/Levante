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

    public partial class ActiveConfig : IConfig
    {
        public static string FilePath { get; } = @"Configs/activeConfig.json";

        // Supporter AFK Users. Used in order to maintain without a limit.
        [JsonProperty("PriorityActiveAFKUsers")]
        public static List<ActiveAFKUser> PriorityActiveAFKUsers { get; set; } = new();

        [JsonProperty("ActiveAFKUsers")]
        public static List<ActiveAFKUser> ActiveAFKUsers { get; set; } = new();

        [JsonProperty("MaximumLoggingUsers")]
        public static int MaximumLoggingUsers = 20;

        [JsonProperty("RefreshesBeforeKick")]
        public static int RefreshesBeforeKick = 2;

        [JsonProperty("TimeBetweenRefresh")]
        public static int TimeBetweenRefresh = 2;

        [JsonProperty("RefreshScaling")]
        public static double RefreshScaling = 0.025;

        public class ActiveAFKUser
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("UniqueBungieName")]
            public string UniqueBungieName { get; set; } = "Guardian#0000";

            [JsonProperty("DiscordChannelID")]
            public ulong DiscordChannelID { get; set; } = 0;

            [JsonProperty("NoXPGainRefreshes")]
            public int NoXPGainRefreshes { get; set; } = 0;

            [JsonProperty("ActivityHash")]
            public long ActivityHash { get; set; } = 0;

            [JsonProperty("Start")]
            public LoggingValuesGroup Start { get; set; }

            [JsonProperty("Last")]
            public LoggingValuesGroup Last { get; set; }
        }

        public static ActiveAFKUser GetActiveAFKUser(ulong DiscordID)
        {
            var aau = ActiveAFKUsers.First(x => x.DiscordID == DiscordID);
            aau ??= PriorityActiveAFKUsers.First(x => x.DiscordID == DiscordID);

            return aau;
        }

        #region JSONHandling

        public static void UpdateActiveAFKUsersList()
        {
            string json = File.ReadAllText(FilePath);
            ActiveAFKUsers.Clear();
            ActiveConfig jsonObj = JsonConvert.DeserializeObject<ActiveConfig>(json);
        }

        public static void UpdateActiveAFKUsersConfig()
        {
            ActiveConfig ac = new ActiveConfig();
            string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void ClearActiveAFKUsersList()
        {
            ActiveAFKUsers.Clear();
            string output = JsonConvert.SerializeObject(new ActiveConfig(), Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void AddActiveUserToConfig(ActiveAFKUser aau, LoggingType Type)
        {
            //string json = File.ReadAllText(FilePath);
            //ActiveAFKUsers.Clear();
            //ActiveConfig jsonObj = JsonConvert.DeserializeObject<ActiveConfig>(json);

            switch (Type)
            {
                case LoggingType.Basic: ActiveAFKUsers.Add(aau); break;
                case LoggingType.Priority: PriorityActiveAFKUsers.Add(aau); break;
                default: ActiveAFKUsers.Add(aau); break;
            }
            
            //ActiveConfig ac = new ActiveConfig();
            //string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            //File.WriteAllText(FilePath, output);
        }

        public static void DeleteActiveUserFromConfig(ulong DiscordID)
        {
            if (ActiveAFKUsers.Exists(x => x.DiscordID == DiscordID))
                ActiveAFKUsers.Remove(ActiveAFKUsers.First(x => x.DiscordID == DiscordID));
            else
                PriorityActiveAFKUsers.Remove(PriorityActiveAFKUsers.First(x => x.DiscordID == DiscordID));
            //ActiveConfig ac = new ActiveConfig();
            //string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            //File.WriteAllText(FilePath, output);
        }

        public static bool IsExistingActiveUser(ulong DiscordID) => ActiveAFKUsers.Exists(x => x.DiscordID == DiscordID) || PriorityActiveAFKUsers.Exists(x => x.DiscordID == DiscordID);

        #endregion
    }

    public enum LoggingType
    {
        Basic,
        Priority,
    }

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
    }
}
