using Levante.Configs;
using Discord;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using Levante.Helpers;

namespace Levante.Util
{
    public class Guardian
    {
        protected string UniqueBungieName;
        protected string MembershipID;
        protected string MembershipType;
        protected string CharacterID;
        protected Emblem Emblem;

        protected string GuardianContent;
        protected string APIUrl;

        public Guardian(string bungieName, string membershipId, string membershipType, string characterId)
        {
            UniqueBungieName = bungieName;
            MembershipID = membershipId;
            MembershipType = membershipType;
            CharacterID = characterId;

            APIUrl = $"https://www.bungie.net/platform/Destiny2/" + MembershipType + "/Profile/" + MembershipID + "/Character/" + CharacterID + "/?components=200,204,205";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync(APIUrl).Result;
                GuardianContent = response.Content.ReadAsStringAsync().Result;
            }
            dynamic item = JsonConvert.DeserializeObject(GuardianContent);
            Emblem = new Emblem((long)item.Response.character.data.emblemHash);
        }

        public Class GetClass()
        {
            dynamic item = JsonConvert.DeserializeObject(GuardianContent);
            return item.Response.character.data.classType;
        }

        public string GetClassEmote()
        {
            switch (GetClass())
            {
                case Class.Titan: return $"{DestinyEmote.Titan}";
                case Class.Hunter: return $"{DestinyEmote.Hunter}";
                case Class.Warlock: return $"{DestinyEmote.Warlock}";
                default:
                    break;
            }
            return null;
        }

        public Race GetRace()
        {
            dynamic item = JsonConvert.DeserializeObject(GuardianContent);
            return item.Response.character.data.raceType;
        }

        public Gender GetGender()
        {
            dynamic item = JsonConvert.DeserializeObject(GuardianContent);
            return item.Response.character.data.genderType;
        }

        public int GetLightLevel()
        {
            dynamic item = JsonConvert.DeserializeObject(GuardianContent);
            return item.Response.character.data.light;
        }

        public Emblem GetEmblem()
        {
            return Emblem;
        }

        public string GetSeal()
        {
            dynamic item1 = JsonConvert.DeserializeObject(GuardianContent);
            if (item1.Response.character.data.titleRecordHash == null)
                return null;
            if (item1.Response.records == null)
                return null;
            long sealHash = (long)item1.Response.character.data.titleRecordHash;
            string sealResult = $"{ManifestHelper.Seals[sealHash]}";

            if (ManifestHelper.GildableSeals.ContainsKey(sealHash))
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);
                    var trackHash = ManifestHelper.GildableSeals[sealHash];
                    var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + MembershipType + "/Profile/" + MembershipID + "/?components=900").Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    dynamic item2 = JsonConvert.DeserializeObject(content);
                    bool isGildedThisSeason = item2.Response.profileRecords.data.records[$"{trackHash}"].objectives[0].complete;
                    if (isGildedThisSeason && item2.Response.profileRecords.data.records[$"{trackHash}"].completedCount != 0)
                        sealResult += $" {DestinyEmote.Gilded}{item2.Response.profileRecords.data.records[$"{trackHash}"].completedCount}";
                    else if (item2.Response.profileRecords.data.records[$"{trackHash}"].completedCount != 0)
                        sealResult += $" {DestinyEmote.GildedPurple}{item2.Response.profileRecords.data.records[$"{trackHash}"].completedCount}";
                }
            }
            return sealResult;
        }

        public EmbedBuilder GetGuardianEmbed()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"{UniqueBungieName}: {GetClass()}",
                IconUrl = GetEmblem().GetIconUrl(),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by the Bungie API"
            };
            int[] emblemRGB = GetEmblem().GetRGBAsIntArray();
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(emblemRGB[0], emblemRGB[1], emblemRGB[2]),
                Author = auth,
                Footer = foot
            };
            var seal = GetSeal();
            embed.Description =
                $"{GetClassEmote()} **{GetRace()} {GetGender()} {GetClass()}** {GetClassEmote()}\n" +
                $"{DestinyEmote.Light}{GetLightLevel()}";
            embed.ThumbnailUrl = GetEmblem().GetIconUrl();

            dynamic item = JsonConvert.DeserializeObject(GuardianContent);
            var wep1 = new Weapon((long)item.Response.equipment.data.items[0].itemHash);
            var wep2 = new Weapon((long)item.Response.equipment.data.items[1].itemHash);
            var wep3 = new Weapon((long)item.Response.equipment.data.items[2].itemHash);
            embed.AddField(x =>
            {
                x.Name = "Weapons";
                x.Value = $"{wep1.GetDamageTypeEmote()} {wep1.GetName()}\n" +
                    $"{wep2.GetDamageTypeEmote()} {wep2.GetName()}\n" +
                    $"{wep3.GetDamageTypeEmote()} {wep3.GetName()}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Armor";
                x.Value = $"{DestinyEmote.Helmet} {new Weapon((long)item.Response.equipment.data.items[3].itemHash).GetName()}\n" +
                    $"{DestinyEmote.Arms} {new Weapon((long)item.Response.equipment.data.items[4].itemHash).GetName()}\n" +
                    $"{DestinyEmote.Chest} {new Weapon((long)item.Response.equipment.data.items[5].itemHash).GetName()}\n" +
                    $"{DestinyEmote.Legs} {new Weapon((long)item.Response.equipment.data.items[6].itemHash).GetName()}\n" +
                    $"{DestinyEmote.Class} {new Weapon((long)item.Response.equipment.data.items[7].itemHash).GetName()}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Stats";
                x.Value = $"{DestinyEmote.Mobility} {item.Response.character.data.stats["2996146975"]}\n" +
                    $"{DestinyEmote.Resilience} {item.Response.character.data.stats["392767087"]}\n" +
                    $"{DestinyEmote.Recovery} {item.Response.character.data.stats["1943323491"]}\n" +
                    $"{DestinyEmote.Discipline} {item.Response.character.data.stats["1735777505"]}\n" +
                    $"{DestinyEmote.Intellect} {item.Response.character.data.stats["144602215"]}\n" +
                    $"{DestinyEmote.Strength} {item.Response.character.data.stats["4244567218"]}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Emblem";
                x.Value = $"[{GetEmblem().GetName()}]({GetEmblem().GetDECUrl()})";
                x.IsInline = true;
            });

            if (seal != null)
            {
                embed.AddField(x =>
                {
                    x.Name = "Title";
                    x.Value = $"{seal}";
                    x.IsInline = true;
                });
            }

            return embed;
        }

        public enum Class
        {
            Titan,
            Hunter,
            Warlock
        }

        public enum Race
        {
            Human,
            Awoken,
            Exo
        }

        public enum Gender
        {
            Male,
            Female
        }

        public enum Platform
        {
            None, // Not Used
            Xbox,
            PSN,
            Steam,
            Blizzard, // Not Used
            Stadia,
        }
    }
}
