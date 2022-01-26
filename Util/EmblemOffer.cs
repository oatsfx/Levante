using DestinyUtility.Configs;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DestinyUtility.Util
{
    public class EmblemOffer
    {
        public static List<EmblemOffer> CurrentOffers = new List<EmblemOffer>();

        [JsonProperty("OfferedEmblem")]
        public readonly Emblem OfferedEmblem;

        [JsonProperty("OfferType")]
        public readonly EmblemOfferType OfferType;

        [JsonProperty("StartDate")]
        public readonly DateTime StartDate;

        [JsonProperty("EndDate")]
        public readonly DateTime EndDate;

        [JsonProperty("Description")]
        public readonly string Description;

        [JsonProperty("ImageUrl")]
        public readonly string ImageUrl;

        [JsonProperty("SpecialUrl")]
        public readonly string SpecialUrl;

        public EmblemOffer(long HashCode, EmblemOfferType offerType, DateTime startDate, DateTime endDate, string desc, string imageUrl, string specialUrl = null)
        {
            OfferedEmblem = new Emblem(HashCode);
            OfferType = offerType;
            StartDate = startDate;
            EndDate = endDate;
            Description = desc;
            ImageUrl = imageUrl;
            SpecialUrl = specialUrl;

            CurrentOffers.Add(this);
            CreateJSON(this);
        }

        public EmblemOffer(Emblem emblem, EmblemOfferType offerType, DateTime startDate, DateTime endDate, string desc, string imageUrl, string specialUrl = null)
        {
            OfferedEmblem = emblem;
            OfferType = offerType;
            StartDate = startDate;
            EndDate = endDate;
            Description = desc;
            ImageUrl = imageUrl;
            SpecialUrl = specialUrl;

            CurrentOffers.Add(this);
            CreateJSON(this);
        }

        public EmbedBuilder BuildEmbed()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Emblem Available: {OfferedEmblem.GetName()}",
                IconUrl = OfferedEmblem.GetIconUrl(),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by @OatsFX"
            };
            int[] emblemRGB = OfferedEmblem.GetRGBAsIntArray();
            var embed = new EmbedBuilder()
            {
                Color = new Color(emblemRGB[0], emblemRGB[1], emblemRGB[2]),
                Author = auth,
                Footer = foot
            };
            embed.Description =
                $"__How to get this emblem:__\n" +
                $"{Description} {(SpecialUrl != null ? $"\n[LINK]({SpecialUrl})" : "")}\n" +
                $"Time Window (M/D/Y): **{GetDateRange()}**\n";
            embed.ThumbnailUrl = OfferedEmblem.GetIconUrl();
            embed.ImageUrl = ImageUrl;
            return embed;
        }

        public string GetDateRange() => $"{TimestampTag.FromDateTime(StartDate)} - {TimestampTag.FromDateTime(EndDate)}";

        public static EmbedBuilder GetRandomOfferEmbed()
        {
            Random rng = new Random();
            if (CurrentOffers.Count != 0)
                return CurrentOffers[rng.Next(0, CurrentOffers.Count)].BuildEmbed();
            else
                return null;
        }

        public static EmbedBuilder GetOfferListEmbed()
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

            string desc = $"__Offers:__\n";

            if (CurrentOffers.Count != 0)
            {
                foreach (var Offer in CurrentOffers)
                    desc += $"> [{Offer.OfferedEmblem.GetName()}]({Offer.ImageUrl}) ({Offer.OfferedEmblem.GetItemHash()})\n";
                desc += $"\n*Want specific details? Use the command '/emblem-offers [HASH CODE]'.*";
            }
            else
                desc = "There are currently no limited time emblem offers; you are all caught up!";

            embed.Description = desc;

            return embed;
        }

        public void DeleteEmblemOffer(long emblemHash)
        {
            foreach (var Offer in CurrentOffers)
                if (Offer.OfferedEmblem.GetItemHash() == emblemHash)
                    CurrentOffers.Remove(Offer);
        }

        public void CreateJSON(EmblemOffer offer)
        {
            string emblemOfferPath = @"Configs/EmblemOffers/" + offer.OfferedEmblem.GetItemHash() + ".json";

            if (!File.Exists(emblemOfferPath))
                File.Create(emblemOfferPath).Close();

            string output = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(emblemOfferPath, output);
        }

        public static void LoadCurrentOffers()
        {
            CurrentOffers.Clear();
            string emblemOfferPath = @"Configs/CurrentEmblemOffers/";

            foreach (string fileName in Directory.GetFiles(emblemOfferPath))
            {
                string json = File.ReadAllText(fileName);
                CurrentOffers.Add(JsonConvert.DeserializeObject<EmblemOffer>(json));
            }
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

        public static void DeleteOffer(long HashCode)
        {
            foreach (var Offer in CurrentOffers)
                if (Offer.OfferedEmblem.GetItemHash() == HashCode)
                    DeleteOffer(Offer);
        }

        public static void DeleteOffer(EmblemOffer offerToDelete)
        {
            string emblemOfferPath = @"Configs/CurrentEmblemOffers/";
            CurrentOffers.Remove(offerToDelete);
            File.Delete(emblemOfferPath + @"/" + offerToDelete.OfferedEmblem.GetItemHash() + @".txt");
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
