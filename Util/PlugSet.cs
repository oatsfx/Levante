using BungieSharper.Entities.Destiny.Definitions.Sockets;
using Discord.WebSocket;
using Levante.Configs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Util
{
    public class PlugSet
    {
        public List<WeaponPerk> WeaponPerks { get; }
        // Name, Level, Enhanced Level
        public Dictionary<string, KeyValuePair<int, int>> PerkLevels { get; }
        private long HashCode { get; set; }
        private string APIUrl { get; set; }
        private string Content { get; set; }
        private bool IsCrafting { get; set; }

        public PlugSet(long hashCode, bool isCrafting = false)
        {
            HashCode = hashCode;
            APIUrl = "https://www.bungie.net/platform/Destiny2/Manifest/DestinyPlugSetDefinition/" + HashCode;
            WeaponPerks = new List<WeaponPerk>();
            PerkLevels = new();
            IsCrafting = isCrafting;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync(APIUrl).Result;
                Content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(Content);

                for (int i = 0; i < item.Response.reusablePlugItems.Count; i++)
                {
                   
                    var perk = new WeaponPerk((long)item.Response.reusablePlugItems[i].plugItemHash);
                    // For duplicates and retired perks.
                    if (WeaponPerks.Count == 0 || ((bool)item.Response.reusablePlugItems[i].currentlyCanRoll == true && !WeaponPerks.Any(x => x.GetItemHash() == (long)item.Response.reusablePlugItems[i].plugItemHash)))
                        WeaponPerks.Add(new WeaponPerk((long)item.Response.reusablePlugItems[i].plugItemHash));

                    if (!PerkLevels.ContainsKey(perk.GetName().Replace(" Enhanced", "")))
                        PerkLevels.Add(perk.GetName(), new KeyValuePair<int, int>(-1, -1));

                    int reqLevel = 1;
                    if (item.Response.reusablePlugItems[i].craftingRequirements != null &&
                        (int)item.Response.reusablePlugItems[i].craftingRequirements.materialRequirementHashes.Count > 0)
                    {
                        if (item.Response.reusablePlugItems[i].craftingRequirements.requiredLevel != null)
                            reqLevel = item.Response.reusablePlugItems[i].craftingRequirements.requiredLevel;

                        // Override IsCrafting if it is false and crafting levels are found.
                        if (!IsCrafting) IsCrafting = true;
                    }

                    if (perk.IsEnhanced())
                    {
                        if (!PerkLevels.ContainsKey(perk.GetName().Replace(" Enhanced", "")))
                            PerkLevels.Add(perk.GetName().Replace(" Enhanced", ""), new KeyValuePair<int, int>(-1, reqLevel));

                        else if (PerkLevels[perk.GetName().Replace(" Enhanced", "")].Value == -1)
                        {
                            var newKVP = new KeyValuePair<int, int>(PerkLevels[perk.GetName().Replace(" Enhanced", "")].Key, reqLevel);
                            PerkLevels[perk.GetName().Replace(" Enhanced", "")] = newKVP;
                        }
                    }
                    else
                    {
                        if (!PerkLevels.ContainsKey(perk.GetName()))
                            PerkLevels.Add(perk.GetName(), new KeyValuePair<int, int>(reqLevel, -1));

                        else if (PerkLevels[perk.GetName()].Key == -1)
                        {
                            var newKVP = new KeyValuePair<int, int>(reqLevel, PerkLevels[perk.GetName()].Value);
                            PerkLevels[perk.GetName()] = newKVP;
                        }
                    }
                }
            }
        }

        public string BuildStringList(bool makeEmote = true)
        {
            string result = "";
            DestinyPlugSetDefinition item = JsonConvert.DeserializeObject<DestinyPlugSetDefinition>(Content);

            // Name, Level, Enhanced Level
            Dictionary<string, KeyValuePair<int, int>> perkCraftLevels = new();

            var perkList = new List<WeaponPerk>();
            foreach (WeaponPerk perk in WeaponPerks.Where(perk => !perk.IsEnhanced()))
                perkList.Add(perk);

            // Don't want to make emotes on Debug.
            if (makeEmote)
                makeEmote = !BotConfig.IsDebug;

            string json = File.ReadAllText(EmoteConfig.FilePath);
            var emoteCfg = JsonConvert.DeserializeObject<EmoteConfig>(json);
            foreach (var Perk in perkList)
            {
                if (makeEmote)
                {
                    if (!emoteCfg.Emotes.ContainsKey(Perk.GetName().Replace(" ", "").Replace("-", "").Replace("'", "")))
                    {
                        var byteArray = new HttpClient().GetByteArrayAsync($"{Perk.GetIconUrl()}").Result;
                        Task.Run(() => emoteCfg.AddEmote(Perk.GetName().Replace(" ", "").Replace("-", "").Replace("'", ""), new Discord.Image(new MemoryStream(byteArray)))).Wait();
                    }
                    result += $"{emoteCfg.Emotes[Perk.GetName().Replace(" ", "").Replace("-", "").Replace("'", "")]}{Perk.GetName()}";
                }
                else
                {
                    if (emoteCfg.Emotes.ContainsKey(Perk.GetName().Replace(" ", "").Replace("-", "").Replace("'", "")))
                    {
                        result += $"{emoteCfg.Emotes[Perk.GetName().Replace(" ", "").Replace("-", "").Replace("'", "")]}";
                    }
                    result += $"{Perk.GetName()}";
                }


                if (IsCrafting)
                    if (PerkLevels[Perk.GetName()].Value <= 0)
                        result += $" ({PerkLevels[Perk.GetName()].Key})\n";
                    else
                        result += $" ({PerkLevels[Perk.GetName()].Key}/{DestinyEmote.Enhanced}{PerkLevels[Perk.GetName()].Value})\n";
                else
                    result += "\n";
            }
            emoteCfg.UpdateJSON();
            return result;
        }
    }
}
