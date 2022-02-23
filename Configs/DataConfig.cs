using Levante.Rotations;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Levante.Util;
using Levante.Helpers;
using System.Linq;

namespace Levante.Configs
{
    public partial class DataConfig : IConfig
    {
        public static string FilePath { get; } = @"Configs/dataConfig.json";

        [JsonProperty("DiscordIDLinks")]
        public static List<DiscordIDLink> DiscordIDLinks { get; set; } = new List<DiscordIDLink>();

        [JsonProperty("AnnounceDailyLinks")]
        public static List<ulong> AnnounceDailyLinks { get; set; } = new List<ulong>();

        [JsonProperty("AnnounceWeeklyLinks")]
        public static List<ulong> AnnounceWeeklyLinks { get; set; } = new List<ulong>();

        [JsonProperty("AnnounceEmblemLinks")]
        public static List<EmblemAnnounceLink> AnnounceEmblemLinks { get; set; } = new List<EmblemAnnounceLink>();

        public class DiscordIDLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("BungieMembershipID")]
            public string BungieMembershipID { get; set; } = "-1";

            [JsonProperty("BungieMembershipType")]
            public string BungieMembershipType { get; set; } = "-1";

            [JsonProperty("UniqueBungieName")]
            public string UniqueBungieName { get; set; } = "Guardian#0000";
        }

        public class EmblemAnnounceLink
        {
            [JsonProperty("ChannelID")]
            public ulong ChannelID { get; set; } = 0;

            [JsonProperty("RoleID")]
            public ulong RoleID { get; set; } = 0;
        }

        public static string GetValidDestinyMembership(string BungieTag, Guardian.Platform Platform, out string MembershipType)
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
                        if (((int)memItem.Response.profile.data.userInfo.crossSaveOverride == (int)memItem.Response.profile.data.userInfo.membershipType) ||
                                ((int)memItem.Response.profile.data.userInfo.crossSaveOverride == 0 && (int)memItem.Response.profile.data.userInfo.membershipType == ((int)Platform)))
                        {
                            MembershipType = memType;
                            return memId;
                        }
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

        public static DiscordIDLink GetLinkedUser(ulong DiscordID)
        {
            foreach (DiscordIDLink dil in DiscordIDLinks)
                if (dil.DiscordID == DiscordID)
                    return dil;
            return null;
        }

        public static void AddUserToConfig(ulong DiscordID, string MembershipID, string MembershipType, string BungieName)
        {
            DiscordIDLink dil = new DiscordIDLink()
            {
                DiscordID = DiscordID,
                BungieMembershipID = MembershipID,
                BungieMembershipType = MembershipType,
                UniqueBungieName = BungieName
            };
            string json = File.ReadAllText(FilePath);
            DiscordIDLinks.Clear();
            AnnounceDailyLinks.Clear();
            AnnounceWeeklyLinks.Clear();
            AnnounceEmblemLinks.Clear();
            DataConfig dc = JsonConvert.DeserializeObject<DataConfig>(json);

            DiscordIDLinks.Add(dil);
            string output = JsonConvert.SerializeObject(dc, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void AddUserToConfig(DiscordIDLink dil)
        {
            string json = File.ReadAllText(FilePath);
            DiscordIDLinks.Clear();
            AnnounceDailyLinks.Clear();
            AnnounceWeeklyLinks.Clear();
            AnnounceEmblemLinks.Clear();
            DataConfig dc = JsonConvert.DeserializeObject<DataConfig>(json);

            DiscordIDLinks.Add(dil);
            string output = JsonConvert.SerializeObject(dc, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void AddChannelToRotationConfig(ulong ChannelID, bool IsDaily)
        {
            string json = File.ReadAllText(FilePath);
            DiscordIDLinks.Clear();
            AnnounceDailyLinks.Clear();
            AnnounceWeeklyLinks.Clear();
            AnnounceEmblemLinks.Clear();
            DataConfig dc = JsonConvert.DeserializeObject<DataConfig>(json);
            
            if (IsDaily)
            {
                AnnounceDailyLinks.Add(ChannelID);
                string output = JsonConvert.SerializeObject(dc, Formatting.Indented);
                File.WriteAllText(FilePath, output);
            }
            else
            {
                AnnounceWeeklyLinks.Add(ChannelID);
                string output = JsonConvert.SerializeObject(dc, Formatting.Indented);
                File.WriteAllText(FilePath, output);
            }
        }

        public static void AddEmblemChannel(ulong ChannelID, IRole Role)
        {
            string json = File.ReadAllText(FilePath);
            DiscordIDLinks.Clear();
            AnnounceDailyLinks.Clear();
            AnnounceWeeklyLinks.Clear();
            AnnounceEmblemLinks.Clear();
            DataConfig dc = JsonConvert.DeserializeObject<DataConfig>(json);

            if (Role != null)
                AnnounceEmblemLinks.Add(new EmblemAnnounceLink() { ChannelID = ChannelID, RoleID = Role.Id });
            else
                AnnounceEmblemLinks.Add(new EmblemAnnounceLink() { ChannelID = ChannelID, RoleID = 0 });

            string output = JsonConvert.SerializeObject(dc, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void DeleteUserFromConfig(ulong DiscordID)
        {
            string json = File.ReadAllText(FilePath);
            DiscordIDLinks.Clear();
            AnnounceDailyLinks.Clear();
            AnnounceWeeklyLinks.Clear();
            AnnounceEmblemLinks.Clear();
            DataConfig dc = JsonConvert.DeserializeObject<DataConfig>(json);
            for (int i = 0; i < DiscordIDLinks.Count; i++)
                if (DiscordIDLinks[i].DiscordID == DiscordID)
                    DiscordIDLinks.RemoveAt(i);
            string output = JsonConvert.SerializeObject(dc, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void DeleteChannelFromRotationConfig(ulong ChannelID, bool IsDaily)
        {
            string json = File.ReadAllText(FilePath);
            DiscordIDLinks.Clear();
            AnnounceDailyLinks.Clear();
            AnnounceWeeklyLinks.Clear();
            AnnounceEmblemLinks.Clear();
            DataConfig dc = JsonConvert.DeserializeObject<DataConfig>(json);

            if (IsDaily)
            {
                for (int i = 0; i < AnnounceDailyLinks.Count; i++)
                    if (AnnounceDailyLinks[i] == ChannelID)
                        AnnounceDailyLinks.RemoveAt(i);
            }
            else
            {
                for (int i = 0; i < AnnounceWeeklyLinks.Count; i++)
                    if (AnnounceWeeklyLinks[i] == ChannelID)
                        AnnounceWeeklyLinks.RemoveAt(i);
            }

            string output = JsonConvert.SerializeObject(dc, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void DeleteEmblemChannel(ulong ChannelID)
        {
            string json = File.ReadAllText(FilePath);
            DiscordIDLinks.Clear();
            AnnounceDailyLinks.Clear();
            AnnounceWeeklyLinks.Clear();
            AnnounceEmblemLinks.Clear();
            DataConfig dc = JsonConvert.DeserializeObject<DataConfig>(json);

            for (int i = 0; i < AnnounceEmblemLinks.Count; i++)
                if (AnnounceEmblemLinks[i].ChannelID == ChannelID)
                    AnnounceEmblemLinks.RemoveAt(i);

            string output = JsonConvert.SerializeObject(dc, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static bool IsExistingLinkedUser(ulong DiscordID)
        {
            foreach (DiscordIDLink dil in DiscordIDLinks)
                if (dil.DiscordID == DiscordID)
                    return true;
            return false;
        }

        public static bool IsExistingLinkedChannel(ulong ChannelID, bool IsDaily)
        {
            if (IsDaily)
            {
                foreach (ulong chanId in AnnounceDailyLinks)
                    if (chanId == ChannelID)
                        return true;
                return false;
            }
            else
            {
                foreach (ulong chanId in AnnounceWeeklyLinks)
                    if (chanId == ChannelID)
                        return true;
                return false;
            }
        }

        public static bool IsExistingEmblemLinkedChannel(ulong ChannelID)
        {
            foreach (var Link in AnnounceEmblemLinks)
                if (Link.ChannelID == ChannelID)
                    return true;
            return false;
        }

        // This returns the level, then other parameters are XPProgress (int) and IsInShatteredThrone (bool). Reduces the amount of calls to the Bungie API.
        public static int GetAFKValues(ulong DiscordID, out int XPProgress, out PrivacySetting FireteamPrivacy, out string CharacterId, out string ErrorStatus)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    int Level = 0;
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                    var dil = GetLinkedUser(DiscordID);

                    var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + dil.BungieMembershipType + "/Profile/" + dil.BungieMembershipID + "/?components=100,202,204,1000").Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    dynamic item = JsonConvert.DeserializeObject(content);

                    ErrorStatus = $"{item.ErrorStatus}";
                    if (!ErrorStatus.Equals("Success"))
                    {
                        XPProgress = -1;
                        FireteamPrivacy = PrivacySetting.Open;
                        CharacterId = null;
                        return -1;
                    }

                    if (item.Response.profileTransitoryData.data == null)
                    {
                        ErrorStatus = $"PlayerNotOnline";
                        XPProgress = -1;
                        FireteamPrivacy = PrivacySetting.Open;
                        CharacterId = null;
                        return -1;
                    }

                    CharacterId = "";
                    DateTime mostRecentDate = new DateTime();
                    for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                    {
                        string charId = item.Response.profile.data.characterIds[i];
                        var activityTime = DateTime.Parse($"{item.Response.characterActivities.data[$"{charId}"].dateActivityStarted}");
                        //ulong activityHash = item.Response.characterActivities.data[$"{charId}"].currentActivityHash;
                        if (activityTime > mostRecentDate) // shattered throne
                        {
                            mostRecentDate = activityTime;
                            CharacterId = $"{charId}";
                        }
                    }

                    FireteamPrivacy = item.Response.profileTransitoryData.data.joinability.privacySetting;

                    //first 100 levels: 4095505052 (S15); 2069932355 (S16)
                    //anything after: 1531004716 (S15): 1787069365 (S16)

                    if (item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"2069932355"].level == 100)
                    {
                        int extraLevel = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"1787069365"].level;
                        Level = 100 + extraLevel;
                        XPProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"1787069365"].progressToNextLevel;
                    }
                    else
                    {
                        Level = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"2069932355"].level;
                        XPProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"2069932355"].progressToNextLevel;
                    }

                    return Level;
                }
            }
            catch
            {
                ErrorStatus = $"ResponseError";
                XPProgress = -1;
                FireteamPrivacy = PrivacySetting.Open;
                CharacterId = null;
                return -1;
            }
        }

        public static int GetAFKValues(ulong DiscordID, out int XPProgress, out bool IsPlaying, out string ErrorStatus)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    int Level = 0;
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                    var dil = GetLinkedUser(DiscordID);

                    var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + dil.BungieMembershipType + "/Profile/" + dil.BungieMembershipID + "/?components=100,202,204,1000").Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    dynamic item = JsonConvert.DeserializeObject(content);

                    ErrorStatus = $"{item.ErrorStatus}";
                    IsPlaying = true;

                    if (item.Response.profileTransitoryData.data == null)
                    {
                        ErrorStatus = $"PlayerNotOnline";
                        XPProgress = -1;
                        IsPlaying = false;
                        return -1;
                    }

                    //first 100 levels: 4095505052 (S15); 2069932355 (S16)
                    //anything after: 1531004716 (S15): 1787069365 (S16)

                    if (item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"2069932355"].level == 100)
                    {
                        int extraLevel = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"1787069365"].level;
                        Level = 100 + extraLevel;
                        XPProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"1787069365"].progressToNextLevel;
                    }
                    else
                    {
                        Level = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"2069932355"].level;
                        XPProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"2069932355"].progressToNextLevel;
                    }

                    return Level;
                }
            }
            catch
            {
                ErrorStatus = $"ResponseError";
                XPProgress = -1;
                IsPlaying = false;
                return -1;
            }
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

                //first 100 levels: 4095505052 (S15); 2069932355 (S16)
                //anything after: 1531004716 (S15): 1787069365 (S16)

                if (item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"2069932355"].level == 100)
                {
                    int extraLevel = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"1787069365"].level;
                    Level = 100 + extraLevel;
                    XPProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"1787069365"].progressToNextLevel;
                }
                else
                {
                    Level = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"2069932355"].level;
                    XPProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"2069932355"].progressToNextLevel;
                }

                return Level;
            }
        }

        public static async Task PostDailyResetUpdate(DiscordSocketClient Client)
        { 
            List<ulong> guildsWithKeptChannel = new List<ulong>();
            List<ulong> keptChannels = new List<ulong>();
            foreach (ulong ChannelID in AnnounceDailyLinks.ToList())
            {
                try
                {
                    var channel = Client.GetChannel(ChannelID) as SocketTextChannel;
                    var guildChannel = Client.GetChannel(ChannelID) as SocketGuildChannel;
                    if (channel == null)
                    {
                        LogHelper.ConsoleLog($"Could not find channel {ChannelID}. Removing this element.");
                        DeleteChannelFromRotationConfig(ChannelID, true);
                        continue;
                    }

                    if (!guildsWithKeptChannel.Contains(guildChannel.Guild.Id))
                    {
                        keptChannels.Add(ChannelID);
                        guildsWithKeptChannel.Add(guildChannel.Guild.Id);
                    }

                    foreach (var chan in guildChannel.Guild.TextChannels)
                    {
                        if (IsExistingLinkedChannel(chan.Id, true) && ChannelID != chan.Id && guildsWithKeptChannel.Contains(chan.Guild.Id) && !keptChannels.Contains(chan.Id))
                        {
                            LogHelper.ConsoleLog($"Duplicate channel detected. Removing: {chan.Id}");
                            DeleteChannelFromRotationConfig(chan.Id, true);
                        }
                    }

                    await channel.SendMessageAsync($"", embed: CurrentRotations.DailyResetEmbed().Build());
                }
                catch
                {
                    LogHelper.ConsoleLog($"Error on Channel ID: {ChannelID}");
                }
            }
        }

        public static async Task PostWeeklyResetUpdate(DiscordSocketClient Client)
        {
            List<ulong> guildsWithKeptChannel = new List<ulong>();
            List<ulong> keptChannels = new List<ulong>();
            foreach (ulong ChannelID in AnnounceWeeklyLinks.ToList())
            {
                try
                {
                    var channel = Client.GetChannel(ChannelID) as SocketTextChannel;
                    var guildChannel = Client.GetChannel(ChannelID) as SocketGuildChannel;
                    if (channel == null)
                    {
                        LogHelper.ConsoleLog($"Could not find channel {ChannelID}. Removing this element.");
                        DeleteChannelFromRotationConfig(ChannelID, false);
                        continue;
                    }

                    if (!guildsWithKeptChannel.Contains(guildChannel.Guild.Id))
                    {
                        keptChannels.Add(ChannelID);
                        guildsWithKeptChannel.Add(guildChannel.Guild.Id);
                    }

                    foreach (var chan in guildChannel.Guild.TextChannels)
                    {
                        if (IsExistingLinkedChannel(chan.Id, false) && ChannelID != chan.Id && guildsWithKeptChannel.Contains(chan.Guild.Id) && !keptChannels.Contains(chan.Id))
                        {
                            LogHelper.ConsoleLog($"Duplicate channel detected. Removing: {chan.Id}");
                            DeleteChannelFromRotationConfig(chan.Id, false);
                        }
                    }

                    await channel.SendMessageAsync($"", embed: CurrentRotations.WeeklyResetEmbed().Build());
                }
                catch
                {
                    LogHelper.ConsoleLog($"Error on Channel ID: {ChannelID}");
                }
            }
        }
    }
}
