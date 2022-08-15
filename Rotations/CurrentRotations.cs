using Levante.Configs;
using Levante.Util;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Levante.Helpers;
using System.Net.Http;
using System.Reflection.Emit;
using System.Linq;

namespace Levante.Rotations
{
    public class CurrentRotations
    {
        public static string FilePath { get; } = @"Configs/currentRotations.json";

        [JsonProperty("DailyResetTimestamp")]
        public static DateTime DailyResetTimestamp = DateTime.Now;

        [JsonProperty("WeeklyResetTimestamp")]
        public static DateTime WeeklyResetTimestamp = DateTime.Now;

        // Dailies

        [JsonProperty("LostSector")]
        public static LostSector LostSector = LostSector.K1CrewQuarters;

        [JsonProperty("LostSectorArmorDrop")]
        public static ExoticArmorType LostSectorArmorDrop = ExoticArmorType.Legs;

        [JsonProperty("AltarWeapon")]
        public static AltarsOfSorrow AltarWeapon = AltarsOfSorrow.Shotgun;

        [JsonProperty("Wellspring")]
        public static Wellspring Wellspring = Wellspring.Golmag;

        // Weeklies

        [JsonProperty("LWChallengeEncounter")]
        public static LastWishEncounter LWChallengeEncounter = LastWishEncounter.Kalli;

        [JsonProperty("DSCChallengeEncounter")]
        public static DeepStoneCryptEncounter DSCChallengeEncounter = DeepStoneCryptEncounter.Security;

        [JsonProperty("GoSChallengeEncounter")]
        public static GardenOfSalvationEncounter GoSChallengeEncounter = GardenOfSalvationEncounter.Evade;

        [JsonProperty("VoGChallengeEncounter")]
        public static VaultOfGlassEncounter VoGChallengeEncounter = VaultOfGlassEncounter.Confluxes;

        [JsonProperty("VowChallengeEncounter")]
        public static VowOfTheDiscipleEncounter VowChallengeEncounter = VowOfTheDiscipleEncounter.Acquisition;

        [JsonProperty("FeaturedRaid")]
        public static Raid FeaturedRaid = Raid.LastWish;

        [JsonProperty("CurseWeek")]
        public static CurseWeek CurseWeek = CurseWeek.Weak;

        [JsonProperty("AscendantChallenge")]
        public static AscendantChallenge AscendantChallenge = AscendantChallenge.AgonarchAbyss;

        [JsonProperty("Nightfall")]
        public static Nightfall Nightfall = Nightfall.ProvingGrounds;

        [JsonProperty("NightfallWeaponDrop")]
        public static NightfallWeapon NightfallWeaponDrop = NightfallWeapon.DutyBound;

        [JsonProperty("EmpireHunt")]
        public static EmpireHunt EmpireHunt = EmpireHunt.Warrior;

        [JsonProperty("NightmareHunts")]
        public static NightmareHunt[] NightmareHunts = { NightmareHunt.Crota, NightmareHunt.Phogoth, NightmareHunt.Ghaul };

        [JsonProperty("Ada1Mods")]
        public static Dictionary<long, string> Ada1Mods = new Dictionary<long, string>();

        public static void DailyRotation()
        {
            LostSector = LostSector == LostSector.TheQuarry ? LostSector.K1CrewQuarters : LostSector + 1;
            LostSectorArmorDrop = LostSectorArmorDrop == ExoticArmorType.Chest ? ExoticArmorType.Helmet : LostSectorArmorDrop + 1;

            AltarWeapon = AltarWeapon == AltarsOfSorrow.Rocket ? AltarsOfSorrow.Shotgun : AltarWeapon + 1;
            Wellspring = Wellspring == Wellspring.Zeerik ? Wellspring.Golmag : Wellspring + 1;

            Ada1Rotation.GetAda1Inventory();

            DailyResetTimestamp = DateTime.Now;

            UpdateRotationsJSON();
        }

        public static void WeeklyRotation()
        {
            LWChallengeEncounter = LWChallengeEncounter == LastWishEncounter.Riven ? LastWishEncounter.Kalli : LWChallengeEncounter + 1;
            DSCChallengeEncounter = DSCChallengeEncounter == DeepStoneCryptEncounter.Taniks ? DeepStoneCryptEncounter.Security : DSCChallengeEncounter + 1;
            GoSChallengeEncounter = GoSChallengeEncounter == GardenOfSalvationEncounter.SanctifiedMind ? GardenOfSalvationEncounter.Evade : GoSChallengeEncounter + 1;
            VoGChallengeEncounter = VoGChallengeEncounter == VaultOfGlassEncounter.Atheon ? VaultOfGlassEncounter.Confluxes : VoGChallengeEncounter + 1;
            VowChallengeEncounter = VowChallengeEncounter == VowOfTheDiscipleEncounter.Rhulk ? VowOfTheDiscipleEncounter.Acquisition : VowChallengeEncounter + 1;
            FeaturedRaid = FeaturedRaid == Raid.VaultOfGlass ? Raid.LastWish : FeaturedRaid + 1;
            CurseWeek = CurseWeek == CurseWeek.Strong ? CurseWeek.Weak : CurseWeek + 1;
            AscendantChallenge = AscendantChallenge == AscendantChallenge.KeepOfHonedEdges ? AscendantChallenge.AgonarchAbyss : AscendantChallenge + 1;
            Nightfall = Nightfall == Nightfall.TheArmsDealer ? Nightfall.ProvingGrounds : Nightfall + 1;
            // Missing data.
            NightfallWeaponDrop = NightfallWeaponDrop == NightfallWeapon.PlugOne1 ? NightfallWeapon.SiliconNeuroma : NightfallWeaponDrop + 1;
            EmpireHunt = EmpireHunt == EmpireHunt.DarkPriestess ? EmpireHunt.Warrior : EmpireHunt + 1;

            NightmareHunts[0] = NightmareHunts[0] >= NightmareHunt.Skolas ? NightmareHunts[0] - 5 : NightmareHunts[0] + 3;
            NightmareHunts[1] = NightmareHunts[1] >= NightmareHunt.Skolas ? NightmareHunts[1] - 5 : NightmareHunts[1] + 3;
            NightmareHunts[2] = NightmareHunts[2] >= NightmareHunt.Skolas ? NightmareHunts[2] - 5 : NightmareHunts[2] + 3;

            WeeklyResetTimestamp = DateTime.Now;

            // Because weekly is also a daily reset.
            DailyRotation();

            // We don't call UpdateRotationsJSON() because it's called in DailyRotation().
        }

        public static int GetTotalLinks()
        {
            return AltarsOfSorrowRotation.AltarsOfSorrowLinks.Count +
                AscendantChallengeRotation.AscendantChallengeLinks.Count +
                CurseWeekRotation.CurseWeekLinks.Count +
                DeepStoneCryptRotation.DeepStoneCryptLinks.Count +
                EmpireHuntRotation.EmpireHuntLinks.Count +
                FeaturedRaidRotation.FeaturedRaidLinks.Count +
                GardenOfSalvationRotation.GardenOfSalvationLinks.Count +
                LastWishRotation.LastWishLinks.Count +
                LostSectorRotation.LostSectorLinks.Count +
                NightfallRotation.NightfallLinks.Count +
                NightmareHuntRotation.NightmareHuntLinks.Count +
                VaultOfGlassRotation.VaultOfGlassLinks.Count +
                VowOfTheDiscipleRotation.VowOfTheDiscipleLinks.Count +
                WellspringRotation.WellspringLinks.Count;
        }

        public static void CreateJSONs()
        {
            if (!Directory.Exists("Trackers"))
                Directory.CreateDirectory("Trackers");

            CurrentRotations cr;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                cr = JsonConvert.DeserializeObject<CurrentRotations>(json);
            }
            else
            {
                cr = new CurrentRotations();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(cr, Formatting.Indented));
                Console.WriteLine($"No currentRotations.json file detected. Restart the program and change the values accordingly.");
            }

            // Create/Check the tracking JSONs.
            AltarsOfSorrowRotation.CreateJSON();
            AscendantChallengeRotation.CreateJSON();
            CurseWeekRotation.CreateJSON();
            DeepStoneCryptRotation.CreateJSON();
            EmpireHuntRotation.CreateJSON();
            FeaturedRaidRotation.CreateJSON();
            GardenOfSalvationRotation.CreateJSON();
            LastWishRotation.CreateJSON();
            LostSectorRotation.CreateJSON();
            NightfallRotation.CreateJSON();
            NightmareHuntRotation.CreateJSON();
            VaultOfGlassRotation.CreateJSON();
            VowOfTheDiscipleRotation.CreateJSON();
            WellspringRotation.CreateJSON();

            Ada1Rotation.CreateJSON();
        }

        private static void UpdateRotationsJSON()
        {
            CurrentRotations cr = new CurrentRotations();
            string output = JsonConvert.SerializeObject(cr, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static EmbedBuilder DailyResetEmbed()
        {
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {BotConfig.AppName} v{String.Format("{0:0.00#}", BotConfig.Version)}"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Footer = foot,
            };
            embed.Title = $"Daily Reset of {TimestampTag.FromDateTime(DailyResetTimestamp, TimestampTagStyles.ShortDate)}";
            embed.Description = "Below are some of the things that are available today.";

            string adaMods = "";
            foreach (var pair in Ada1Mods)
            {
                adaMods += $"{pair.Value}\n";
            }

            embed.AddField(x =>
             {
                 x.Name = "Lost Sector";
                 x.Value =
                     $"{LostSectorRotation.GetArmorEmote(LostSectorArmorDrop)} {LostSectorArmorDrop}\n" +
                     $"{DestinyEmote.LostSector} {LostSectorRotation.GetLostSectorString(LostSector)}";
                 x.IsInline = true;
             }).AddField(x =>
            {
                x.Name = $"The Wellspring: {WellspringRotation.GetWellspringTypeString(Wellspring)}";
                x.Value =
                    $"{WellspringRotation.GetWeaponEmote(Wellspring)} {WellspringRotation.GetWeaponNameString(Wellspring)}\n" +
                    $"{DestinyEmote.WellspringActivity} {WellspringRotation.GetWellspringBossString(Wellspring)}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Altars of Sorrow";
                x.Value =
                    $"{AltarsOfSorrowRotation.GetWeaponEmote(AltarWeapon)} {AltarsOfSorrowRotation.GetWeaponNameString(AltarWeapon)}\n" +
                    $"{DestinyEmote.Luna} {AltarsOfSorrowRotation.GetAltarBossString(AltarWeapon)}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Ada-1 Mod Sales";
                x.Value =
                    $"{adaMods}";
                x.IsInline = true;
            });

            return embed;
        }

        public static EmbedBuilder WeeklyResetEmbed()
        {
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {BotConfig.AppName} v{String.Format("{0:0.00#}", BotConfig.Version)}"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Footer = foot,
            };
            embed.Title = $"Weekly Reset of {TimestampTag.FromDateTime(WeeklyResetTimestamp, TimestampTagStyles.ShortDate)}";
            embed.Description = "Below are some of the things that are available this week.";

            embed.AddField(x =>
            {
                x.Name = "> Raid Challenges";
                x.Value = $"★ - Featured Raid";
                x.IsInline = false;
            })
            .AddField(x =>
            {
                x.Name = $"{(FeaturedRaid == Raid.LastWish ? "★ " : "")}Last Wish";
                x.Value = $"{DestinyEmote.RaidChallenge} {(FeaturedRaid == Raid.LastWish ? "All challenges available." : $"{LastWishRotation.GetEncounterString(LWChallengeEncounter)} ({LastWishRotation.GetChallengeString(LWChallengeEncounter)})")}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"{(FeaturedRaid == Raid.GardenOfSalvation ? "★ " : "")}Garden of Salvation";
                x.Value = $"{DestinyEmote.RaidChallenge} {(FeaturedRaid == Raid.GardenOfSalvation ? "All challenges available." : $"{GardenOfSalvationRotation.GetEncounterString(GoSChallengeEncounter)} ({GardenOfSalvationRotation.GetChallengeString(GoSChallengeEncounter)})")}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"{(FeaturedRaid == Raid.DeepStoneCrypt ? "★ " : "")}Deep Stone Crypt";
                x.Value = $"{DestinyEmote.RaidChallenge} {(FeaturedRaid == Raid.DeepStoneCrypt ? "All challenges available." : $"{DeepStoneCryptRotation.GetEncounterString(DSCChallengeEncounter)} ({DeepStoneCryptRotation.GetChallengeString(DSCChallengeEncounter)})")}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"{(FeaturedRaid == Raid.VaultOfGlass ? "★ " : "")}Vault of Glass";
                x.Value = $"{DestinyEmote.VoGRaidChallenge} {(FeaturedRaid == Raid.VaultOfGlass ? "All challenges available." : $"{VaultOfGlassRotation.GetEncounterString(VoGChallengeEncounter)} ({VaultOfGlassRotation.GetChallengeString(VoGChallengeEncounter)})\nWeapon Drop: {VaultOfGlassRotation.GetChallengeRewardString(VoGChallengeEncounter)}")}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "Vow of the Disciple";
                x.Value = $"{DestinyEmote.VowRaidChallenge} {VowOfTheDiscipleRotation.GetEncounterString(VowChallengeEncounter)} ({VowOfTheDiscipleRotation.GetChallengeString(VowChallengeEncounter)})";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "> Nightfall: The Ordeal";
                x.Value = $"*Nightfall Strike and Weapon Rotation*";
                x.IsInline = false;
            })
            .AddField(x =>
            {
                x.Name = $"{NightfallRotation.GetStrikeNameString(Nightfall)}";
                x.Value = $"{DestinyEmote.Nightfall} {NightfallRotation.GetStrikeBossString(Nightfall)}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "Weapon";
                x.Value = $"{NightfallRotation.GetWeaponEmote(NightfallWeaponDrop)} {NightfallRotation.GetWeaponString(NightfallWeaponDrop)}";
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
                x.Value = $"{DestinyEmote.DreamingCity} {CurseWeek}\n" +
                    $"{DestinyEmote.AscendantChallengeBounty} {AscendantChallengeRotation.GetChallengeNameString(AscendantChallenge)} ({AscendantChallengeRotation.GetChallengeLocationString(AscendantChallenge)})";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"Nightmare Hunts";
                x.Value = $"{DestinyEmote.Luna} {NightmareHuntRotation.GetHuntNameString(NightmareHunts[0])} ({NightmareHuntRotation.GetHuntBossString(NightmareHunts[0])})\n" +
                    $"{DestinyEmote.Luna} {NightmareHuntRotation.GetHuntNameString(NightmareHunts[1])} ({NightmareHuntRotation.GetHuntBossString(NightmareHunts[1])})\n" +
                    $"{DestinyEmote.Luna} {NightmareHuntRotation.GetHuntNameString(NightmareHunts[2])} ({NightmareHuntRotation.GetHuntBossString(NightmareHunts[2])})";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = $"Empire Hunt";
                x.Value = $"{DestinyEmote.Europa} {EmpireHuntRotation.GetHuntNameString(EmpireHunt)} ({EmpireHuntRotation.GetHuntBossString(EmpireHunt)})";
                x.IsInline = false;
            });

            return embed;
        }

        public static async Task CheckUsersDailyTracking(DiscordSocketClient Client)
        {
            var ada1Temp = new List<Ada1Rotation.Ada1ModLink>();
            foreach (var Link in Ada1Rotation.Ada1ModLinks)
            {
                try
                {
                    IUser user;
                    if (Client.GetUser(Link.DiscordID) == null)
                        user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                    else
                        user = Client.GetUser(Link.DiscordID);

                    if (Ada1Mods.ContainsKey(Link.ModHash))
                        await user.SendMessageAsync($"> Hey {user.Mention}! Ada-1 is selling **{ManifestHelper.Ada1ArmorMods[Link.ModHash]}** today. I have removed your tracking, good luck!");
                    else
                        ada1Temp.Add(Link);
                }
                catch
                {
                    LogHelper.ConsoleLog($"Unable to send message to user: {Link.DiscordID}.");
                    continue;
                }
            }
            Ada1Rotation.Ada1ModLinks = ada1Temp;
            Ada1Rotation.UpdateJSON();

            var altarTemp = new List<AltarsOfSorrowRotation.AltarsOfSorrowLink>();
            foreach (var Link in AltarsOfSorrowRotation.AltarsOfSorrowLinks)
            {
                try
                {
                    IUser user;
                    if (Client.GetUser(Link.DiscordID) == null)
                        user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                    else
                        user = Client.GetUser(Link.DiscordID);

                    if (AltarWeapon == Link.WeaponDrop)
                        await user.SendMessageAsync($"> Hey {user.Mention}! Altars of Sorrow is dropping **{AltarsOfSorrowRotation.GetWeaponNameString(AltarWeapon)}** (**{AltarWeapon}**) today. I have removed your tracking, good luck!");
                    else
                        altarTemp.Add(Link);
                }
                catch
                {
                    LogHelper.ConsoleLog($"Unable to send message to user: {Link.DiscordID}.");
                    continue;
                }
            }
            AltarsOfSorrowRotation.AltarsOfSorrowLinks = altarTemp;
            AltarsOfSorrowRotation.UpdateJSON();
            
            var lsTemp = new List<LostSectorRotation.LostSectorLink>();
            foreach (var Link in LostSectorRotation.LostSectorLinks)
            {
                IUser user;
                if (Client.GetUser(Link.DiscordID) == null)
                    user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                else
                    user = Client.GetUser(Link.DiscordID);

                if (LostSector == Link.LostSector && Link.ArmorDrop == null)
                    await user.SendMessageAsync($"> Hey {user.Mention}! The Lost Sector is **{LostSectorRotation.GetLostSectorString(LostSector)}** (Requested) and is dropping **{LostSectorArmorDrop}** today. I have removed your tracking, good luck!");
                else if (Link.LostSector == null && LostSectorArmorDrop == Link.ArmorDrop)
                    await user.SendMessageAsync($"> Hey {user.Mention}! The Lost Sector is **{LostSectorRotation.GetLostSectorString(LostSector)}** and is dropping **{LostSectorArmorDrop}** (Requested) today. I have removed your tracking, good luck!");
                else if (LostSector == Link.LostSector && LostSectorArmorDrop == Link.ArmorDrop)
                    await user.SendMessageAsync($"> Hey {user.Mention}! The Lost Sector is **{LostSectorRotation.GetLostSectorString(LostSector)}** and is dropping **{LostSectorArmorDrop}** today. I have removed your tracking, good luck!");
                else
                    lsTemp.Add(Link);
            }
            LostSectorRotation.LostSectorLinks = lsTemp;
            LostSectorRotation.UpdateJSON();

            var wellspringTemp = new List<WellspringRotation.WellspringLink>();
            foreach (var Link in WellspringRotation.WellspringLinks)
            {
                try
                {
                    IUser user;
                    if (Client.GetUser(Link.DiscordID) == null)
                        user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                    else
                        user = Client.GetUser(Link.DiscordID);

                    if (Wellspring == Link.WellspringBoss)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Wellspring: {WellspringRotation.GetWellspringTypeString(Wellspring)} ({WellspringRotation.GetWellspringBossString(Wellspring)}) is dropping **{WellspringRotation.GetWeaponNameString(Wellspring)}** (**{WellspringRotation.GetWeaponTypeString(Wellspring)}**) today. I have removed your tracking, good luck!");
                    else
                        wellspringTemp.Add(Link);
                }
                catch
                {
                    LogHelper.ConsoleLog($"Unable to send message to user: {Link.DiscordID}.");
                    continue;
                }
            }
            WellspringRotation.WellspringLinks = wellspringTemp;
            WellspringRotation.UpdateJSON();
        }

        public static async Task CheckUsersWeeklyTracking(DiscordSocketClient Client)
        {
            var chalTemp = new List<AscendantChallengeRotation.AscendantChallengeLink>();
            foreach (var Link in AscendantChallengeRotation.AscendantChallengeLinks)
            {
                try
                {
                    IUser user;
                    if (Client.GetUser(Link.DiscordID) == null)
                        user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                    else
                        user = Client.GetUser(Link.DiscordID);

                    if (AscendantChallenge == Link.AscendantChallenge)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Ascendant Challenge is **{AscendantChallengeRotation.GetChallengeNameString(AscendantChallenge)}** (**{AscendantChallengeRotation.GetChallengeLocationString(AscendantChallenge)}**) this week. I have removed your tracking, good luck!");
                    else
                        chalTemp.Add(Link);
                }
                catch
                {
                    LogHelper.ConsoleLog($"Unable to send message to user: {Link.DiscordID}.");
                    continue;
                }
            }
            AscendantChallengeRotation.AscendantChallengeLinks = chalTemp;
            AscendantChallengeRotation.UpdateJSON();

            var curseTemp = new List<CurseWeekRotation.CurseWeekLink>();
            foreach (var Link in CurseWeekRotation.CurseWeekLinks)
            {
                try
                {
                    IUser user;
                    if (Client.GetUser(Link.DiscordID) == null)
                        user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                    else
                        user = Client.GetUser(Link.DiscordID);

                    if (CurseWeek == Link.Strength)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Curse Strength is **{CurseWeek}** this week. I have removed your tracking, good luck!");
                    else
                        curseTemp.Add(Link);
                }
                catch
                {
                    LogHelper.ConsoleLog($"Unable to send message to user: {Link.DiscordID}.");
                    continue;
                }
            }
            CurseWeekRotation.CurseWeekLinks = curseTemp;
            CurseWeekRotation.UpdateJSON();

            var dscTemp = new List<DeepStoneCryptRotation.DeepStoneCryptLink>();
            foreach (var Link in DeepStoneCryptRotation.DeepStoneCryptLinks)
            {
                try
                {
                    IUser user;
                    if (Client.GetUser(Link.DiscordID) == null)
                        user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                    else
                        user = Client.GetUser(Link.DiscordID);

                    if (FeaturedRaid == Raid.DeepStoneCrypt)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The featured raid is Deep Stone Crypt this week, meaning that all challenges including, **{DeepStoneCryptRotation.GetChallengeString(DSCChallengeEncounter)}** (**{DeepStoneCryptRotation.GetEncounterString(DSCChallengeEncounter)}**), are available this week. I have removed your tracking, good luck!");
                    else if (DSCChallengeEncounter == Link.Encounter)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Deep Stone Crypt challenge is **{DeepStoneCryptRotation.GetChallengeString(DSCChallengeEncounter)}** (**{DeepStoneCryptRotation.GetEncounterString(DSCChallengeEncounter)}**) this week. I have removed your tracking, good luck!");
                    else
                        dscTemp.Add(Link);
                }
                catch
                {
                    LogHelper.ConsoleLog($"Unable to send message to user: {Link.DiscordID}.");
                    continue;
                }
            }
            DeepStoneCryptRotation.DeepStoneCryptLinks = dscTemp;
            DeepStoneCryptRotation.UpdateJSON();

            var ehuntTemp = new List<EmpireHuntRotation.EmpireHuntLink>();
            foreach (var Link in EmpireHuntRotation.EmpireHuntLinks)
            {
                try
                {
                    IUser user;
                    if (Client.GetUser(Link.DiscordID) == null)
                        user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                    else
                        user = Client.GetUser(Link.DiscordID);

                    if (EmpireHunt == Link.EmpireHunt)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Empire Hunt is **{EmpireHuntRotation.GetHuntBossString(EmpireHunt)}** this week. I have removed your tracking, good luck!");
                    else
                        ehuntTemp.Add(Link);
                }
                catch
                {
                    LogHelper.ConsoleLog($"Unable to send message to user: {Link.DiscordID}.");
                    continue;
                }
            }
            EmpireHuntRotation.EmpireHuntLinks = ehuntTemp;
            EmpireHuntRotation.UpdateJSON();

            var frTemp = new List<FeaturedRaidRotation.FeaturedRaidLink>();
            foreach (var Link in FeaturedRaidRotation.FeaturedRaidLinks)
            {
                try
                {
                    IUser user;
                    if (Client.GetUser(Link.DiscordID) == null)
                        user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                    else
                        user = Client.GetUser(Link.DiscordID);

                    if (FeaturedRaid == Link.FeaturedRaid)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The featured raid is **{FeaturedRaidRotation.GetRaidString(FeaturedRaid)}** this week. I have removed your tracking, good luck!");
                    else
                        frTemp.Add(Link);
                }
                catch
                {
                    LogHelper.ConsoleLog($"Unable to send message to user: {Link.DiscordID}.");
                    continue;
                }
            }
            FeaturedRaidRotation.FeaturedRaidLinks = frTemp;
            FeaturedRaidRotation.UpdateJSON();

            var gosTemp = new List<GardenOfSalvationRotation.GardenOfSalvationLink>();
            foreach (var Link in GardenOfSalvationRotation.GardenOfSalvationLinks)
            {
                try
                {
                    IUser user;
                    if (Client.GetUser(Link.DiscordID) == null)
                        user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                    else
                        user = Client.GetUser(Link.DiscordID);

                    if (FeaturedRaid == Raid.GardenOfSalvation)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The featured raid is Garden of Salvation this week, meaning that all challenges including, **{GardenOfSalvationRotation.GetChallengeString(GoSChallengeEncounter)}** (**{GardenOfSalvationRotation.GetEncounterString(GoSChallengeEncounter)}**), are available this week. I have removed your tracking, good luck!");
                    else if (GoSChallengeEncounter == Link.Encounter)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Garden of Salvation challenge is **{GardenOfSalvationRotation.GetChallengeString(GoSChallengeEncounter)}** (**{GardenOfSalvationRotation.GetEncounterString(GoSChallengeEncounter)}**) this week. I have removed your tracking, good luck!");
                    else
                        gosTemp.Add(Link);
                }
                catch
                {
                    LogHelper.ConsoleLog($"Unable to send message to user: {Link.DiscordID}.");
                    continue;
                }
            }
            GardenOfSalvationRotation.GardenOfSalvationLinks = gosTemp;
            GardenOfSalvationRotation.UpdateJSON();

            var lwTemp = new List<LastWishRotation.LastWishLink>();
            foreach (var Link in LastWishRotation.LastWishLinks)
            {
                try
                {
                    IUser user;
                    if (Client.GetUser(Link.DiscordID) == null)
                        user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                    else
                        user = Client.GetUser(Link.DiscordID);

                    if (FeaturedRaid == Raid.LastWish)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The featured raid is Last Wish this week, meaning that all challenges including, **{LastWishRotation.GetChallengeString(LWChallengeEncounter)}** (**{LastWishRotation.GetEncounterString(LWChallengeEncounter)}**), are available this week. I have removed your tracking, good luck!");
                    else if (LWChallengeEncounter == Link.Encounter)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Last Wish challenge is **{LastWishRotation.GetChallengeString(LWChallengeEncounter)}** (**{LastWishRotation.GetEncounterString(LWChallengeEncounter)}**) this week. I have removed your tracking, good luck!");
                    else
                        lwTemp.Add(Link);
                }
                catch
                {
                    LogHelper.ConsoleLog($"Unable to send message to user: {Link.DiscordID}.");
                    continue;
                }
            }
            LastWishRotation.LastWishLinks = lwTemp;
            LastWishRotation.UpdateJSON();

            var nfTemp = new List<NightfallRotation.NightfallLink>();
            foreach (var Link in NightfallRotation.NightfallLinks)
            {
                IUser user;
                if (Client.GetUser(Link.DiscordID) == null)
                    user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                else
                    user = Client.GetUser(Link.DiscordID);

                if (Link.Nightfall == Nightfall || Link.WeaponDrop == null)
                    await user.SendMessageAsync($"> Hey {user.Mention}! The Nightfall is **{NightfallRotation.GetStrikeNameString(Nightfall)}** (Requested) and is dropping **{NightfallRotation.GetWeaponString(NightfallWeaponDrop)}** today. I have removed your tracking, good luck!");
                else if (Link.Nightfall == null || Link.WeaponDrop == NightfallWeaponDrop)
                    await user.SendMessageAsync($"> Hey {user.Mention}! The Nightfall is **{NightfallRotation.GetStrikeNameString(Nightfall)}** and is dropping **{NightfallRotation.GetWeaponString(NightfallWeaponDrop)}** (Requested) today. I have removed your tracking, good luck!");
                else if (Link.Nightfall == Nightfall || Link.WeaponDrop == NightfallWeaponDrop)
                    await user.SendMessageAsync($"> Hey {user.Mention}! The Nightfall is **{NightfallRotation.GetStrikeNameString(Nightfall)}** and is dropping **{NightfallRotation.GetWeaponString(NightfallWeaponDrop)}** today. I have removed your tracking, good luck!");
                else
                    nfTemp.Add(Link);
            }
            NightfallRotation.NightfallLinks = nfTemp;
            NightfallRotation.UpdateJSON();

            var nhuntTemp = new List<NightmareHuntRotation.NightmareHuntLink>();
            foreach (var Link in NightmareHuntRotation.NightmareHuntLinks)
            {
                try
                {
                    IUser user;
                    if (Client.GetUser(Link.DiscordID) == null)
                        user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                    else
                        user = Client.GetUser(Link.DiscordID);

                    if (NightmareHunts[0] == Link.NightmareHunt)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Nightmare Hunt is **{NightmareHuntRotation.GetHuntNameString(NightmareHunts[0])}** (**{NightmareHuntRotation.GetHuntBossString(NightmareHunts[0])}**) this week. I have removed your tracking, good luck!");
                    else if (NightmareHunts[1] == Link.NightmareHunt)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Nightmare Hunt is **{NightmareHuntRotation.GetHuntNameString(NightmareHunts[1])}** (**{NightmareHuntRotation.GetHuntBossString(NightmareHunts[1])}**) this week. I have removed your tracking, good luck!");
                    else if (NightmareHunts[2] == Link.NightmareHunt)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Nightmare Hunt is **{NightmareHuntRotation.GetHuntNameString(NightmareHunts[2])}** (**{NightmareHuntRotation.GetHuntBossString(NightmareHunts[2])}**) this week. I have removed your tracking, good luck!");
                    else
                        nhuntTemp.Add(Link);
                }
                catch
                {
                    LogHelper.ConsoleLog($"Unable to send message to user: {Link.DiscordID}.");
                    continue;
                }
            }
            NightmareHuntRotation.NightmareHuntLinks = nhuntTemp;
            NightmareHuntRotation.UpdateJSON();

            var vogTemp = new List<VaultOfGlassRotation.VaultOfGlassLink>();
            foreach (var Link in VaultOfGlassRotation.VaultOfGlassLinks)
            {
                try
                {
                    IUser user;
                    if (Client.GetUser(Link.DiscordID) == null)
                        user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                    else
                        user = Client.GetUser(Link.DiscordID);

                    if (FeaturedRaid == Raid.VaultOfGlass)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The featured raid is Vault of Glass this week, meaning that all challenges including, **{VaultOfGlassRotation.GetChallengeString(VoGChallengeEncounter)}** (**{VaultOfGlassRotation.GetEncounterString(VoGChallengeEncounter)}**), are available this week. I have removed your tracking, good luck!");
                    else if (VoGChallengeEncounter == Link.Encounter)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Vault of Glass challenge is **{VaultOfGlassRotation.GetChallengeString(VoGChallengeEncounter)}** (**{VaultOfGlassRotation.GetEncounterString(VoGChallengeEncounter)}**) this week. I have removed your tracking, good luck!");
                    else
                        vogTemp.Add(Link);
                }
                catch
                {
                    LogHelper.ConsoleLog($"Unable to send message to user: {Link.DiscordID}.");
                    continue;
                }
            }
            VaultOfGlassRotation.VaultOfGlassLinks = vogTemp;
            VaultOfGlassRotation.UpdateJSON();

            var vowTemp = new List<VowOfTheDiscipleRotation.VowOfTheDiscipleLink>();
            foreach (var Link in VowOfTheDiscipleRotation.VowOfTheDiscipleLinks)
            {
                try
                {
                    IUser user;
                    if (Client.GetUser(Link.DiscordID) == null)
                        user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                    else
                        user = Client.GetUser(Link.DiscordID);

                    if (VowChallengeEncounter == Link.Encounter)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Vow of the Disciple challenge is **{VowOfTheDiscipleRotation.GetChallengeString(VowChallengeEncounter)}** (**{VowOfTheDiscipleRotation.GetEncounterString(VowChallengeEncounter)}**) this week. I have removed your tracking, good luck!");
                    else
                        vowTemp.Add(Link);
                }
                catch
                {
                    LogHelper.ConsoleLog($"Unable to send message to user: {Link.DiscordID}.");
                    continue;
                }
            }
            VowOfTheDiscipleRotation.VowOfTheDiscipleLinks = vowTemp;
            VowOfTheDiscipleRotation.UpdateJSON();
        }
    }
}
