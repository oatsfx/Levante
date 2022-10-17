using Levante.Configs;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Levante.Helpers;
using System.Linq;

namespace Levante.Util
{
    public class EmblemOffer
    {
        public static List<EmblemOffer> CurrentOffers = new List<EmblemOffer>();

        [JsonProperty("EmblemHashCode")]
        public readonly long EmblemHashCode;

        public readonly Emblem OfferedEmblem;

        [JsonProperty("OfferType")]
        public readonly EmblemOfferType OfferType;

        [JsonProperty("StartDate")]
        public readonly DateTime StartDate;

        [JsonProperty("EndDate")]
        public readonly DateTime? EndDate;

        [JsonProperty("Description")]
        public readonly string Description;

        [JsonProperty("ImageUrl")]
        public readonly string ImageUrl;

        [JsonProperty("SpecialUrl")]
        public readonly string SpecialUrl;

        public readonly bool IsActive;

        [JsonConstructor]
        public EmblemOffer(long emblemHashCode, EmblemOfferType offerType, DateTime startDate, DateTime? endDate, string description, string imageUrl, string specialUrl = null)
        {
            EmblemHashCode = emblemHashCode;
            OfferedEmblem = new Emblem(EmblemHashCode);
            OfferType = offerType;
            StartDate = startDate;
            EndDate = endDate;
            Description = description;
            ImageUrl = imageUrl;
            SpecialUrl = specialUrl;
            if (DateTime.Now < startDate)
                IsActive = false;
            else
                IsActive = true;
        }

        public EmbedBuilder BuildEmbed()
        {
            // Appends the word "Soon" to an offer that is not yet available.
            string s = !IsActive ? " Soon" : "";
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Emblem Available{s}: {OfferedEmblem.GetName()}",
                IconUrl = OfferedEmblem.GetIconUrl(),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {BotConfig.AppName} v{String.Format("{0:0.00#}", BotConfig.Version)}"
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
            
            embed.ThumbnailUrl = OfferedEmblem.GetIconUrl();
            embed.ImageUrl = ImageUrl;
            embed.AddField(x =>
            {
                x.Name = "How To Obtain";
                x.Value = $"{Description} {(SpecialUrl != null ? $"\n[LINK]({SpecialUrl})" : "")}";
                x.IsInline = false;
            }).AddField(x =>
            {
                x.Name = "Offer Type";
                x.Value = GetOfferTypeString(OfferType);
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Hash Code";
                x.Value = $"{EmblemHashCode}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Time Window";
                x.Value = $"{GetDateRange()}\n{end}";
                x.IsInline = false;
            });
            return embed;
        }

        public string GetDateRange() => $"{TimestampTag.FromDateTime(StartDate, TimestampTagStyles.ShortDate)} - {(EndDate != null ? $"{TimestampTag.FromDateTime((DateTime)EndDate, TimestampTagStyles.ShortDate)}" : "UNKNOWN")}";

        public static EmbedBuilder GetRandomOfferEmbed()
        {
            Random rng = new Random();
            if (CurrentOffers.Count != 0)
                return CurrentOffers[rng.Next(0, CurrentOffers.Count)].BuildEmbed();
            else
                return null;
        }

        public static EmbedBuilder GetOfferListEmbed(int Page = 0)
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"List of Available Emblem Offers",
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"This command only tracks emblems that are available for a limited time."
            };
            var embed = new EmbedBuilder()
            {
                Color = new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot
            };

            string desc = $"__Offers ({(10 * Page) + 1}/{((10 * Page) + 10 > CurrentOffers.Count ? CurrentOffers.Count : (10 * Page) + 10)}):__\n";

            if (CurrentOffers.Count != 0)
            {
                foreach (var Offer in CurrentOffers.GetRange(10*Page, (10 * Page) + 10 > CurrentOffers.Count ? CurrentOffers.Count - (10 * Page) : 10))
                    desc += $"> [{Offer.OfferedEmblem.GetName()}]({Offer.SpecialUrl}) [[IMAGE]({Offer.ImageUrl})]\n";
                desc += $"\n*Want specific details? Use the command \"/current-offers [EMBLEM NAME]\".*";
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
            LogHelper.ConsoleLog($"[OFFERS] Created Emblem Offer for emblem: {OfferedEmblem.GetName()} ({EmblemHashCode}).");
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
            LogHelper.ConsoleLog($"[OFFERS] Loaded {CurrentOffers.Count} Emblem Offers.");
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
            LogHelper.ConsoleLog($"[OFFERS] Deleted Emblem Offer for emblem: {offerToDelete.OfferedEmblem.GetName()} ({offerToDelete.EmblemHashCode}).");
        }

        public static string GetOfferTypeString(EmblemOfferType OfferType)
        {
            switch (OfferType)
            {
                case EmblemOfferType.InGame: return "In-Game";
                case EmblemOfferType.BungieStore: return "Bungie Store";
                case EmblemOfferType.BungieRewards: return "Bungie Rewards";
                case EmblemOfferType.Donation: return "Donation";
                case EmblemOfferType.ThirdParty: return "Third Party";
                case EmblemOfferType.Other: return "Other";
                default: return "Emblem Offer Type";
            }
        }
    }

    public enum EmblemOfferType
    {
        InGame,
        BungieStore,
        BungieRewards,
        Donation,
        ThirdParty,
        Other,
    }
}
