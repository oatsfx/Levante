using Levante.Configs;
using Levante.Util;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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

        [JsonProperty("LegendLostSector")]
        public static LostSector LegendLostSector = LostSector.ChamberOfStarlight;

        [JsonProperty("LegendLostSectorArmorDrop")]
        public static ExoticArmorType LegendLostSectorArmorDrop = ExoticArmorType.Legs;

        [JsonProperty("MasterLostSector")]
        public static LostSector MasterLostSector = LostSector.BayOfDrownedWishes;

        [JsonProperty("MasterLostSectorArmorDrop")]
        public static ExoticArmorType MasterLostSectorArmorDrop = ExoticArmorType.Helmet;

        [JsonProperty("AltarWeapon")]
        public static AltarsOfSorrow AltarWeapon = AltarsOfSorrow.Shotgun;

        // Weeklies

        [JsonProperty("LWChallengeEncounter")]
        public static LastWishEncounter LWChallengeEncounter = LastWishEncounter.Kalli;

        [JsonProperty("DSCChallengeEncounter")]
        public static DeepStoneCryptEncounter DSCChallengeEncounter = DeepStoneCryptEncounter.Security;

        [JsonProperty("GoSChallengeEncounter")]
        public static GardenOfSalvationEncounter GoSChallengeEncounter = GardenOfSalvationEncounter.Evade;

        [JsonProperty("VoGChallengeEncounter")]
        public static VaultOfGlassEncounter VoGChallengeEncounter = VaultOfGlassEncounter.Confluxes;

        [JsonProperty("CurseWeek")]
        public static CurseWeek CurseWeek = CurseWeek.Weak;

        [JsonProperty("AscendantChallenge")]
        public static AscendantChallenge AscendantChallenge = AscendantChallenge.AgonarchAbyss;

        [JsonProperty("Nightfall")]
        public static Nightfall Nightfall = Nightfall.TheHollowedLair;

        [JsonProperty("NightfallWeaponDrops")]
        public static NightfallWeapon[] NightfallWeaponDrops = { NightfallWeapon.ThePalindrome, NightfallWeapon.TheSWARM };

        [JsonProperty("EmpireHunt")]
        public static EmpireHunt EmpireHunt = EmpireHunt.Warrior;

        [JsonProperty("NightmareHunts")]
        public static NightmareHunt[] NightmareHunts = { NightmareHunt.Crota , NightmareHunt.Phogoth, NightmareHunt.Ghaul };

        public static void DailyRotation()
        {
            LegendLostSector = LegendLostSector == LostSector.Perdition ? LostSector.BayOfDrownedWishes : LegendLostSector + 1;
            LegendLostSectorArmorDrop = LegendLostSectorArmorDrop == ExoticArmorType.Chest ? ExoticArmorType.Helmet : LegendLostSectorArmorDrop + 1;

            MasterLostSector = MasterLostSector == LostSector.Perdition ? LostSector.BayOfDrownedWishes : MasterLostSector + 1;
            MasterLostSectorArmorDrop = MasterLostSectorArmorDrop == ExoticArmorType.Chest ? ExoticArmorType.Helmet : MasterLostSectorArmorDrop + 1;

            AltarWeapon = AltarWeapon == AltarsOfSorrow.Rocket ? AltarsOfSorrow.Shotgun : AltarWeapon + 1;

            DailyResetTimestamp = DateTime.Now;

            UpdateRotationsJSON();
        }

        public static void WeeklyRotation()
        {
            LWChallengeEncounter = LWChallengeEncounter == LastWishEncounter.Riven ? LastWishEncounter.Kalli : LWChallengeEncounter + 1;
            DSCChallengeEncounter = DSCChallengeEncounter == DeepStoneCryptEncounter.Taniks ? DeepStoneCryptEncounter.Security : DSCChallengeEncounter + 1;
            GoSChallengeEncounter = GoSChallengeEncounter == GardenOfSalvationEncounter.SanctifiedMind ? GardenOfSalvationEncounter.Evade : GoSChallengeEncounter + 1;
            VoGChallengeEncounter = VoGChallengeEncounter == VaultOfGlassEncounter.Atheon ? VaultOfGlassEncounter.Confluxes : VoGChallengeEncounter + 1;
            CurseWeek = CurseWeek == CurseWeek.Strong ? CurseWeek.Weak : CurseWeek + 1;
            AscendantChallenge = AscendantChallenge == AscendantChallenge.KeepOfHonedEdges ? AscendantChallenge.AgonarchAbyss : AscendantChallenge + 1;
            Nightfall = Nightfall == Nightfall.ProvingGrounds ? Nightfall.TheHollowedLair : Nightfall + 1;
            // This one rotates between the same 4, because there are an even amount of Nightfall weapons. This makes the first weapon always Palindrome.
            NightfallWeaponDrops[0] = NightfallWeaponDrops[0] >= NightfallWeapon.PlugOne1 ? NightfallWeapon.ThePalindrome : NightfallWeaponDrops[0] + 2;
            // This makes the second weapons always The SWARM.
            NightfallWeaponDrops[1] = NightfallWeaponDrops[1] >= NightfallWeapon.PlugOne1 ? NightfallWeapon.TheSWARM : NightfallWeaponDrops[1] + 2;
            EmpireHunt = EmpireHunt == EmpireHunt.DarkPriestess ? EmpireHunt.Warrior : EmpireHunt + 1;

            NightmareHunts[0] = NightmareHunts[0] >= NightmareHunt.Skolas ? NightmareHunts[0] - 5 : NightmareHunts[0] + 3;
            NightmareHunts[1] = NightmareHunts[1] >= NightmareHunt.Skolas ? NightmareHunts[1] - 5 : NightmareHunts[1] + 3;
            NightmareHunts[2] = NightmareHunts[2] >= NightmareHunt.Skolas ? NightmareHunts[2] - 5 : NightmareHunts[2] + 3;

            WeeklyResetTimestamp = DateTime.Now;

            // Because weekly is also a daily reset.
            DailyRotation();

            // We don't call UpdateRotationsJSON() because it's called in DailyRotation().
        }

        public static void CreateJSONs()
        {
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
            GardenOfSalvationRotation.CreateJSON();
            LastWishRotation.CreateJSON();
            LostSectorRotation.CreateJSON();
            NightfallRotation.CreateJSON();
            NightmareHuntRotation.CreateJSON();
            VaultOfGlassRotation.CreateJSON();
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
                Text = $"Powered by Bungie API"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Footer = foot,
            };
            embed.Title = $"Daily Reset of {TimestampTag.FromDateTime(DailyResetTimestamp, TimestampTagStyles.ShortDate)}";
            embed.Description = "Below are some of the things that are available today.";

            embed.AddField(x =>
            {
                x.Name = "Lost Sectors";
                x.Value =
                    $"Legend: {LostSectorRotation.GetLostSectorString(LegendLostSector)} {LostSectorRotation.GetArmorEmote(LegendLostSectorArmorDrop)}\n" +
                    $"Master: {LostSectorRotation.GetLostSectorString(MasterLostSector)} {LostSectorRotation.GetArmorEmote(MasterLostSectorArmorDrop)}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "Altars of Sorrow";
                x.Value =
                    $"Weapon: {AltarsOfSorrowRotation.GetWeaponEmote(AltarWeapon)} {AltarWeapon}\n" +
                    $"{DestinyEmote.Luna} {AltarsOfSorrowRotation.GetAltarBossString(AltarWeapon)}";
                x.IsInline = true;
            });

            return embed;
        }

        public static EmbedBuilder WeeklyResetEmbed()
        {
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by Bungie API"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Footer = foot,
            };
            embed.Title = $"Weekly Reset of {TimestampTag.FromDateTime(WeeklyResetTimestamp, TimestampTagStyles.ShortDate)}";

            embed.AddField(x =>
            {
                x.Name = "> Raid Challenges";
                x.Value = $"*Use command /raid for more info!*";
                x.IsInline = false;
            })
            .AddField(x =>
            {
                x.Name = "Last Wish";
                x.Value = $"{DestinyEmote.RaidBounty} {LastWishRotation.GetEncounterString(LWChallengeEncounter)} ({LastWishRotation.GetChallengeString(LWChallengeEncounter)})";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "Garden of Salvation";
                x.Value = $"{DestinyEmote.RaidChallenge} {GardenOfSalvationRotation.GetEncounterString(GoSChallengeEncounter)} ({GardenOfSalvationRotation.GetChallengeString(GoSChallengeEncounter)})";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "Deep Stone Crypt";
                x.Value = $"{DestinyEmote.RaidChallenge} {DeepStoneCryptRotation.GetEncounterString(DSCChallengeEncounter)} ({DeepStoneCryptRotation.GetChallengeString(DSCChallengeEncounter)})";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "Vault of Glass";
                x.Value = $"{DestinyEmote.RaidChallenge} {VaultOfGlassRotation.GetEncounterString(VoGChallengeEncounter)} ({VaultOfGlassRotation.GetChallengeString(VoGChallengeEncounter)})\n" +
                    $"Weapon Drop: {VaultOfGlassRotation.GetChallengeRewardString(VoGChallengeEncounter)}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "> Nightfall: The Ordeal";
                x.Value = $"*Use command /nightfall for more info!*";
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
                x.Name = "Weapons";
                x.Value = $"{NightfallRotation.GetWeaponEmote(NightfallWeaponDrops[0])} {NightfallRotation.GetWeaponString(NightfallWeaponDrops[0])}\n" +
                    $"{NightfallRotation.GetWeaponEmote(NightfallWeaponDrops[1])} {NightfallRotation.GetWeaponString(NightfallWeaponDrops[1])}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "> Patrol";
                x.Value = $"*Use command /patrol for more info!*";
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
            var altarTemp = new List<AltarsOfSorrowRotation.AltarsOfSorrowLink>();
            foreach (var Link in AltarsOfSorrowRotation.AltarsOfSorrowLinks)
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

                if (Link.Difficulty != null)
                {
                    if (Link.Difficulty == LostSectorDifficulty.Legend && (LegendLostSector == Link.LostSector || LegendLostSectorArmorDrop == Link.ArmorDrop))
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Legend Lost Sector is **{LostSectorRotation.GetLostSectorString(LegendLostSector)}** and is dropping **{LegendLostSectorArmorDrop}** today. I have removed your tracking, good luck!");
                    else if (Link.Difficulty == LostSectorDifficulty.Master && (MasterLostSector == Link.LostSector || MasterLostSectorArmorDrop == Link.ArmorDrop))
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Master Lost Sector is **{LostSectorRotation.GetLostSectorString(MasterLostSector)}** and is dropping **{MasterLostSectorArmorDrop}** today. I have removed your tracking, good luck!");
                    else
                        lsTemp.Add(Link);
                }
                else
                {
                    if (LegendLostSector == Link.LostSector || LegendLostSectorArmorDrop == Link.ArmorDrop)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Legend Lost Sector is **{LostSectorRotation.GetLostSectorString(LegendLostSector)}** and is dropping **{LegendLostSectorArmorDrop}** today. I have removed your tracking, good luck!");
                    else if (MasterLostSector == Link.LostSector || MasterLostSectorArmorDrop == Link.ArmorDrop)
                        await user.SendMessageAsync($"> Hey {user.Mention}! The Master Lost Sector is **{LostSectorRotation.GetLostSectorString(MasterLostSector)}** and is dropping **{MasterLostSectorArmorDrop}** today. I have removed your tracking, good luck!");
                    else
                        lsTemp.Add(Link);
                }
            }
            LostSectorRotation.LostSectorLinks = lsTemp;
            LostSectorRotation.UpdateJSON();
        }

        public static async Task CheckUsersWeeklyTracking(DiscordSocketClient Client)
        {
            var chalTemp = new List<AscendantChallengeRotation.AscendantChallengeLink>();
            foreach (var Link in AscendantChallengeRotation.AscendantChallengeLinks)
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
            AscendantChallengeRotation.AscendantChallengeLinks = chalTemp;
            AscendantChallengeRotation.UpdateJSON();

            var curseTemp = new List<CurseWeekRotation.CurseWeekLink>();
            foreach (var Link in CurseWeekRotation.CurseWeekLinks)
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
            CurseWeekRotation.CurseWeekLinks = curseTemp;
            CurseWeekRotation.UpdateJSON();

            var dscTemp = new List<DeepStoneCryptRotation.DeepStoneCryptLink>();
            foreach (var Link in DeepStoneCryptRotation.DeepStoneCryptLinks)
            {
                IUser user;
                if (Client.GetUser(Link.DiscordID) == null)
                    user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                else
                    user = Client.GetUser(Link.DiscordID);

                if (DSCChallengeEncounter == Link.Encounter)
                    await user.SendMessageAsync($"> Hey {user.Mention}! The Deep Stone Crypt challenge is **{DeepStoneCryptRotation.GetChallengeString(DSCChallengeEncounter)}** (**{DeepStoneCryptRotation.GetEncounterString(DSCChallengeEncounter)}**) this week. I have removed your tracking, good luck!");
                else
                    dscTemp.Add(Link);
            }
            DeepStoneCryptRotation.DeepStoneCryptLinks = dscTemp;
            DeepStoneCryptRotation.UpdateJSON();

            var ehuntTemp = new List<EmpireHuntRotation.EmpireHuntLink>();
            foreach (var Link in EmpireHuntRotation.EmpireHuntLinks)
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
            EmpireHuntRotation.EmpireHuntLinks = ehuntTemp;
            EmpireHuntRotation.UpdateJSON();

            var gosTemp = new List<GardenOfSalvationRotation.GardenOfSalvationLink>();
            foreach (var Link in GardenOfSalvationRotation.GardenOfSalvationLinks)
            {
                IUser user;
                if (Client.GetUser(Link.DiscordID) == null)
                    user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                else
                    user = Client.GetUser(Link.DiscordID);

                if (GoSChallengeEncounter == Link.Encounter)
                    await user.SendMessageAsync($"> Hey {user.Mention}! The Garden of Salvation challenge is **{GardenOfSalvationRotation.GetChallengeString(GoSChallengeEncounter)}** (**{GardenOfSalvationRotation.GetEncounterString(GoSChallengeEncounter)}**) this week. I have removed your tracking, good luck!");
                else
                    gosTemp.Add(Link);

            }
            GardenOfSalvationRotation.GardenOfSalvationLinks = gosTemp;
            GardenOfSalvationRotation.UpdateJSON();

            var lwTemp = new List<LastWishRotation.LastWishLink>();
            foreach (var Link in LastWishRotation.LastWishLinks)
            {
                IUser user;
                if (Client.GetUser(Link.DiscordID) == null)
                    user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                else
                    user = Client.GetUser(Link.DiscordID);

                if (LWChallengeEncounter == Link.Encounter)
                    await user.SendMessageAsync($"> Hey {user.Mention}! The Last Wish challenge is **{LastWishRotation.GetChallengeString(LWChallengeEncounter)}** (**{LastWishRotation.GetEncounterString(LWChallengeEncounter)}**) this week. I have removed your tracking, good luck!");
                else
                    lwTemp.Add(Link);
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

                if (Link.Nightfall == Nightfall || Link.WeaponDrop == NightfallWeaponDrops[0] || Link.WeaponDrop == NightfallWeaponDrops[1])
                    await user.SendMessageAsync($"> Hey {user.Mention}! The Nightfall is **{NightfallRotation.GetStrikeNameString(Nightfall)}** and is dropping **{NightfallRotation.GetWeaponString(NightfallWeaponDrops[0])}** and **{NightfallRotation.GetWeaponString(NightfallWeaponDrops[1])}** this week. I have removed your tracking, good luck!");
                else
                    nfTemp.Add(Link);
            }
            NightfallRotation.NightfallLinks = nfTemp;
            NightfallRotation.UpdateJSON();

            var nhuntTemp = new List<NightmareHuntRotation.NightmareHuntLink>();
            foreach (var Link in NightmareHuntRotation.NightmareHuntLinks)
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
            NightmareHuntRotation.NightmareHuntLinks = nhuntTemp;
            NightmareHuntRotation.UpdateJSON();

            var vogTemp = new List<VaultOfGlassRotation.VaultOfGlassLink>();
            foreach (var Link in VaultOfGlassRotation.VaultOfGlassLinks)
            {
                IUser user;
                if (Client.GetUser(Link.DiscordID) == null)
                    user = Client.Rest.GetUserAsync(Link.DiscordID).Result;
                else
                    user = Client.GetUser(Link.DiscordID);

                if (VoGChallengeEncounter == Link.Encounter)
                    await user.SendMessageAsync($"> Hey {user.Mention}! The Vault of Glass challenge is **{VaultOfGlassRotation.GetChallengeString(VoGChallengeEncounter)}** (**{VaultOfGlassRotation.GetEncounterString(VoGChallengeEncounter)}**) this week. I have removed your tracking, good luck!");
                else
                    vogTemp.Add(Link);
            }
            VaultOfGlassRotation.VaultOfGlassLinks = vogTemp;
            VaultOfGlassRotation.UpdateJSON();
        }
    }
}
