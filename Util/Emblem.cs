using APIHelper;
using Discord;
using Levante.Configs;
using Levante.Helpers;
using Newtonsoft.Json;
using System.Net.Http;
using HtmlAgilityPack;
using System.Net;
using System;

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
            if (Content.BackgroundColor == null)
            {
                result[0] = BotConfig.EmbedColorGroup.R;
                result[1] = BotConfig.EmbedColorGroup.G;
                result[2] = BotConfig.EmbedColorGroup.B;
            }
            else
            {
                result[0] = (int)Content.BackgroundColor.Red;
                result[1] = (int)Content.BackgroundColor.Green;
                result[2] = (int)Content.BackgroundColor.Blue;
            }
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
                    if ($"{item.Response.displayProperties.name}".Equals("Classified"))
                        return "Source: Classified. Keep it secret. Keep it safe.";
                    else
                        return item.Response.sourceString;
                }
            }
            catch
            {
                return "";
            }
        }

        public string GetBackgroundUrl() => "https://www.bungie.net" + Content.SecondaryIcon;

        // Use DEC's information on how to unlock an emblem, if DEC has it in their data.
        public string GetEmblemUnlock()
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(new HttpClient().GetStringAsync($"https://destinyemblemcollector.com/emblem?id={HashCode}").Result);
                var emblemUnlock = doc.DocumentNode.SelectNodes("//div[@class='gridemblem-emblemdetail']")[8].InnerHtml;
                return emblemUnlock.Split("<li>")[1].Split("</li>")[0];
            }
            catch
            {
                // lazy catchall, but it just seems to hit internal server error if id isn't found
                return "";
            }
        }

        public string GetDECUrl() => $"https://destinyemblemcollector.com/emblem?id={GetItemHash()}";

        public override EmbedBuilder GetEmbed()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Emblem Details: {GetName()}",
                IconUrl = GetIconUrl(),
                Url = GetDECUrl(),
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
                int[] emblemRGB = GetRGBAsIntArray();
                embed.WithColor(emblemRGB[0], emblemRGB[1], emblemRGB[2]);
                var unlock = GetEmblemUnlock();
                string offerStr = "Unavailable or is not a limited-time offer.";
                if (EmblemOffer.HasExistingOffer(GetItemHash()))
                {
                    var offer = EmblemOffer.GetSpecificOffer(GetItemHash());
                    if (offer.StartDate > DateTime.Now)
                        offerStr = $"This emblem is [available]({offer.SpecialUrl}) {TimestampTag.FromDateTime(offer.StartDate, TimestampTagStyles.Relative)}!\n";
                    else
                        offerStr = $"This emblem is [currently available]({offer.SpecialUrl})!\n";
                }
                
                if (BotConfig.UniversalCodes.Exists(x => x.Name.Equals(GetName())))
                {
                    var uniCode = BotConfig.UniversalCodes.Find(x => x.Name.Equals(GetName()));
                    offerStr = $"This emblem is available via a code: {uniCode.Code}.\nRedeem it [here](https://www.bungie.net/7/en/Codes/Redeem).";
                }

                var sourceStr = GetSourceString();
                embed.Description = (sourceStr.Equals("") ? "No source data provided." : sourceStr) + "\n";
                embed.ImageUrl = GetBackgroundUrl();
                embed.ThumbnailUrl = GetIconUrl();

                embed.AddField(x =>
                {
                    x.Name = "Hash Code";
                    x.Value = $"{GetItemHash()}";
                    x.IsInline = true;
                }).AddField(x =>
                {
                    x.Name = "Collectible Hash";
                    x.Value = $"{GetCollectableHash()}";
                    x.IsInline = true;
                })
                .AddField(x =>
                {
                    x.Name = "Availbility";
                    x.Value = $"{offerStr}";
                    x.IsInline = false;
                });

                if (!string.IsNullOrEmpty(unlock))
                {
                    embed.AddField(x =>
                    {
                        x.Name = "Unlock";
                        x.Value = $"[DEC](https://destinyemblemcollector.com/emblem?id={GetItemHash()}): {System.Web.HttpUtility.HtmlDecode(unlock)}";
                        x.IsInline = false;
                    });
                }
            }
            catch
            {
                embed.WithColor(new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B));
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
