using Discord;
using BungieSharper.Entities.Destiny.Definitions;
using BungieSharper.Entities.Destiny;
using Levante.Util;

namespace Levante.Structs
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

        public string GetIconUrl() => Content.DisplayProperties.Icon;

        public string GetItemTypeDisplayName() => Content.ItemTypeDisplayName;

        public DestinyItemType GetItemType() => Content.ItemType;

        public string GetSpecificItemType() => Content.ItemTypeAndTierDisplayName;

        public abstract EmbedBuilder GetEmbed();
    }
}
