using Discord;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System;
using BungieSharper.Entities.Destiny;
using System.Linq;
using Levante.Services;
using Levante.Configs;
using Levante.Helpers;
using Levante.Util;
using System.Threading.Tasks;

namespace Levante.Structs
{
    public class Guardian
    {
        private User _user;

        protected string UniqueBungieName;
        protected string MembershipId;
        protected string MembershipType;
        protected string CharacterID;
        public readonly Emblem Emblem;

        public readonly DestinyRace Race;
        public readonly DestinyGender Gender;
        public readonly DestinyClass Class;
        public readonly int LightLevel;
        public readonly string SealDiscordString;
        // This is zero indexed.
        public readonly int Rank;
        public readonly int RankProgress;
        public readonly int RankCompletion;
        public readonly int CommendationTotal;

        public readonly int Mobility;
        public readonly int Resilience;
        public readonly int Recovery;
        public readonly int Discipline;
        public readonly int Intellect;
        public readonly int Strength;

        public readonly InstancedWeapon Kinetic;
        public readonly InstancedWeapon Energy;
        public readonly InstancedWeapon Heavy;

        public readonly Weapon Helmet;
        public readonly Weapon Arms;
        public readonly Weapon Chest;
        public readonly Weapon Legs;
        public readonly Weapon ClassItem;

        public readonly long ActivityHash = -1;
        public readonly DateTime ActivityStarted;

        protected string GuardianContent;
        protected string APIUrl;

        public Guardian(string bungieName, string membershipId, string membershipType, string characterId, User linkedUser = null)
        {
            UniqueBungieName = bungieName;
            MembershipId = membershipId;
            MembershipType = membershipType;
            CharacterID = characterId;

            _user = linkedUser;

            APIUrl = $"/Platform/Destiny2/{MembershipType}/Profile/{MembershipId}/Character/{CharacterID}/?components=200,204,205";
            GuardianContent = Task.Run(() => BungieApi.Query(APIUrl)).Result;

            dynamic item = JsonConvert.DeserializeObject(GuardianContent);
            Emblem = new Emblem((long)item.Response.character.data.emblemHash);
            Race = item.Response.character.data.raceType;
            Class = item.Response.character.data.classType;
            Gender = item.Response.character.data.genderType;
            LightLevel = item.Response.character.data.light;

            Mobility = item.Response.character.data.stats["2996146975"];
            Resilience = item.Response.character.data.stats["392767087"];
            Recovery = item.Response.character.data.stats["1943323491"];
            Discipline = item.Response.character.data.stats["1735777505"];
            Intellect = item.Response.character.data.stats["144602215"];
            Strength = item.Response.character.data.stats["4244567218"];

            Kinetic = new InstancedWeapon((long)item.Response.equipment.data.items[0].itemHash, membershipId, membershipType, (string)item.Response.equipment.data.items[0].itemInstanceId, linkedUser);
            Energy = new InstancedWeapon((long)item.Response.equipment.data.items[1].itemHash, membershipId, membershipType, (string)item.Response.equipment.data.items[1].itemInstanceId, linkedUser);
            Heavy = new InstancedWeapon((long)item.Response.equipment.data.items[2].itemHash, membershipId, membershipType, (string)item.Response.equipment.data.items[2].itemInstanceId, linkedUser);

            Helmet = new Weapon((long)item.Response.equipment.data.items[3].itemHash);
            Arms = new Weapon((long)item.Response.equipment.data.items[4].itemHash);
            Chest = new Weapon((long)item.Response.equipment.data.items[5].itemHash);
            Legs = new Weapon((long)item.Response.equipment.data.items[6].itemHash);
            ClassItem = new Weapon((long)item.Response.equipment.data.items[7].itemHash);

            if (item.Response.activities.data != null && item.Response.activities.data.currentActivityHash != 0)
            {
                ActivityHash = (long)item.Response.activities.data.currentActivityHash;
                ActivityStarted = DateTime.SpecifyKind(DateTime.Parse($"{item.Response.activities.data.dateActivityStarted}"), DateTimeKind.Utc);
            }

            var token = linkedUser?.BungieCredentials.AccessToken;
            var content = Task.Run(() => BungieApi.Query($"/Platform/Destiny2/{MembershipType}/Profile/{MembershipId}/?components=700,900,1400", token)).Result;
            dynamic item1 = JsonConvert.DeserializeObject(content);

            try
            {
                CommendationTotal = item1.Response.profileCommendations.data.totalScore;
            }
            catch
            {
                CommendationTotal = -1;
            }

            try
            {
                foreach (var rank in ManifestHelper.GuardianRanks)
                {
                    var rankObj = item1.Response.profilePresentationNodes.data.nodes[$"{rank.Key}"];
                    if ((int)rankObj.progressValue < (int)rankObj.completionValue)
                    {
                        RankProgress = (int)rankObj.progressValue;
                        RankCompletion = (int)rankObj.completionValue;
                        break;
                    }
                    Rank++;
                }
            }
            catch
            {
                Rank = -1;
            }


            if (item.Response.character.data.titleRecordHash != null)
            {
                long sealHash = (long)item.Response.character.data.titleRecordHash;
                SealDiscordString = $"{ManifestHelper.Seals[sealHash]}";

                if (ManifestHelper.GildableSeals.ContainsKey(sealHash))
                {
                    var trackHash = ManifestHelper.GildableSeals[sealHash];
                    if (item1.Response.profileRecords.data != null)
                    {
                        bool isGildedThisSeason = item1.Response.profileRecords.data.records[$"{trackHash}"].objectives[0].complete;
                        if (isGildedThisSeason && item1.Response.profileRecords.data.records[$"{trackHash}"].completedCount != 0)
                            SealDiscordString += $" {EmoteConfig.GetEmote("Gilded")}{item1.Response.profileRecords.data.records[$"{trackHash}"].completedCount}";
                        else if (item1.Response.profileRecords.data.records[$"{trackHash}"].completedCount != 0)
                            SealDiscordString += $" {EmoteConfig.GetEmote("GildedPurple")}{item1.Response.profileRecords.data.records[$"{trackHash}"].completedCount}";
                    }
                }
            }
        }

        public string GetClassEmote()
        {
            return Class switch
            {
                DestinyClass.Titan => $"{EmoteConfig.GetEmote("Titan")}",
                DestinyClass.Hunter => $"{EmoteConfig.GetEmote("Hunter")}",
                DestinyClass.Warlock => $"{EmoteConfig.GetEmote("Warlock")}",
                _ => "",
            };
        }

        public EmbedBuilder GetGuardianEmbed()
        {
            string badge = "";

            if (_user.IsDeveloper())
                badge = $"\n{EmoteConfig.GetEmote("LevanteDev")} {AppConfig.App.Name} Developer";
            else if (_user.IsStaff())
                badge = $"\n{EmoteConfig.GetEmote("LevanteStaff")} {AppConfig.App.Name} Staff";
            else if (_user.IsSupporter())
                badge = $"\n{EmoteConfig.GetEmote("LevanteSupporter")} {AppConfig.App.Name} Supporter";

            var auth = new EmbedAuthorBuilder()
            {
                Name = $"{UniqueBungieName}: {Class}",
                IconUrl = Emblem.GetIconUrl(),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by the Bungie API"
            };
            int[] emblemRGB = Emblem.GetRGBAsIntArray();
            var embed = new EmbedBuilder
            {
                Color = new Color(emblemRGB[0], emblemRGB[1], emblemRGB[2]),
                Author = auth,
                Footer = foot,
                Description =
                    $"{GetClassEmote()} **{Race} {Gender} {Class}** {GetClassEmote()}\n" +
                    $"{EmoteConfig.GetEmote("LightLevel")}{LightLevel}",
                ThumbnailUrl = Emblem.GetIconUrl()
            };

            if (Rank >= 0)
                embed.Description += $" {EmoteConfig.GetEmote("GuardianRank")}{Rank} {ManifestHelper.GuardianRanks.ElementAt(Rank - 1).Value} {(RankProgress == RankCompletion ? "" : $"({RankProgress}/{RankCompletion})")}";

            if (CommendationTotal >= 0)
                embed.Description += $" {EmoteConfig.GetEmote("Commendations")}{CommendationTotal}";

            embed.Description += badge;

            embed.AddField(x =>
            {
                x.Name = "Weapons";
                x.Value = $"{Kinetic.GetDamageTypeEmote()} {Kinetic.GetName()}\n{Kinetic.PerksToString()}\n" +
                    $"{Energy.GetDamageTypeEmote()} {Energy.GetName()}\n{Energy.PerksToString()}\n" +
                    $"{Heavy.GetDamageTypeEmote()} {Heavy.GetName()}\n{Heavy.PerksToString()}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Armor";
                x.Value = $"{EmoteConfig.GetEmote("Helmet")} {Helmet.GetName()}\n" +
                    $"{EmoteConfig.GetEmote("Arms")} {Arms.GetName()}\n" +
                    $"{EmoteConfig.GetEmote("Chest")} {Chest.GetName()}\n" +
                    $"{EmoteConfig.GetEmote("Legs")} {Legs.GetName()}\n" +
                    $"{EmoteConfig.GetEmote("Class")} {ClassItem.GetName()}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Stats";
                x.Value = $"{EmoteConfig.GetEmote("Mobility")} {Mobility}\n" +
                    $"{EmoteConfig.GetEmote("Resilience")} {Resilience}\n" +
                    $"{EmoteConfig.GetEmote("Recovery")} {Recovery}\n" +
                    $"{EmoteConfig.GetEmote("Discipline")} {Discipline}\n" +
                    $"{EmoteConfig.GetEmote("Intellect")} {Intellect}\n" +
                    $"{EmoteConfig.GetEmote("Strength")} {Strength}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Emblem";
                x.Value = $"[{Emblem.GetName()}]({Emblem.GetDECUrl()})";
                x.IsInline = true;
            });


            if (ActivityHash != -1)
            {
                embed.AddField(x =>
                {
                    x.Name = "Activity";
                    x.Value = $"{ManifestHelper.Activities[ActivityHash]}\n" +
                        $"Started {TimestampTag.FromDateTime(ActivityStarted, TimestampTagStyles.Relative)}.";
                    x.IsInline = true;
                });
            }

            if (SealDiscordString != null)
            {
                embed.AddField(x =>
                {
                    x.Name = "Title";
                    x.Value = $"{SealDiscordString}";
                    x.IsInline = true;
                });
            }

            return embed;
        }

        public enum Platform
        {
            None, // Not Used
            Xbox,
            PSN,
            Steam,
            Blizzard, // Not Used
            Stadia,
            EpicGames,
        }
    }
}
