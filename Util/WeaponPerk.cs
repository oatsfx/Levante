using APIHelper;
using BungieSharper.Entities.Destiny.Definitions;
using Discord;
using Levante.Configs;
using Levante.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Levante.Util
{
    public class WeaponPerk : InventoryItem
    {
        public WeaponPerk(long hashCode)
        {
            HashCode = hashCode;
            APIUrl = $"https://www.bungie.net/platform/Destiny2/Manifest/DestinyInventoryItemDefinition/" + HashCode;

            Content = ManifestConnection.GetInventoryItemById(unchecked((int)hashCode));
        }

        public string GetDescription() => Content.DisplayProperties.Description;

        public bool IsEnhanced() => Content.ItemTypeDisplayName.Contains("Enhanced");

        public IEnumerable<DestinyItemPerkEntryDefinition> GetSandboxPerks() => Content.Perks;

        public override EmbedBuilder GetEmbed()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Weapon Perk: {GetName()}",
                IconUrl = GetIconUrl(),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by the Bungie API"
            };
            var embed = new EmbedBuilder()
            {
                Author = auth,
                Footer = foot
            };
            try
            {
                embed.WithColor(254, 254, 254);

                embed.ThumbnailUrl = GetIconUrl();

                embed.AddField(x =>
                {
                    x.Name = "Base";
                    x.Value = $"{GetDescription()}";
                    x.IsInline = false;
                });

                var insightHash = HashCode;

                if (ManifestHelper.EnhancedPerks.ContainsValue(GetName()))
                {
                    var enhanced = new WeaponPerk(ManifestHelper.EnhancedPerks.FirstOrDefault(x => x.Value.Equals(GetName())).Key);
                    embed.AddField(x =>
                    {
                        x.Name = $"Enhanced {DestinyEmote.Enhanced}";
                        x.Value = $"{enhanced.GetDescription()}";
                        x.IsInline = false;
                    });

                    insightHash = enhanced.HashCode;
                }

                if (ManifestHelper.ClarityDescriptions.ContainsKey(insightHash))
                {
                    embed.AddField(x =>
                    {
                        x.Name = "Community Insight";
                        x.Value = $"[Clarity](https://www.d2clarity.com/):\n{ManifestHelper.ClarityDescriptions[insightHash]}";
                        x.IsInline = false;
                    });
                }
            }
            catch
            {
                embed.WithColor(new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B));
                embed.Description = "This perk is missing some API values, sorry about that!";
            }

            return embed;
        }
    }
}
