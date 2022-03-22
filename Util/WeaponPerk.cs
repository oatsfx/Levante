using APIHelper;
using Discord;
using Levante.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

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

        public bool IsEnhanced() => Content.ItemTypeDisplayName.Contains("Enhanced"); 

        public override EmbedBuilder GetEmbed()
        {
            throw new NotImplementedException();
        }
    }
}
