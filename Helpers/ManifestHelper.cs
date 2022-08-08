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

namespace Levante.Helpers
{
    public class ManifestHelper
    {
        // Name, Hash
        public static Dictionary<long, string> Emblems = new Dictionary<long, string>();
        // Inv Hash, Collectible Hash
        public static Dictionary<long, uint> EmblemsCollectible = new Dictionary<long, uint>();
        public static Dictionary<long, string> Weapons = new Dictionary<long, string>();
        public static Dictionary<long, string> Seals = new Dictionary<long, string>();
        // Seal Hash, Tracker Hash
        public static Dictionary<long, long> GildableSeals = new Dictionary<long, long>();

        private static Dictionary<string, int> SeasonIconURLs = new Dictionary<string, int>();

        public static void LoadManifestDictionaries()
        {
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

                string invItemUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyInventoryItemDefinition"]}";
                var response1 = client.GetAsync(invItemUrl).Result;
                var content1 = response1.Content.ReadAsStringAsync().Result;
                var invItemList = JsonConvert.DeserializeObject<Dictionary<string, DestinyInventoryItemDefinition>>(content1);

                LogHelper.ConsoleLog($"[MANIFEST] Populating Weapon and Emblem Dictionaries...");
                try
                {
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
                                        Console.WriteLine($"Weapon Dupe: {weapon.Value}");
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
