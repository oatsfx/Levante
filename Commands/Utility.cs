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

namespace Levante.Commands
{
    public class Utility : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("link", "Link your Bungie account to your Discord account.")]
        public async Task Link()
        {
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {BotConfig.AppName} v{BotConfig.Version}"
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
            [SlashCommand("altars-of-sorrow", "Be notified when an Altars of Sorrow weapon is active.")]
            public async Task AltarsOfSorrow([Summary("weapon", "Altars of Sorrow weapon."),
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
            public async Task AscendantChallenge([Summary("ascendant-challenge", "Ascendant Challenge name."),
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
            public async Task CurseWeek([Summary("strength", "Curse Week strength.")] CurseWeek ArgCurseWeek)
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
            public async Task DeepStoneCrypt([Summary("challenge", "Deep Stone Crypt challenge."),
                Choice("Crypt Security (Red Rover)", 0), Choice("Atraks-1 (Copies of Copies)", 1),
                Choice("The Descent (Of All Trades)", 2), Choice("Taniks (The Core Four)", 3)] int ArgEncounter)
            {
                if (DeepStoneCryptRotation.GetUserTracking(Context.User.Id, out var Encounter) != null)
                {
                    await RespondAsync($"You already have tracking for Deep Stone Crypt challenges. I am watching for {DeepStoneCryptRotation.GetEncounterString(Encounter)} ({DeepStoneCryptRotation.GetChallengeString(Encounter)}).", ephemeral: true);
                    return;
                }
                Encounter = (DeepStoneCryptEncounter)ArgEncounter;

                DeepStoneCryptRotation.AddUserTracking(Context.User.Id, Encounter);
                await RespondAsync($"I will remind you when {DeepStoneCryptRotation.GetEncounterString(Encounter)} ({DeepStoneCryptRotation.GetChallengeString(Encounter)}) is in rotation, which will be on {TimestampTag.FromDateTime(DeepStoneCryptRotation.DatePrediction(Encounter), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("empire-hunt", "Be notified when an Empire Hunt is active.")]
            public async Task EmpireHunt([Summary("empire-hunt", "Empire Hunt boss."),
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

            [SlashCommand("garden-of-salvation", "Be notified when a Garden of Salvation challenge is active.")]
            public async Task GardenOfSalvation([Summary("challenge", "Garden of Salvation challenge."),
                Choice("Evade the Consecrated Mind (Staying Alive)", 0), Choice("Summon the Consecrated Mind (A Link to the Chain)", 1),
                Choice("Consecrated Mind (To the Top)", 2), Choice("Sanctified Mind (Zero to One Hundred)", 3)] int ArgEncounter)
            {
                if (GardenOfSalvationRotation.GetUserTracking(Context.User.Id, out var Encounter) != null)
                {
                    await RespondAsync($"You already have tracking for Garden of Salvation challenges. I am watching for {GardenOfSalvationRotation.GetEncounterString(Encounter)} ({GardenOfSalvationRotation.GetChallengeString(Encounter)}).", ephemeral: true);
                    return;
                }
                Encounter = (GardenOfSalvationEncounter)ArgEncounter;

                GardenOfSalvationRotation.AddUserTracking(Context.User.Id, Encounter);
                await RespondAsync($"I will remind you when {GardenOfSalvationRotation.GetEncounterString(Encounter)} ({GardenOfSalvationRotation.GetChallengeString(Encounter)}) is in rotation, which will be on {TimestampTag.FromDateTime(GardenOfSalvationRotation.DatePrediction(Encounter), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("last-wish", "Be notified when a Last Wish challenge is active.")]
            public async Task LastWish([Summary("challenge", "Last Wish challenge."),
                Choice("Kalli (Summoning Ritual)", 0), Choice("Shuro Chi (Which Witch)", 1), Choice("Morgeth (Forever Fight)", 2),
                Choice("Vault (Keep Out)", 3), Choice("Riven (Strength of Memory)", 4)] int ArgEncounter)
            {
                if (LastWishRotation.GetUserTracking(Context.User.Id, out var Encounter) != null)
                {
                    await RespondAsync($"You already have tracking for Last Wish challenges. I am watching for {LastWishRotation.GetEncounterString(Encounter)} ({LastWishRotation.GetChallengeString(Encounter)}).", ephemeral: true);
                    return;
                }
                Encounter = (LastWishEncounter)ArgEncounter;

                LastWishRotation.AddUserTracking(Context.User.Id, Encounter);
                await RespondAsync($"I will remind you when {LastWishRotation.GetEncounterString(Encounter)} ({LastWishRotation.GetChallengeString(Encounter)}) is in rotation, which will be on {TimestampTag.FromDateTime(LastWishRotation.DatePrediction(Encounter), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("lost-sector", "Be notified when a Lost Sector and/or Armor Drop is active.")]
            public async Task LostSector([Summary("lost-sector", "Lost Sector name."),
                Choice("K1 Crew Quarters", 0), Choice("K1 Logistics", 1), Choice("K1 Revelation", 2),
                Choice("K1 Communion", 3), Choice("The Conflux", 4), Choice("Metamorphosis", 5),
                Choice("Sepulcher", 6), Choice("Extraction", 7), Choice("Excavation Site XII", 8),
                Choice("Skydock IV", 9), Choice("The Quarry", 10)] int? ArgLS = null,
                [Summary("armor-drop", "Lost Sector Exotic armor drop.")] ExoticArmorType? ArgEAT = null)
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

                LostSectorRotation.AddUserTracking(Context.User.Id, LS, EAT);
                if (LS == null && EAT == null)
                    await RespondAsync($"An error has occurred.", ephemeral: true);
                else if (LS != null && EAT == null)
                    await RespondAsync($"I will remind you when {LostSectorRotation.GetLostSectorString((LostSector)LS)} is in rotation, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                else if (LS == null && EAT != null)
                    await RespondAsync($"I will remind you when Lost Sectors are dropping {EAT}, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                else if (LS != null && EAT != null)
                    await RespondAsync($"I will remind you when {LostSectorRotation.GetLostSectorString((LostSector)LS)} is dropping {EAT}, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);

                return;
            }

            [SlashCommand("nightfall", "Be notified when a Nightfall and/or Weapon is active.")]
            public async Task Nightfall([Summary("nightfall", "Nightfall Strike."),
                Choice("Proving Grounds", 0), Choice("The Insight Terminus", 1), Choice("Warden of Nothing", 2),
                Choice("The Corrupted", 3), Choice("The Inverted Spire", 4), Choice("The Arms Dealer", 5)] int? ArgNF = null,
                [Summary("weapon", "Nightfall Strike Weapon drop."),
                Choice("Silicon Neuroma", 0), Choice("D.F.A.", 1), Choice("Duty Bound", 2),
                Choice("Horror's Least", 3), Choice("The Hothead", 4), Choice("PLUG ONE.1", 5)] int? ArgWeapon = null)
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
                Weapon = (NightfallWeapon)ArgWeapon;

                NightfallRotation.AddUserTracking(Context.User.Id, NF, Weapon);
                if (NF == null && Weapon == null)
                    await RespondAsync($"An error has occurred.", ephemeral: true);
                else if (NF != null && Weapon == null)
                    await RespondAsync($"I will remind you when {NightfallRotation.GetStrikeNameString((Nightfall)NF)} is in rotation, which will be on {TimestampTag.FromDateTime(NightfallRotation.DatePrediction(NF, Weapon), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                else if (NF == null && Weapon != null)
                    await RespondAsync($"I will remind you when {NightfallRotation.GetWeaponString((NightfallWeapon)Weapon)} is in rotation, which will be on {TimestampTag.FromDateTime(NightfallRotation.DatePrediction(NF, Weapon), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                else if (NF != null && Weapon != null)
                    await RespondAsync($"I will remind you when {NightfallRotation.GetStrikeNameString((Nightfall)NF)} is dropping {NightfallRotation.GetWeaponString((NightfallWeapon)Weapon)}, which will be on {TimestampTag.FromDateTime(NightfallRotation.DatePrediction(NF, Weapon), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                
                //await RespondAsync($"Gathering data on new Nightfalls. Check back later!");
                //return;
            }

            [SlashCommand("nightmare-hunt", "Be notified when an Nightmare Hunt is active.")]
            public async Task NightmareHunt([Summary("nightmare-hunt", "Nightmare Hunt boss."),
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
            public async Task VaultOfGlass([Summary("challenge", "Vault of Glass challenge."),
                Choice("Confluxes (Wait for It...)", 0), Choice("Oracles (The Only Oracle for You)", 1), Choice("Templar (Out of Its Way)", 2),
                Choice("Gatekeepers (Strangers in Time)", 3), Choice("Atheon (Ensemble's Refrain)", 4)] int ArgEncounter)
            {
                if (VaultOfGlassRotation.GetUserTracking(Context.User.Id, out var Encounter) != null)
                {
                    await RespondAsync($"You already have tracking for Vault of Glass challenges. I am watching for {VaultOfGlassRotation.GetEncounterString(Encounter)} ({VaultOfGlassRotation.GetChallengeString(Encounter)}).", ephemeral: true);
                    return;
                }
                Encounter = (VaultOfGlassEncounter)ArgEncounter;

                VaultOfGlassRotation.AddUserTracking(Context.User.Id, Encounter);
                await RespondAsync($"I will remind you when {VaultOfGlassRotation.GetEncounterString(Encounter)} ({VaultOfGlassRotation.GetChallengeString(Encounter)}) is in rotation, which will be on {TimestampTag.FromDateTime(VaultOfGlassRotation.DatePrediction(Encounter), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
            }

            [SlashCommand("vow-of-the-disciple", "Be notified when a Vow of the Disciple challenge is active.")]
            public async Task VowOfTheDisciple([Summary("challenge", "Vow of the Disciple challenge."),
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
            public async Task Wellspring([Summary("wellspring", "Wellspring weapon drop."),
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
            public async Task AltarsOfSorrow([Summary("weapon", "Altars of Sorrow weapon."),
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
            public async Task AscendantChallenge([Summary("ascendant-challenge", "Ascendant Challenge name."),
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
            public async Task DeepStoneCrypt([Summary("challenge", "Deep Stone Crypt challenge."),
                Choice("Crypt Security (Red Rover)", 0), Choice("Atraks-1 (Copies of Copies)", 1),
                Choice("The Descent (Of All Trades)", 2), Choice("Taniks (The Core Four)", 3)] int ArgEncounter)
            {
                DeepStoneCryptEncounter Encounter = (DeepStoneCryptEncounter)ArgEncounter;

                var predictedDate = DeepStoneCryptRotation.DatePrediction(Encounter);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Deep Stone Crypt";
                embed.Description =
                    $"Next occurrance of {DeepStoneCryptRotation.GetEncounterString(Encounter)} ({DeepStoneCryptRotation.GetChallengeString(Encounter)}) " +
                        $"is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("empire-hunt", "Find out when an Empire Hunt is active next.")]
            public async Task EmpireHunt([Summary("empire-hunt", "Empire Hunt boss."),
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

            [SlashCommand("garden-of-salvation", "Find out when a Garden of Salvation challenge is active next.")]
            public async Task GardenOfSalvation([Summary("challenge", "Garden of Salvation challenge."),
                Choice("Evade the Consecrated Mind (Staying Alive)", 0), Choice("Summon the Consecrated Mind (A Link to the Chain)", 1),
                Choice("Consecrated Mind (To the Top)", 2), Choice("Sanctified Mind (Zero to One Hundred)", 3)] int ArgEncounter)
            {
                GardenOfSalvationEncounter Encounter = (GardenOfSalvationEncounter)ArgEncounter;

                var predictedDate = GardenOfSalvationRotation.DatePrediction(Encounter);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Garden of Salvation";
                embed.Description =
                    $"Next occurrance of {GardenOfSalvationRotation.GetEncounterString(Encounter)} ({GardenOfSalvationRotation.GetChallengeString(Encounter)}) " +
                        $"is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("last-wish", "Find out when a Last Wish challenge is active next.")]
            public async Task LastWish([Summary("challenge", "Last Wish challenge."),
                Choice("Kalli (Summoning Ritual)", 0), Choice("Shuro Chi (Which Witch)", 1), Choice("Morgeth (Forever Fight)", 2),
                Choice("Vault (Keep Out)", 3), Choice("Riven (Strength of Memory)", 4)] int ArgEncounter)
            {
                LastWishEncounter Encounter = (LastWishEncounter)ArgEncounter;

                var predictedDate = LastWishRotation.DatePrediction(Encounter);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Last Wish";
                embed.Description =
                    $"Next occurrance of {LastWishRotation.GetEncounterString(Encounter)} ({LastWishRotation.GetChallengeString(Encounter)}) " +
                        $"is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("lost-sector", "Be notified when a Lost Sector and/or Armor Drop is active.")]
            public async Task LostSector([Summary("lost-sector", "Lost Sector name."),
                Choice("K1 Crew Quarters", 0), Choice("K1 Logistics", 1), Choice("K1 Revelation", 2),
                Choice("K1 Communion", 3), Choice("The Conflux", 4), Choice("Metamorphosis", 5),
                Choice("Sepulcher", 6), Choice("Extraction", 7), Choice("Excavation Site XII", 8),
                Choice("Skydock IV", 9), Choice("The Quarry", 10)] int? ArgLS = null,
                [Summary("armor-drop", "Lost Sector Exotic armor drop.")] ExoticArmorType? ArgEAT = null)
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
            public async Task Nightfall([Summary("nightfall", "Nightfall Strike."),
                Choice("Proving Grounds", 0), Choice("The Insight Terminus", 1), Choice("Warden of Nothing", 2),
                Choice("The Corrupted", 3), Choice("The Inverted Spire", 4), Choice("The Arms Dealer", 5)] int? ArgNF = null,
                [Summary("weapon", "Nightfall Strike Weapon drop."),
                Choice("Silicon Neuroma", 0), Choice("D.F.A.", 1), Choice("Duty Bound", 2),
                Choice("Horror's Least", 3), Choice("The Hothead", 4), Choice("PLUG ONE.1", 5)] int? ArgWeapon = null)
            {
                Nightfall? NF = (Nightfall?)ArgNF;
                NightfallWeapon? Weapon = (NightfallWeapon?)ArgWeapon;

                var predictedDate = NightfallRotation.DatePrediction(NF, Weapon);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Nightfall";
                if (NF == null && Weapon == null)
                    await RespondAsync($"An error has occurred. No parameters.", ephemeral: true);
                else if (NF != null && Weapon == null)
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
            public async Task NightmareHunt([Summary("nightmare-hunt", "Nightmare Hunt boss."),
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
            public async Task VaultOfGlass([Summary("challenge", "Vault of Glass challenge."),
                Choice("Confluxes (Wait for It...)", 0), Choice("Oracles (The Only Oracle for You)", 1), Choice("Templar (Out of Its Way)", 2),
                Choice("Gatekeepers (Strangers in Time)", 3), Choice("Atheon (Ensemble's Refrain)", 4)] int ArgEncounter)
            {
                VaultOfGlassEncounter Encounter = (VaultOfGlassEncounter)ArgEncounter;

                var predictedDate = VaultOfGlassRotation.DatePrediction(Encounter);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Vault of Glass";
                embed.Description =
                    $"Next occurrance of {VaultOfGlassRotation.GetEncounterString(Encounter)} ({VaultOfGlassRotation.GetChallengeString(Encounter)}), " +
                        $"which drops {VaultOfGlassRotation.GetChallengeRewardString(Encounter)} on Master, is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("vow-of-the-disciple", "Be notified when a Vow of the Disciple challenge is active.")]
            public async Task VowOfTheDisciple([Summary("challenge", "Vow of the Disciple challenge."),
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
            public async Task Wellspring([Summary("wellspring", "Wellspring weapon drop."),
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

        [SlashCommand("unlink", "Unlink your Bungie tag from your Discord account.")]
        public async Task Unlink()
        {
            if (!DataConfig.IsExistingLinkedUser(Context.User.Id))
            {
                await RespondAsync("You do not have a Bungie account linked. Use the command \"/link\" to begin the linking process!", ephemeral: true);
                return;
            }

            var linkedUser = DataConfig.GetLinkedUser(Context.User.Id);
            DataConfig.DeleteUserFromConfig(Context.User.Id);

            // Remove leaderboard data to respect user data.
            LevelData.DeleteEntryFromConfig(linkedUser.UniqueBungieName);
            LongestSessionData.DeleteEntryFromConfig(linkedUser.UniqueBungieName);
            MostXPLoggingTimeData.DeleteEntryFromConfig(linkedUser.UniqueBungieName);
            PowerLevelData.DeleteEntryFromConfig(linkedUser.UniqueBungieName);
            XPPerHourData.DeleteEntryFromConfig(linkedUser.UniqueBungieName);

            await RespondAsync($"Your Bungie account: {linkedUser.UniqueBungieName} has been unlinked. Use the command \"/link\" if you want to re-link!", ephemeral: true);
        }
    }
}
