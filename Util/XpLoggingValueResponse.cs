using APIHelper;
using Levante.Configs;
using Levante.Helpers;
using Newtonsoft.Json;
using Serilog;
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
        public readonly long OverrideHash = 0;
        public readonly int OverrideValue = -1;
        public readonly List<string> OverrideInventoryList = new();

        public readonly string ErrorStatus = "ResponseError";

        public XpLoggingValueResponse(ulong discordId, LoggingOverride loggingOverride = null)
        {
            try
            {
                LevelCap = ManifestHelper.CurrentLevelCap;

                var dil = DataConfig.GetLinkedUser(discordId);
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-API-Key", AppConfig.Credentials.BungieApiKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {dil.AccessToken}");

                var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + dil.BungieMembershipType + "/Profile/" + dil.BungieMembershipID + "/?components=100,102,104,201,202,204,1000").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                ErrorStatus = $"{item.ErrorStatus}";
                if (!ErrorStatus.Equals("Success"))
                {
                    return;
                }

                if (item.Response.profileTransitoryData.data == null)
                {
                    ErrorStatus = $"PlayerOffline";
                    return;
                }

                if (item.Response.characterProgressions.data == null)
                {
                    ErrorStatus = $"PlayerProgressionPrivate";
                    return;
                }

                if (item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.BaseRanks}"].level >= LevelCap)
                {
                    var progression = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.ExtraRanks}"];
                    int extraLevel = progression.level;
                    CurrentLevel = LevelCap;
                    CurrentExtraLevel = extraLevel;
                    XpProgress = progression.progressToNextLevel;
                    NextLevelAt = progression.nextLevelAt;
                }
                else
                {
                    var progression = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.BaseRanks}"];
                    var extraProgression = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.ExtraRanks}"];
                    CurrentLevel = progression.level;
                    CurrentExtraLevel = extraProgression.level;
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

                // Override Stat Handling

                if (loggingOverride == null)
                    return;

                switch (loggingOverride.OverrideType)
                {
                    case LoggingOverrideType.ProfileProgression:
                        {
                            // Not implemented yet.
                            // Probably won't be because there is nothing here other than checklists and artifact data.

                            // Likely tracking an integer.
                            break;
                        }
                    case LoggingOverrideType.CharacterProgression:
                        {
                            var progression = item.Response.characterProgressions.data[CharacterId].progressions[$"{loggingOverride.Hash}"];

                            // Likely tracking an integer.
                            OverrideValue = progression.currentProgress;

                            break;
                        }
                    case LoggingOverrideType.InventoryItem:
                        {
                            // Likely tracking a list of instance IDs.
                            // Check vault and postmasters.
                            var invItems = new List<string>();
                            for (int i = 0; i < item.Response.profileInventory.data.items.Count; i++)
                            {
                                long hash = item.Response.profileInventory.data.items[i].itemHash;

                                if (hash == loggingOverride.Hash)
                                {
                                    invItems.Add($"{item.Response.profileInventory.data.items[i].itemInstanceId}");
                                    Log.Debug($"Found {item.Response.profileInventory.data.items[i].itemHash} - {item.Response.profileInventory.data.items[i].itemInstanceId}");
                                }
                            }

                            // Check every character.
                            for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                            {
                                string charId = $"{item.Response.profile.data.characterIds[i]}";
                                for (int j = 0; j < item.Response.characterInventories.data[$"{charId}"].items.Count; j++)
                                {
                                    long hash = item.Response.characterInventories.data[$"{charId}"].items[j].itemHash;

                                    if (hash == loggingOverride.Hash)
                                    {
                                        invItems.Add($"{item.Response.characterInventories.data[$"{charId}"].items[j].itemInstanceId}");
                                        Log.Debug($"Found {item.Response.characterInventories.data[$"{charId}"].items[j].itemHash} - {item.Response.characterInventories.data[$"{charId}"].items[j].itemInstanceId} on {charId}");
                                    }
                                }
                            }
                            OverrideInventoryList = invItems;
                            OverrideValue = 0;
                            break;
                        }
                    case LoggingOverrideType.Consumable:
                        {
                            // Likely tracking an integer.
                            var itemCount = 0;
                            // Check vault and postmasters.
                            for (int i = 0; i < item.Response.profileInventory.data.items.Count; i++)
                            {
                                long hash = item.Response.profileInventory.data.items[i].itemHash;

                                if (hash == loggingOverride.Hash)
                                {
                                    itemCount += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}");
                                    Log.Debug($"Found {item.Response.profileInventory.data.items[i].itemHash} - {item.Response.profileInventory.data.items[i].quantity}");
                                }
                            }

                            // Check every character.
                            for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                            {
                                string charId = $"{item.Response.profile.data.characterIds[i]}";
                                for (int j = 0; j < item.Response.characterInventories.data[$"{charId}"].items.Count; j++)
                                {
                                    long hash = item.Response.characterInventories.data[$"{charId}"].items[j].itemHash;

                                    if (hash == loggingOverride.Hash)
                                    {
                                        int.Parse($"{item.Response.characterInventories.data[$"{charId}"].items[j].quantity}");
                                        Log.Debug($"Found {item.Response.characterInventories.data[$"{charId}"].items[j].itemHash} - {item.Response.characterInventories.data[$"{charId}"].items[j].quantity}");
                                    }
                                }
                            }
                            OverrideValue = itemCount;
                            break;
                        }
                    case LoggingOverrideType.ProfileStringVariable:
                        {
                            // Likely tracking an integer.
                            OverrideValue = int.Parse($"{item.Response.profileStringVariables.data.integerValuesByHash[$"{loggingOverride.Hash}"]}");
                            break;
                        }
                    case LoggingOverrideType.CharacterStringVariable:
                        {
                            // Likely tracking an integer.
                            OverrideValue = int.Parse($"{item.Response.characterStringVariables.data[CharacterId].integerValuesByHash[$"{loggingOverride.Hash}"]}");

                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"{ex}");
                return;
            }
        }
    }
}
