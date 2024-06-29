using Levante.Configs;
using Levante.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Util
{
    public class XpLoggingValueResponse
    {
        public readonly int CurrentLevel = -1;
        public readonly int CurrentExtraLevel = -1;
        public readonly int XpProgress = -1;
        public readonly int NextLevelAt = -1;
        public readonly int PowerBonus = -1;
        public readonly int LevelCap = -1;
        public readonly long ActivityHash = 0;
        public readonly PrivacySetting FireteamPrivacy = PrivacySetting.Open;
        public readonly string CharacterId = "";

        public readonly string ErrorStatus = "ResponseError";

        public XpLoggingValueResponse(ulong discordId)
        {
            try
            {
                LevelCap = ManifestHelper.CurrentLevelCap;

                var dil = DataConfig.GetLinkedUser(discordId);
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {dil.AccessToken}");

                var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + dil.BungieMembershipType + "/Profile/" + dil.BungieMembershipID + "/?components=100,104,202,204,1000").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                ErrorStatus = $"{item.ErrorStatus}";
                if (!ErrorStatus.Equals("Success"))
                {
                    return;
                }

                if (item.Response.profileTransitoryData.data == null)
                {
                    ErrorStatus = $"NoTransitoryData";
                    return;
                }

                if (item.Response.characterProgressions.data == null)
                {
                    ErrorStatus = $"PlayerProgressionPrivate";
                    return;
                }

                if (item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{BotConfig.Hashes.First100Ranks}"].level >= LevelCap)
                {
                    var progression = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{BotConfig.Hashes.Above100Ranks}"];
                    int extraLevel = progression.level;
                    CurrentLevel = LevelCap;
                    CurrentExtraLevel = extraLevel;
                    XpProgress = progression.progressToNextLevel;
                    NextLevelAt = progression.nextLevelAt;
                }
                else
                {
                    var progression = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{BotConfig.Hashes.First100Ranks}"];
                    CurrentLevel = progression.level;
                    CurrentExtraLevel = 0;
                    XpProgress = progression.progressToNextLevel;
                    NextLevelAt = progression.nextLevelAt;
                }

                // Figure out which character is most recent.
                ActivityHash = 0;
                DateTime mostRecentDate = new();
                for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                {
                    string charId = item.Response.profile.data.characterIds[i];
                    var activityTime = DateTime.Parse($"{item.Response.characterActivities.data[$"{charId}"].dateActivityStarted}");
                    if (activityTime > mostRecentDate)
                    {
                        mostRecentDate = activityTime;
                        CharacterId = $"{charId}";
                        ActivityHash = item.Response.characterActivities.data[$"{charId}"].currentActivityHash;
                    }
                }

                FireteamPrivacy = item.Response.profileTransitoryData.data.joinability.privacySetting;
                PowerBonus = item.Response.profileProgression.data.seasonalArtifact.powerBonus;
            }
            catch
            {
                XpProgress = -1;
                PowerBonus = -1;
                ActivityHash = 0;
                return;
            }
        }
    }
}
