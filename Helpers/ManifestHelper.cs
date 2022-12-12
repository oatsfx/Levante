using BungieSharper.Entities.Destiny.Definitions;
using Levante.Configs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using BungieSharper.Entities.Destiny;
using BungieSharper.Entities.Destiny.Definitions.Records;
using Levante.Rotations;
using BungieSharper.Entities.Destiny.Definitions.ActivityModifiers;
using BungieSharper.Entities.Destiny.Definitions.Presentation;
using Levante.Util;
using System.IO;
using Serilog;

namespace Levante.Helpers
{
    public class ManifestHelper
    {
        // Inv Hash, Collectible Hash
        public static Dictionary<long, uint> EmblemsCollectible = new();
        // Hash, Name
        public static Dictionary<long, string> Emblems = new();
        public static Dictionary<long, string> Weapons = new();
        public static Dictionary<long, string> Seals = new();
        public static Dictionary<long, string> Ada1ArmorMods = new();
        public static Dictionary<long, string> Activities = new();
        // Seal Hash, Tracker Hash
        public static Dictionary<long, long> GildableSeals = new();
        // Whatever Hash is found First, Nightfall Name
        public static Dictionary<long, string> Nightfalls = new();

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
                if (DestinyManifestVersion.Equals($"{item.Response.version}"))
                    return false;
                else
                    return true;
            }
        }

        public static void LoadManifestDictionaries()
        {
            if (!Directory.Exists("Data/Manifest/JSONs"))
                Directory.CreateDirectory("Data/Manifest/JSONs");

            Emblems.Clear();
            EmblemsCollectible.Clear();
            Weapons.Clear();
            Seals.Clear();
            Ada1ArmorMods.Clear();
            GildableSeals.Clear();
            Activities.Clear();
            SeasonIconURLs.Clear();
            using (var client = new HttpClient())
            {
                var dimAiResponse = client.GetAsync($"https://raw.githubusercontent.com/DestinyItemManager/d2-additional-info/master/output/watermark-to-season.json").Result;
                SeasonIconURLs = JsonConvert.DeserializeObject<Dictionary<string, int>>(dimAiResponse.Content.ReadAsStringAsync().Result);
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/Manifest/").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);
                DestinyManifestVersion = item.Response.version;
                if (!File.Exists($"Data/Manifest/JSONs/{DestinyManifestVersion}.json"))
                {
                    Log.Information("[{Type}] Found v.{ManifestVersion}. Downloading and storing locally...",
                        "Manifest", DestinyManifestVersion);
                    File.WriteAllText($"Data/Manifest/JSONs/{DestinyManifestVersion}.json", JsonConvert.SerializeObject(item, Formatting.Indented));
                }
                else
                {
                    Log.Information("[{Type}] Found v.{ManifestVersion}. No download needed.",
                       "Manifest", DestinyManifestVersion);
                }

                // Inventory Items
                string path = item.Response.jsonWorldComponentContentPaths.en["DestinyInventoryItemDefinition"];
                string fileName = path.Split('/').LastOrDefault();
                Dictionary<string, DestinyInventoryItemDefinition> invItemList = new();
                if (!Directory.Exists("Data/Manifest/JSONs/DestinyInventoryItemDefinition"))
                    Directory.CreateDirectory("Data/Manifest/JSONs/DestinyInventoryItemDefinition");
                if (!File.Exists($"Data/Manifest/JSONs/DestinyInventoryItemDefinition/{fileName}"))
                {
                    Log.Information("[{Type}] Storing {def} locally...",
                        "Manifest", "DestinyInventoryItemDefinition");
                    string invItemUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyInventoryItemDefinition"]}";
                    response = client.GetAsync(invItemUrl).Result;
                    content = response.Content.ReadAsStringAsync().Result;
                    invItemList = JsonConvert.DeserializeObject<Dictionary<string, DestinyInventoryItemDefinition>>(content);
                    File.WriteAllText($"Data/Manifest/JSONs/DestinyInventoryItemDefinition/{fileName}", JsonConvert.SerializeObject(invItemList, Formatting.Indented));
                }
                else
                {
                    content = File.ReadAllText($"Data/Manifest/JSONs/DestinyInventoryItemDefinition/{fileName}");
                    invItemList = JsonConvert.DeserializeObject<Dictionary<string, DestinyInventoryItemDefinition>>(content);
                    Log.Information("[{Type}] Loaded {def} from local.",
                        "Manifest", "DestinyInventoryItemDefinition");
                }

                // Vendors
                path = item.Response.jsonWorldComponentContentPaths.en["DestinyVendorDefinition"];
                fileName = path.Split('/').LastOrDefault();
                Dictionary<string, DestinyVendorDefinition> vendorList = new();
                if (!Directory.Exists("Data/Manifest/JSONs/DestinyVendorDefinition"))
                    Directory.CreateDirectory("Data/Manifest/JSONs/DestinyVendorDefinition");
                if (!File.Exists($"Data/Manifest/JSONs/DestinyVendorDefinition/{fileName}"))
                {
                    Log.Information("[{Type}] Storing {def} locally...",
                        "Manifest", "DestinyVendorDefinition");
                    string vendorListUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyVendorDefinition"]}";
                    response = client.GetAsync(vendorListUrl).Result;
                    content = response.Content.ReadAsStringAsync().Result;
                    vendorList = JsonConvert.DeserializeObject<Dictionary<string, DestinyVendorDefinition>>(content);
                    File.WriteAllText($"Data/Manifest/JSONs/DestinyVendorDefinition/{fileName}", JsonConvert.SerializeObject(vendorList, Formatting.Indented));
                }
                else
                {
                    content = File.ReadAllText($"Data/Manifest/JSONs/DestinyVendorDefinition/{fileName}");
                    vendorList = JsonConvert.DeserializeObject<Dictionary<string, DestinyVendorDefinition>>(content);
                    Log.Information("[{Type}] Loaded {def} from local.",
                        "Manifest", "DestinyVendorDefinition");
                }
                var ada1ItemList = vendorList["350061650"].ItemList.Select(x => x.ItemHash);

                // Activities
                path = item.Response.jsonWorldComponentContentPaths.en["DestinyActivityDefinition"];
                fileName = path.Split('/').LastOrDefault();
                Dictionary<string, DestinyActivityDefinition> activityList = new();
                if (!Directory.Exists("Data/Manifest/JSONs/DestinyActivityDefinition"))
                    Directory.CreateDirectory("Data/Manifest/JSONs/DestinyActivityDefinition");
                if (!File.Exists($"Data/Manifest/JSONs/DestinyActivityDefinition/{fileName}"))
                {
                    Log.Information("[{Type}] Storing {def} locally...",
                        "Manifest", "DestinyActivityDefinition");
                    string activityListUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyActivityDefinition"]}";
                    response = client.GetAsync(activityListUrl).Result;
                    content = response.Content.ReadAsStringAsync().Result;
                    activityList = JsonConvert.DeserializeObject<Dictionary<string, DestinyActivityDefinition>>(content);
                    File.WriteAllText($"Data/Manifest/JSONs/DestinyActivityDefinition/{fileName}", JsonConvert.SerializeObject(activityList, Formatting.Indented));
                }
                else
                {
                    content = File.ReadAllText($"Data/Manifest/JSONs/DestinyActivityDefinition/{fileName}");
                    activityList = JsonConvert.DeserializeObject<Dictionary<string, DestinyActivityDefinition>>(content);
                    Log.Information("[{Type}] Loaded {def} from local.",
                        "Manifest", "DestinyActivityDefinition");
                }

                // Places
                path = item.Response.jsonWorldComponentContentPaths.en["DestinyPlaceDefinition"];
                fileName = path.Split('/').LastOrDefault();
                Dictionary<string, DestinyPlaceDefinition> placeList = new();
                if (!Directory.Exists("Data/Manifest/JSONs/DestinyPlaceDefinition"))
                    Directory.CreateDirectory("Data/Manifest/JSONs/DestinyPlaceDefinition");
                if (!File.Exists($"Data/Manifest/JSONs/DestinyPlaceDefinition/{fileName}"))
                {
                    Log.Information("[{Type}] Storing {def} locally...",
                        "Manifest", "DestinyPlaceDefinition");
                    string placeListUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyPlaceDefinition"]}";
                    response = client.GetAsync(placeListUrl).Result;
                    content = response.Content.ReadAsStringAsync().Result;
                    placeList = JsonConvert.DeserializeObject<Dictionary<string, DestinyPlaceDefinition>>(content);
                    File.WriteAllText($"Data/Manifest/JSONs/DestinyPlaceDefinition/{fileName}", JsonConvert.SerializeObject(placeList, Formatting.Indented));
                }
                else
                {
                    content = File.ReadAllText($"Data/Manifest/JSONs/DestinyPlaceDefinition/{fileName}");
                    placeList = JsonConvert.DeserializeObject<Dictionary<string, DestinyPlaceDefinition>>(content);
                    Log.Information("[{Type}] Loaded {def} from local.",
                        "Manifest", "DestinyPlaceDefinition");
                }

                // Modifiers
                path = item.Response.jsonWorldComponentContentPaths.en["DestinyActivityModifierDefinition"];
                fileName = path.Split('/').LastOrDefault();
                Dictionary<string, DestinyActivityModifierDefinition> modifierList = new();
                if (!Directory.Exists("Data/Manifest/JSONs/DestinyActivityModifierDefinition"))
                    Directory.CreateDirectory("Data/Manifest/JSONs/DestinyActivityModifierDefinition");
                if (!File.Exists($"Data/Manifest/JSONs/DestinyActivityModifierDefinition/{fileName}"))
                {
                    Log.Information("[{Type}] Storing {def} locally...",
                        "Manifest", "DestinyActivityModifierDefinition");
                    string modifierListUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyActivityModifierDefinition"]}";
                    response = client.GetAsync(modifierListUrl).Result;
                    content = response.Content.ReadAsStringAsync().Result;
                    modifierList = JsonConvert.DeserializeObject<Dictionary<string, DestinyActivityModifierDefinition>>(content);
                    File.WriteAllText($"Data/Manifest/JSONs/DestinyActivityModifierDefinition/{fileName}", JsonConvert.SerializeObject(modifierList, Formatting.Indented));
                }
                else
                {
                    content = File.ReadAllText($"Data/Manifest/JSONs/DestinyActivityModifierDefinition/{fileName}");
                    modifierList = JsonConvert.DeserializeObject<Dictionary<string, DestinyActivityModifierDefinition>>(content);
                    Log.Information("[{Type}] Loaded {def} from local.",
                        "Manifest", "DestinyActivityModifierDefinition");
                }

                // Records/Triumph
                path = item.Response.jsonWorldComponentContentPaths.en["DestinyRecordDefinition"];
                fileName = path.Split('/').LastOrDefault();
                Dictionary<string, DestinyRecordDefinition> recordList = new();
                if (!Directory.Exists("Data/Manifest/JSONs/DestinyRecordDefinition"))
                    Directory.CreateDirectory("Data/Manifest/JSONs/DestinyRecordDefinition");
                if (!File.Exists($"Data/Manifest/JSONs/DestinyRecordDefinition/{fileName}"))
                {
                    Log.Information("[{Type}] Storing {def} locally...",
                        "Manifest", "DestinyRecordDefinition");
                    string recordUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyRecordDefinition"]}";
                    response = client.GetAsync(recordUrl).Result;
                    content = response.Content.ReadAsStringAsync().Result;
                    recordList = JsonConvert.DeserializeObject<Dictionary<string, DestinyRecordDefinition>>(content);
                    File.WriteAllText($"Data/Manifest/JSONs/DestinyRecordDefinition/{fileName}", JsonConvert.SerializeObject(recordList, Formatting.Indented));
                }
                else
                {
                    content = File.ReadAllText($"Data/Manifest/JSONs/DestinyRecordDefinition/{fileName}");
                    recordList = JsonConvert.DeserializeObject<Dictionary<string, DestinyRecordDefinition>>(content);
                    Log.Information("[{Type}] Loaded {def} from local.",
                        "Manifest", "DestinyRecordDefinition");
                }

                // Presentation Nodes
                path = item.Response.jsonWorldComponentContentPaths.en["DestinyPresentationNodeDefinition"];
                fileName = path.Split('/').LastOrDefault();
                Dictionary<string, DestinyPresentationNodeDefinition> presentNodeList = new();
                if (!Directory.Exists("Data/Manifest/JSONs/DestinyPresentationNodeDefinition"))
                    Directory.CreateDirectory("Data/Manifest/JSONs/DestinyPresentationNodeDefinition");
                if (!File.Exists($"Data/Manifest/JSONs/DestinyPresentationNodeDefinition/{fileName}"))
                {
                    Log.Information("[{Type}] Storing {def} locally...",
                        "Manifest", "DestinyPresentationNodeDefinition");
                    string presentNodeUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyPresentationNodeDefinition"]}";
                    response = client.GetAsync(presentNodeUrl).Result;
                    content = response.Content.ReadAsStringAsync().Result;
                    presentNodeList = JsonConvert.DeserializeObject<Dictionary<string, DestinyPresentationNodeDefinition>>(content);
                    File.WriteAllText($"Data/Manifest/JSONs/DestinyPresentationNodeDefinition/{fileName}", JsonConvert.SerializeObject(presentNodeList, Formatting.Indented));
                }
                else
                {
                    content = File.ReadAllText($"Data/Manifest/JSONs/DestinyPresentationNodeDefinition/{fileName}");
                    presentNodeList = JsonConvert.DeserializeObject<Dictionary<string, DestinyPresentationNodeDefinition>>(content);
                    Log.Information("[{Type}] Loaded {def} from local.",
                        "Manifest", "DestinyPresentationNodeDefinition");
                }

                Log.Information("[{Type}] Populating Dictionaries...",
                        "Manifest");
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

                    foreach (var node in presentNodeList)
                    {
                        if (node.Value.DisplayProperties == null) continue;
                        if (node.Value.Children == null) continue;
                        if (node.Value.Children.Records.Count() == 0) continue;
                        if (node.Value.CompletionRecordHash == null) continue;
                        foreach (var child in node.Value.Children.Records)
                        {
                            if (GildableSeals.ContainsKey((long)node.Value.CompletionRecordHash) && recordList.ContainsKey(child.RecordHash.ToString()))
                            {
                                string recordName = recordList[child.RecordHash.ToString()].DisplayProperties.Name;
                                if (recordName.Contains("Grandmaster:"))
                                {
                                    //Console.WriteLine(recordName.Replace("Grandmaster: ", ""));
                                    NightfallRotation.Nightfalls.Add(recordName.Replace("Grandmaster: ", ""));
                                }
                            }
                        }
                    }

                    foreach (var activity in activityList)
                    {
                        if (String.IsNullOrEmpty(activity.Value.DisplayProperties.Name))
                            if (placeList.ContainsKey($"{activity.Value.PlaceHash}"))
                            {
                                Activities.Add(activity.Value.Hash, placeList[$"{activity.Value.PlaceHash}"].DisplayProperties.Name);
                                continue;
                            }

                        Activities.Add(activity.Value.Hash, activity.Value.DisplayProperties.Name);
                        if (NightfallRotation.Nightfalls.Contains(activity.Value.DisplayProperties.Description) && !Nightfalls.ContainsValue(activity.Value.DisplayProperties.Description))
                        {
                            Nightfalls.Add(activity.Value.Hash, activity.Value.DisplayProperties.Description);
                            //Console.WriteLine($"{activity.Value.Hash}: {activity.Value.DisplayProperties.Name} ({activity.Value.DisplayProperties.Description})");
                        }
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
                                LostSectorRotation.LostSectors[index].PGCRImage = "https://bungie.net" + activity.Value.PgcrImage;
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
                                var dupeWeapons = Weapons.Where(x => x.Value.Split('[')[0].Equals(invItem.Value.DisplayProperties.Name));
                                if (dupeWeapons.Any())
                                {
                                    int dupeCount = dupeWeapons.Count();
                                    foreach (var weapon in dupeWeapons.ToList())
                                    {
                                        if (!weapon.Value.Contains("[S") && !weapon.Value.Contains("[V"))
                                        {
                                            Weapons.Remove(weapon.Key);
                                            if (invItemList[$"{weapon.Key}"].IconWatermark == null)
                                                Weapons.Add(weapon.Key, $"{weapon.Value} [S1]");
                                            else if (SeasonIconURLs.ContainsKey(invItem.Value.IconWatermark))
                                                Weapons.Add(weapon.Key, $"{weapon.Value} [S{SeasonIconURLs[$"{invItemList[$"{weapon.Key}"].IconWatermark}"]}]");
                                            else // This handles event weapons because their icon does not have the season one. Lazy implementation that does not take into account whether or not the weapon is the more recent version.
                                            {
                                                //Log.Debug("Added: {Name} [V{Count}]", weapon.Value, dupeCount + 1);
                                                Weapons.Add(weapon.Key, $"{weapon.Value} [V{dupeCount + 1}]");
                                            }
                                        }
                                    }
                                    if (invItem.Value.IconWatermark == null)
                                        Weapons.Add(invItem.Value.Hash, $"{invItem.Value.DisplayProperties.Name} [S1]");
                                    else if (SeasonIconURLs.ContainsKey(invItem.Value.IconWatermark))
                                        Weapons.Add(invItem.Value.Hash, $"{invItem.Value.DisplayProperties.Name} [S{SeasonIconURLs[$"{invItem.Value.IconWatermark}"]}]");
                                    else
                                    {
                                        //Log.Debug("Added: {Name} [V{Count}]", invItem.Value.DisplayProperties.Name, 1);
                                        Weapons.Add(invItem.Value.Hash, $"{invItem.Value.DisplayProperties.Name} [V1]");
                                    }
                                }
                                else
                                    Weapons.Add(invItem.Value.Hash, $"{invItem.Value.DisplayProperties.Name}");
                            }

                            int index = NightfallRotation.NightfallWeapons.FindIndex(x => x.Hash == invItem.Value.Hash);
                            if (index != -1)
                            {
                                NightfallRotation.NightfallWeapons[index].Name = invItem.Value.DisplayProperties.Name;
                                bool isHeavyGL = invItem.Value.ItemSubType == DestinyItemSubType.GrenadeLauncher && invItem.Value.ItemCategoryHashes.Contains<uint>(4);
                                NightfallRotation.NightfallWeapons[index].Emote = DestinyEmote.MatchWeaponItemSubtypeToEmote(invItem.Value.ItemSubType, isHeavyGL);
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
                    Log.Fatal("[{Type}] Population error. {exception}", "Manifest", x);
                }
            }

            Log.Information("[{Type}] Dictionary population complete.", "Manifest");
        }
    }
}
