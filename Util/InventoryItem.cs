using Discord;
using APIHelper.Structs;
using Levante.Configs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using BungieSharper.Entities.Destiny.Definitions;

namespace Levante.Util
{
    public abstract class InventoryItem
    {
        protected long HashCode { get; set; }
        protected string APIUrl { get; set; }
        protected DestinyInventoryItemDefinition Content { get; set; }

        public string GetName() => Content.DisplayProperties.Name;

        public string GetFlavorText() => Content.FlavorText;

        public long GetItemHash() => HashCode;

        public uint? GetCollectableHash() => Content.CollectibleHash;

        public string GetIconUrl() => "https://www.bungie.net" + Content.DisplayProperties.Icon;

        public string GetItemType() => Content.ItemTypeDisplayName;

        public string GetSpecificItemType() => Content.ItemTypeAndTierDisplayName;

        public abstract EmbedBuilder GetEmbed();
    }
}
