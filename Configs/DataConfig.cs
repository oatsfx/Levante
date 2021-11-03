using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using static DestinyUtility.Configs.DataConfig;

namespace DestinyUtility.Configs
{
    public partial class DataConfig
    {
        [JsonProperty("DiscordIDLinks")]
        public static List<DiscordIDLink> DiscordIDLinks { get; set; } = new List<DiscordIDLink>();

        public partial class DiscordIDLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("BungieMembershipID")]
            public string BungieMembershipID { get; set; } = "Hello World";

            [JsonProperty("BungieMembershipType")]
            public string BungieMembershipType { get; set; } = "-1";
        }

        public static string GetUniqueBungieName(ulong DiscordID)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + GetLinkedUser(DiscordID).BungieMembershipType + "/Profile/" + GetLinkedUser(DiscordID).BungieMembershipID + "/?components=100").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);
                return $"{item.Response.profile.data.userInfo.bungieGlobalDisplayName}#{item.Response.profile.data.userInfo.bungieGlobalDisplayNameCode:0000}";
            }
        }

        public static string GetValidDestinyMembership(string BungieTag, out string MembershipType)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/SearchDestinyPlayer/-1/" + Uri.EscapeDataString(BungieTag)).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                if (IsBungieAPIDown(content))
                {
                    MembershipType = null;
                    return null;
                }

                string memId = "";
                string memType = "";
                for (int i = 0; i < item.Response.Count; i++)
                {
                    memId = item.Response[i].membershipId;
                    memType = item.Response[i].membershipType;

                    var memResponse = client.GetAsync($"https://www.bungie.net/platform/Destiny2/" + memType + "/Profile/" + memId + "/?components=100").Result;
                    var memContent = memResponse.Content.ReadAsStringAsync().Result;
                    dynamic memItem = JsonConvert.DeserializeObject(memContent);

                    if (memItem.ErrorCode == 1)
                    {
                        MembershipType = memType;
                        return memId;
                    }
                }
            }
            MembershipType = null;
            return null;
        }

        public static bool IsPublicAccount(string BungieTag)
        {
            bool isPublic = false;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/SearchDestinyPlayer/3/" + Uri.EscapeDataString(BungieTag)).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                isPublic = item.Response[0].isPublic;
            }
            return isPublic;
        }
        public static bool IsBungieAPIDown(string JSONContent)
        {
            dynamic item = JsonConvert.DeserializeObject(JSONContent);
            string status = item.ErrorStatus;
            return !status.Equals("Success");
        }

        public static void UpdateUsersList()
        {
            string json = File.ReadAllText(DestinyUtilityCord.DataConfigPath);
            DiscordIDLinks.Clear();
            DataConfig jsonObj = JsonConvert.DeserializeObject<DataConfig>(json);
        }

        public static void ClearUsersList()
        {
            DiscordIDLinks.Clear();
            string output = JsonConvert.SerializeObject(new DataConfig(), Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.DataConfigPath, output);
        }

        public static DiscordIDLink GetLinkedUser(ulong DiscordID)
        {
            string json = File.ReadAllText(DestinyUtilityCord.DataConfigPath);
            DiscordIDLinks.Clear();
            DataConfig jsonObj = JsonConvert.DeserializeObject<DataConfig>(json);
            foreach (DiscordIDLink dil in DiscordIDLinks)
                if (dil.DiscordID == DiscordID)
                    return dil;
            return null;
        }

        public static void AddUserToConfig(ulong DiscordID, string MembershipID, string MembershipType)
        {
            DiscordIDLink dil = new DiscordIDLink()
            {
                DiscordID = DiscordID,
                BungieMembershipID = MembershipID,
                BungieMembershipType = MembershipType
            };
            string json = File.ReadAllText(DestinyUtilityCord.DataConfigPath);
            DiscordIDLinks.Clear();
            DataConfig jsonObj = JsonConvert.DeserializeObject<DataConfig>(json);

            DiscordIDLinks.Add(dil);
            DataConfig ac = new DataConfig();
            string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.DataConfigPath, output);
        }

        public static void AddUserToConfig(DiscordIDLink dil)
        {
            string json = File.ReadAllText(DestinyUtilityCord.DataConfigPath);
            DiscordIDLinks.Clear();
            DataConfig jsonObj = JsonConvert.DeserializeObject<DataConfig>(json);

            DiscordIDLinks.Add(dil);
            DataConfig ac = new DataConfig();
            string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.DataConfigPath, output);
        }

        public static void DeleteUserFromConfig(ulong DiscordID)
        {
            string json = File.ReadAllText(DestinyUtilityCord.DataConfigPath);
            DiscordIDLinks.Clear();
            DataConfig ac = JsonConvert.DeserializeObject<DataConfig>(json);
            for (int i = 0; i < DiscordIDLinks.Count; i++)
                if (DiscordIDLinks[i].DiscordID == DiscordID)
                    DiscordIDLinks.RemoveAt(i);
            string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.DataConfigPath, output);
        }

        public static bool IsExistingLinkedUser(ulong DiscordID)
        {
            string json = File.ReadAllText(DestinyUtilityCord.DataConfigPath);
            DiscordIDLinks.Clear();
            DataConfig jsonObj = JsonConvert.DeserializeObject<DataConfig>(json);
            foreach (DiscordIDLink dil in DiscordIDLinks)
                if (dil.DiscordID == DiscordID)
                    return true;
            return false;
        }

        public static int GetUserSeasonPassLevel(ulong DiscordID, out int XPProgress)
        {
            using (var client = new HttpClient())
            {
                int Level = 0;
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var dil = GetLinkedUser(DiscordID);

                var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + dil.BungieMembershipType + "/Profile/" + dil.BungieMembershipID + "/?components=100,202").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                //first 100 levels: 4095505052
                //anything after: 1531004716

                if (item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"4095505052"].level == 100)
                {
                    int extraLevel = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"1531004716"].level;
                    Level = 100 + extraLevel;
                    XPProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"1531004716"].progressToNextLevel;
                }
                else
                {
                    Level = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"4095505052"].level;
                    XPProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"4095505052"].progressToNextLevel;
                }

                return Level;
            }
        }
    }
}
