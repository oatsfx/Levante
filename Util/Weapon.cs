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
        private readonly bool IsCraftable;

        public Weapon(long hashCode)
        {
            HashCode = hashCode;
            APIUrl = $"https://www.bungie.net/platform/Destiny2/Manifest/DestinyInventoryItemDefinition/" + HashCode;

            Content = ManifestConnection.GetInventoryItemById(unchecked((int)hashCode));
            IsCraftable = (Content.Inventory != null && Content.Inventory.RecipeItemHash != null) || Content.ItemType == BungieSharper.Entities.Destiny.DestinyItemType.Pattern;
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
                if ($"{item.Response.displayProperties.name}".Equals("Classified"))
                    return "Source: Classified. Keep it secret. Keep it safe.";
                else
                    return item.Response.sourceString;
            }
        }

        public string GetDamageType() => $"{GetDamageTypeEmote()} {(DamageType)Content.DefaultDamageType}";

        public string GetDamageTypeEmote() => DestinyEmote.MatchEmote($"{(DamageType)Content.DefaultDamageType}");

        public WeaponPerk GetIntrinsic() => new WeaponPerk(Content.Sockets.SocketEntries.ElementAt(0).SingleInitialItemHash);

        public PlugSet GetRandomPerks(int Column /*This parameter is the desired column for weapon perks.*/)
        {
            try
            {
                List<int> perkIndexes = new List<int>();
                for (int i = 0; i < Content.Sockets.SocketCategories.Count(); i++)
                {
                    if (Content.Sockets.SocketCategories.ElementAt(i).SocketCategoryHash == 4241085061 ||
                        Content.Sockets.SocketCategories.ElementAt(i).SocketCategoryHash == 3410521964)
                        for (int j = 0; j < Content.Sockets.SocketCategories.ElementAt(i).SocketIndexes.Count(); j++)
                            perkIndexes.Add(Convert.ToInt32(Content.Sockets.SocketCategories.ElementAt(i).SocketIndexes.ElementAt(j)));
                }

                if (Column > perkIndexes.Count)
                    return null;

                if (Content.Sockets.SocketEntries.ElementAt(perkIndexes[Column - 1]).PreventInitializationOnVendorPurchase)
                    return null;

                if (Content.Sockets.SocketEntries.ElementAt(perkIndexes[Column - 1]).RandomizedPlugSetHash == null)
                    return new PlugSet((long)Content.Sockets.SocketEntries.ElementAt(perkIndexes[Column - 1]).ReusablePlugSetHash, IsCraftable);
                else
                    return new PlugSet((long)Content.Sockets.SocketEntries.ElementAt(perkIndexes[Column - 1]).RandomizedPlugSetHash, IsCraftable);
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
                x.Value += $"{DestinyEmote.Pattern}Craftable?: {(IsCraftable ? "Yes" : "No")}";
                x.IsInline = false;
            }).AddField(x =>
            {
                x.Name = $"> Perks";
                x.Value = $"*List of perks for this weapon, per column.*";
                x.IsInline = false;
            });

            if (IsCraftable)
            {
                var recipeDef = new Weapon((long)Content.Inventory.RecipeItemHash);
                var plug1 = recipeDef.GetRandomPerks(1);
                var plug2 = recipeDef.GetRandomPerks(2);
                var plug3 = recipeDef.GetRandomPerks(3);
                var plug4 = recipeDef.GetRandomPerks(4);
                var plug5 = recipeDef.GetRandomPerks(5);

                embed.AddField(x =>
                {
                    x.Name = "Intrinsic";
                    x.Value = $"{(plug1 == null ? "No intrinsic." : plug1.BuildStringList(false))}";
                    x.IsInline = false;
                }).AddField(x =>
                {
                    x.Name = $"Column 1";
                    x.Value = $"{(plug2 == null ? "No perks." : plug2.BuildStringList())}";
                    x.IsInline = true;
                }).AddField(x =>
                {
                    x.Name = $"Column 2";
                    x.Value = $"{(plug3 == null ? "No perks." : plug3.BuildStringList())}";
                    x.IsInline = true;
                }).AddField("\u200b", '\u200b').AddField(x =>
                {
                    x.Name = $"Column 3";
                    x.Value = $"{(plug4 == null ? "No perks." : plug4.BuildStringList())}";
                    x.IsInline = true;
                }).AddField(x =>
                {
                    x.Name = $"Column 4";
                    x.Value = $"{(plug5 == null ? "No perks." : plug5.BuildStringList())}";
                    x.IsInline = true;
                });
            }
            else
            {
                var plug1 = GetRandomPerks(1);
                var plug2 = GetRandomPerks(2);
                var plug3 = GetRandomPerks(3);
                var plug4 = GetRandomPerks(4);

                embed.AddField(x =>
                {
                    x.Name = "Intrinsic";
                    x.Value = $"{GetIntrinsic().GetName()}";
                    x.IsInline = false;
                }).AddField(x =>
                {
                    x.Name = $"Column 1";
                    x.Value = $"{(GetRandomPerks(1) == null ? "No perks." : GetRandomPerks(1).BuildStringList())}";
                    x.IsInline = true;
                }).AddField(x =>
                {
                    x.Name = $"Column 2";
                    x.Value = $"{(GetRandomPerks(2) == null ? "No perks." : GetRandomPerks(2).BuildStringList())}";
                    x.IsInline = true;
                }).AddField("\u200b", '\u200b').AddField(x =>
                {
                    x.Name = $"Column 3";
                    x.Value = $"{(GetRandomPerks(3) == null ? "No perks." : GetRandomPerks(3).BuildStringList())}";
                    x.IsInline = true;
                }).AddField(x =>
                {
                    x.Name = $"Column 4";
                    x.Value = $"{(GetRandomPerks(4) == null ? "No perks." : GetRandomPerks(4).BuildStringList())}";
                    x.IsInline = true;
                });
            }

            if (GetFoundryPerks() != null)
            {
                embed.AddField(x => {
                    x.Name = $"Column 5 (Foundry/Origin)";
                    x.Value = $"{GetFoundryPerks().BuildStringList()}";
                    x.IsInline = false;
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
