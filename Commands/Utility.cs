using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Levante.Configs;
using Levante.Helpers;
using Levante.Leaderboards;
using Levante.Rotations;
using Levante.Util;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Levante.Util.Attributes;

namespace Levante.Commands
{
    public class Utility : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("link", "Link your Bungie account to your Discord account.")]
        public async Task Link()
        {
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {BotConfig.AppName} v{String.Format("{0:0.00#}", BotConfig.Version)}"
            };
            var auth = new EmbedAuthorBuilder()
            {
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                Name = "Account Linking"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Footer = foot,
                Author = auth
            };
            var plainTextBytes = Encoding.UTF8.GetBytes($"{Context.User.Id}");
            string state = Convert.ToBase64String(plainTextBytes);

            embed.Title = $"Click here to start the linking process.";
            embed.Url = $"https://www.bungie.net/en/OAuth/Authorize?client_id={BotConfig.BungieClientID}&response_type=code&state={state}";
            embed.Description = $"- Linking allows you to start XP Tracking, quick '/guardian' commands, and more.\n" +
                $"- After linking is complete, you'll receive another DM from me to confirm.\n" +
                $"- Experienced a name change? Relinking will update your name with our data.";

            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }

        [Group("notify", "Be notified when a specific rotation is active.")]
        public class Notify : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("ada-1", "Be notified when an armor mod is for sale at Ada-1.")]
            public async Task Ada1([Summary("mod-name", "Armor mod to be alerted for."), Autocomplete(typeof(ArmorModsAutocomplete))] string ArmorModHash)
            {
                if (!long.TryParse(ArmorModHash, out long ModHashArg))
                {
                    await RespondAsync($"Invalid search, please try again. Make sure to choose one of the autocomplete options!", ephemeral: true);
                    return;
                }

                if (Ada1Rotation.GetUserTracking(Context.User.Id, out var ModHash) != null)
                {
                    await RespondAsync($"You already have tracking for Ada-1 Armor Mods. I am watching for {ManifestHelper.Ada1ArmorMods[ModHash]}.", ephemeral: true);
                    return;
                }

                Ada1Rotation.AddUserTracking(Context.User.Id, ModHashArg);
                await RespondAsync($"I will remind you when {ManifestHelper.Ada1ArmorMods[ModHashArg]} is being sold at Ada-1; I cannot provide a prediction for when it will return.", ephemeral: true);
                return;
            }

            [SlashCommand("altars-of-sorrow", "Be notified when an Altars of Sorrow weapon is active.")]
            public async Task AltarsOfSorrow([Summary("weapon", "Altars of Sorrow weapon to be alerted for."),
                Choice("Blasphemer (Shotgun)", 0), Choice("Apostate (Sniper)", 1), Choice("Heretic (Rocket)", 2)] int ArgWeapon)
            {
                if (AltarsOfSorrowRotation.GetUserTracking(Context.User.Id, out var Weapon) != null)
                {
                    await RespondAsync($"You already have tracking for Altars of Sorrow. I am watching for {AltarsOfSorrowRotation.GetWeaponNameString(Weapon)} ({Weapon}).", ephemeral: true);
                    return;
                }
                Weapon = (AltarsOfSorrow)ArgWeapon;

                AltarsOfSorrowRotation.AddUserTracking(Context.User.Id, Weapon);
                await RespondAsync($"I will remind you when {AltarsOfSorrowRotation.GetWeaponNameString(Weapon)} ({Weapon}) is in rotation, which will be on {TimestampTag.FromDateTime(AltarsOfSorrowRotation.DatePrediction(Weapon), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("ascendant-challenge", "Be notified when an Ascendant Challenge is active.")]
            public async Task AscendantChallenge([Summary("ascendant-challenge", "Ascendant Challenge to be alerted for."),
                Choice("Agonarch Abyss (Bay of Drowned Wishes)", 0), Choice("Cimmerian Garrison (Chamber of Starlight)", 1),
                Choice("Ouroborea (Aphelion's Rest)", 2), Choice("Forfeit Shrine (Gardens of Esila)", 3),
                Choice("Shattered Ruins (Spine of Keres)", 4), Choice("Keep of Honed Edges (Harbinger's Seclude)", 5)] int ArgAscendantChallenge)
            {
                if (AscendantChallengeRotation.GetUserTracking(Context.User.Id, out var AscendantChallenge) != null)
                {
                    await RespondAsync($"You already have tracking for Ascendant Challenges. I am watching for {AscendantChallengeRotation.GetChallengeNameString(AscendantChallenge)} ({AscendantChallengeRotation.GetChallengeLocationString(AscendantChallenge)}).", ephemeral: true);
                    return;
                }
                AscendantChallenge = (AscendantChallenge)ArgAscendantChallenge;

                AscendantChallengeRotation.AddUserTracking(Context.User.Id, AscendantChallenge);
                await RespondAsync($"I will remind you when {AscendantChallengeRotation.GetChallengeNameString(AscendantChallenge)} ({AscendantChallengeRotation.GetChallengeLocationString(AscendantChallenge)}) is in rotation, which will be on {TimestampTag.FromDateTime(AscendantChallengeRotation.DatePrediction(AscendantChallenge), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("curse-week", "Be notified when a Curse Week strength is active.")]
            public async Task CurseWeek([Summary("strength", "Curse Week strength to be alerted for.")] CurseWeek ArgCurseWeek)
            {
                if (CurseWeekRotation.GetUserTracking(Context.User.Id, out var CurseWeek) != null)
                {
                    await RespondAsync($"You already have tracking for Curse Weeks. I am watching for {CurseWeek} Strength.", ephemeral: true);
                    return;
                }
                CurseWeek = ArgCurseWeek;

                CurseWeekRotation.AddUserTracking(Context.User.Id, CurseWeek);
                await RespondAsync($"I will remind you when {CurseWeek} Strength is in rotation, which will be on {TimestampTag.FromDateTime(CurseWeekRotation.DatePrediction(CurseWeek), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("deep-stone-crypt", "Be notified when a Deep Stone Crypt challenge is active.")]
            public async Task DeepStoneCrypt([Summary("challenge", "Deep Stone Crypt challenge to be alerted for."),
                Choice("Crypt Security (Red Rover)", 0), Choice("Atraks-1 (Copies of Copies)", 1),
                Choice("The Descent (Of All Trades)", 2), Choice("Taniks (The Core Four)", 3)] int ArgEncounter)
            {
                if (DeepStoneCryptRotation.GetUserTracking(Context.User.Id, out var Encounter) != null)
                {
                    await RespondAsync($"You already have tracking for Deep Stone Crypt challenges. I am watching for {DeepStoneCryptRotation.GetEncounterString(Encounter)} ({DeepStoneCryptRotation.GetChallengeString(Encounter)}).", ephemeral: true);
                    return;
                }
                Encounter = (DeepStoneCryptEncounter)ArgEncounter;

                var predictedDate = DeepStoneCryptRotation.DatePrediction(Encounter);
                if (predictedDate >= FeaturedRaidRotation.DatePrediction(Raid.DeepStoneCrypt))
                    predictedDate = FeaturedRaidRotation.DatePrediction(Raid.DeepStoneCrypt);

                DeepStoneCryptRotation.AddUserTracking(Context.User.Id, Encounter);
                await RespondAsync($"I will remind you when {DeepStoneCryptRotation.GetEncounterString(Encounter)} ({DeepStoneCryptRotation.GetChallengeString(Encounter)}) is in rotation, which will be on {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("empire-hunt", "Be notified when an Empire Hunt is active.")]
            public async Task EmpireHunt([Summary("empire-hunt", "Empire Hunt boss to be alerted for."),
                Choice("Phylaks, the Warrior", 0), Choice("Praksis, the Technocrat", 1), Choice("Kridis, the Dark Priestess", 2)] int ArgHunt)
            {
                if (EmpireHuntRotation.GetUserTracking(Context.User.Id, out var Hunt) != null)
                {
                    await RespondAsync($"You already have tracking for Empire Hunts. I am watching for {EmpireHuntRotation.GetHuntBossString(Hunt)}.", ephemeral: true);
                    return;
                }
                Hunt = (EmpireHunt)ArgHunt;

                EmpireHuntRotation.AddUserTracking(Context.User.Id, Hunt);
                await RespondAsync($"I will remind you when {EmpireHuntRotation.GetHuntBossString(Hunt)} is in rotation, which will be on {TimestampTag.FromDateTime(EmpireHuntRotation.DatePrediction(Hunt), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("featured-raid", "Be notified when a raid is featured.")]
            public async Task FeaturedRaid([Summary("raid", "Legacy raid activity to be alerted for."),
                Choice("Last Wish", 0), Choice("Garden of Salvation", 1), Choice("Deep Stone Crypt", 2), Choice("Vault of Glass", 3)] int ArgRaid)
            {
                if (FeaturedRaidRotation.GetUserTracking(Context.User.Id, out var Raid) != null)
                {
                    await RespondAsync($"You already have tracking for Featured Raids. I am watching for {FeaturedRaidRotation.GetRaidString(Raid)}.", ephemeral: true);
                    return;
                }
                Raid = (Raid)ArgRaid;

                FeaturedRaidRotation.AddUserTracking(Context.User.Id, Raid);
                await RespondAsync($"I will remind you when {FeaturedRaidRotation.GetRaidString(Raid)} is the featured raid, which will be on {TimestampTag.FromDateTime(FeaturedRaidRotation.DatePrediction(Raid), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("garden-of-salvation", "Be notified when a Garden of Salvation challenge is active.")]
            public async Task GardenOfSalvation([Summary("challenge", "Garden of Salvation challenge to be alerted for."),
                Choice("Evade the Consecrated Mind (Staying Alive)", 0), Choice("Summon the Consecrated Mind (A Link to the Chain)", 1),
                Choice("Consecrated Mind (To the Top)", 2), Choice("Sanctified Mind (Zero to One Hundred)", 3)] int ArgEncounter)
            {
                if (GardenOfSalvationRotation.GetUserTracking(Context.User.Id, out var Encounter) != null)
                {
                    await RespondAsync($"You already have tracking for Garden of Salvation challenges. I am watching for {GardenOfSalvationRotation.GetEncounterString(Encounter)} ({GardenOfSalvationRotation.GetChallengeString(Encounter)}).", ephemeral: true);
                    return;
                }
                Encounter = (GardenOfSalvationEncounter)ArgEncounter;

                var predictedDate = GardenOfSalvationRotation.DatePrediction(Encounter);
                if (predictedDate >= FeaturedRaidRotation.DatePrediction(Raid.GardenOfSalvation))
                    predictedDate = FeaturedRaidRotation.DatePrediction(Raid.GardenOfSalvation);

                GardenOfSalvationRotation.AddUserTracking(Context.User.Id, Encounter);
                await RespondAsync($"I will remind you when {GardenOfSalvationRotation.GetEncounterString(Encounter)} ({GardenOfSalvationRotation.GetChallengeString(Encounter)}) is in rotation, which will be on {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("last-wish", "Be notified when a Last Wish challenge is active.")]
            public async Task LastWish([Summary("challenge", "Last Wish challenge to be alerted for."),
                Choice("Kalli (Summoning Ritual)", 0), Choice("Shuro Chi (Which Witch)", 1), Choice("Morgeth (Forever Fight)", 2),
                Choice("Vault (Keep Out)", 3), Choice("Riven (Strength of Memory)", 4)] int ArgEncounter)
            {
                if (LastWishRotation.GetUserTracking(Context.User.Id, out var Encounter) != null)
                {
                    await RespondAsync($"You already have tracking for Last Wish challenges. I am watching for {LastWishRotation.GetEncounterString(Encounter)} ({LastWishRotation.GetChallengeString(Encounter)}).", ephemeral: true);
                    return;
                }
                Encounter = (LastWishEncounter)ArgEncounter;

                var predictedDate = LastWishRotation.DatePrediction(Encounter);
                if (predictedDate >= FeaturedRaidRotation.DatePrediction(Raid.LastWish))
                    predictedDate = FeaturedRaidRotation.DatePrediction(Raid.LastWish);

                LastWishRotation.AddUserTracking(Context.User.Id, Encounter);
                await RespondAsync($"I will remind you when {LastWishRotation.GetEncounterString(Encounter)} ({LastWishRotation.GetChallengeString(Encounter)}) is in rotation, which will be on {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("lost-sector", "Be notified when a Lost Sector and/or Armor Drop is active.")]
            public async Task LostSector([Summary("lost-sector", "Lost Sector to be alerted for."), Autocomplete(typeof(LostSectorAutocomplete))] int? ArgLS = null,
                [Summary("armor-drop", "Lost Sector Exotic armor drop to be alerted for.")] ExoticArmorType? ArgEAT = null)
            {
                //await RespondAsync($"Gathering data on new Lost Sectors. Check back later!", ephemeral: true);
                //return;

                if (LostSectorRotation.GetUserTracking(Context.User.Id, out var LS, out var EAT) != null)
                {
                    if (LS == null && EAT == null)
                        await RespondAsync($"An error has occurred.", ephemeral: true);
                    else if (LS != null && EAT == null)
                        await RespondAsync($"You already have tracking for Lost Sectors. I am watching for {LostSectorRotation.GetLostSectorString((LostSector)LS)}.", ephemeral: true);
                    else if (LS == null && EAT != null)
                        await RespondAsync($"You already have tracking for Lost Sectors. I am watching for {EAT} armor drop.", ephemeral: true);
                    else if (LS != null && EAT != null)
                        await RespondAsync($"You already have tracking for Lost Sectors. I am watching for {LostSectorRotation.GetLostSectorString((LostSector)LS)} dropping {EAT}.", ephemeral: true);

                    return;
                }
                LS = (LostSector?)ArgLS;
                EAT = ArgEAT;

                if (LS == null && EAT == null)
                {
                    await RespondAsync($"You left both arguments blank; I can't track nothing!", ephemeral: true);
                    return;
                }

                LostSectorRotation.AddUserTracking(Context.User.Id, LS, EAT);
                if (LS != null && EAT == null)
                    await RespondAsync($"I will remind you when {LostSectorRotation.GetLostSectorString((LostSector)LS)} is in rotation, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                else if (LS == null && EAT != null)
                    await RespondAsync($"I will remind you when Lost Sectors are dropping {EAT}, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                else if (LS != null && EAT != null)
                    await RespondAsync($"I will remind you when {LostSectorRotation.GetLostSectorString((LostSector)LS)} is dropping {EAT}, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);

                return;
            }

            [SlashCommand("nightfall", "Be notified when a Nightfall and/or Weapon is active.")]
            public async Task Nightfall([Summary("nightfall", "Nightfall Strike to be alerted for."), Autocomplete(typeof(NightfallAutocomplete))] int? ArgNF = null,
                [Summary("weapon", "Nightfall Strike weapon drop to be alerted for."), Autocomplete(typeof(NightfallWeaponAutocomplete))] int? ArgWeapon = null)
            {
                if (NightfallRotation.GetUserTracking(Context.User.Id, out var NF, out var Weapon) != null)
                {
                    if (NF == null && Weapon == null)
                        await RespondAsync($"An error has occurred.", ephemeral: true);
                    else if (NF != null && Weapon == null)
                        await RespondAsync($"You already have tracking for Nightfalls. I am watching for {NightfallRotation.GetStrikeNameString((Nightfall)NF)}.", ephemeral: true);
                    else if (NF == null && Weapon != null)
                        await RespondAsync($"You already have tracking for Nightfalls. I am watching for {NightfallRotation.GetWeaponString((NightfallWeapon)Weapon)} weapon drops.", ephemeral: true);
                    else if (NF != null && Weapon != null)
                        await RespondAsync($"You already have tracking for Nightfalls. I am watching for {NightfallRotation.GetStrikeNameString((Nightfall)NF)} with {NightfallRotation.GetWeaponString((NightfallWeapon)Weapon)} weapon drops.", ephemeral: true);
                    return;
                }
                NF = (Nightfall?)ArgNF;
                Weapon = (NightfallWeapon?)ArgWeapon;

                if (NF == null && Weapon == null)
                {
                    await RespondAsync($"You left both arguments blank; I can't track nothing!", ephemeral: true);
                    return;
                }
                var predictDate = NightfallRotation.DatePrediction(NF, Weapon);
                if (predictDate < DateTime.Now)
                {
                    await RespondAsync($"This rotation is not possible this season.", ephemeral: true);
                    return;
                }

                NightfallRotation.AddUserTracking(Context.User.Id, NF, Weapon);
                if (NF != null && Weapon == null)
                    await RespondAsync($"I will remind you when {NightfallRotation.GetStrikeNameString((Nightfall)NF)} is in rotation, which will be on {TimestampTag.FromDateTime(predictDate, TimestampTagStyles.ShortDate)}.", ephemeral: true);
                else if (NF == null && Weapon != null)
                    await RespondAsync($"I will remind you when {NightfallRotation.GetWeaponString((NightfallWeapon)Weapon)} is in rotation, which will be on {TimestampTag.FromDateTime(predictDate, TimestampTagStyles.ShortDate)}.", ephemeral: true);
                else if (NF != null && Weapon != null)
                    await RespondAsync($"I will remind you when {NightfallRotation.GetStrikeNameString((Nightfall)NF)} is dropping {NightfallRotation.GetWeaponString((NightfallWeapon)Weapon)}, which will be on {TimestampTag.FromDateTime(predictDate, TimestampTagStyles.ShortDate)}.", ephemeral: true);
                
                //await RespondAsync($"Gathering data on new Nightfalls. Check back later!");
                //return;
            }

            [SlashCommand("nightmare-hunt", "Be notified when an Nightmare Hunt is active.")]
            public async Task NightmareHunt([Summary("nightmare-hunt", "Nightmare Hunt boss to be alerted for."),
                Choice("Despair (Crota, Son of Oryx)", 0), Choice("Fear (Phogoth, the Untamed)", 1), Choice("Rage (Dominus Ghaul)", 2),
                Choice("Isolation (Taniks, the Scarred)", 3), Choice("Servitude (Zydron, Gate Lord)", 4),
                Choice("Pride (Skolas, Kell of Kells)", 5), Choice("Anguish (Omnigul, Will of Crota)", 5),
                Choice("Insanity (Fikrul, the Fanatic)", 7)] int ArgHunt)
            {
                if (NightmareHuntRotation.GetUserTracking(Context.User.Id, out var Hunt) != null)
                {
                    await RespondAsync($"You already have tracking for Nightmare Hunts. I am watching for {NightmareHuntRotation.GetHuntNameString(Hunt)} ({NightmareHuntRotation.GetHuntBossString(Hunt)}).", ephemeral: true);
                    return;
                }
                Hunt = (NightmareHunt)ArgHunt;

                NightmareHuntRotation.AddUserTracking(Context.User.Id, Hunt);
                await RespondAsync($"I will remind you when {NightmareHuntRotation.GetHuntNameString(Hunt)} ({NightmareHuntRotation.GetHuntBossString(Hunt)}) is in rotation, which will be on {TimestampTag.FromDateTime(NightmareHuntRotation.DatePrediction(Hunt), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("vault-of-glass", "Be notified when a Vault of Glass challenge is active.")]
            public async Task VaultOfGlass([Summary("challenge", "Vault of Glass challenge to be alerted for."),
                Choice("Confluxes (Wait for It...)", 0), Choice("Oracles (The Only Oracle for You)", 1), Choice("Templar (Out of Its Way)", 2),
                Choice("Gatekeepers (Strangers in Time)", 3), Choice("Atheon (Ensemble's Refrain)", 4)] int ArgEncounter)
            {
                if (VaultOfGlassRotation.GetUserTracking(Context.User.Id, out var Encounter) != null)
                {
                    await RespondAsync($"You already have tracking for Vault of Glass challenges. I am watching for {VaultOfGlassRotation.GetEncounterString(Encounter)} ({VaultOfGlassRotation.GetChallengeString(Encounter)}).", ephemeral: true);
                    return;
                }
                Encounter = (VaultOfGlassEncounter)ArgEncounter;

                var predictedDate = VaultOfGlassRotation.DatePrediction(Encounter);
                if (predictedDate >= FeaturedRaidRotation.DatePrediction(Raid.VaultOfGlass))
                    predictedDate = FeaturedRaidRotation.DatePrediction(Raid.VaultOfGlass);

                VaultOfGlassRotation.AddUserTracking(Context.User.Id, Encounter);
                await RespondAsync($"I will remind you when {VaultOfGlassRotation.GetEncounterString(Encounter)} ({VaultOfGlassRotation.GetChallengeString(Encounter)}) is in rotation, which will be on {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("vow-of-the-disciple", "Be notified when a Vow of the Disciple challenge is active.")]
            public async Task VowOfTheDisciple([Summary("challenge", "Vow of the Disciple challenge to be alerted for."),
                Choice("Acquisition (Swift Destruction)", 0), Choice("Caretaker (Base Information)", 1),
                Choice("Exhibition (Defenses Down)", 2), Choice("Rhulk (Looping Catalyst)", 3)] int ArgEncounter)
            {
                if (VowOfTheDiscipleRotation.GetUserTracking(Context.User.Id, out var Encounter) != null)
                {
                    await RespondAsync($"You already have tracking for Vow of the Disciple challenges. I am watching for {VowOfTheDiscipleRotation.GetEncounterString(Encounter)} ({VowOfTheDiscipleRotation.GetChallengeString(Encounter)}).", ephemeral: true);
                    return;
                }
                Encounter = (VowOfTheDiscipleEncounter)ArgEncounter;

                VowOfTheDiscipleRotation.AddUserTracking(Context.User.Id, Encounter);
                await RespondAsync($"I will remind you when {VowOfTheDiscipleRotation.GetEncounterString(Encounter)} ({VowOfTheDiscipleRotation.GetChallengeString(Encounter)}) is in rotation, which will be on {TimestampTag.FromDateTime(VowOfTheDiscipleRotation.DatePrediction(Encounter), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("wellspring", "Be notified when a Wellspring boss/weapon is active.")]
            public async Task Wellspring([Summary("wellspring", "Wellspring weapon drop to be alerted for."),
                Choice("Come to Pass (Auto)", 0), Choice("Tarnation (Grenade Launcher)", 1), Choice("Fel Taradiddle (Bow)", 2), Choice("Father's Sins (Sniper)", 3)] int ArgWellspring)
            {
                if (WellspringRotation.GetUserTracking(Context.User.Id, out var Wellspring) != null)
                {
                    await RespondAsync($"You already have tracking for The Wellspring. I am watching for The Wellspring: {WellspringRotation.GetWellspringTypeString(Wellspring)} ({WellspringRotation.GetWellspringBossString(Wellspring)}), " +
                        $"which drops {WellspringRotation.GetWeaponNameString(Wellspring)} ({WellspringRotation.GetWeaponTypeString(Wellspring)}).", ephemeral: true);
                    return;
                }
                Wellspring = (Wellspring)ArgWellspring;

                WellspringRotation.AddUserTracking(Context.User.Id, Wellspring);
                await RespondAsync($"I will remind you when The Wellspring: {WellspringRotation.GetWellspringTypeString(Wellspring)} ({WellspringRotation.GetWellspringBossString(Wellspring)}), " +
                    $"which drops {WellspringRotation.GetWeaponNameString(Wellspring)} ({WellspringRotation.GetWeaponTypeString(Wellspring)}), is in rotation, " +
                    $"which will be on {TimestampTag.FromDateTime(WellspringRotation.DatePrediction(Wellspring), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("remove", "Remove an active tracking notification.")]
            public async Task Remove()
            {
                // Build a selection menu with a list of all of the active trackings a user has.
                var menuBuilder = new SelectMenuBuilder()
                    .WithPlaceholder("Select one of your active trackers")
                    .WithCustomId("notifyRemovalMenu")
                    .WithMinValues(1)
                    .WithMaxValues(1);

                if (Ada1Rotation.GetUserTracking(Context.User.Id, out var ModHash) != null)
                    menuBuilder.AddOption("Ada-1", "ada-1", $"{ManifestHelper.Ada1ArmorMods[ModHash]}");

                if (AltarsOfSorrowRotation.GetUserTracking(Context.User.Id, out var Weapon) != null)
                    menuBuilder.AddOption("Altars of Sorrow", "altars-of-sorrow", $"{AltarsOfSorrowRotation.GetWeaponNameString(Weapon)} ({Weapon})");

                if (AscendantChallengeRotation.GetUserTracking(Context.User.Id, out var Challenge) != null)
                    menuBuilder.AddOption("Ascendant Challenge", "ascendant-challenge", $"{AscendantChallengeRotation.GetChallengeNameString(Challenge)} ({AscendantChallengeRotation.GetChallengeLocationString(Challenge)})");

                if (CurseWeekRotation.GetUserTracking(Context.User.Id, out var Strength) != null)
                    menuBuilder.AddOption("Curse Week", "curse-week", $"{Strength} Strength");

                if (DeepStoneCryptRotation.GetUserTracking(Context.User.Id, out var DSCEncounter) != null)
                    menuBuilder.AddOption("Deep Stone Crypt Challenge", "dsc-challenge", $"{DeepStoneCryptRotation.GetEncounterString(DSCEncounter)} ({DeepStoneCryptRotation.GetChallengeString(DSCEncounter)})");

                if (EmpireHuntRotation.GetUserTracking(Context.User.Id, out var EmpireHunt) != null)
                    menuBuilder.AddOption("Empire Hunt", "empire-hunt", $"{EmpireHuntRotation.GetHuntBossString(EmpireHunt)}");

                if (GardenOfSalvationRotation.GetUserTracking(Context.User.Id, out var GoSEncounter) != null)
                    menuBuilder.AddOption("Garden of Salvation Challenge", "gos-challenge", $"{GardenOfSalvationRotation.GetEncounterString(GoSEncounter)} ({GardenOfSalvationRotation.GetChallengeString(GoSEncounter)})");

                if (LastWishRotation.GetUserTracking(Context.User.Id, out var LWEncounter) != null)
                    menuBuilder.AddOption("Last Wish Challenge", "lw-challenge", $"{LastWishRotation.GetEncounterString(LWEncounter)} ({LastWishRotation.GetChallengeString(LWEncounter)})");

                if (LostSectorRotation.GetUserTracking(Context.User.Id, out var LS, out var EAT) != null)
                {
                    if (LS == null && EAT == null)
                        menuBuilder.AddOption("Lost Sector", "remove-error", $"Nothing found");
                    else if (LS != null && EAT == null)
                        menuBuilder.AddOption("Lost Sector", "lost-sector", $"{LostSectorRotation.GetLostSectorString((LostSector)LS)}");
                    else if (LS == null && EAT != null)
                        menuBuilder.AddOption("Lost Sector", "lost-sector", $"{EAT} Drop");
                    else if (LS != null && EAT != null)
                        menuBuilder.AddOption("Lost Sector", "lost-sector", $"{LostSectorRotation.GetLostSectorString((LostSector)LS)} dropping {EAT}");
                }

                if (NightfallRotation.GetUserTracking(Context.User.Id, out var NF, out var NFWeapon) != null)
                {
                    if (NF == null && NFWeapon == null)
                        menuBuilder.AddOption("Nightfall", "remove-error", $"Nothing found");
                    else if (NF != null && NFWeapon == null)
                        menuBuilder.AddOption("Nightfall", "nightfall", $"{NightfallRotation.GetStrikeNameString((Nightfall)NF)}");
                    else if (NF == null && NFWeapon != null)
                        menuBuilder.AddOption("Nightfall", "nightfall", $"{NightfallRotation.GetWeaponString((NightfallWeapon)NFWeapon)} Drop");
                    else if (NF != null && NFWeapon != null)
                        menuBuilder.AddOption("Nightfall", "nightfall", $"{NightfallRotation.GetStrikeNameString((Nightfall)NF)} dropping {NightfallRotation.GetWeaponString((NightfallWeapon)NFWeapon)}");
                }

                if (NightmareHuntRotation.GetUserTracking(Context.User.Id, out var NightmareHunt) != null)
                    menuBuilder.AddOption("Nightmare Hunt", "nightmare-hunt", $"{NightmareHuntRotation.GetHuntNameString(NightmareHunt)} ({NightmareHuntRotation.GetHuntBossString(NightmareHunt)})");

                if (VaultOfGlassRotation.GetUserTracking(Context.User.Id, out var VoGEncounter) != null)
                    menuBuilder.AddOption("Vault of Glass Challenge", "vog-challenge", $"{VaultOfGlassRotation.GetEncounterString(VoGEncounter)} ({VaultOfGlassRotation.GetChallengeString(VoGEncounter)})");

                if (VowOfTheDiscipleRotation.GetUserTracking(Context.User.Id, out var VowEncounter) != null)
                    menuBuilder.AddOption("Vow of the Disciple Challenge", "vow-challenge", $"{VowOfTheDiscipleRotation.GetEncounterString(VowEncounter)} ({VowOfTheDiscipleRotation.GetChallengeString(VowEncounter)})");

                if (WellspringRotation.GetUserTracking(Context.User.Id, out var WellspringBoss) != null)
                    menuBuilder.AddOption($"The Wellspring: {WellspringRotation.GetWellspringTypeString(WellspringBoss)}", "wellspring", $"{WellspringRotation.GetWeaponNameString(WellspringBoss)} ({WellspringRotation.GetWeaponTypeString(WellspringBoss)})");

                var builder = new ComponentBuilder()
                    .WithSelectMenu(menuBuilder);

                try
                {
                    await RespondAsync($"Which rotation tracker did you want me to remove? Please dismiss this message after you are done.", ephemeral: true, components: builder.Build());
                }
                catch
                {
                    await RespondAsync($"You do not have any active trackers. Use \"/notify\" to activate your first one!", ephemeral: true);
                }
            }
        }

        [Group("next", "Be notified when a specific rotation is active.")]
        public class Next : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("altars-of-sorrow", "Find out when an Altars of Sorrow weapon is active next.")]
            public async Task AltarsOfSorrow([Summary("weapon", "Altars of Sorrow weapon to predict its next appearance."),
                Choice("Blasphemer (Shotgun)", 0), Choice("Apostate (Sniper)", 1), Choice("Heretic (Rocket)", 2)] int ArgWeapon)
            {
                AltarsOfSorrow Weapon = (AltarsOfSorrow)ArgWeapon;

                var predictedDate = AltarsOfSorrowRotation.DatePrediction(Weapon);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Altars of Sorrow";
                embed.Description =
                    $"Next occurrance of {AltarsOfSorrowRotation.GetWeaponNameString(Weapon)} ({Weapon}) " +
                        $"is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("ascendant-challenge", "Find out when an Ascendant Challenge is active next.")]
            public async Task AscendantChallenge([Summary("ascendant-challenge", "Ascendant Challenge to predict its next appearance."),
                Choice("Agonarch Abyss (Bay of Drowned Wishes)", 0), Choice("Cimmerian Garrison (Chamber of Starlight)", 1),
                Choice("Ouroborea (Aphelion's Rest)", 2), Choice("Forfeit Shrine (Gardens of Esila)", 3),
                Choice("Shattered Ruins (Spine of Keres)", 4), Choice("Keep of Honed Edges (Harbinger's Seclude)", 5)] int ArgAscendantChallenge)
            {
                AscendantChallenge AscendantChallenge = (AscendantChallenge)ArgAscendantChallenge;

                var predictedDate = AscendantChallengeRotation.DatePrediction(AscendantChallenge);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Ascendant Challenge";
                embed.Description =
                    $"Next occurrance of {AscendantChallengeRotation.GetChallengeNameString(AscendantChallenge)} " +
                        $"({AscendantChallengeRotation.GetChallengeLocationString(AscendantChallenge)}) is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("curse-week", "Find out when a Curse Week strength is active.")]
            public async Task CurseWeek([Summary("strength", "Curse Week strength.")] CurseWeek ArgCurseWeek)
            {
                CurseWeek CurseWeek = ArgCurseWeek;

                var predictedDate = CurseWeekRotation.DatePrediction(CurseWeek);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Curse Week";
                embed.Description =
                    $"Next occurrance of {CurseWeek} Curse Strength " +
                        $"is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("deep-stone-crypt", "Find out when a Deep Stone Crypt challenge is active next.")]
            public async Task DeepStoneCrypt([Summary("challenge", "Deep Stone Crypt challenge to predict its next appearance."),
                Choice("Crypt Security (Red Rover)", 0), Choice("Atraks-1 (Copies of Copies)", 1),
                Choice("The Descent (Of All Trades)", 2), Choice("Taniks (The Core Four)", 3)] int ArgEncounter)
            {
                DeepStoneCryptEncounter Encounter = (DeepStoneCryptEncounter)ArgEncounter;

                var predictedDate = DeepStoneCryptRotation.DatePrediction(Encounter);
                var predictedFeaturedDate = FeaturedRaidRotation.DatePrediction(Raid.DeepStoneCrypt);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Deep Stone Crypt";
                if (predictedDate < predictedFeaturedDate)
                    embed.Description =
                        $"Next occurrance of {DeepStoneCryptRotation.GetEncounterString(Encounter)} ({DeepStoneCryptRotation.GetChallengeString(Encounter)}) " +
                            $"is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";
                else
                    embed.Description =
                        $"Next occurrance of {DeepStoneCryptRotation.GetEncounterString(Encounter)} ({DeepStoneCryptRotation.GetChallengeString(Encounter)}) " +
                            $"is: {TimestampTag.FromDateTime(predictedFeaturedDate, TimestampTagStyles.ShortDate)}. Deep Stone Crypt will be the featured raid, making this challenge, and all others, available.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("empire-hunt", "Find out when an Empire Hunt is active next.")]
            public async Task EmpireHunt([Summary("empire-hunt", "Empire Hunt boss to predict its next appearance."),
                Choice("Phylaks, the Warrior", 0), Choice("Praksis, the Technocrat", 1), Choice("Kridis, the Dark Priestess", 2)] int ArgHunt)
            {
                EmpireHunt Hunt = (EmpireHunt)ArgHunt;

                var predictedDate = EmpireHuntRotation.DatePrediction(Hunt);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Empire Hunt";
                embed.Description =
                    $"Next occurrance of {EmpireHuntRotation.GetHuntNameString(Hunt)} " +
                        $"({EmpireHuntRotation.GetHuntBossString(Hunt)}) is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("featured-raid", "Find out when a raid is being featured next.")]
            public async Task FeaturedRaid([Summary("raid", "Legacy raid activity to predict its next appearance."),
                Choice("Last Wish", 0), Choice("Garden of Salvation", 1), Choice("Deep Stone Crypt", 2), Choice("Vault of Glass", 3)] int ArgRaid)
            {
                Raid Raid = (Raid)ArgRaid;

                var predictedDate = FeaturedRaidRotation.DatePrediction(Raid);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Featured Raid";
                embed.Description =
                    $"Next occurrance of {FeaturedRaidRotation.GetRaidString(Raid)} " +
                        $"is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("garden-of-salvation", "Find out when a Garden of Salvation challenge is active next.")]
            public async Task GardenOfSalvation([Summary("challenge", "Garden of Salvation challenge to predict its next appearance."),
                Choice("Evade the Consecrated Mind (Staying Alive)", 0), Choice("Summon the Consecrated Mind (A Link to the Chain)", 1),
                Choice("Consecrated Mind (To the Top)", 2), Choice("Sanctified Mind (Zero to One Hundred)", 3)] int ArgEncounter)
            {
                GardenOfSalvationEncounter Encounter = (GardenOfSalvationEncounter)ArgEncounter;

                var predictedDate = GardenOfSalvationRotation.DatePrediction(Encounter);
                var predictedFeaturedDate = FeaturedRaidRotation.DatePrediction(Raid.GardenOfSalvation);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Garden of Salvation";
                if (predictedDate < predictedFeaturedDate)
                    embed.Description =
                        $"Next occurrance of {GardenOfSalvationRotation.GetEncounterString(Encounter)} ({GardenOfSalvationRotation.GetChallengeString(Encounter)}) " +
                            $"is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";
                else
                    embed.Description =
                        $"Next occurrance of {GardenOfSalvationRotation.GetEncounterString(Encounter)} ({GardenOfSalvationRotation.GetChallengeString(Encounter)}) " +
                            $"is: {TimestampTag.FromDateTime(predictedFeaturedDate, TimestampTagStyles.ShortDate)}. Garden of Salvation will be the featured raid, making this challenge, and all others, available.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("last-wish", "Find out when a Last Wish challenge is active next.")]
            public async Task LastWish([Summary("challenge", "Last Wish challenge to predict its next appearance."),
                Choice("Kalli (Summoning Ritual)", 0), Choice("Shuro Chi (Which Witch)", 1), Choice("Morgeth (Forever Fight)", 2),
                Choice("Vault (Keep Out)", 3), Choice("Riven (Strength of Memory)", 4)] int ArgEncounter)
            {
                LastWishEncounter Encounter = (LastWishEncounter)ArgEncounter;

                var predictedDate = LastWishRotation.DatePrediction(Encounter);
                var predictedFeaturedDate = FeaturedRaidRotation.DatePrediction(Raid.LastWish);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Last Wish";
                if (predictedDate < predictedFeaturedDate)
                    embed.Description =
                        $"Next occurrance of {LastWishRotation.GetEncounterString(Encounter)} ({LastWishRotation.GetChallengeString(Encounter)}) " +
                            $"is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";
                else
                    embed.Description =
                        $"Next occurrance of {LastWishRotation.GetEncounterString(Encounter)} ({LastWishRotation.GetChallengeString(Encounter)}) " +
                            $"is: {TimestampTag.FromDateTime(predictedFeaturedDate, TimestampTagStyles.ShortDate)}. Last Wish will be the featured raid, making this challenge, and all others, available.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("lost-sector", "Be notified when a Lost Sector and/or Armor Drop is active.")]
            public async Task LostSector([Summary("lost-sector", "Lost Sector to predict its next appearance."), Autocomplete(typeof(LostSectorAutocomplete))] int? ArgLS = null,
                [Summary("armor-drop", "Lost Sector Exotic armor drop to predict its next appearance.")] ExoticArmorType? ArgEAT = null)
            {
                //await RespondAsync($"Gathering data on new Lost Sectors. Check back later!", ephemeral: true);
                //return;

                LostSector? LS = (LostSector?)ArgLS;
                ExoticArmorType? EAT = ArgEAT;

                var predictedDate = LostSectorRotation.DatePrediction(LS, EAT);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Lost Sectors";
                if (LS == null && EAT == null)
                    await RespondAsync($"An error has occurred. No parameters.", ephemeral: true);
                else if (LS != null && EAT == null)
                    embed.Description =
                        $"Next occurrance of {LostSectorRotation.GetLostSectorString((LostSector)LS)} is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";
                else if (LS == null && EAT != null)
                    embed.Description =
                        $"Next occurrance of Lost Sectors" +
                            $"{(EAT != null ? $" dropping {EAT}" : "")} is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";
                else if (LS != null && EAT != null)
                    embed.Description =
                        $"Next occurrance of {LostSectorRotation.GetLostSectorString((LostSector)LS)}" +
                            $"{(EAT != null ? $" dropping {EAT}" : "")} is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("nightfall", "Find out when a Nightfall and/or Weapon is active next.")]
            public async Task Nightfall([Summary("nightfall", "Nightfall Strike to predict its next appearance."), Autocomplete(typeof(NightfallAutocomplete))] int? ArgNF = null,
                [Summary("weapon", "Nightfall Strike Weapon drop."), Autocomplete(typeof(NightfallWeaponAutocomplete))] int? ArgWeapon = null)
            {
                Nightfall? NF = (Nightfall?)ArgNF;
                NightfallWeapon? Weapon = (NightfallWeapon?)ArgWeapon;

                var predictedDate = NightfallRotation.DatePrediction(NF, Weapon);

                if (NF == null && Weapon == null)
                {
                    await RespondAsync($"You left both arguments blank; I can't predict nothing!", ephemeral: true);
                    return;
                }
                var predictDate = NightfallRotation.DatePrediction(NF, Weapon);
                if (predictDate < DateTime.Now)
                {
                    await RespondAsync($"This rotation is not possible this season.", ephemeral: true);
                    return;
                }

                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Nightfall";
                if (NF != null && Weapon == null)
                    embed.Description =
                        $"Next occurrance of {NightfallRotation.GetStrikeNameString((Nightfall)NF)} is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";
                else if (NF == null && Weapon != null)
                    embed.Description =
                        $"Next occurrance of Nightfalls" +
                            $"{(Weapon != null ? $" dropping {NightfallRotation.GetWeaponString((NightfallWeapon)Weapon)}" : "")} is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";
                else if (NF != null && Weapon != null)
                    embed.Description =
                        $"Next occurrance of {NightfallRotation.GetStrikeNameString((Nightfall)NF)}" +
                            $"{(Weapon != null ? $" dropping {NightfallRotation.GetWeaponString((NightfallWeapon)Weapon)}" : "")} is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}."; 

                await RespondAsync($"", embed: embed.Build());
                //await RespondAsync($"Gathering data on new Nightfalls. Check back later!");
                //return;
            }

            [SlashCommand("nightmare-hunt", "Find out when an Nightmare Hunt is active next.")]
            public async Task NightmareHunt([Summary("nightmare-hunt", "Nightmare Hunt boss to predict its next appearance."),
                Choice("Despair (Crota, Son of Oryx)", 0), Choice("Fear (Phogoth, the Untamed)", 1), Choice("Rage (Dominus Ghaul)", 2),
                Choice("Isolation (Taniks, the Scarred)", 3), Choice("Servitude (Zydron, Gate Lord)", 4),
                Choice("Pride (Skolas, Kell of Kells)", 5), Choice("Anguish (Omnigul, Will of Crota)", 5),
                Choice("Insanity (Fikrul, the Fanatic)", 7)] int ArgHunt)
            {
                NightmareHunt Hunt = (NightmareHunt)ArgHunt;

                var predictedDate = NightmareHuntRotation.DatePrediction(Hunt);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Nightmare Hunt";
                embed.Description =
                    $"Next occurrance of {NightmareHuntRotation.GetHuntNameString(Hunt)} " +
                        $"({NightmareHuntRotation.GetHuntBossString(Hunt)}) is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("vault-of-glass", "Find out when a Vault of Glass challenge is active next.")]
            public async Task VaultOfGlass([Summary("challenge", "Vault of Glass challenge to predict its next appearance."),
                Choice("Confluxes (Wait for It...)", 0), Choice("Oracles (The Only Oracle for You)", 1), Choice("Templar (Out of Its Way)", 2),
                Choice("Gatekeepers (Strangers in Time)", 3), Choice("Atheon (Ensemble's Refrain)", 4)] int ArgEncounter)
            {
                VaultOfGlassEncounter Encounter = (VaultOfGlassEncounter)ArgEncounter;

                var predictedDate = VaultOfGlassRotation.DatePrediction(Encounter);
                var predictedFeaturedDate = FeaturedRaidRotation.DatePrediction(Raid.VaultOfGlass);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Vault of Glass";
                if (predictedDate < predictedFeaturedDate)
                    embed.Description =
                        $"Next occurrance of {VaultOfGlassRotation.GetEncounterString(Encounter)} ({VaultOfGlassRotation.GetChallengeString(Encounter)}), " +
                            $"which drops {VaultOfGlassRotation.GetChallengeRewardString(Encounter)} on Master, is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";
                else
                    embed.Description =
                        $"Next occurrance of {VaultOfGlassRotation.GetEncounterString(Encounter)} ({VaultOfGlassRotation.GetChallengeString(Encounter)}), " +
                            $"which drops {VaultOfGlassRotation.GetChallengeRewardString(Encounter)} on Master, is: {TimestampTag.FromDateTime(predictedFeaturedDate, TimestampTagStyles.ShortDate)}. " +
                            $"Vault of Glass will be the featured raid, making this challenge, and all others, available.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("vow-of-the-disciple", "Be notified when a Vow of the Disciple challenge is active.")]
            public async Task VowOfTheDisciple([Summary("challenge", "Vow of the Disciple challenge to predict its next appearance."),
                Choice("Acquisition (Swift Destruction)", 0), Choice("Caretaker (Base Information)", 1),
                Choice("Exhibition (Defenses Down)", 2), Choice("Rhulk (Looping Catalyst)", 3)] int ArgEncounter)
            {
                VowOfTheDiscipleEncounter Encounter = (VowOfTheDiscipleEncounter)ArgEncounter;

                var predictedDate = VowOfTheDiscipleRotation.DatePrediction(Encounter);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Vow of the Disciple";
                embed.Description =
                    $"Next occurrance of {VowOfTheDiscipleRotation.GetEncounterString(Encounter)} ({VowOfTheDiscipleRotation.GetChallengeString(Encounter)}) " +
                        $"is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("wellspring", "Find out when a Wellspring boss/weapon is active next.")]
            public async Task Wellspring([Summary("wellspring", "Wellspring weapon drop to predict its next appearance."),
                Choice("Come to Pass (Auto)", 0), Choice("Tarnation (Grenade Launcher)", 1), Choice("Fel Taradiddle (Bow)", 2), Choice("Father's Sins (Sniper)", 3)] int ArgWellspring)
            {
                Wellspring Wellspring = (Wellspring)ArgWellspring;

                var predictedDate = WellspringRotation.DatePrediction(Wellspring);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "The Wellspring";
                embed.Description =
                    $"Next occurrance of The Wellspring: {WellspringRotation.GetWellspringTypeString(Wellspring)} ({WellspringRotation.GetWellspringBossString(Wellspring)}) which drops {WellspringRotation.GetWeaponNameString(Wellspring)} ({WellspringRotation.GetWeaponTypeString(Wellspring)}) " +
                        $"is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }
        }

        [SlashCommand("rank", "Display a Destiny 2 leaderboard of choice.")]
        public async Task Rank([Summary("leaderboard", "Specific leaderboard to display."),
            Choice("Season Pass Level", 0), Choice("Longest XP Logging Session", 1), Choice("Most Logged XP Per Hour", 2),
            Choice("Total XP Logging Time", 3), Choice("Equipped Power Level", 4)] int ArgLeaderboard,
            [Summary("season", "Season of the specific leaderboard. Defaults to the current season."),
            Choice("Season of the Lost", 15), Choice("Season of the Risen", 16), Choice("Season of the Haunted", 17)] int Season = 17)
        {
            Leaderboard LeaderboardType = (Leaderboard)ArgLeaderboard;

            EmbedBuilder embed = new EmbedBuilder();
            switch (LeaderboardType)
            {
                case Leaderboard.Level:
                    {
                        string json = null;
                        if (Season == 15)
                            json = File.ReadAllText(LevelData.FilePathS15);
                        else if (Season == 16)
                            json = File.ReadAllText(LevelData.FilePathS16);
                        else if (Season == 17)
                            json = File.ReadAllText(LevelData.FilePath);
                        else
                        {
                            await RespondAsync($"Issue with Season number argument.");
                            return;
                        }
                        LevelData ld = JsonConvert.DeserializeObject<LevelData>(json);

                        embed = LeaderboardHelper.GetLeaderboardEmbed(ld.GetSortedLevelData(), Context.User, Season);
                        break;
                    }
                case Leaderboard.LongestSession:
                    {
                        string json = null;
                        if (Season == 15)
                            json = File.ReadAllText(LongestSessionData.FilePathS15);
                        else if (Season == 16)
                            json = File.ReadAllText(LongestSessionData.FilePathS16);
                        else if (Season == 17)
                            json = File.ReadAllText(LongestSessionData.FilePath);
                        else
                        {
                            await RespondAsync($"Issue with Season number argument.");
                            return;
                        }
                        LongestSessionData lsd = JsonConvert.DeserializeObject<LongestSessionData>(json);

                        embed = LeaderboardHelper.GetLeaderboardEmbed(lsd.GetSortedLevelData(), Context.User, Season);
                        break;
                    }
                case Leaderboard.XPPerHour:
                    {
                        string json = null;
                        if (Season == 15)
                            json = File.ReadAllText(XPPerHourData.FilePathS15);
                        else if (Season == 16)
                            json = File.ReadAllText(XPPerHourData.FilePathS16);
                        else if (Season == 17)
                            json = File.ReadAllText(XPPerHourData.FilePath);
                        else
                        {
                            await RespondAsync($"Issue with Season number argument.");
                            return;
                        }
                        XPPerHourData xph = JsonConvert.DeserializeObject<XPPerHourData>(json);

                        embed = LeaderboardHelper.GetLeaderboardEmbed(xph.GetSortedLevelData(), Context.User, Season);
                        break;
                    }
                case Leaderboard.MostXPLoggingTime:
                    {
                        string json = null;
                        if (Season == 15)
                            json = File.ReadAllText(MostXPLoggingTimeData.FilePathS15);
                        else if (Season == 16)
                            json = File.ReadAllText(MostXPLoggingTimeData.FilePathS16);
                        else if (Season == 17)
                            json = File.ReadAllText(MostXPLoggingTimeData.FilePath);
                        else
                        {
                            await RespondAsync($"Issue with Season number argument.");
                            return;
                        }
                        MostXPLoggingTimeData mttd = JsonConvert.DeserializeObject<MostXPLoggingTimeData>(json);

                        embed = LeaderboardHelper.GetLeaderboardEmbed(mttd.GetSortedLevelData(), Context.User, Season);
                        break;
                    }
                case Leaderboard.PowerLevel:
                    {
                        string json = null;
                        if (Season == 15)
                            json = File.ReadAllText(PowerLevelData.FilePathS15);
                        else if (Season == 16)
                            json = File.ReadAllText(PowerLevelData.FilePathS16);
                        else if (Season == 17)
                            json = File.ReadAllText(PowerLevelData.FilePath);
                        else
                        {
                            await RespondAsync($"Issue with Season number argument.");
                            return;
                        }
                        PowerLevelData pld = JsonConvert.DeserializeObject<PowerLevelData>(json);

                        embed = LeaderboardHelper.GetLeaderboardEmbed(pld.GetSortedLevelData(), Context.User, Season);
                        break;
                    }
            }

            await RespondAsync($"", embed: embed.Build());
        }

        [RequireBungieOauth]
        [SlashCommand("unlink", "Unlink your Bungie tag from your Discord account.")]
        public async Task Unlink([Summary("delete-leaderboards", "Delete your leaderboard stats when you unlink. This is true by default.")]bool RemoveLeaderboard = true)
        {
            var linkedUser = DataConfig.GetLinkedUser(Context.User.Id);
            DataConfig.DeleteUserFromConfig(Context.User.Id);

            // Remove leaderboard data to respect user data.
            if (RemoveLeaderboard)
            {
                LevelData.DeleteEntryFromConfig(linkedUser.UniqueBungieName);
                LongestSessionData.DeleteEntryFromConfig(linkedUser.UniqueBungieName);
                MostXPLoggingTimeData.DeleteEntryFromConfig(linkedUser.UniqueBungieName);
                PowerLevelData.DeleteEntryFromConfig(linkedUser.UniqueBungieName);
                XPPerHourData.DeleteEntryFromConfig(linkedUser.UniqueBungieName);
            }

            await RespondAsync($"Your Bungie account: {linkedUser.UniqueBungieName} has been unlinked. Use the command \"/link\" if you want to re-link!", ephemeral: true);
        }

        // Attempt to add Autocomplete to /next and /notify. Ran into issue with activities that have 2 rotations, like Lost Sectors and Nightfalls.

        //[SlashCommand("notify-test", "Display a Destiny 2 leaderboard of choice.")]
        //public async Task Test([Summary("rotation-type", "The activity with rotations of interest."), Autocomplete(typeof(RotationAutocomplete))] string Rotation,
        //    [Summary("rotation", "The rotation of interest."), Autocomplete(typeof(RotationArgAutocomplete))] string Arg)
        //{
        //    var next = new Next();
        //    switch (Rotation)
        //    {
        //        case "Altars of Sorrow":
        //            {
        //                AltarsOfSorrow Weapon = Enum.Parse<AltarsOfSorrow>(Arg);

        //                var predictedDate = AltarsOfSorrowRotation.DatePrediction(Weapon);
        //                var embed = new EmbedBuilder()
        //                {
        //                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
        //                };
        //                embed.Title = "Altars of Sorrow";
        //                embed.Description =
        //                    $"Next occurrance of {AltarsOfSorrowRotation.GetWeaponNameString(Weapon)} ({Weapon}) " +
        //                        $"is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

        //                await RespondAsync($"", embed: embed.Build());
        //                return;
        //            }
        //        default: return;
        //    }
        //}

        //public class RotationAutocomplete : AutocompleteHandler
        //{
        //    List<AutocompleteResult> resultOptions = new()
        //    {
        //        new AutocompleteResult("Altars of Sorrow", "Altars of Sorrow"),
        //        new AutocompleteResult("Ascendant Challenge", "Ascendant Challenge"),
        //        new AutocompleteResult("Dreaming City Curse Strength", "Dreaming City Curse Strength"),
        //        new AutocompleteResult("Deep Stone Crypt Challenge", "Deep Stone Crypt Challenge"),
        //        new AutocompleteResult("Empire Hunt", "Empire Hunt"),
        //        new AutocompleteResult("Featured Raid", "Featured Raid"),
        //        //new AutocompleteResult("Featured Dungeon", "Featured Dungeon"),
        //        new AutocompleteResult("Garden of Salvation Challenge", "Garden of Salvation Challenge"),
        //        new AutocompleteResult("Last Wish Challenge", "Last Wish Challenge"),
        //        new AutocompleteResult("Lost Sector", "Lost Sector"),
        //        new AutocompleteResult("Nightfall", "Nightfall"),
        //        new AutocompleteResult("Nightmare Hunt", "Nightmare Hunt"),
        //        //new AutocompleteResult("Shadowkeep Mission Rotation", "Shadowkeep Mission Rotation"),
        //        new AutocompleteResult("Vault of Glass Challenge", "Vault of Glass Challenge"),
        //        new AutocompleteResult("Vow of the Disciple Challenge", "Vow of the Disciple Challenge"),
        //        new AutocompleteResult("Wellspring", "Wellspring"),
        //        new AutocompleteResult("Witch Queen Mission Rotation", "Witch Queen Mission Rotation"),
        //    };

        //    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        //    {
        //        await Task.Delay(0);
        //        // Create a collection with suggestions for autocomplete
                
        //        List<AutocompleteResult> results = new();
        //        string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();

        //        if (String.IsNullOrWhiteSpace(SearchQuery))
        //            results = resultOptions;
        //        else
        //            foreach (var Rotation in resultOptions)
        //                if (Rotation.Name.ToLower().Contains(SearchQuery.ToLower()))
        //                    results.Add(Rotation);

        //        results = results.OrderBy(x => x.Name).ToList();

        //        // max - 25 suggestions at a time (API limit)
        //        Console.WriteLine($"Completion Success");
        //        return AutocompletionResult.FromSuccess(results);
        //    }
        //}

        //public class RotationArgAutocomplete : AutocompleteHandler
        //{
        //    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        //    {
        //        await Task.Delay(0);
        //        // Create a collection with suggestions for autocomplete

        //        List<AutocompleteResult> results = new();
        //        string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();

        //        var rotation = autocompleteInteraction.Data.Options.FirstOrDefault(x => x.Name.Equals("rotation-type")).Value.ToString();
        //        Console.WriteLine($"Parsing: {rotation}");
        //        switch ($"{rotation}")
        //        {
        //            case "Altars of Sorrow":
        //                {
        //                    Console.WriteLine("Altars of Sorrow");
        //                    for (int i = 0; i < AltarsOfSorrowRotation.AltarWeaponCount; i++)
        //                        results.Add(new AutocompleteResult($"{AltarsOfSorrowRotation.GetWeaponNameString((AltarsOfSorrow)i)} ({(AltarsOfSorrow)i})", $"{(AltarsOfSorrow)i}"));
        //                    break;
        //                }
        //            case "Ascendant Challenge":
        //                {
        //                    Console.WriteLine("Ascendant Challenge");
        //                    for (int i = 0; i < AscendantChallengeRotation.AscendantChallengeCount; i++)
        //                        results.Add(new AutocompleteResult($"{AscendantChallengeRotation.GetChallengeNameString((AscendantChallenge)i)} ({AscendantChallengeRotation.GetChallengeLocationString((AscendantChallenge)i)})", $"{(AscendantChallenge)i}"));
        //                    break;
        //                }
        //            case "Dreaming City Curse Strength":
        //                {
        //                    Console.WriteLine("Dreaming City Curse Strength");
        //                    for (int i = 0; i < CurseWeekRotation.CurseWeekCount; i++)
        //                        results.Add(new AutocompleteResult($"{(CurseWeek)i}", $"{(CurseWeek)i}"));
        //                    break;
        //                }
        //            case "Deep Stone Crypt Challenge":
        //                {
        //                    Console.WriteLine("Deep Stone Crypt Challenge");
        //                    for (int i = 0; i < DeepStoneCryptRotation.DeepStoneCryptEncounterCount; i++)
        //                        results.Add(new AutocompleteResult($"{DeepStoneCryptRotation.GetEncounterString((DeepStoneCryptEncounter)i)} ({DeepStoneCryptRotation.GetChallengeString((DeepStoneCryptEncounter)i)})", $"{(DeepStoneCryptEncounter)i}"));
        //                    break;
        //                }
        //            case "Empire Hunt":
        //                {
        //                    Console.WriteLine("Empire Hunt");
        //                    for (int i = 0; i < EmpireHuntRotation.EmpireHuntCount; i++)
        //                        results.Add(new AutocompleteResult($"{EmpireHuntRotation.GetHuntNameString((EmpireHunt)i)} ({EmpireHuntRotation.GetHuntBossString((EmpireHunt)i)})", $"{(EmpireHunt)i}"));
        //                    break;
        //                }
        //            case "Featured Raid":
        //                {
        //                    Console.WriteLine("Featured Raid");
        //                    for (int i = 0; i < FeaturedRaidRotation.FeaturedRaidCount; i++)
        //                        results.Add(new AutocompleteResult($"{FeaturedRaidRotation.GetRaidString((Raid)i)}", $"{(Raid)i}"));
        //                    break;
        //                }
        //            case "Garden of Salvation Challenge":
        //                {
        //                    Console.WriteLine("Garden of Salvation Challenge");
        //                    for (int i = 0; i < GardenOfSalvationRotation.GardenOfSalvationEncounterCount; i++)
        //                        results.Add(new AutocompleteResult($"{GardenOfSalvationRotation.GetEncounterString((GardenOfSalvationEncounter)i)} ({GardenOfSalvationRotation.GetChallengeString((GardenOfSalvationEncounter)i)})", $"{(GardenOfSalvationEncounter)i}"));
        //                    break;
        //                }
        //            case "Last Wish Challenge":
        //                {
        //                    Console.WriteLine("Last Wish Challenge");
        //                    for (int i = 0; i < LastWishRotation.LastWishEncounterCount; i++)
        //                        results.Add(new AutocompleteResult($"{LastWishRotation.GetEncounterString((LastWishEncounter)i)} ({LastWishRotation.GetChallengeString((LastWishEncounter)i)})", $"{(LastWishEncounter)i}"));
        //                    break;
        //                }
        //            case "Lost Sector":
        //                {
        //                    Console.WriteLine("Lost Sector");
        //                    for (int i = 0; i < LastWishRotation.LastWishEncounterCount; i++)
        //                        results.Add(new AutocompleteResult($"{LastWishRotation.GetEncounterString((LastWishEncounter)i)} ({LastWishRotation.GetChallengeString((LastWishEncounter)i)})", $"{(LastWishEncounter)i}"));
        //                    break;
        //                }
        //            default: break;
        //        }

        //        // max - 25 suggestions at a time (API limit)
        //        Console.WriteLine($"Completion Success");
        //        return AutocompletionResult.FromSuccess(results);
        //    }
        //}
    }
}
