using BungieSharper.Entities.Destiny.Definitions;
using Levante.Configs;
using Newtonsoft.Json;
using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BungieSharper.Entities.Destiny;
using BungieSharper.Entities.Destiny.Definitions.Records;
using Levante.Rotations;
using BungieSharper.Entities.Destiny.Definitions.ActivityModifiers;
using System.Diagnostics;
using BungieSharper.Entities.Destiny.Definitions.Lore;

namespace Levante.Helpers
{
    public class ManifestHelper
    {
        // Name, Hash
        public static Dictionary<long, string> Emblems = new();
        // Inv Hash, Collectible Hash
        public static Dictionary<long, uint> EmblemsCollectible = new();
        public static Dictionary<long, string> Weapons = new();
        public static Dictionary<long, string> Seals = new();
        public static Dictionary<long, string> Ada1ArmorMods = new();
        // Seal Hash, Tracker Hash
        public static Dictionary<long, long> GildableSeals = new();
        public static Dictionary<long, string> Activities = new();

        private static Dictionary<string, int> SeasonIconURLs = new Dictionary<string, int>();

        public static string DestinyManifestVersion { get; internal set; } = "[VERSION]";

        public static bool IsNewManifest()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/Manifest/").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);
                if (DestinyManifestVersion.Equals(item.Response.version))
                    return false;
                else
                    return true;
            }
        }

        public static void LoadManifestDictionaries()
        {
            Emblems.Clear();
            EmblemsCollectible.Clear();
            Weapons.Clear();
            Seals.Clear();
            Ada1ArmorMods.Clear();
            GildableSeals.Clear();
            Activities.Clear();
            SeasonIconURLs.Clear();
            LogHelper.ConsoleLog($"[MANIFEST] Begin emblem and weapon Dictionary population.");
            using (var client = new HttpClient())
            {
                var dimAiResponse = client.GetAsync($"https://raw.githubusercontent.com/DestinyItemManager/d2-additional-info/master/output/watermark-to-season.json").Result;
                SeasonIconURLs = JsonConvert.DeserializeObject<Dictionary<string, int>>(dimAiResponse.Content.ReadAsStringAsync().Result);
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/Manifest/").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);
                LogHelper.ConsoleLog($"[MANIFEST] Found v.{item.Response.version}. Pulling DestinyInventoryItemDefinition (EN)...");
                DestinyManifestVersion = item.Response.version;

                string invItemUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyInventoryItemDefinition"]}";
                response = client.GetAsync(invItemUrl).Result;
                content = response.Content.ReadAsStringAsync().Result;
                var invItemList = JsonConvert.DeserializeObject<Dictionary<string, DestinyInventoryItemDefinition>>(content);

                string vendorListUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyVendorDefinition"]}";
                response = client.GetAsync(vendorListUrl).Result;
                content = response.Content.ReadAsStringAsync().Result;
                var vendorList = JsonConvert.DeserializeObject<Dictionary<string, DestinyVendorDefinition>>(content);
                var ada1ItemList = vendorList["350061650"].ItemList.Select(x => x.ItemHash);

                string activityListUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyActivityDefinition"]}";
                response = client.GetAsync(activityListUrl).Result;
                content = response.Content.ReadAsStringAsync().Result;
                var activityList = JsonConvert.DeserializeObject<Dictionary<string, DestinyActivityDefinition>>(content);

                string placeListUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyPlaceDefinition"]}";
                response = client.GetAsync(placeListUrl).Result;
                content = response.Content.ReadAsStringAsync().Result;
                var placeList = JsonConvert.DeserializeObject<Dictionary<string, DestinyPlaceDefinition>>(content);

                string modifierListUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyActivityModifierDefinition"]}";
                response = client.GetAsync(modifierListUrl).Result;
                content = response.Content.ReadAsStringAsync().Result;
                var modifierList = JsonConvert.DeserializeObject<Dictionary<string, DestinyActivityModifierDefinition>>(content);

                string loreListUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyLoreDefinition"]}";
                response = client.GetAsync(loreListUrl).Result;
                content = response.Content.ReadAsStringAsync().Result;
                var loreList = JsonConvert.DeserializeObject<Dictionary<string, DestinyLoreDefinition>>(content);

                LogHelper.ConsoleLog($"[MANIFEST] Populating Dictionaries...");
                try
                {
                    foreach (var activity in activityList)
                    {
                        if (String.IsNullOrEmpty(activity.Value.DisplayProperties.Name))
                            if (placeList.ContainsKey($"{activity.Value.PlaceHash}"))
                            {
                                Console.WriteLine($"{placeList[$"{activity.Value.PlaceHash}"].DisplayProperties.Name}");
                                Activities.Add(activity.Value.Hash, placeList[$"{activity.Value.PlaceHash}"].DisplayProperties.Name);
                                continue;
                            }
                        Activities.Add(activity.Value.Hash, activity.Value.DisplayProperties.Name);
                        int index = LostSectorRotation.LostSectors.FindIndex(x => activity.Key.Equals($"{x.LegendActivityHash}"));
                        if (index != -1)
                        {
                            LostSectorRotation.LostSectors[index].Name = activity.Value.OriginalDisplayProperties.Name;
                            LostSectorRotation.LostSectors[index].Location = placeList[$"{activity.Value.PlaceHash}"].DisplayProperties.Name;
                            foreach (var mod in activity.Value.Modifiers)
                            {
                                if (String.IsNullOrEmpty(modifierList[$"{mod.ActivityModifierHash}"].DisplayProperties.Name) ||
                                    modifierList[$"{mod.ActivityModifierHash}"].DisplayProperties.Name.Contains("Champion") ||
                                    modifierList[$"{mod.ActivityModifierHash}"].DisplayProperties.Name.Contains("Shielded") ||
                                    modifierList[$"{mod.ActivityModifierHash}"].DisplayProperties.Name.Contains("Modifiers"))
                                {
                                    continue;
                                }
                                LostSectorRotation.LostSectors[index].LegendModifiers.Add(modifierList[$"{mod.ActivityModifierHash}"].DisplayProperties.Name);
                            }
                        }
                        else
                        {
                            index = LostSectorRotation.LostSectors.FindIndex(x => activity.Key.Equals($"{x.MasterActivityHash}"));
                            if (index != -1)
                            {
                                LostSectorRotation.LostSectors[index].Name = activity.Value.OriginalDisplayProperties.Name;
                                LostSectorRotation.LostSectors[index].Location = placeList[$"{activity.Value.PlaceHash}"].DisplayProperties.Name;
                                foreach (var mod in activity.Value.Modifiers)
                                {
                                    // Don't want champion modifier(s) because we have those.
                                    if (String.IsNullOrEmpty(modifierList[$"{mod.ActivityModifierHash}"].DisplayProperties.Name) ||
                                            modifierList[$"{mod.ActivityModifierHash}"].DisplayProperties.Name.Contains("Champion") ||
                                            modifierList[$"{mod.ActivityModifierHash}"].DisplayProperties.Name.Contains("Shielded") ||
                                            modifierList[$"{mod.ActivityModifierHash}"].DisplayProperties.Name.Contains("Modifiers"))
                                    {
                                        continue;
                                    }
                                    LostSectorRotation.LostSectors[index].MasterModifiers.Add(modifierList[$"{mod.ActivityModifierHash}"].DisplayProperties.Name);
                                }
                            }
                        }
                    }
                    foreach (var invItem in invItemList)
                    {
                        if (invItem.Value == null ||
                            string.IsNullOrWhiteSpace(invItem.Value.DisplayProperties.Name) ||
                            string.IsNullOrWhiteSpace(invItem.Value.ItemTypeDisplayName)) continue;

                        //Console.WriteLine($"{invItem.Value.DisplayProperties.Name}");
                        if (/*invItem.Value.TraitIds.Contains("item_type.emblem") || */invItem.Value.ItemType == DestinyItemType.Emblem && invItem.Value.BackgroundColor != null)
                        {
                            if (invItem.Value.Hash == 1968995963) // соняшник (Sunflower) Ukraine Relief Emblem
                                Emblems.Add(invItem.Value.Hash, $"{invItem.Value.DisplayProperties.Name} (Sunflower)");
                            else
                                Emblems.Add(invItem.Value.Hash, invItem.Value.DisplayProperties.Name);

                            if (invItem.Value.CollectibleHash != null)
                                EmblemsCollectible.Add(invItem.Value.Hash, (uint)invItem.Value.CollectibleHash);
                        }
                            

                        if (/*invItem.Value.TraitIds != null && invItem.Value.TraitIds.Contains("item_type.weapon")*/invItem.Value.ItemType == DestinyItemType.Weapon)
                        {
                            if (invItem.Value.DisplayProperties.Name == null)
                                continue; 

                            if (invItem.Value.Hash == 417164956) // Jötunn
                                Weapons.Add(invItem.Value.Hash, $"{invItem.Value.DisplayProperties.Name} (Jotunn)");
                            else
                            {
                                var dupeWeapons = Weapons.Where(x => x.Value.Contains(invItem.Value.DisplayProperties.Name));
                                if (dupeWeapons.Count() > 0)
                                {
                                    foreach (var weapon in dupeWeapons.ToList())
                                    {
                                        if (!weapon.Value.Contains("[S"))
                                        {
                                            Weapons.Remove(weapon.Key);
                                            if (invItemList[$"{weapon.Key}"].IconWatermark == null)
                                                Weapons.Add(weapon.Key, $"{weapon.Value} [S1]");
                                            else if (SeasonIconURLs.ContainsKey(invItem.Value.IconWatermark))
                                                Weapons.Add(weapon.Key, $"{weapon.Value} [S{SeasonIconURLs[$"{invItemList[$"{weapon.Key}"].IconWatermark}"]}]");
                                        }
                                    }
                                    if (invItem.Value.IconWatermark == null)
                                        Weapons.Add(invItem.Value.Hash, $"{invItem.Value.DisplayProperties.Name} [S1]");
                                    else if (SeasonIconURLs.ContainsKey(invItem.Value.IconWatermark))
                                        Weapons.Add(invItem.Value.Hash, $"{invItem.Value.DisplayProperties.Name} [S{SeasonIconURLs[$"{invItem.Value.IconWatermark}"]}]");
                                }
                                else
                                    Weapons.Add(invItem.Value.Hash, $"{invItem.Value.DisplayProperties.Name}");
                            }
                                
                        }
                        
                        if (invItem.Value.ItemType == DestinyItemType.Mod && invItem.Value.ItemSubType == 0)
                        {
                            //if (invItem.Value.DisplayProperties.Name == null || 
                            //    invItem.Value.DisplayProperties.Name.Equals("Empty Mod Socket") || 
                            //    invItem.Value.DisplayProperties.Name.Equals("Deprecated Armor Mod"))
                            //    continue;

                            //if (!invItem.Value.ItemCategoryHashes.Contains(4104513227))
                            //    continue;

                            if (!ada1ItemList.Contains(invItem.Value.Hash))
                                continue;
                            Ada1ArmorMods.Add(invItem.Value.Hash, $"{invItem.Value.DisplayProperties.Name}");
                        }
                    }
                }
                catch (Exception x)
                {
                    Console.WriteLine($"{x}");
                }

                LogHelper.ConsoleLog($"[MANIFEST] Populating Title/Seal Dictionaries...");
                string recordUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyRecordDefinition"]}";
                var response2 = client.GetAsync(recordUrl).Result;
                var content2 = response2.Content.ReadAsStringAsync().Result;
                var recordList = JsonConvert.DeserializeObject<Dictionary<string, DestinyRecordDefinition>>(content2);

                try
                {
                    foreach (var record in recordList)
                    {
                        if (record.Value.TitleInfo == null) continue;
                        if (record.Value.TitleInfo.HasTitle)
                        {
                            Seals.Add(record.Value.Hash, $"{record.Value.TitleInfo.TitlesByGender.Values.FirstOrDefault()}");
                            if (record.Value.TitleInfo.GildingTrackingRecordHash != null)
                                GildableSeals.Add(record.Value.Hash, (long)record.Value.TitleInfo.GildingTrackingRecordHash);
                        }
                            
                    }
                }
                catch (Exception x)
                {
                    Console.WriteLine($"{x}");
                }
            }
            LogHelper.ConsoleLog($"[MANIFEST] Dictionary population complete.");
        }
    }
}
