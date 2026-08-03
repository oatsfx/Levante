using BungieSharper.Entities.Destiny.Definitions.Sockets;
using Discord.WebSocket;
using Levante.Configs;
using Levante.Services;
using Levante.Util;
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

namespace Levante.Structs
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
            APIUrl = "/Platform/Destiny2/Manifest/DestinyPlugSetDefinition/" + HashCode;
            WeaponPerks = [];
            PerkLevels = [];
            IsCrafting = isCrafting;

            Content = Task.Run(() => BungieApi.Query(APIUrl)).Result;
            dynamic item = JsonConvert.DeserializeObject(Content);

            for (int i = 0; i < item.Response.reusablePlugItems.Count; i++)
            {

                var perk = new WeaponPerk((long)item.Response.reusablePlugItems[i].plugItemHash);
                // For duplicates and retired perks.
                if (WeaponPerks.Count == 0 || (bool)item.Response.reusablePlugItems[i].currentlyCanRoll && !WeaponPerks.Any(x => x.GetItemHash() == (long)item.Response.reusablePlugItems[i].plugItemHash))
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

        public string BuildStringList(bool makeEmote = true)
        {
            string result = "";

            // Name, Level, Enhanced Level
            Dictionary<string, KeyValuePair<int, int>> perkCraftLevels = new();

            var perkList = new List<WeaponPerk>();
            foreach (WeaponPerk perk in WeaponPerks.Where(perk => !perk.IsEnhanced()))
                perkList.Add(perk);

            foreach (var Perk in perkList)
            {
                var formattedPerk = Perk.GetName().Replace(" ", "").Replace("-", "").Replace("'", "");
                if (makeEmote)
                {
                    var emote = Task.Run(() => EmoteConfig.GetEmote(formattedPerk)).Result;
                    if (!Task.Run(() => EmoteConfig.HasEmote(formattedPerk)).Result)
                    {
                        var byteArray = Task.Run(() => BungieApi.GetImage(Perk.GetIconUrl())).Result;
                        emote = Task.Run(() => EmoteConfig.AddEmote(formattedPerk, new Discord.Image(new MemoryStream(byteArray)))).Result;
                    }
                    result += $"{emote}{Perk.GetName()}";
                }
                else
                {
                    result += $"{Task.Run(() => EmoteConfig.GetEmote(formattedPerk)).Result}{Perk.GetName()}";
                }


                if (IsCrafting)
                    if (PerkLevels[Perk.GetName()].Value <= 0)
                        result += $" ({PerkLevels[Perk.GetName()].Key})\n";
                    else
                        result += $" ({PerkLevels[Perk.GetName()].Key}/{EmoteConfig.GetEmote("Enhanced")}{PerkLevels[Perk.GetName()].Value})\n";
                else
                    result += "\n";
            }
            return result;
        }
    }
}
