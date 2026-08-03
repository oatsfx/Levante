using Discord;
using Levante.Helpers;
using Newtonsoft.Json;
using System.Net.Http;
using HtmlAgilityPack;
using System.Net;
using System;
using Levante.Services;
using Levante.Configs;
using Levante.Util;
using System.Threading.Tasks;

namespace Levante.Structs
{
    public class Emblem : InventoryItem
    {
        public Emblem(long hashCode)
        {
            HashCode = hashCode;
            APIUrl = $"/Platform/Destiny2/ManifestHelper/DestinyInventoryItemDefinition/{HashCode}";

            Content = ManifestHelper.GetInventoryItemById(unchecked((int)hashCode));
        }

        public int[] GetRGBAsIntArray()
        {
            int[] result = new int[3];
            if (Content.BackgroundColor == null)
            {
                result[0] = AppConfig.Discord.EmbedColor.R;
                result[1] = AppConfig.Discord.EmbedColor.G;
                result[2] = AppConfig.Discord.EmbedColor.B;
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
                var content = Task.Run(() => BungieApi.Query($"/Platform/Destiny2/Manifest/DestinyCollectibleDefinition/{GetCollectableHash()}")).Result;
                dynamic item = JsonConvert.DeserializeObject(content);
                if ($"{item.Response.displayProperties.name}".Equals("Classified"))
                    return "Source: Classified. Keep it secret. Keep it safe.";
                else
                    return item.Response.sourceString;
            }
            catch
            {
                return "";
            }
        }

        public string GetBackgroundUrl() => BungieApi.API_BASE_URL + Content.SecondaryIcon;

        public string GetWideBackgroundUrl() => BungieApi.API_BASE_URL + Content.SecondarySpecial;

        // Use DEC's information on how to unlock an emblem, if DEC has it in their data.
        public string GetEmblemUnlock()
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(new HttpClient().GetStringAsync(GetDECUrl()).Result);
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

        public override EmbedBuilder GetEmbed() => GetEmbed(false);

        public EmbedBuilder GetEmbed(bool showWideBackground)
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Emblem Details: {GetName()}",
                IconUrl = BungieApi.API_BASE_URL + GetIconUrl(),
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
                    offerStr = offer.StartDate > DateTime.Now
                        ? $"This emblem is {(string.IsNullOrEmpty(offer.SpecialUrl) ? "available" : $"[available]({offer.SpecialUrl})")} {TimestampTag.FromDateTime(offer.StartDate, TimestampTagStyles.Relative)}!\n"
                        : $"This emblem is {(string.IsNullOrEmpty(offer.SpecialUrl) ? "currently available" : $"[currently available]({offer.SpecialUrl})")}!\n";
                }

                if (AppConfig.UniversalCodes.Exists(x => x.Name.Equals(GetName())))
                {
                    var uniCode = AppConfig.UniversalCodes.Find(x => x.Name.Equals(GetName()));
                    offerStr = $"This emblem is available via a code: {uniCode.Code}.\nRedeem it [here]({BungieApi.API_BASE_URL}/7/en/Codes/Redeem).";
                }

                var sourceStr = GetSourceString();
                embed.Description = (sourceStr.Equals("") ? "No source data provided." : sourceStr) + "\n";
                embed.ImageUrl = showWideBackground ? BungieApi.API_BASE_URL + GetWideBackgroundUrl() : BungieApi.API_BASE_URL + GetBackgroundUrl();
                embed.ThumbnailUrl = BungieApi.API_BASE_URL + GetIconUrl();

                embed.AddField(x =>
                {
                    x.Name = "Availability";
                    x.Value = $"{offerStr}";
                    x.IsInline = false;
                });

                try
                {
                    var acquisition = new EmblemReport((long)GetCollectableHash());

                    embed.AddField(x =>
                    {
                        x.Name = "Redeemed";
                        x.Value = $"> {acquisition.Data.Find(x => x.CollectibleHash == (long)GetCollectableHash()).Acquisition:n0}";
                        x.IsInline = true;
                    }).AddField(x =>
                    {
                        x.Name = "Rarity";
                        x.Value = $"> {acquisition.Data.Find(x => x.CollectibleHash == (long)GetCollectableHash()).Percentage}%";
                        x.IsInline = true;
                    });
                }
                catch
                {
                    embed.AddField(x =>
                    {
                        x.Name = "Error";
                        x.Value = "emblem.report returned no values.";
                        x.IsInline = false;
                    });
                }


                if (!string.IsNullOrEmpty(unlock))
                {
                    embed.AddField(x =>
                    {
                        x.Name = "Unlock";
                        x.Value = $"[DEC](https://destinyemblemcollector.com/emblem?id={GetItemHash()}): {System.Web.HttpUtility.HtmlDecode(unlock)}";
                        x.IsInline = false;
                    });
                }

                embed.AddField(x =>
                {
                    x.Name = "Other Sources";
                    x.Value =
                        $"[Destiny Emblem Collector](https://destinyemblemcollector.com/emblem?id={GetItemHash()}) | " +
                        $"[emblem.report](https://emblem.report/{GetItemHash()})";
                    x.IsInline = false;
                });
            }
            catch
            {
                embed.WithColor(new Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B));
                embed.Description = "This emblem is missing some API values, sorry about that!";
            }

            return embed;
        }
    }
}
