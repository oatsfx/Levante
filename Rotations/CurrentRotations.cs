using Discord;
using Discord.WebSocket;
using Levante.Configs;
using Levante.Rotations.Abstracts;
using Levante.Rotations.Interfaces;
using Levante.Util;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Levante.Rotations
{
    public class CurrentRotations
    {
        public static string FilePath = @"Configs/currentRotations.json";

        public static Ada1Rotation Ada1 = new();
        public static AltarsOfSorrowRotation AltarsOfSorrow = new();
        public static AscendantChallengeRotation AscendantChallenge = new();
        public static CurseWeekRotation CurseWeek = new();
        public static DaresOfEternityRotation DaresOfEternity = new();
        public static DeepStoneCryptRotation DeepStoneCrypt = new();
        public static EmpireHuntRotation EmpireHunt = new();
        public static FeaturedDungeonRotation FeaturedDungeon = new();
        public static FeaturedRaidRotation FeaturedRaid = new();
        public static GardenOfSalvationRotation GardenOfSalvation = new();
        public static KingsFallRotation KingsFall = new();
        public static LastWishRotation LastWish = new();
        public static LightfallMissionRotation LightfallMission = new();
        public static LostSectorRotation LostSector = new();
        public static NightfallRotation Nightfall = new();
        public static NightmareHuntRotation NightmareHunt = new();
        public static RootOfNightmaresRotation RootOfNightmares = new();
        public static ShadowkeepMissionRotation ShadowkeepMission = new();
        public static TerminalOverloadRotation TerminalOverload = new();
        public static VaultOfGlassRotation VaultOfGlass = new();
        public static VowOfTheDiscipleRotation VowOfTheDisciple = new();
        public static WellspringRotation Wellspring = new();
        public static WitchQueenMissionRotation WitchQueenMission = new();

        public static Actives Actives = new();

        public static void DailyRotation()
        {
            Actives.LostSector = Actives.LostSector == LostSector.Rotations.Count - 1 ? 0 : Actives.LostSector + 1;
            Actives.LostSectorArmorDrop = Actives.LostSectorArmorDrop == LostSector.ArmorRotations.Count - 1 ? 0 : Actives.LostSectorArmorDrop + 1;

            Actives.AltarWeapon = Actives.AltarWeapon == AltarsOfSorrow.Rotations.Count - 1 ? 0 : Actives.AltarWeapon + 1;
            Actives.TerminalOverload = Actives.TerminalOverload == TerminalOverload.Rotations.Count - 1 ? 0 : Actives.TerminalOverload + 1;
            Actives.Wellspring = Actives.Wellspring == Wellspring.Rotations.Count - 1 ? 0 : Actives.Wellspring + 1;

            Actives.DailyResetTimestamp = DateTime.Now;

            UpdateRotationsJSON();
        }

        public static void WeeklyRotation()
        {
            Nightfall.GetCurrentNightfall();
            Ada1.GetAda1Inventory();

            Actives.LWChallenge = Actives.LWChallenge == LastWish.Rotations.Count - 1 ? 0 : Actives.LWChallenge + 1;
            Actives.DSCChallenge = Actives.DSCChallenge == DeepStoneCrypt.Rotations.Count - 1 ? 0 : Actives.DSCChallenge + 1;
            Actives.GoSChallenge = Actives.GoSChallenge == GardenOfSalvation.Rotations.Count - 1 ? 0 : Actives.GoSChallenge + 1;
            Actives.VoGChallenge = Actives.VoGChallenge == VaultOfGlass.Rotations.Count - 1 ? 0 : Actives.VoGChallenge + 1;
            Actives.VowChallenge = Actives.VowChallenge == VowOfTheDisciple.Rotations.Count - 1 ? 0 : Actives.VowChallenge + 1;
            Actives.KFChallenge = Actives.KFChallenge == KingsFall.Rotations.Count - 1 ? 0 : Actives.KFChallenge + 1;
            Actives.RoNChallenge = Actives.RoNChallenge == RootOfNightmares.Rotations.Count - 1 ? 0 : Actives.RoNChallenge + 1;
            Actives.FeaturedRaid = Actives.FeaturedRaid == FeaturedRaid.Rotations.Count - 1 ? 0 : Actives.FeaturedRaid + 1;
            Actives.CurseWeek = Actives.CurseWeek == CurseWeek.Rotations.Count - 1 ? 0 : Actives.CurseWeek + 1;
            Actives.AscendantChallenge = Actives.AscendantChallenge == AscendantChallenge.Rotations.Count - 1 ? 0 : Actives.AscendantChallenge + 1;
            Actives.NightfallWeaponDrop = Actives.NightfallWeaponDrop == Nightfall.WeaponRotations.Count - 1 ? 0 : Actives.NightfallWeaponDrop + 1;
            Actives.EmpireHunt = Actives.EmpireHunt == EmpireHunt.Rotations.Count - 1 ? 0 : Actives.EmpireHunt + 1;

            Actives.FeaturedDungeon = Actives.FeaturedDungeon == FeaturedDungeon.Rotations.Count - 1 ? 0 : Actives.FeaturedDungeon + 1;

            Actives.NightmareHunts[0] = Actives.NightmareHunts[0] >= NightmareHunt.Rotations.Count - 3 ? Actives.NightmareHunts[0] - 5 : Actives.NightmareHunts[0] + 3;
            Actives.NightmareHunts[1] = Actives.NightmareHunts[1] >= NightmareHunt.Rotations.Count - 3 ? Actives.NightmareHunts[1] - 5 : Actives.NightmareHunts[1] + 3;
            Actives.NightmareHunts[2] = Actives.NightmareHunts[2] >= NightmareHunt.Rotations.Count - 3 ? Actives.NightmareHunts[2] - 5 : Actives.NightmareHunts[2] + 3;

            Actives.ShadowkeepMission = Actives.ShadowkeepMission >= ShadowkeepMission.Rotations.Count - 1 ? 0 : Actives.ShadowkeepMission + 1;
            Actives.WitchQueenMission = Actives.WitchQueenMission >= WitchQueenMission.Rotations.Count - 1 ? 0 : Actives.WitchQueenMission + 1;
            Actives.LightfallMission = Actives.LightfallMission >= LightfallMission.Rotations.Count - 1 ? 0 : Actives.LightfallMission + 1;

            Actives.WeeklyResetTimestamp = DateTime.Now;

            // Because weekly is also a daily reset.
            DailyRotation();

            // We don't call UpdateRotationsJSON() because it's called in DailyRotation().
        }

        public static int GetTotalLinks()
        {
            return 
                Ada1.GetLinkCount() +
                AltarsOfSorrow.GetLinkCount() +
                AscendantChallenge.GetLinkCount() +
                CurseWeek.GetLinkCount() +
                DeepStoneCrypt.GetLinkCount() +
                EmpireHunt.GetLinkCount() +
                FeaturedRaid.GetLinkCount() +
                GardenOfSalvation.GetLinkCount() +
                KingsFall.GetLinkCount() +
                LastWish.GetLinkCount() +
                LostSector.GetLinkCount() +
                Nightfall.GetLinkCount() +
                NightmareHunt.GetLinkCount() +
                RootOfNightmares.GetLinkCount() +
                TerminalOverload.GetLinkCount() +
                VaultOfGlass.GetLinkCount() +
                VowOfTheDisciple.GetLinkCount() +
                Wellspring.GetLinkCount();
        }

        public static void CreateJSONs()
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                Actives = JsonConvert.DeserializeObject<Actives>(json);
            }
            else
            {
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(Actives, Formatting.Indented));
                Console.WriteLine($"No currentRotations.json file detected. Restart the program and change the values accordingly.");
            }
        }

        public static void UpdateRotationsJSON()
        {
            Ada1.GetAda1Inventory();
            Nightfall.GetCurrentNightfall();
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(Actives, Formatting.Indented));
        }

        public static EmbedBuilder DailyResetEmbed()
        {
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {BotConfig.AppName} v{String.Format("{0:0.00#}", BotConfig.Version)}"
            };
            var embed = new EmbedBuilder
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Footer = foot,
                Title = $"Daily Reset of {TimestampTag.FromDateTime(Actives.DailyResetTimestamp, TimestampTagStyles.ShortDate)}",
                Description = "Below are some of the things that are available today."
            };

            embed.AddField(x =>
            {
                x.Name = "Lost Sector";
                x.Value = $"{LostSector.ArmorRotations[Actives.LostSectorArmorDrop].ArmorEmote} {LostSector.ArmorRotations[Actives.LostSectorArmorDrop].Type}\n";
                if (LostSector.Rotations.Count <= 0)
                {
                    x.Value += $"{DestinyEmote.LostSector} Rotation Unknown";
                }
                else
                {
                    x.Value += $"{DestinyEmote.LostSector} {LostSector.Rotations[Actives.LostSector].Name}";
                }
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = $"The Wellspring: {Wellspring.Rotations[Actives.Wellspring].Type}";
                x.Value =
                    $"{Wellspring.Rotations[Actives.Wellspring].WeaponEmote} {Wellspring.Rotations[Actives.Wellspring].Weapon}\n" +
                    $"{DestinyEmote.WellspringActivity} {Wellspring.Rotations[Actives.Wellspring].Boss}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Altars of Sorrow";
                x.Value =
                    $"{AltarsOfSorrow.Rotations[Actives.AltarWeapon].WeaponEmote} {AltarsOfSorrow.Rotations[Actives.AltarWeapon].Weapon} ({AltarsOfSorrow.Rotations[Actives.AltarWeapon].WeaponType})\n" +
                    $"{DestinyEmote.Luna} {AltarsOfSorrow.Rotations[Actives.AltarWeapon].Boss}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Terminal Overload";
                x.Value =
                    $"{TerminalOverload.Rotations[Actives.TerminalOverload].WeaponEmote} {TerminalOverload.Rotations[Actives.TerminalOverload].Weapon}\n" +
                    $"{DestinyEmote.TerminalOverload} {TerminalOverload.Rotations[Actives.TerminalOverload].Location}";
            });

            return embed;
        }

        public static EmbedBuilder WeeklyResetEmbed()
        {
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {BotConfig.AppName} v{String.Format("{0:0.00#}", BotConfig.Version)}"
            };
            var embed = new EmbedBuilder
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Footer = foot,
                Title = $"Weekly Reset of {TimestampTag.FromDateTime(Actives.WeeklyResetTimestamp, TimestampTagStyles.ShortDate)}",
                Description = "Below are some of the things that are available this week."
            };

            string adaItems = "";
            foreach (var pair in Actives.Ada1Items)
                adaItems += $"{DestinyEmote.Ada1} {pair.ItemName}\n";

            embed.AddField(x =>
            {
                x.Name = "> Raid Challenges";
                x.Value = $"★ - Featured Raid";
                x.IsInline = false;
            })
            .AddField(x =>
            {
                x.Name = $"{(FeaturedRaid.Rotations[Actives.FeaturedRaid].Raid.Equals("Last Wish") ? "★ " : "")}Last Wish";
                x.Value = $"{DestinyEmote.RaidChallenge} {(FeaturedRaid.Rotations[Actives.FeaturedRaid].Raid.Equals("Last Wish") ? "All challenges available." : $"{LastWish.Rotations[Actives.LWChallenge]}")}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"{(FeaturedRaid.Rotations[Actives.FeaturedRaid].Raid.Equals("Garden of Salvation") ? "★ " : "")}Garden of Salvation";
                x.Value = $"{DestinyEmote.RaidChallenge} {(FeaturedRaid.Rotations[Actives.FeaturedRaid].Raid.Equals("Garden of Salvation") ? "All challenges available." : $"{GardenOfSalvation.Rotations[Actives.GoSChallenge]}")}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"{(FeaturedRaid.Rotations[Actives.FeaturedRaid].Raid.Equals("Deep Stone Crypt") ? "★ " : "")}Deep Stone Crypt";
                x.Value = $"{DestinyEmote.RaidChallenge} {(FeaturedRaid.Rotations[Actives.FeaturedRaid].Raid.Equals("Deep Stone Crypt") ? "All challenges available." : $"{DeepStoneCrypt.Rotations[Actives.DSCChallenge]}")}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"{(FeaturedRaid.Rotations[Actives.FeaturedRaid].Raid.Equals("Vault of Glass") ? "★ " : "")}Vault of Glass";
                x.Value = $"{DestinyEmote.VoGRaidChallenge} {(FeaturedRaid.Rotations[Actives.FeaturedRaid].Raid.Equals("Vault of Glass") ? "All challenges available." : $"{VaultOfGlass.Rotations[Actives.VoGChallenge]}")}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"{(FeaturedRaid.Rotations[Actives.FeaturedRaid].Raid.Equals("Vow of the Disciple") ? "★ " : "")}Vow of the Disciple";
                x.Value = $"{DestinyEmote.VowRaidChallenge} {(FeaturedRaid.Rotations[Actives.FeaturedRaid].Raid.Equals("Vow of the Disciple") ? "All challenges available." : $"{VowOfTheDisciple.Rotations[Actives.VowChallenge]}")}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"{(FeaturedRaid.Rotations[Actives.FeaturedRaid].Raid.Equals("King's Fall") ? "★ " : "")}King's Fall";
                x.Value = $"{DestinyEmote.KFRaidChallenge} {(FeaturedRaid.Rotations[Actives.FeaturedRaid].Raid.Equals("King's Fall") ? "All challenges available." : $"{KingsFall.Rotations[Actives.KFChallenge]}")}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"{(FeaturedRaid.Rotations[Actives.FeaturedRaid].Raid.Equals("Root of Nightmares") ? "★ " : "")}Root of Nightmares";
                x.Value = $"{DestinyEmote.RoNRaidChallenge} {RootOfNightmares.Rotations[Actives.RoNChallenge]}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "> Nightfall";
                x.Value = $"*Nightfall Strike and Weapon Rotation*";
                x.IsInline = false;
            })
            .AddField(x =>
            {
                x.Name = $"Strike";
                x.Value = $"{DestinyEmote.Nightfall} {(Nightfall.Rotations.Count > 0 ? Nightfall.Rotations[Actives.Nightfall] : "No rotation found.")}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "Weapon";
                x.Value = Nightfall.WeaponRotations.Count == 0
                    ? "Weapon Rotation Unknown"
                    : $"{Nightfall.WeaponRotations[Actives.NightfallWeaponDrop].Emote} {Nightfall.WeaponRotations[Actives.NightfallWeaponDrop].Name}";

                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "> Patrol";
                x.Value = $"*Patrol Location Rotations*";
                x.IsInline = false;
            })
            .AddField(x =>
            {
                x.Name = $"The Dreaming City";
                x.Value = $"{DestinyEmote.DreamingCity} {CurseWeek.Rotations[Actives.CurseWeek]}\n" +
                    $"{DestinyEmote.AscendantChallengeBounty} {AscendantChallenge.Rotations[Actives.AscendantChallenge]}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"Nightmare Hunts";
                x.Value = $"{DestinyEmote.Luna} {NightmareHunt.Rotations[Actives.NightmareHunts[0]]}\n" +
                    $"{DestinyEmote.Luna} {NightmareHunt.Rotations[Actives.NightmareHunts[1]]}\n" +
                    $"{DestinyEmote.Luna} {NightmareHunt.Rotations[Actives.NightmareHunts[2]]}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"Empire Hunt";
                x.Value = $"{DestinyEmote.Europa} {EmpireHunt.Rotations[Actives.EmpireHunt]}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "> Miscellaneous";
                x.Value = "*Any other important rotations.*";
                x.IsInline = false;
            })
            .AddField(x =>
            {
                x.Name = $"Ada-1 Sales";
                x.Value =
                    $"{adaItems}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"Dungeon";
                x.Value = $"{DestinyEmote.Dungeon} {FeaturedDungeon.Rotations[Actives.FeaturedDungeon]}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "Story Missions";
                x.Value = $"{DestinyEmote.Shadowkeep} {ShadowkeepMission.Rotations[Actives.ShadowkeepMission]}\n" +
                          $"{DestinyEmote.WitchQueen} {WitchQueenMission.Rotations[Actives.WitchQueenMission]}\n" +
                          $"{DestinyEmote.Lightfall} {LightfallMission.Rotations[Actives.LightfallMission]}";
                x.IsInline = true;
            });

            return embed;
        }

        public static async Task CheckUsersDailyTracking(DiscordShardedClient Client)
        {
            await AltarsOfSorrow.CheckTrackers(Client);
            await LostSector.CheckTrackers(Client);
            await TerminalOverload.CheckTrackers(Client);
            await Wellspring.CheckTrackers(Client);
        }

        public static async Task CheckUsersWeeklyTracking(DiscordShardedClient Client)
        {
            await Ada1.CheckTrackers(Client);
            await AscendantChallenge.CheckTrackers(Client);
            await CurseWeek.CheckTrackers(Client);
            await DeepStoneCrypt.CheckTrackers(Client);
            await EmpireHunt.CheckTrackers(Client);
            await FeaturedDungeon.CheckTrackers(Client);
            await FeaturedRaid.CheckTrackers(Client);
            await GardenOfSalvation.CheckTrackers(Client);
            await KingsFall.CheckTrackers(Client);
            await LastWish.CheckTrackers(Client);
            await Nightfall.CheckTrackers(Client);
            await NightmareHunt.CheckTrackers(Client);
            await RootOfNightmares.CheckTrackers(Client);
            //await ShadowkeepMission.
            await VaultOfGlass.CheckTrackers(Client);
            await VowOfTheDisciple.CheckTrackers(Client);
            //await Wi
        }
    }

    public class Actives
    {
        [JsonProperty("DailyResetTimestamp")]
        public DateTime DailyResetTimestamp = DateTime.Now;

        [JsonProperty("WeeklyResetTimestamp")]
        public DateTime WeeklyResetTimestamp = DateTime.Now;

        // Dailies

        [JsonProperty("LostSector")]
        public int LostSector = 0;

        [JsonProperty("LostSectorArmorDrop")]
        public int LostSectorArmorDrop = 0;

        [JsonProperty("AltarWeapon")]
        public int AltarWeapon = 0;

        [JsonProperty("Wellspring")]
        public int Wellspring = 0;

        [JsonProperty("TerminalOverload")]
        public int TerminalOverload = 0;

        // Weeklies

        [JsonProperty("LWChallenge")]
        public int LWChallenge = 0;

        [JsonProperty("DSCChallenge")]
        public int DSCChallenge = 0;

        [JsonProperty("GoSChallenge")]
        public int GoSChallenge = 0;

        [JsonProperty("VoGChallenge")]
        public int VoGChallenge = 0;

        [JsonProperty("VowChallenge")]
        public int VowChallenge = 0;

        [JsonProperty("KFChallenge")]
        public int KFChallenge = 0;

        [JsonProperty("RoNChallenge")]
        public int RoNChallenge = 0;

        [JsonProperty("FeaturedRaid")]
        public int FeaturedRaid = 0;

        [JsonProperty("FeaturedDungeon")]
        public int FeaturedDungeon = 0;

        [JsonProperty("CurseWeek")]
        public int CurseWeek = 0;

        [JsonProperty("AscendantChallenge")]
        public int AscendantChallenge = 0;

        [JsonProperty("Nightfall")]
        public int Nightfall = 0;

        [JsonProperty("NightfallWeaponDrop")]
        public int NightfallWeaponDrop = 0;

        [JsonProperty("EmpireHunt")]
        public int EmpireHunt = 0;

        [JsonProperty("ShadowkeepMission")]
        public int ShadowkeepMission = 0;

        [JsonProperty("WitchQueenMission")]
        public int WitchQueenMission = 0;

        [JsonProperty("LightfallMission")]
        public int LightfallMission = 0;

        [JsonProperty("DaresOfEternity")]
        public int DaresOfEternity = 0;

        [JsonProperty("NightmareHunts")]
        public int[] NightmareHunts = { 0, 1, 2 };

        [JsonProperty("Ada1Items")]
        public List<Ada1> Ada1Items = new();

        // TODO: Implement a seasonal rotation type for things like Legend Defiant Battlegrounds?
        [JsonProperty("Seasonals")]
        public Dictionary<string, int> Seasonals = new();
    }
}
