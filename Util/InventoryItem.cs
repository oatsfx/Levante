using Newtonsoft.Json;
﻿using Discord;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using HtmlAgilityPack;

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

        public string GetEmblemUnlock(long emblemId)
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(new WebClient().DownloadString($"https://destinyemblemcollector.com/emblem?id={emblemId}"));
                var emblemUnlock = doc.DocumentNode.SelectNodes("//div[@class='gridemblem-emblemdetail']")[8].InnerHtml;
                return emblemUnlock.Split("<li>")[1].Split("</li>")[0];
            }
            catch
            {
                // lazy catchall, but it just seems to hit internal server error if id isn't found
                return "";
            }
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
