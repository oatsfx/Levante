using DestinyUtility.Configs;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DestinyUtility.Rotations
{
    public class CurrentRotations
    {
        [JsonProperty("LegendLostSector")]
        public static LostSector LegendLostSector = LostSector.BayOfDrownedWishes;

        [JsonProperty("LegendArmorDrop")]
        public static ExoticArmorType LegendArmorDrop = ExoticArmorType.Arms;

        [JsonProperty("MasterLostSector")]
        public static LostSector MasterLostSector = LostSector.BayOfDrownedWishes;

        [JsonProperty("MasterArmorDrop")]
        public static ExoticArmorType MasterArmorDrop = ExoticArmorType.Arms;

        [JsonProperty("AlterWeapon")]
        public static AltarsOfSorrow AlterWeapon = AltarsOfSorrow.Shotgun;

        [JsonProperty("LWChallengeEncounter")]
        public static LastWishEncounter LWChallengeEncounter = LastWishEncounter.Kalli;

        [JsonProperty("DSCChallengeEncounter")]
        public static DeepStoneCryptEncounter DSCChallengeEncounter = DeepStoneCryptEncounter.Security;

        [JsonProperty("GoSChallengeEncounter")]
        public static GardenOfSalvationEncounter GoSChallengeEncounter = GardenOfSalvationEncounter.Evade;

        [JsonProperty("VoGChallengeEncounter")]
        public static VaultOfGlassEncounter VoGChallengeEncounter = VaultOfGlassEncounter.Confluxes;

        public static void DailyRotation()
        {
            LegendLostSector = LegendLostSector == LostSector.Perdition ? LostSector.BayOfDrownedWishes : LegendLostSector++;
            LegendArmorDrop = LegendArmorDrop == ExoticArmorType.Chest ? ExoticArmorType.Helmet : LegendArmorDrop++;

            MasterLostSector = MasterLostSector == LostSector.Perdition ? LostSector.BayOfDrownedWishes : MasterLostSector++;
            MasterArmorDrop = MasterArmorDrop == ExoticArmorType.Chest ? ExoticArmorType.Helmet : MasterArmorDrop++;

            AlterWeapon = AlterWeapon == AltarsOfSorrow.Rocket ? AltarsOfSorrow.Shotgun : AlterWeapon++;
        }

        public static void WeeklyRotation()
        {
            // Because weekly is also a daily reset.
            DailyRotation();

            LWChallengeEncounter = LWChallengeEncounter == LastWishEncounter.Riven ? LastWishEncounter.Kalli : LWChallengeEncounter++;
            DSCChallengeEncounter = DSCChallengeEncounter == DeepStoneCryptEncounter.Taniks ? DeepStoneCryptEncounter.Security : DSCChallengeEncounter++;
            GoSChallengeEncounter = GoSChallengeEncounter == GardenOfSalvationEncounter.SanctifiedMind ? GardenOfSalvationEncounter.Evade : GoSChallengeEncounter++;
            VoGChallengeEncounter = VoGChallengeEncounter == VaultOfGlassEncounter.Atheon ? VaultOfGlassEncounter.Confluxes : VoGChallengeEncounter++;
        }

        public static void UpdateLostSectorsJSON()
        {
            LostSectorRotation ac = new LostSectorRotation();
            string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.LostSectorTrackingConfigPath, output);
        }

        public static async Task<EmbedBuilder> DailyResetEmbed(DiscordSocketClient Client)
        {
            var app = await Client.GetApplicationInfoAsync();
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by Bungie API"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Footer = foot,
            };
            embed.Title = $"Daily Reset of {TimestampTag.FromDateTime(DateTime.Now, TimestampTagStyles.ShortDate)}";
            embed.Description = "";
            embed.ThumbnailUrl = app.IconUrl;

            embed.AddField(x =>
            {
                x.Name = "Lost Sectors";
                x.Value =
                    $"Legend: {LostSectorRotation.GetLostSectorString(LostSectorRotation.CurrentLegendLostSector)} ({LostSectorRotation.CurrentLegendArmorDrop})\n" +
                    $"Master: {LostSectorRotation.GetLostSectorString(LostSectorRotation.GetMasterLostSector())} ({LostSectorRotation.GetMasterLostSectorArmorDrop()})\n" +
                    $"*Use command /lostsectorinfo for more info.*";
                x.IsInline = true;
            });

            return embed;
        }
    }
}
