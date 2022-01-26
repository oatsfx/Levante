using DestinyUtility.Configs;
using Discord;
using Newtonsoft.Json;
using System.Net.Http;

namespace DestinyUtility.Util
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

            APIUrl = $"https://www.bungie.net/platform/Destiny2/" + MembershipType + "/Profile/" + MembershipID + "/Character/" + CharacterID + "/?components=200,205";

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

        public EmbedBuilder GetGuardianEmbed()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"{UniqueBungieName}: {GetClass()}",
                IconUrl = GetEmblem().GetIconUrl(),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by Bungie API"
            };
            int[] emblemRGB = GetEmblem().GetRGBAsIntArray();
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(emblemRGB[0], emblemRGB[1], emblemRGB[2]),
                Author = auth,
                Footer = foot
            };
            embed.Description =
                $"{GetClassEmote()} **{GetRace()} {GetGender()} {GetClass()}** {GetClassEmote()}\n" +
                $"Light Level: {DestinyEmote.Light}{GetLightLevel()}\n";
            embed.ThumbnailUrl = GetEmblem().GetIconUrl();

            dynamic item = JsonConvert.DeserializeObject(GuardianContent);
            embed.AddField(x =>
            {
                x.Name = "Weapons";
                x.Value = $"K: {new Weapon((long)item.Response.equipment.data.items[0].itemHash).GetName()}\n" +
                    $"E: {new Weapon((long)item.Response.equipment.data.items[1].itemHash).GetName()}\n" +
                    $"P: {new Weapon((long)item.Response.equipment.data.items[2].itemHash).GetName()}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Armor";
                x.Value = $"{DestinyEmote.Helmet}: {new Weapon((long)item.Response.equipment.data.items[3].itemHash).GetName()}\n" +
                    $"{DestinyEmote.Arms}: {new Weapon((long)item.Response.equipment.data.items[4].itemHash).GetName()}\n" +
                    $"{DestinyEmote.Chest}: {new Weapon((long)item.Response.equipment.data.items[5].itemHash).GetName()}\n" +
                    $"{DestinyEmote.Legs}: {new Weapon((long)item.Response.equipment.data.items[6].itemHash).GetName()}\n" +
                    $"{DestinyEmote.Class}: {new Weapon((long)item.Response.equipment.data.items[7].itemHash).GetName()}";
                x.IsInline = true;
            });

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
    }
}
