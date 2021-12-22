using DestinyUtility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text;

namespace DestinyUtility.Configs
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

        [JsonProperty("ActiveAFKUsers")]
        public static List<ActiveAFKUser> ActiveAFKUsers { get; set; } = new List<ActiveAFKUser>();

        [JsonProperty("MaximumThrallwayUsers")]
        public static int MaximumThrallwayUsers = 20;

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

            [JsonProperty("PrivacySetting")]
            public PrivacySetting PrivacySetting { get; set; } = 0;
        }

        public static ActiveAFKUser GetActiveAFKUser(ulong DiscordID)
        {
            string json = File.ReadAllText(FilePath);
            ActiveAFKUsers.Clear();
            ActiveConfig jsonObj = JsonConvert.DeserializeObject<ActiveConfig>(json);
            foreach (ActiveAFKUser aau in ActiveAFKUsers)
                if (aau.DiscordID == DiscordID)
                    return aau;
            return null;
        }

        public static bool IsInShatteredThrone(string BungieMembershipID, string BungieMembershipType)
        {
            // 2032534090 shattered throne hash code
            bool result = false;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + BungieMembershipType + "/Profile/" + BungieMembershipID + "/?components=100,204").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                {
                    string charId = item.Response.profile.data.characterIds[i];
                    ulong activityHash = item.Response.characterActivities.data[$"{charId}"].currentActivityHash;
                    if (activityHash == 2032534090) // shattered throne
                    {
                        result = true;
                    }
                }

                return result;
            }
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

        public static void AddActiveUserToConfig(ActiveAFKUser aau)
        {
            string json = File.ReadAllText(FilePath);
            ActiveAFKUsers.Clear();
            ActiveConfig jsonObj = JsonConvert.DeserializeObject<ActiveConfig>(json);

            ActiveAFKUsers.Add(aau);
            ActiveConfig ac = new ActiveConfig();
            string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void DeleteActiveUserFromConfig(ulong DiscordID)
        {
            string json = File.ReadAllText(FilePath);
            ActiveAFKUsers.Clear();
            ActiveConfig ac = JsonConvert.DeserializeObject<ActiveConfig>(json);
            for (int i = 0; i < ActiveAFKUsers.Count; i++)
                if (ActiveAFKUsers[i].DiscordID == DiscordID)
                    ActiveAFKUsers.RemoveAt(i);
            string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static bool IsExistingActiveUser(ulong DiscordID)
        {
            string json = File.ReadAllText(FilePath);
            ActiveAFKUsers.Clear();
            ActiveConfig jsonObj = JsonConvert.DeserializeObject<ActiveConfig>(json);
            foreach (ActiveAFKUser aau in ActiveAFKUsers)
                if (aau.DiscordID == DiscordID)
                    return true;
            return false;
        }

        #endregion
    }
}
