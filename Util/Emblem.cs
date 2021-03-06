using APIHelper;
using Discord;
using Levante.Configs;
using Levante.Helpers;
using Newtonsoft.Json;
using System.Net.Http;
using HtmlAgilityPack;
using System.Net;

namespace Levante.Util
{
    public class Emblem : InventoryItem
    {
        public Emblem(long hashCode)
        {
            HashCode = hashCode;
            APIUrl = $"https://www.bungie.net/platform/Destiny2/Manifest/DestinyInventoryItemDefinition/" + HashCode;

            Content = ManifestConnection.GetInventoryItemById(unchecked((int)hashCode));
        }

        public int[] GetRGBAsIntArray()
        {
            int[] result = new int[3];
            result[0] = (int)Content.BackgroundColor.Red;
            result[1] = (int)Content.BackgroundColor.Green;
            result[2] = (int)Content.BackgroundColor.Blue;
            return result;
        }

        public string GetSourceString()
        {
            try
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
            catch
            {
                return "";
            }
        }

        public string GetBackgroundUrl() => "https://www.bungie.net" + Content.SecondaryIcon;

        public static bool HashIsAnEmblem(long HashCode)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/Manifest/DestinyInventoryItemDefinition/" + HashCode).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);
                string displayType = $"{item.Response.itemTypeDisplayName}";
                if (!displayType.Equals($"Emblem"))
                    return false;
                else
                    return true;
            }
        }

        // Use DEC's information on how to unlock an emblem, if DEC has it in their data.
        public string GetEmblemUnlock()
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(new WebClient().DownloadString($"https://destinyemblemcollector.com/emblem?id={HashCode}"));
                var emblemUnlock = doc.DocumentNode.SelectNodes("//div[@class='gridemblem-emblemdetail']")[8].InnerHtml;
                return emblemUnlock.Split("<li>")[1].Split("</li>")[0];
            }
            catch
            {
                // lazy catchall, but it just seems to hit internal server error if id isn't found
                return "";
            }
        }

        public override EmbedBuilder GetEmbed()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Emblem Details: {GetName()}",
                IconUrl = GetIconUrl(),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by the Bungie API"
            };
            int[] emblemRGB = GetRGBAsIntArray();
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(emblemRGB[0], emblemRGB[1], emblemRGB[2]),
                Author = auth,
                Footer = foot
            };
            try
            {
                var unlock = GetEmblemUnlock();
                var source = string.IsNullOrEmpty(unlock) ? "" : $"[Unlock (DEC)](https://destinyemblemcollector.com/emblem?id={GetItemHash()}): {unlock}\n";

                embed.Description = (GetSourceString().Equals("") ? "No source data provided." : GetSourceString()) + "\n" +
                        $"Hash Code: {GetItemHash()}\n" +
                        $"Collectible Hash: {GetCollectableHash()}\n" +
                        $"{source}";
                embed.ImageUrl = GetBackgroundUrl();
                embed.ThumbnailUrl = GetIconUrl();
            }
            catch
            {
                embed.Description = "This emblem is missing some API values, sorry about that!";
            }
            
            return embed;
        }
    }

    public class EmblemSearch
    {
        private long HashCode;
        private string Name;

        public EmblemSearch(long hashCode, string name)
        {
            HashCode = hashCode;
            Name = name;
        }

        public string GetName() => Name;

        public long GetEmblemHash() => HashCode;
    }
}
