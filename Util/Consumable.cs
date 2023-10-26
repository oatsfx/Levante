using APIHelper;
using Discord;
using Levante.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Util
{
    public class Consumable : InventoryItem
    {
        public Consumable(long hashCode)
        {
            HashCode = hashCode;
            APIUrl = $"https://www.bungie.net/platform/Destiny2/Manifest/DestinyInventoryItemDefinition/" + HashCode;

            Content = ManifestConnection.GetInventoryItemById(unchecked((int)hashCode));
        }

        public int GetMaxStackSize() => Content.Inventory.MaxStackSize;

        public override EmbedBuilder GetEmbed()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Consumable Details: {GetName()}",
                IconUrl = GetIconUrl(),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = "Powered by the Bungie API"
            };
            var embed = new EmbedBuilder()
            {
                Author = auth,
                Footer = foot
            };
            try
            {
                int[] embedRGB = DestinyColors.GetColorFromString(GetSpecificItemType());
                embed.WithColor(embedRGB[0], embedRGB[1], embedRGB[2]);

                embed.ThumbnailUrl = GetIconUrl();
                embed.Description = Content.DisplayProperties.Description;

                embed.Description += $"\n### Max per Stack: {GetMaxStackSize():n0}\n";

                embed.Description += Content.Action.ConsumeEntireStack ? $"Upon dismantle, the __entire stack__ of **{GetName()}** will be deleted." : $"Upon dismantle, only __one__ **{GetName()}** will be deleted.";

                embed.Description += Content.Inventory.MaxStackSize == 1 ? " Though, since the max is 1, it doesn't matter." : "";

                embed.AddField(x =>
                {
                    x.Name = "> Information";
                    x.Value = $"\n\nStackable?: {(String.IsNullOrEmpty(Content.Inventory.StackUniqueLabel) ? Emotes.Yes : Emotes.No)}" +
                              $"\nPostmaster?: {(Content.Inventory.RecoveryBucketTypeHash != 0 ? Emotes.Yes : Emotes.No)}" +
                              $"\nVault?: {(!Content.NonTransferrable ? Emotes.Yes : Emotes.No)}";
                    x.IsInline = true;
                });
            }
            catch
            {
                embed.WithColor(new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B));
                embed.Description = "This consumable is missing some API values, sorry about that!";
            }

            return embed;
        }
    }
}
