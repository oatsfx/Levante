using Discord;
using Levante.Configs;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Levante.Util
{
    public class EmblemOffer
    {
        public static List<EmblemOffer> CurrentOffers = new();

        [JsonProperty("EmblemHashCode")]
        public readonly long EmblemHashCode;

        public readonly Emblem OfferedEmblem;

        [JsonProperty("StartDate")]
        public readonly DateTime StartDate;

        [JsonProperty("EndDate")]
        public readonly DateTime? EndDate;

        [JsonProperty("Description")]
        public readonly string Description;

        public readonly string ImageUrl;

        [JsonProperty("SpecialUrl")]
        public readonly string SpecialUrl;

        public readonly bool IsActive;
        public readonly bool IsEnded;

        [JsonConstructor]
        public EmblemOffer(long emblemHashCode, DateTime startDate, DateTime? endDate, string description, string specialUrl = null)
        {
            EmblemHashCode = emblemHashCode;
            OfferedEmblem = new Emblem(EmblemHashCode);
            StartDate = startDate;
            EndDate = endDate;
            Description = description;
            ImageUrl = OfferedEmblem.GetBackgroundUrl();
            SpecialUrl = specialUrl;
            IsActive = DateTime.Now >= startDate;
            IsEnded = DateTime.Now >= endDate;
        }

        public EmbedBuilder BuildEmbed()
        {
            // Appends the word "Soon" to an offer that is not yet available.
            string s = !IsActive ? " Soon" : "";
            string avail = !IsEnded ? $"Available{s}" : "Not Available";
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Emblem {avail}: {OfferedEmblem.GetName()}",
                IconUrl = OfferedEmblem.GetIconUrl(),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {BotConfig.AppName} v{BotConfig.Version}"
            };
            int[] emblemRGB = OfferedEmblem.GetRGBAsIntArray();
            var embed = new EmbedBuilder()
            {
                Color = new Color(emblemRGB[0], emblemRGB[1], emblemRGB[2]),
                Author = auth,
                Footer = foot
            };
            // Final line logic.
            string end = EndDate != null ? $"End{(EndDate > DateTime.Now ? "s" : "ed")} {TimestampTag.FromDateTime((DateTime)EndDate, TimestampTagStyles.Relative)}." : "There is no apparent end to this offer.";
            end = !IsActive ? $"Starts {TimestampTag.FromDateTime(StartDate, TimestampTagStyles.Relative)}." : end;

            embed.Url = OfferedEmblem.GetDECUrl();
            embed.ThumbnailUrl = OfferedEmblem.GetIconUrl();
            embed.ImageUrl = ImageUrl;
            embed.AddField(x =>
            {
                x.Name = "How To Obtain";
                x.Value = $"{Description}{(!String.IsNullOrEmpty(SpecialUrl) ? $"\n[Get {OfferedEmblem.GetName()}]({SpecialUrl})" : "")}";
                x.IsInline = false;
            }).AddField(x =>
            {
                x.Name = "Time Window";
                x.Value = $"{GetDateRange()}\n**{end}**";
                x.IsInline = false;
            });
            return embed;
        }

        public ComponentBuilder BuildExternalButton()
        {
            if (String.IsNullOrEmpty(SpecialUrl))
                return new ComponentBuilder();

            return new ComponentBuilder()
                    .WithButton($"Get {OfferedEmblem.GetName()}", style: ButtonStyle.Link, url: SpecialUrl, emote: Emote.Parse(DestinyEmote.Emblem), row: 0);
        }

        public string GetDateRange()
        {
            var timestampStyle = TimestampTagStyles.ShortDate;
            if (EndDate - StartDate < TimeSpan.FromDays(1))
                timestampStyle = TimestampTagStyles.ShortDateTime;

            return $"{TimestampTag.FromDateTime(StartDate, timestampStyle)} - {(EndDate != null ? $"{TimestampTag.FromDateTime((DateTime)EndDate, timestampStyle)}" : "UNKNOWN")}";
        }

        public static EmblemOffer GetRandomOffer()
        {
            var rng = new Random();
            return CurrentOffers.Count != 0 ? CurrentOffers[rng.Next(0, CurrentOffers.Count)] : null;
        }

        public static EmbedBuilder GetOfferListEmbed(int Page = 0)
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = "List of Available Emblem Offers",
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = "This command only tracks select emblems that are available for a limited time."
            };
            var embed = new EmbedBuilder()
            {
                Color = new Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = auth,
                Footer = foot,
            };

            var count = CurrentOffers.Count;
            var pageNumStr = $"Showing {(10 * Page) + 1}-{((10 * Page) + 10 > count ? count : (10 * Page) + 10)} offers of {count} total.";

            string desc = "";
            if (CurrentOffers.Count != 0)
            {
                foreach (var Offer in CurrentOffers.GetRange(10 * Page, (10 * Page) + 10 > count ? count - (10 * Page) : 10))
                {
                    string s = Offer.SpecialUrl != null
                        ? $"[{Offer.OfferedEmblem.GetName()}]({Offer.SpecialUrl})"
                        : Offer.OfferedEmblem.GetName();
                    desc += $"> {s} [[IMAGE]({Offer.ImageUrl})]\n";
                }
                    
                desc += $"\n*Want specific details? Use the command `/current-offers [EMBLEM NAME]`.*\n\n{pageNumStr}";
            }
            else
                desc = "There are currently no limited time emblem offers; you are all caught up!";

            embed.Description = desc;

            return embed;
        }

        public void CreateJSON()
        {
            string emblemOfferPath = @"Configs/EmblemOffers/" + OfferedEmblem.GetItemHash() + ".json";
            CurrentOffers.Add(this);

            if (!File.Exists(emblemOfferPath))
                File.Create(emblemOfferPath).Close();

            string output = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(emblemOfferPath, output);
            Log.Information("[{Type}] Created Emblem Offer for emblem: {Name} ({Hash}).", "Offers", OfferedEmblem.GetName(), EmblemHashCode);
        }

        public static void LoadCurrentOffers()
        {
            CurrentOffers.Clear();
            string emblemOfferPath = @"Configs/EmblemOffers/";

            foreach (string fileName in Directory.GetFiles(emblemOfferPath).Where(x => x.EndsWith(".json")))
            {
                string json = File.ReadAllText(fileName);
                var offer = JsonConvert.DeserializeObject<EmblemOffer>(json);
                CurrentOffers.Add(offer);
            }
            Log.Information("[{Type}] Loaded {count} Emblem Offers.", "Offers", CurrentOffers.Count);
        }

        public static bool HasExistingOffer(long HashCode)
        {
            foreach (var Offer in CurrentOffers)
                if (Offer.OfferedEmblem.GetItemHash() == HashCode)
                    return true;
            return false;
        }

        public static EmblemOffer GetSpecificOffer(long HashCode)
        {
            foreach (var Offer in CurrentOffers)
                if (Offer.OfferedEmblem.GetItemHash() == HashCode)
                    return Offer;
            return null;
        }

        public static void DeleteOffer(EmblemOffer offerToDelete)
        {
            string emblemOfferPath = @"Configs/EmblemOffers/";
            CurrentOffers.Remove(offerToDelete);
            File.Delete(emblemOfferPath + @"/" + offerToDelete.EmblemHashCode + @".json");
            Log.Information("[{Type}] Deleted Emblem Offer for emblem: {Name} ({Hash}).", "Offers", offerToDelete.OfferedEmblem.GetName(), offerToDelete.EmblemHashCode);
        }

        public static void CheckEmblemOffers()
        {
            foreach (var offer in CurrentOffers.ToList())
                if (DateTime.Now >= offer.EndDate)
                    DeleteOffer(offer);
        }
    }
}
