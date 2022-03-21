using Newtonsoft.Json;
﻿using Discord;

namespace Levante.Util
{
    public abstract class InventoryItem
    {
        protected long HashCode { get; set; }
        protected string APIUrl { get; set; }
        protected string Content { get; set; }

        public string GetName()
        {
            dynamic item = JsonConvert.DeserializeObject(Content);
            return item.Response.displayProperties.name;
        }

        public long GetItemHash() => HashCode;

        public long GetCollectableHash()
        {
            dynamic item = JsonConvert.DeserializeObject(Content);
            return item.Response.collectibleHash;
        }

        public string GetIconUrl()
        {
            dynamic item = JsonConvert.DeserializeObject(Content);
            return "https://www.bungie.net" + item.Response.displayProperties.icon;
        }

        public string GetItemType()
        {
            dynamic item = JsonConvert.DeserializeObject(Content);
            return $"{item.Response.itemTypeDisplayName}";
        }

        public string GetSpecificItemType()
        {
            dynamic item = JsonConvert.DeserializeObject(Content);
            return $"{item.Response.itemTypeAndTierDisplayName}";
        }

        public abstract EmbedBuilder GetEmbed();
    }
}
