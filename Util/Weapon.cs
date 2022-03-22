using APIHelper;
using APIHelper.Structs;
using Discord;
using Discord.WebSocket;
using Levante.Configs;
using Levante.Helpers;
using Newtonsoft.Json;
using System;
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

        public WeaponPerk GetFrame() => new WeaponPerk(Content.Sockets.SocketEntries[0].SingleInitialItemHash);

        public PlugSet GetRandomPerks(int Column)
        {
            try
            {
                if (Content.Sockets.SocketEntries[Column].RandomizedPlugSetHash == null)
                    return new PlugSet((long)Content.Sockets.SocketEntries[Column].ReusablePlugSetHash);
                else
                    return new PlugSet((long)Content.Sockets.SocketEntries[Column].RandomizedPlugSetHash);
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
                    $"Intrinsic Trait: {GetFrame().GetName()}\n";
                if (Content.TooltipNotifications.Count() > 0)
                    x.Value += $"Craftable?: {(Content.TooltipNotifications.GetValue(0).ToString().Contains("This weapon's Pattern can be extracted.") ? "Yes" : "No")}";
                else
                    x.Value += "Craftable?: No";
                x.IsInline = false;
            })
            .AddField(x =>
            {
                x.Name = $"> Perks";
                x.Value = $"*List of perks for this weapon, per column.*";
                x.IsInline = false;
            })
            .AddField(x =>
            {
                x.Name = $"Column 1";
                x.Value = $"{GetRandomPerks(1).BuildStringList()}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"Column 2";
                x.Value = $"{GetRandomPerks(2).BuildStringList()}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"Column 3";
                x.Value = $"{GetRandomPerks(3).BuildStringList()}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"Column 4";
                x.Value = $"{GetRandomPerks(4).BuildStringList()}";
                x.IsInline = true;
            });

            if (GetRandomPerks(8) != null && Content.Sockets.SocketEntries[8].SocketTypeHash == 3993098925 /*Weapon Perk Hash*/)
            {
                embed.AddField(x => {
                    x.Name = $"Column 5 (Foundry/Origin)";
                    x.Value = $"{GetRandomPerks(8).BuildStringList()}";
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
