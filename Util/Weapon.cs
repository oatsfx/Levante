using APIHelper;
using APIHelper.Structs;
using Discord;
using Discord.WebSocket;
using Levante.Configs;
using Levante.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Levante.Util
{
    public class Weapon : InventoryItem
    {
        public Weapon(long hashCode)
        {
            HashCode = hashCode;
            APIUrl = $"https://www.bungie.net/platform/Destiny2/Manifest/DestinyInventoryItemDefinition/" + HashCode;

            Content = ManifestConnection.GetInventoryItemById(unchecked((int)hashCode));
        }

        public string GetSourceString()
        {
            if (GetCollectableHash() == null)
                return "No source data provided.";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/Manifest/DestinyCollectibleDefinition/" + GetCollectableHash()).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);
                return item.Response.sourceString;
            }
        }

        public string GetDamageType() => $"{DestinyEmote.MatchEmote($"{(DamageType)Content.DefaultDamageType}")} {(DamageType)Content.DefaultDamageType}";

        public WeaponPerk GetIntrinsic() => new WeaponPerk(Content.Sockets.SocketEntries.ElementAt(0).SingleInitialItemHash);

        public PlugSet GetRandomPerks(int Column /*This parameter is the desired column for weapon perks.*/)
        {
            try
            {
                List<int> perkIndexes = new List<int>();
                for (int i = 0; i < Content.Sockets.SocketCategories.Count(); i++)
                {
                    if (Content.Sockets.SocketCategories.ElementAt(i).SocketCategoryHash == 4241085061)
                        for (int j = 0; j < Content.Sockets.SocketCategories.ElementAt(i).SocketIndexes.Count(); j++)
                            perkIndexes.Add(Convert.ToInt32(Content.Sockets.SocketCategories.ElementAt(i).SocketIndexes.ElementAt(j)));
                }

                if (Column > perkIndexes.Count)
                    return null;

                if (Content.Sockets.SocketEntries.ElementAt(perkIndexes[Column - 1]).PreventInitializationOnVendorPurchase)
                    return null;

                if (Content.Sockets.SocketEntries.ElementAt(perkIndexes[Column - 1]).RandomizedPlugSetHash == null)
                    return new PlugSet((long)Content.Sockets.SocketEntries.ElementAt(perkIndexes[Column - 1]).ReusablePlugSetHash);
                else
                    return new PlugSet((long)Content.Sockets.SocketEntries.ElementAt(perkIndexes[Column - 1]).RandomizedPlugSetHash);
            }
            catch
            {
                return null;
            }
        }

        public PlugSet GetFoundryPerks()
        {
            try
            {
                int foundryIndex = -1;
                for (int i = 0; i < Content.Sockets.SocketEntries.Count(); i++)
                {
                    if (Content.Sockets.SocketEntries.ElementAt(i).SocketTypeHash == 3993098925)
                        foundryIndex = i;
                }

                if (foundryIndex == -1)
                    return null;

                if (Content.Sockets.SocketEntries.ElementAt(foundryIndex).RandomizedPlugSetHash == null)
                    return new PlugSet((long)Content.Sockets.SocketEntries.ElementAt(foundryIndex).ReusablePlugSetHash);
                else
                    return new PlugSet((long)Content.Sockets.SocketEntries.ElementAt(foundryIndex).RandomizedPlugSetHash);
            }
            catch
            {
                return null;
            }
        }

        public override EmbedBuilder GetEmbed()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Weapon Details: {GetName()}",
                IconUrl = GetIconUrl(),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by the Bungie API"
            };
            int[] embedRGB = DestinyColors.GetColorFromString(GetSpecificItemType());
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(embedRGB[0], embedRGB[1], embedRGB[2]),
                Author = auth,
                Footer = foot
            };
            try
            {
                embed.Description = (GetSourceString().Equals("") ? "No source data provided." : GetSourceString()) + "\n" +
                        $"Hash Code: {GetItemHash()}\n";
                embed.ThumbnailUrl = GetIconUrl();
            }
            catch
            {
                embed.Description = "This weapon is missing some API values, sorry about that!";
            }

            embed.AddField(x =>
            {
                x.Name = $"> Information";
                x.Value = $"{GetDamageType()} {GetSpecificItemType()}\n" +
                    $"*{GetFlavorText()}*\n";
                x.Value += $"Craftable?: {(Content.Inventory.RecipeItemHash != null ? "Yes" : "No")}";
                x.IsInline = false;
            })
            .AddField(x =>
            {
                x.Name = $"> Perks";
                x.Value = $"*List of perks for this weapon, per column.*\n" +
                    $"Intrinsic: {GetIntrinsic().GetName()}\n";
                x.IsInline = false;
            })
            .AddField(x =>
            {
                x.Name = $"Column 1";
                x.Value = $"{(GetRandomPerks(1) == null ? "No perks." : GetRandomPerks(1).BuildStringList())}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"Column 2";
                x.Value = $"{(GetRandomPerks(2) == null ? "No perks." : GetRandomPerks(2).BuildStringList())}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"Column 3";
                x.Value = $"{(GetRandomPerks(3) == null ? "No perks." : GetRandomPerks(3).BuildStringList())}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"Column 4";
                x.Value = $"{(GetRandomPerks(4) == null ? "No perks." : GetRandomPerks(4).BuildStringList())}";
                x.IsInline = true;
            });

            if (GetFoundryPerks() != null)
            {
                embed.AddField(x => {
                    x.Name = $"Column 5 (Foundry/Origin)";
                    x.Value = $"{GetFoundryPerks().BuildStringList()}";
                    x.IsInline = true;
                });
            }

            return embed;
        }
    }

    public class WeaponSearch
    {
        private long HashCode;
        private string Name;

        public WeaponSearch(long hashCode, string name)
        {
            HashCode = hashCode;
            Name = name;
        }

        public string GetName() => Name;

        public long GetWeaponHash() => HashCode;
    }

    public enum RarityType
    {
        Common,
        Uncommon,
        Rare,
        Legendary,
        Exotic,
    }

    public enum DamageType
    {
        None, // Not Used
        Kinetic,
        Arc,
        Solar,
        Void,
        Raid, // Not Used
        Stasis,
    }
}
