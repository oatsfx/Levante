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

        public static bool IsRefreshing { get; set; } = false;

        // Supporter AFK Users. Used in order to maintain without a limit.
        [JsonProperty("PriorityActiveAFKUsers")]
        public static List<ActiveAFKUser> PriorityActiveAFKUsers { get; set; } = new List<ActiveAFKUser>();

        [JsonProperty("ActiveAFKUsers")]
        public static List<ActiveAFKUser> ActiveAFKUsers { get; set; } = new List<ActiveAFKUser>();

        [JsonProperty("MaximumLoggingUsers")]
        public static int MaximumLoggingUsers = 20;

        [JsonProperty("RefreshesBeforeKick")]
        public static int RefreshesBeforeKick = 2;

        [JsonProperty("TimeBetweenRefresh")]
        public static int TimeBetweenRefresh = 2;

        public partial class ActiveAFKUser
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("BungieMembershipID")]
            public string BungieMembershipID { get; set; } = "Hello World";

            [JsonProperty("UniqueBungieName")]
            public string UniqueBungieName { get; set; } = "Guardian#0000";

            [JsonProperty("DiscordChannelID")]
            public ulong DiscordChannelID { get; set; } = 0;

            [JsonProperty("StartLevel")]
            public int StartLevel { get; set; } = 0;

            [JsonProperty("StartLevelProgress")]
            public int StartLevelProgress { get; set; } = 0;

            [JsonProperty("TimeStarted")]
            public DateTime TimeStarted { get; set; } = DateTime.Now;

            [JsonProperty("LastLoggedLevel")]
            public int LastLoggedLevel { get; set; } = 0;

            [JsonProperty("LastLoggedLevelProgress")]
            public int LastLevelProgress { get; set; } = 0;

            [JsonProperty("NoXPGainRefreshes")]
            public int NoXPGainRefreshes { get; set; } = 0;
        }

        public static ActiveAFKUser GetActiveAFKUser(ulong DiscordID)
        {
            foreach (ActiveAFKUser aau in ActiveAFKUsers)
                if (aau.DiscordID == DiscordID)
                    return aau;
            foreach (ActiveAFKUser aau in PriorityActiveAFKUsers)
                if (aau.DiscordID == DiscordID)
                    return aau;
            return null;
        }

        public static bool IsPlayerOnline(string BungieMembershipID, string BungieMembershipType)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + BungieMembershipType + "/Profile/" + BungieMembershipID + "/?components=1000").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                if (item.Response.profileTransitoryData.data == null)
                    return false;
                else
                    return true;
            }
        }

        public static PrivacySetting GetFireteamPrivacy(string BungieMembershipID, string BungieMembershipType)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + BungieMembershipType + "/Profile/" + BungieMembershipID + "/?components=1000").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                PrivacySetting result = item.Response.profileTransitoryData.data.joinability.privacySetting;

                return result;
            }
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
}
