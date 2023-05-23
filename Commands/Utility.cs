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
using System.Xml.Linq;
using Levante.Rotations.Interfaces;
using System.Diagnostics.Metrics;
using Microsoft.VisualBasic;
using System.Diagnostics;

namespace Levante.Commands
{
    public class Utility : InteractionModuleBase<ShardedInteractionContext>
    {
        [SlashCommand("link", "Link your Bungie account to your Discord account through Levante.")]
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
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
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

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Link with Levante", style: ButtonStyle.Link, url: $"https://www.bungie.net/en/OAuth/Authorize?client_id={BotConfig.BungieClientID}&response_type=code&state={state}", emote: Emote.Parse(Emotes.Logo), row: 0);

            await RespondAsync(embed: embed.Build(), components: buttonBuilder.Build(), ephemeral: true);
        }

        [Group("notify", "Be notified when a specific rotation is active.")]
        public class Notify : InteractionModuleBase<ShardedInteractionContext>
        {
            [SlashCommand("ada-1", "Be notified when an armor mod is for sale at Ada-1.")]
            public async Task Ada1([Summary("name", "Item to be alerted for."), Autocomplete(typeof(Ada1ItemsAutocomplete))] string Hash)
            {
                if (!long.TryParse(Hash, out long HashArg))
                {
                    var embed = Embeds.GetErrorEmbed();
                    embed.Description = $"Invalid search, please try again. Make sure to choose one of the autocomplete options!";

                    await RespondAsync(embed: embed.Build(), ephemeral: true);
                    return;
                }

                var tracking = CurrentRotations.Ada1.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Ada-1 Items. I am watching for {ManifestHelper.Ada1Items[tracking.Hash]}.", ephemeral: true);
                    return;
                }

                tracking = new Ada1Link { DiscordID = Context.User.Id, Hash = HashArg };
                CurrentRotations.Ada1.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is being sold at Ada-1; I cannot provide a prediction for when it will return.", ephemeral: true);
            }

            [SlashCommand("altars-of-sorrow", "Be notified when an Altars of Sorrow weapon is active.")]
            public async Task AltarsOfSorrow([Summary("weapon", "Altars of Sorrow weapon to be alerted for."), Autocomplete(typeof(AltarsOfSorrowAutocomplete))] int Weapon)
            {
                var tracking = CurrentRotations.AltarsOfSorrow.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Altars of Sorrow. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new AltarsOfSorrowLink { DiscordID = Context.User.Id, WeaponDrop = Weapon };
                CurrentRotations.AltarsOfSorrow.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.AltarsOfSorrow.DatePrediction(Weapon, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }
            
            [SlashCommand("ascendant-challenge", "Be notified when an Ascendant Challenge is active.")]
            public async Task AscendantChallenge([Summary("ascendant-challenge", "Ascendant Challenge to be alerted for."), Autocomplete(typeof(AscendantChallengeAutocomplete))] int AscendantChallenge)
            {
                var tracking = CurrentRotations.AscendantChallenge.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Ascendant Challenges. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new AscendantChallengeLink { DiscordID = Context.User.Id, AscendantChallenge = AscendantChallenge };
                CurrentRotations.AscendantChallenge.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.AscendantChallenge.DatePrediction(AscendantChallenge, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            [SlashCommand("curse-week", "Be notified when a Curse Week strength is active.")]
            public async Task CurseWeek([Summary("strength", "Curse Week strength to be alerted for."), Autocomplete(typeof(CurseWeekAutocomplete))] int CurseWeek)
            {
                var tracking = CurrentRotations.CurseWeek.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Dreaming City Curse Weeks. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new CurseWeekLink { DiscordID = Context.User.Id, Strength = CurseWeek };
                CurrentRotations.CurseWeek.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.CurseWeek.DatePrediction(CurseWeek, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            [SlashCommand("deep-stone-crypt", "Be notified when a Deep Stone Crypt challenge is active.")]
            public async Task DeepStoneCrypt([Summary("challenge", "Deep Stone Crypt challenge to be alerted for."), Autocomplete(typeof(DeepStoneCryptAutocomplete))] int Encounter)
            {
                var tracking = CurrentRotations.DeepStoneCrypt.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Deep Stone Crypt challenges. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new DeepStoneCryptLink { DiscordID = Context.User.Id, Encounter = Encounter };
                CurrentRotations.DeepStoneCrypt.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.DeepStoneCrypt.DatePrediction(Encounter, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            [SlashCommand("empire-hunt", "Be notified when an Empire Hunt is active.")]
            public async Task EmpireHunt([Summary("empire-hunt", "Empire Hunt boss to be alerted for."), Autocomplete(typeof(EmpireHuntAutocomplete))] int Hunt)
            {
                var tracking = CurrentRotations.EmpireHunt.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Deep Stone Crypt challenges. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new EmpireHuntLink { DiscordID = Context.User.Id, EmpireHunt = Hunt };
                CurrentRotations.EmpireHunt.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.EmpireHunt.DatePrediction(Hunt, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            [SlashCommand("featured-raid", "Be notified when a raid is featured.")]
            public async Task FeaturedRaid([Summary("raid", "Legacy raid activity to be alerted for."), Autocomplete(typeof(FeaturedRaidAutocomplete))] int Raid)
            {
                var tracking = CurrentRotations.FeaturedRaid.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Featured Raids. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new FeaturedRaidLink { DiscordID = Context.User.Id, Raid = Raid };
                CurrentRotations.FeaturedRaid.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.FeaturedRaid.DatePrediction(Raid, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            [SlashCommand("featured-dungeon", "Be notified when a dungeon is featured.")]
            public async Task FeaturedDungeon([Summary("dungeon", "Legacy dungeon activity to be alerted for."), Autocomplete(typeof(FeaturedDungeonAutocomplete))] int Dungeon)
            {
                var tracking = CurrentRotations.FeaturedDungeon.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Featured Dungeons. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new FeaturedDungeonLink { DiscordID = Context.User.Id, Dungeon = Dungeon };
                CurrentRotations.FeaturedDungeon.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.FeaturedRaid.DatePrediction(Dungeon, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            [SlashCommand("garden-of-salvation", "Be notified when a Garden of Salvation challenge is active.")]
            public async Task GardenOfSalvation([Summary("challenge", "Garden of Salvation challenge to be alerted for."), Autocomplete(typeof(GardenOfSalvationAutocomplete))] int Encounter)
            {
                var tracking = CurrentRotations.GardenOfSalvation.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Featured Raids. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new GardenOfSalvationLink { DiscordID = Context.User.Id, Encounter = Encounter };
                CurrentRotations.GardenOfSalvation.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.GardenOfSalvation.DatePrediction(Encounter, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            [SlashCommand("kings-fall", "Be notified when a King's Fall challenge is active.")]
            public async Task KingsFall([Summary("challenge", "King's Fall challenge to be alerted for."), Autocomplete(typeof(KingsFallAutocomplete))] int Encounter)
            {
                var tracking = CurrentRotations.KingsFall.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Deep Stone Crypt challenges. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new KingsFallLink { DiscordID = Context.User.Id, Encounter = Encounter };
                CurrentRotations.KingsFall.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.KingsFall.DatePrediction(Encounter, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            [SlashCommand("last-wish", "Be notified when a Last Wish challenge is active.")]
            public async Task LastWish([Summary("challenge", "Last Wish challenge to be alerted for."), Autocomplete(typeof(LastWishAutocomplete))] int Encounter)
            {
                var tracking = CurrentRotations.LastWish.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Deep Stone Crypt challenges. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new LastWishLink { DiscordID = Context.User.Id, Encounter = Encounter };
                CurrentRotations.LastWish.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.LastWish.DatePrediction(Encounter, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            [SlashCommand("lost-sector", "Be notified when a Lost Sector and/or Armor Drop is active.")]
            public async Task LostSector([Summary("lost-sector", "Lost Sector to be alerted for."), Autocomplete(typeof(LostSectorAutocomplete))] int LS = -1,
                [Summary("armor-drop", "Lost Sector Exotic armor drop to be alerted for."), Autocomplete(typeof(ExoticArmorAutocomplete))] int EAT = -1)
            {
                var tracking = CurrentRotations.LostSector.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    if (LS == -1 && EAT == -1)
                        await RespondAsync($"An error has occurred.", ephemeral: true);
                    else 
                        await RespondAsync($"You already have tracking for Lost Sectors. I am watching for {tracking}.", ephemeral: true);

                    return;
                }

                if (LS == -1 && EAT == -1)
                {
                    await RespondAsync($"You left both arguments blank; I can't track nothing!", ephemeral: true);
                    return;
                }

                tracking = new LostSectorLink { DiscordID = Context.User.Id, LostSector = LS, ArmorDrop = EAT };
                CurrentRotations.LostSector.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.LostSector.DatePrediction(LS, EAT, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            //[SlashCommand("lightfall-mission", "Be notified when a featured Lightfall story mission is active.")]
            //public async Task LightfallMission([Summary("mission", "Lightfall story mission to be alerted for."), Autocomplete(typeof(LightfallMissionAutocomplete))] int Mission)
            //{
            //    var tracking = CurrentRotations.LightfallMission.GetUserTracking(Context.User.Id);
            //    if (tracking != null)
            //    {
            //        await RespondAsync($"You already have tracking for featured Lightfall story missions. I am watching for {tracking}.", ephemeral: true);
            //        return;
            //    }

            //    tracking = new LightfallMissionLink { DiscordID = Context.User.Id, Mission = Mission };
            //    CurrentRotations.LightfallMission.AddUserTracking(tracking);
            //    await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.LightfallMission.DatePrediction(Mission, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            //}

            [SlashCommand("nightfall", "Be notified when a Nightfall and/or Weapon is active.")]
            public async Task Nightfall([Summary("nightfall", "Nightfall Strike to be alerted for."), Autocomplete(typeof(NightfallAutocomplete))] int NF = -1,
                [Summary("weapon", "Nightfall Strike weapon drop to be alerted for."), Autocomplete(typeof(NightfallWeaponAutocomplete))] int Weapon = -1)
            {
                var tracking = CurrentRotations.Nightfall.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    if (NF == -1 && Weapon == -1)
                        await RespondAsync($"An error has occurred.", ephemeral: true);
                    else
                        await RespondAsync($"You already have tracking for Lost Sectors. I am watching for {tracking}.", ephemeral: true);

                    return;
                }

                if (NF == -1 && Weapon == -1)
                {
                    await RespondAsync($"You left both arguments blank; I can't track nothing!", ephemeral: true);
                    return;
                }

                tracking = new NightfallLink { DiscordID = Context.User.Id, Nightfall = NF, WeaponDrop = Weapon };
                CurrentRotations.Nightfall.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.Nightfall.DatePrediction(NF, Weapon, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            [SlashCommand("nightmare-hunt", "Be notified when an Nightmare Hunt is active.")]
            public async Task NightmareHunt([Summary("nightmare-hunt", "Nightmare Hunt to be alerted for."), Autocomplete(typeof(NightmareHuntAutocomplete))] int Hunt)
            {
                var tracking = CurrentRotations.NightmareHunt.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Nightmare Hunts. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new NightmareHuntLink { DiscordID = Context.User.Id, Hunt = Hunt };
                CurrentRotations.NightmareHunt.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.NightmareHunt.DatePrediction(Hunt, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            [SlashCommand("root-of-nightmares", "Be notified when a Root of Nightmares challenge is active.")]
            public async Task RootOfNightmares([Summary("challenge", "Root of Nightmares challenge to be alerted for."), Autocomplete(typeof(RootOfNightmaresAutocomplete))] int Encounter)
            {
                var tracking = CurrentRotations.RootOfNightmares.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Root of Nightmares challenges. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new RootOfNightmaresLink { DiscordID = Context.User.Id, Encounter = Encounter };
                CurrentRotations.RootOfNightmares.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.RootOfNightmares.DatePrediction(Encounter, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            //[SlashCommand("shadowkeep-mission", "Be notified when a featured Shadowkeep story mission is active.")]
            //public async Task ShadowkeepMission([Summary("mission", "Shadowkeep story mission to be alerted for."), Autocomplete(typeof(ShadowkeepMissionAutocomplete))] int Mission)
            //{
            //    var tracking = CurrentRotations.ShadowkeepMission.GetUserTracking(Context.User.Id);
            //    if (tracking != null)
            //    {
            //        await RespondAsync($"You already have tracking for featured Shadowkeep story missions. I am watching for {tracking}.", ephemeral: true);
            //        return;
            //    }

            //    tracking = new ShadowkeepMissionLink { DiscordID = Context.User.Id, Mission = Mission };
            //    CurrentRotations.ShadowkeepMission.AddUserTracking(tracking);
            //    await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.ShadowkeepMission.DatePrediction(Mission, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            //}

            [SlashCommand("terminal-overload", "Be notified when a Terminal Overload location is active.")]
            public async Task TerminalOverload([Summary("location", "Terminal Overload location to be alerted for."), Autocomplete(typeof(TerminalOverloadAutocomplete))] int Location)
            {
                var tracking = CurrentRotations.TerminalOverload.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Terminal Overloads. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new TerminalOverloadLink { DiscordID = Context.User.Id, Location = Location };
                CurrentRotations.TerminalOverload.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.TerminalOverload.DatePrediction(Location, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            [SlashCommand("vault-of-glass", "Be notified when a Vault of Glass challenge is active.")]
            public async Task VaultOfGlass([Summary("challenge", "Vault of Glass challenge to be alerted for."), Autocomplete(typeof(VaultOfGlassAutocomplete))] int Encounter)
            {
                var tracking = CurrentRotations.VaultOfGlass.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Vault of Glass challenges. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new VaultOfGlassLink { DiscordID = Context.User.Id, Encounter = Encounter };
                CurrentRotations.VaultOfGlass.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.VaultOfGlass.DatePrediction(Encounter, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            [SlashCommand("vow-of-the-disciple", "Be notified when a Vow of the Disciple challenge is active.")]
            public async Task VowOfTheDisciple([Summary("challenge", "Vow of the Disciple challenge to be alerted for."), Autocomplete(typeof(VowOfTheDiscipleAutocomplete))] int Encounter)
            {
                var tracking = CurrentRotations.VowOfTheDisciple.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Vow of the Disciple challenges. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new VowOfTheDiscipleLink { DiscordID = Context.User.Id, Encounter = Encounter };
                CurrentRotations.VowOfTheDisciple.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.VowOfTheDisciple.DatePrediction(Encounter, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            [SlashCommand("wellspring", "Be notified when a Wellspring boss/weapon is active.")]
            public async Task Wellspring([Summary("wellspring", "Wellspring weapon drop to be alerted for."), Autocomplete(typeof(WellspringAutocomplete))] int Wellspring)
            {
                var tracking = CurrentRotations.Wellspring.GetUserTracking(Context.User.Id);
                if (tracking != null)
                {
                    await RespondAsync($"You already have tracking for Wellsprings. I am watching for {tracking}.", ephemeral: true);
                    return;
                }

                tracking = new WellspringLink { DiscordID = Context.User.Id, Wellspring = Wellspring };
                CurrentRotations.Wellspring.AddUserTracking(tracking);
                await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.Wellspring.DatePrediction(Wellspring, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            }

            //[SlashCommand("witch-queen-mission", "Be notified when a featured Witch Queen story mission is active.")]
            //public async Task WitchQueenMission([Summary("mission", "Witch Queen story mission to be alerted for."), Autocomplete(typeof(WitchQueenMissionAutocomplete))] int Mission)
            //{
            //    var tracking = CurrentRotations.WitchQueenMission.GetUserTracking(Context.User.Id);
            //    if (tracking != null)
            //    {
            //        await RespondAsync($"You already have tracking for featured Witch Queen story missions. I am watching for {tracking}.", ephemeral: true);
            //        return;
            //    }

            //    tracking = new WitchQueenMissionLink { DiscordID = Context.User.Id, Mission = Mission };
            //    CurrentRotations.WitchQueenMission.AddUserTracking(tracking);
            //    await RespondAsync($"I will remind you when {tracking} is in rotation, which will be on {TimestampTag.FromDateTime(CurrentRotations.WitchQueenMission.DatePrediction(Mission, 0).Date, TimestampTagStyles.ShortDate)}.", ephemeral: true);
            //}

            [SlashCommand("remove", "Remove an active tracking notification.")]
            public async Task Remove([Summary("tracker", "Rotation tracker to remove. This list will be empty if you have no active trackers."), Autocomplete(typeof(TrackersAutocomplete))] string trackerType)
            {
                if (trackerType.Equals("ada-1"))
                {
                    var tracker = CurrentRotations.Ada1.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Ada-1 tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.Ada1.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Ada-1 tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("altars-of-sorrow"))
                {
                    var tracker = CurrentRotations.AltarsOfSorrow.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Altars of Sorrow tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.AltarsOfSorrow.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Altars of Sorrow tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("ascendant-challenge"))
                {
                    var tracker = CurrentRotations.AscendantChallenge.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Ascendant Challenge tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.AscendantChallenge.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Ascendant Challenge tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("curse-week"))
                {
                    var tracker = CurrentRotations.CurseWeek.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Curse Week tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.CurseWeek.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Curse Week tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("deep-stone-crypt"))
                {
                    var tracker = CurrentRotations.DeepStoneCrypt.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Deep Stone Crypt tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.DeepStoneCrypt.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Deep Stone Crypt tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("empire-hunt"))
                {
                    var tracker = CurrentRotations.EmpireHunt.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Empire Hunt tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.EmpireHunt.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Empire Hunt tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("featured-dungeon"))
                {
                    var tracker = CurrentRotations.FeaturedDungeon.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Featured Dungeon tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.FeaturedDungeon.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Featured Dungeon tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("featured-raid"))
                {
                    var tracker = CurrentRotations.FeaturedRaid.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Featured Raid tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.FeaturedRaid.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Featured Raid tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("garden-of-salvation"))
                {
                    var tracker = CurrentRotations.GardenOfSalvation.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Garden of Salvation tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.GardenOfSalvation.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Garden of Salvation tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("kings-fall"))
                {
                    var tracker = CurrentRotations.KingsFall.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No King's Fall tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.KingsFall.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your King's Fall tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("last-wish"))
                {
                    var tracker = CurrentRotations.LastWish.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Last Wish tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.LastWish.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Last Wish tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("lightfall-mission"))
                {
                    var tracker = CurrentRotations.LightfallMission.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Lightfall Weekly Mission tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.LightfallMission.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Lightfall Weekly Mission tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("lost-sector"))
                {
                    var tracker = CurrentRotations.LostSector.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Lost Sector tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.LostSector.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Lost Sector tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("nightfall"))
                {
                    var tracker = CurrentRotations.Nightfall.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Nightfall tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.Nightfall.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Nightfall tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("nightmare-hunt"))
                {
                    var tracker = CurrentRotations.NightmareHunt.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Nightmare Hunt tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.NightmareHunt.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Nightmare Hunt tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("root-of-nightmares"))
                {
                    var tracker = CurrentRotations.RootOfNightmares.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Root of Nightmares tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.RootOfNightmares.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Root of Nightmares tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("shadowkeep-mission"))
                {
                    var tracker = CurrentRotations.ShadowkeepMission.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Shadowkeep Weekly Mission tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.ShadowkeepMission.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Shadowkeep Weekly Mission tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("terminal-overload"))
                {
                    var tracker = CurrentRotations.TerminalOverload.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Terminal Overload tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.TerminalOverload.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Terminal Overload tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("vault-of-glass"))
                {
                    var tracker = CurrentRotations.VaultOfGlass.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Vault of Glass tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.VaultOfGlass.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Vault of Glass tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("vow-of-the-disciple"))
                {
                    var tracker = CurrentRotations.VowOfTheDisciple.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Vow of the Disciple tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.VowOfTheDisciple.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Vow of the Disciple tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("wellspring"))
                {
                    var tracker = CurrentRotations.Wellspring.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Wellspring tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.Wellspring.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Wellspring tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else if (trackerType.Equals("witch-queen-mission"))
                {
                    var tracker = CurrentRotations.WitchQueenMission.GetUserTracking(Context.User.Id);
                    if (tracker == null)
                    {
                        await RespondAsync("No Witch Queen Weekly Mission tracking enabled.", ephemeral: true);
                        return;
                    }
                    CurrentRotations.WitchQueenMission.RemoveUserTracking(Context.User.Id);
                    await RespondAsync($"Removed your Witch Queen Weekly Mission tracking, you will not be notified when {tracker} is available.", ephemeral: true);
                }
                else
                {
                    var errEmbed = Embeds.GetErrorEmbed();
                    errEmbed.Description = "Unable to parse parameters, make sure you're using one of the autocomplete options!";
                    await RespondAsync(embed: errEmbed.Build(), ephemeral: true);
                }
            }
        }

        [Group("next", "Predict when a specific rotation is active next.")]
        public class Next : InteractionModuleBase<ShardedInteractionContext>
        {
            [SlashCommand("altars-of-sorrow", "Find out when an Altars of Sorrow weapon is active next.")]
            public async Task AltarsOfSorrow([Summary("weapon", "Altars of Sorrow weapon to predict its next appearance."), Autocomplete(typeof(AltarsOfSorrowAutocomplete))] int Weapon,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<AltarsOfSorrowPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.AltarsOfSorrow.DatePrediction(Weapon, i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Altars of Sorrow",
                    Description = $"Requested: {CurrentRotations.AltarsOfSorrow.Rotations[Weapon].WeaponEmote}{CurrentRotations.AltarsOfSorrow.Rotations[Weapon]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    bool isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.AltarsOfSorrow.WeaponEmote}{prediction.AltarsOfSorrow} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Occurrence is beyond {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/58bf5b93ae8cfefc55852fe664179757.png";
                await RespondAsync(embed: embed.Build());
            }
            
            [SlashCommand("ascendant-challenge", "Find out when an Ascendant Challenge is active next.")]
            public async Task AscendantChallenge([Summary("ascendant-challenge", "Ascendant Challenge to predict its next appearance."), Autocomplete(typeof(AscendantChallengeAutocomplete))] int AscendantChallenge,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<AscendantChallengePrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.AscendantChallenge.DatePrediction(AscendantChallenge, i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Ascendant Challenge",
                    Description = $"Requested: {CurrentRotations.AscendantChallenge.Rotations[AscendantChallenge]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    bool isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.AscendantChallenge} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/2f9e7dd03c415eb158c16bb59cc24c84.jpg";
                await RespondAsync(embed: embed.Build());
            }

            [SlashCommand("curse-week", "Find out when a Curse Week strength is active.")]
            public async Task CurseWeek([Summary("strength", "Curse Week strength."), Autocomplete(typeof(CurseWeekAutocomplete))] int CurseWeek,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<CurseWeekPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.CurseWeek.DatePrediction(CurseWeek, i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Curse Week",
                    Description = $"Requested: {CurrentRotations.CurseWeek.Rotations[CurseWeek]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    bool isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.CurseWeek} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/e76dac4b765f694bd2e50eef00aa95d4.png";
                await RespondAsync(embed: embed.Build());
            }

            [SlashCommand("deep-stone-crypt", "Find out when a Deep Stone Crypt challenge is active next.")]
            public async Task DeepStoneCrypt([Summary("challenge", "Deep Stone Crypt challenge to predict its next appearance."), Autocomplete(typeof(DeepStoneCryptAutocomplete))] int Encounter,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<DeepStoneCryptPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.DeepStoneCrypt.DatePrediction(Encounter, i));

                var featuredRaidPredictions = new List<FeaturedRaidPrediction>();
                for (int i = 0; i < show; i++)
                    featuredRaidPredictions.Add(CurrentRotations.FeaturedRaid.DatePrediction(CurrentRotations.FeaturedRaid.Rotations.FindIndex(x => x.Raid.Equals("Deep Stone Crypt")), i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Deep Stone Crypt",
                    Description = $"Requested: {CurrentRotations.DeepStoneCrypt.Rotations[Encounter]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    var nextFeatured = featuredRaidPredictions.ElementAt(0);
                    bool isPastSeasonEnd = nextFeatured.Date.ToUniversalTime() >= seasonEndDate;
                    if (prediction.Date >= nextFeatured.Date)
                    {
                        featuredRaidPredictions.RemoveAt(0);
                        embed.AddField(x =>
                        {
                            x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                            x.Value = $"{prediction.DeepStoneCrypt} on {TimestampTag.FromDateTime(nextFeatured.Date, TimestampTagStyles.ShortDate)} (Featured Raid)";
                            x.IsInline = false;
                        });
                        occurrences++;

                        // Don't show duplicate weeks.
                        if (prediction.Date == nextFeatured.Date)
                            continue;
                    }

                    isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.DeepStoneCrypt} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                embed.Fields = embed.Fields.Take(4).ToList();

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/DestinyMilestoneDefinition_427561ad3d02d80a76a9cce7802c1323.png";
                await RespondAsync(embed: embed.Build());
            }
            
            [SlashCommand("empire-hunt", "Find out when an Empire Hunt is active next.")]
            public async Task EmpireHunt([Summary("empire-hunt", "Empire Hunt boss to predict its next appearance."), Autocomplete(typeof(EmpireHuntAutocomplete))] int Hunt,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<EmpireHuntPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.EmpireHunt.DatePrediction(Hunt, i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Empire Hunt",
                    Description = $"Requested: {CurrentRotations.EmpireHunt.Rotations[Hunt]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    bool isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.EmpireHunt} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/f3e9c2260e639dcea4a3135df40e6e5e.png";
                await RespondAsync(embed: embed.Build());
            }

            [SlashCommand("featured-raid", "Find out when a raid is being featured next.")]
            public async Task FeaturedRaid([Summary("raid", "Legacy raid activity to predict its next appearance."), Autocomplete(typeof(FeaturedRaidAutocomplete))] int Raid,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<FeaturedRaidPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.FeaturedRaid.DatePrediction(Raid, i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Featured Raid",
                    Description = $"Requested: {CurrentRotations.FeaturedRaid.Rotations[Raid]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    bool isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.FeaturedRaid} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/8b1bfd1c1ce1cab51d23c78235a6e067.png";
                await RespondAsync(embed: embed.Build());
            }

            [SlashCommand("featured-dungeon", "Find out when a dungeon is being featured next.")]
            public async Task FeaturedDungeon([Summary("raid", "Legacy dungeon activity to predict its next appearance."), Autocomplete(typeof(FeaturedDungeonAutocomplete))] int Dungeon,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<FeaturedDungeonPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.FeaturedDungeon.DatePrediction(Dungeon, i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Featured Dungeon",
                    Description = $"Requested: {CurrentRotations.FeaturedDungeon.Rotations[Dungeon]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    bool isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.FeaturedDungeon} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/DestinyActivityModeDefinition_f20ebb76bee675ca429e470cec58cc7b.png";
                await RespondAsync(embed: embed.Build());
            }

            [SlashCommand("garden-of-salvation", "Find out when a Garden of Salvation challenge is active next.")]
            public async Task GardenOfSalvation([Summary("challenge", "Garden of Salvation challenge to predict its next appearance."), Autocomplete(typeof(GardenOfSalvationAutocomplete))] int Encounter,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<GardenOfSalvationPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.GardenOfSalvation.DatePrediction(Encounter, i));

                var featuredRaidPredictions = new List<FeaturedRaidPrediction>();
                for (int i = 0; i < show; i++)
                    featuredRaidPredictions.Add(CurrentRotations.FeaturedRaid.DatePrediction(CurrentRotations.FeaturedRaid.Rotations.FindIndex(x => x.Raid.Equals("Garden of Salvation")), i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Garden of Salvation",
                    Description = $"Requested: {CurrentRotations.GardenOfSalvation.Rotations[Encounter]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    var nextFeatured = featuredRaidPredictions.ElementAt(0);
                    bool isPastSeasonEnd = nextFeatured.Date.ToUniversalTime() >= seasonEndDate;
                    if (prediction.Date >= nextFeatured.Date)
                    {
                        featuredRaidPredictions.RemoveAt(0);
                        embed.AddField(x =>
                        {
                            x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                            x.Value = $"{prediction.GardenOfSalvation} on {TimestampTag.FromDateTime(nextFeatured.Date, TimestampTagStyles.ShortDate)} (Featured Raid)";
                            x.IsInline = false;
                        });
                        occurrences++;

                        // Don't show duplicate weeks.
                        if (prediction.Date == nextFeatured.Date)
                            continue;
                    }

                    isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.GardenOfSalvation} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                embed.Fields = embed.Fields.Take(4).ToList();

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/DestinyMilestoneDefinition_eb9f4e265e29cb5e50559b6bf814a9c9.png";
                await RespondAsync(embed: embed.Build());
            }

            [SlashCommand("kings-fall", "Find out when a King's Fall challenge is active next.")]
            public async Task KingsFall([Summary("challenge", "King's Fall challenge to predict its next appearance."), Autocomplete(typeof(KingsFallAutocomplete))] int Encounter,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<KingsFallPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.KingsFall.DatePrediction(Encounter, i));

                var featuredRaidPredictions = new List<FeaturedRaidPrediction>();
                for (int i = 0; i < show; i++)
                    featuredRaidPredictions.Add(CurrentRotations.FeaturedRaid.DatePrediction(CurrentRotations.FeaturedRaid.Rotations.FindIndex(x => x.Raid.Equals("King's Fall")), i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "King's Fall",
                    Description = $"Requested: {CurrentRotations.KingsFall.Rotations[Encounter]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    var nextFeatured = featuredRaidPredictions.ElementAt(0);
                    bool isPastSeasonEnd = nextFeatured.Date.ToUniversalTime() >= seasonEndDate;
                    if (prediction.Date >= nextFeatured.Date)
                    {
                        featuredRaidPredictions.RemoveAt(0);
                        embed.AddField(x =>
                        {
                            x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                            x.Value = $"{prediction.KingsFall} on {TimestampTag.FromDateTime(nextFeatured.Date, TimestampTagStyles.ShortDate)} (Featured Raid)";
                            x.IsInline = false;
                        });
                        occurrences++;

                        // Don't show duplicate weeks.
                        if (prediction.Date == nextFeatured.Date)
                            continue;
                    }

                    isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.KingsFall} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                embed.Fields = embed.Fields.Take(4).ToList();

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/DestinyMilestoneDefinition_58109af839b023d7bf44c7b734818e47.png";
                await RespondAsync(embed: embed.Build());
            }

            [SlashCommand("last-wish", "Find out when a Last Wish challenge is active next.")]
            public async Task LastWish([Summary("challenge", "Last Wish challenge to predict its next appearance."), Autocomplete(typeof(LastWishAutocomplete))] int Encounter,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<LastWishPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.LastWish.DatePrediction(Encounter, i));

                var featuredRaidPredictions = new List<FeaturedRaidPrediction>();
                for (int i = 0; i < show; i++)
                    featuredRaidPredictions.Add(CurrentRotations.FeaturedRaid.DatePrediction(CurrentRotations.FeaturedRaid.Rotations.FindIndex(x => x.Raid.Equals("Last Wish")), i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Last Wish",
                    Description = $"Requested: {CurrentRotations.LastWish.Rotations[Encounter]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    var nextFeatured = featuredRaidPredictions.ElementAt(0);
                    bool isPastSeasonEnd = nextFeatured.Date.ToUniversalTime() >= seasonEndDate;
                    if (prediction.Date >= nextFeatured.Date)
                    {
                        featuredRaidPredictions.RemoveAt(0);
                        embed.AddField(x =>
                        {
                            x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                            x.Value = $"{prediction.LastWish} on {TimestampTag.FromDateTime(nextFeatured.Date, TimestampTagStyles.ShortDate)} (Featured Raid)";
                            x.IsInline = false;
                        });
                        occurrences++;

                        // Don't show duplicate weeks.
                        if (prediction.Date == nextFeatured.Date)
                            continue;
                    }

                    isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.LastWish} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                embed.Fields = embed.Fields.Take(4).ToList();

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/DestinyMilestoneDefinition_56d1f52f0cb40248a990408d7ac84bd3.png";
                await RespondAsync(embed: embed.Build());
            }

            //[SlashCommand("lightfall-mission", "Find out when a featured Lightfall mission is active next.")]
            //public async Task LightfallMission([Summary("mission", "Lightfall mission to predict its next appearance."), Autocomplete(typeof(LightfallMissionAutocomplete))] int Mission,
            //    [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            //{
            //    show = show switch
            //    {
            //        < 1 => 1,
            //        > 4 => 4,
            //        _ => show
            //    };

            //    var predictions = new List<LightfallMissionPrediction>();
            //    for (int i = 0; i < show; i++)
            //        predictions.Add(CurrentRotations.LightfallMission.DatePrediction(Mission, i));

            //    var embed = new EmbedBuilder
            //    {
            //        Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
            //        Title = "Lightfall Mission",
            //        Description = $"Requested: {CurrentRotations.LightfallMission.Rotations[Mission]}"
            //    }.WithCurrentTimestamp();

            //    var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
            //    int occurrences = 1;
            //    foreach (var prediction in predictions)
            //    {
            //        bool isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
            //        embed.AddField(x =>
            //        {
            //            x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
            //            x.Value = $"{prediction.LightfallMission} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
            //            x.IsInline = false;
            //        });
            //        occurrences++;
            //    }

            //    if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
            //        embed.Description +=
            //            $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

            //    embed.ThumbnailUrl =
            //        "https://www.bungie.net/common/destiny2_content/icons/DestinyMilestoneDefinition_67326996f903b5961421421e60ba128c.png";
            //    await RespondAsync(embed: embed.Build());
            //}

            [SlashCommand("lost-sector", "Find out when a Lost Sector and/or Armor Drop is active.")]
            public async Task LostSector([Summary("lost-sector", "Lost Sector to predict its next appearance."), Autocomplete(typeof(LostSectorAutocomplete))] int LS = -1,
                [Summary("armor-drop", "Lost Sector Exotic armor drop to predict its next appearance."), Autocomplete(typeof(ExoticArmorAutocomplete))] int EAT = -1,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<LostSectorPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.LostSector.DatePrediction(LS, EAT, i));

                string requested = "Lost Sector";
                if (LS >= 0)
                    requested = $"{CurrentRotations.LostSector.Rotations[LS]}";

                if (EAT >= 0)
                    requested += $" dropping {CurrentRotations.LostSector.ArmorRotations[EAT]}";

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Lost Sector",
                    Description = $"Requested: {requested}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    bool isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.LostSector} dropping {prediction.ExoticArmor} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/6a2761d2475623125d896d1a424a91f9.png";
                await RespondAsync(embed: embed.Build());
            }

            [SlashCommand("nightfall", "Find out when a Nightfall and/or Weapon is active next.")]
            public async Task Nightfall([Summary("nightfall", "Nightfall Strike to predict its next appearance."), Autocomplete(typeof(NightfallAutocomplete))] int NF = -1,
                [Summary("weapon", "Nightfall Strike Weapon drop."), Autocomplete(typeof(NightfallWeaponAutocomplete))] int Weapon = -1,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<NightfallPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.Nightfall.DatePrediction(NF, Weapon, i));

                string requested = "Nightfall";
                if (NF >= 0)
                    requested = $"{CurrentRotations.Nightfall.Rotations[NF]}";

                if (Weapon >= 0)
                    requested += $" dropping {CurrentRotations.Nightfall.WeaponRotations[Weapon]}";

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Nightfall",
                    Description = $"Requested: {requested}"
                }.WithCurrentTimestamp();

                if (predictions.Contains(null))
                {
                    embed.Description += "\nThis rotation is not possible.";
                    await RespondAsync(embed: embed.Build(), ephemeral: true);
                    return;
                }

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    bool isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.Nightfall} dropping {prediction.NightfallWeapon} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/f2154b781b36b19760efcb23695c66fe.png";
                await RespondAsync(embed: embed.Build());
            }

            [SlashCommand("nightmare-hunt", "Find out when an Nightmare Hunt is active next.")]
            public async Task NightmareHunt([Summary("nightmare-hunt", "Nightmare Hunt boss to predict its next appearance."), Autocomplete(typeof(NightmareHuntAutocomplete))] int Hunt,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<NightmareHuntPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.NightmareHunt.DatePrediction(Hunt, i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Nightmare Hunt",
                    Description = $"Requested: {CurrentRotations.NightmareHunt.Rotations[Hunt]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    bool isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.NightmareHunt} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/DestinyActivityModeDefinition_48ad57129cd0c46a355ef8bcaa1acd04.png";
                await RespondAsync(embed: embed.Build());
            }

            [SlashCommand("root-of-nightmares", "Find out when a Root of Nightmares challenge is active next.")]
            public async Task RootOfNightmares([Summary("challenge", "Root of Nightmares challenge to predict its next appearance."), Autocomplete(typeof(RootOfNightmaresAutocomplete))] int Encounter,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<RootOfNightmaresPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.RootOfNightmares.DatePrediction(Encounter, i));

                var featuredRaidPredictions = new List<FeaturedRaidPrediction>();
                for (int i = 0; i < show; i++)
                    featuredRaidPredictions.Add(CurrentRotations.FeaturedRaid.DatePrediction(CurrentRotations.FeaturedRaid.Rotations.FindIndex(x => x.Raid.Equals("Root of Nightmares")), i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Root of Nightmares",
                    Description = $"Requested: {CurrentRotations.RootOfNightmares.Rotations[Encounter]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    var nextFeatured = featuredRaidPredictions.ElementAt(0);
                    bool isPastSeasonEnd = nextFeatured.Date.ToUniversalTime() >= seasonEndDate;
                    if (prediction.Date >= nextFeatured.Date)
                    {
                        featuredRaidPredictions.RemoveAt(0);
                        embed.AddField(x =>
                        {
                            x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                            x.Value = $"{prediction.RootOfNightmares} on {TimestampTag.FromDateTime(nextFeatured.Date, TimestampTagStyles.ShortDate)} (Featured Raid)";
                            x.IsInline = false;
                        });
                        occurrences++;

                        // Don't show duplicate weeks.
                        if (prediction.Date == nextFeatured.Date)
                            continue;
                    }

                    isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.RootOfNightmares} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                embed.Fields = embed.Fields.Take(4).ToList();

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/DestinyMilestoneDefinition_d3dc8747ee63f991c6a56ac7908047ba.png";
                await RespondAsync(embed: embed.Build());
            }

            //[SlashCommand("shadowkeep-mission", "Find out when a featured Shadowkeep mission is active next.")]
            //public async Task ShadowkeepMission([Summary("mission", "Shadowkeep mission to predict its next appearance."), Autocomplete(typeof(ShadowkeepMissionAutocomplete))] int Mission,
            //    [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            //{
            //    show = show switch
            //    {
            //        < 1 => 1,
            //        > 4 => 4,
            //        _ => show
            //    };

            //    var predictions = new List<ShadowkeepMissionPrediction>();
            //    for (int i = 0; i < show; i++)
            //        predictions.Add(CurrentRotations.ShadowkeepMission.DatePrediction(Mission, i));

            //    var embed = new EmbedBuilder
            //    {
            //        Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
            //        Title = "Shadowkeep Mission",
            //        Description = $"Requested: {CurrentRotations.ShadowkeepMission.Rotations[Mission]}"
            //    }.WithCurrentTimestamp();

            //    var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
            //    int occurrences = 1;
            //    foreach (var prediction in predictions)
            //    {
            //        bool isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
            //        embed.AddField(x =>
            //        {
            //            x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
            //            x.Value = $"{prediction.ShadowkeepMission} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
            //            x.IsInline = false;
            //        });
            //        occurrences++;
            //    }

            //    if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
            //        embed.Description +=
            //            $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

            //    embed.ThumbnailUrl =
            //        "https://www.bungie.net/common/destiny2_content/icons/DestinyActivityModeDefinition_48ad57129cd0c46a355ef8bcaa1acd04.png";
            //    await RespondAsync(embed: embed.Build());
            //}

            [SlashCommand("terminal-overload", "Find out when a Terminal Overload location is active next.")]
            public async Task TerminalOverload([Summary("location", "Terminal Overload location to predict its next appearance."), Autocomplete(typeof(TerminalOverloadAutocomplete))] int Location,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<TerminalOverloadPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.TerminalOverload.DatePrediction(Location, i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Terminal Overload",
                    Description = $"Requested: {CurrentRotations.TerminalOverload.Rotations[Location]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    bool isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.TerminalOverload} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/70cbe108aa054523eac9defadfa27a57.png";
                await RespondAsync(embed: embed.Build());
            }

            [SlashCommand("vault-of-glass", "Find out when a Vault of Glass challenge is active next.")]
            public async Task VaultOfGlass([Summary("challenge", "Vault of Glass challenge to predict its next appearance."), Autocomplete(typeof(LastWishAutocomplete))] int Encounter,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<LastWishPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.LastWish.DatePrediction(Encounter, i));

                var featuredRaidPredictions = new List<FeaturedRaidPrediction>();
                for (int i = 0; i < show; i++)
                    featuredRaidPredictions.Add(CurrentRotations.FeaturedRaid.DatePrediction(CurrentRotations.FeaturedRaid.Rotations.FindIndex(x => x.Raid.Equals("Vault of Glass")), i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Vault of Glass",
                    Description = $"Requested: {CurrentRotations.LastWish.Rotations[Encounter]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    var nextFeatured = featuredRaidPredictions.ElementAt(0);
                    bool isPastSeasonEnd = nextFeatured.Date.ToUniversalTime() >= seasonEndDate;
                    if (prediction.Date >= nextFeatured.Date)
                    {
                        featuredRaidPredictions.RemoveAt(0);
                        embed.AddField(x =>
                        {
                            x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                            x.Value = $"{prediction.LastWish} on {TimestampTag.FromDateTime(nextFeatured.Date, TimestampTagStyles.ShortDate)} (Featured Raid)";
                            x.IsInline = false;
                        });
                        occurrences++;

                        // Don't show duplicate weeks.
                        if (prediction.Date == nextFeatured.Date)
                            continue;
                    }

                    isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.LastWish} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                embed.Fields = embed.Fields.Take(4).ToList();

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/DestinyMilestoneDefinition_44a010ae763cd975d56c632ff72c48a1.png";
                await RespondAsync(embed: embed.Build());
            }

            [SlashCommand("vow-of-the-disciple", "Be notified when a Vow of the Disciple challenge is active.")]
            public async Task VowOfTheDisciple([Summary("challenge", "Vow of the Disciple challenge to predict its next appearance."), Autocomplete(typeof(VowOfTheDiscipleAutocomplete))] int Encounter,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<LastWishPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.LastWish.DatePrediction(Encounter, i));

                var featuredRaidPredictions = new List<FeaturedRaidPrediction>();
                for (int i = 0; i < show; i++)
                    featuredRaidPredictions.Add(CurrentRotations.FeaturedRaid.DatePrediction(CurrentRotations.FeaturedRaid.Rotations.FindIndex(x => x.Raid.Equals("Vow of the Disciple")), i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Vow of the Disciple",
                    Description = $"Requested: {CurrentRotations.LastWish.Rotations[Encounter]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    var nextFeatured = featuredRaidPredictions.ElementAt(0);
                    bool isPastSeasonEnd = nextFeatured.Date.ToUniversalTime() >= seasonEndDate;
                    if (prediction.Date >= nextFeatured.Date)
                    {
                        featuredRaidPredictions.RemoveAt(0);
                        embed.AddField(x =>
                        {
                            x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                            x.Value = $"{prediction.LastWish} on {TimestampTag.FromDateTime(nextFeatured.Date, TimestampTagStyles.ShortDate)} (Featured Raid)";
                            x.IsInline = false;
                        });
                        occurrences++;

                        // Don't show duplicate weeks.
                        if (prediction.Date == nextFeatured.Date)
                            continue;
                    }

                    isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.LastWish} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                embed.Fields = embed.Fields.Take(4).ToList();

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/DestinyMilestoneDefinition_bc5d4a8377955b809dbbe0fb71645e6e.png";
                await RespondAsync(embed: embed.Build());
            }

            [SlashCommand("wellspring", "Find out when a Wellspring boss/weapon is active next.")]
            public async Task Wellspring([Summary("wellspring", "Wellspring weapon drop to predict its next appearance."), Autocomplete(typeof(WellspringAutocomplete))] int Wellspring,
                [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            {
                show = show switch
                {
                    < 1 => 1,
                    > 4 => 4,
                    _ => show
                };

                var predictions = new List<WellspringPrediction>();
                for (int i = 0; i < show; i++)
                    predictions.Add(CurrentRotations.Wellspring.DatePrediction(Wellspring, i));

                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = $"Wellspring: {CurrentRotations.Wellspring.Rotations[Wellspring].Type}",
                    Description = $"Requested: {CurrentRotations.Wellspring.Rotations[Wellspring].WeaponEmote}{CurrentRotations.Wellspring.Rotations[Wellspring]}"
                }.WithCurrentTimestamp();

                var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
                int occurrences = 1;
                foreach (var prediction in predictions)
                {
                    bool isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
                    embed.AddField(x =>
                    {
                        x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
                        x.Value = $"{prediction.Wellspring.WeaponEmote}{prediction.Wellspring} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
                        x.IsInline = false;
                    });
                    occurrences++;
                }

                if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
                    embed.Description +=
                        $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

                embed.ThumbnailUrl =
                    "https://www.bungie.net/common/destiny2_content/icons/e17d13013bad7d53c47b0231b9784e1e.png";
                await RespondAsync(embed: embed.Build());
            }

            //[SlashCommand("witch-queen-mission", "Find out when a featured Shadowkeep mission is active next.")]
            //public async Task WitchQueenMission([Summary("mission", "Witch Queen mission to predict its next appearance."), Autocomplete(typeof(WitchQueenMissionAutocomplete))] int Mission,
            //    [Summary("show-next", "Number of next occurrences to show. Default: 1. Max: 4.")] int show = 1)
            //{
            //    show = show switch
            //    {
            //        < 1 => 1,
            //        > 4 => 4,
            //        _ => show
            //    };

            //    var predictions = new List<WitchQueenMissionPrediction>();
            //    for (int i = 0; i < show; i++)
            //        predictions.Add(CurrentRotations.WitchQueenMission.DatePrediction(Mission, i));

            //    var embed = new EmbedBuilder
            //    {
            //        Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
            //        Title = "Witch Queen Mission",
            //        Description = $"Requested: {CurrentRotations.WitchQueenMission.Rotations[Mission]}"
            //    }.WithCurrentTimestamp();

            //    var seasonEndDate = (DateTime)ManifestHelper.CurrentSeason.EndDate;
            //    int occurrences = 1;
            //    foreach (var prediction in predictions)
            //    {
            //        bool isPastSeasonEnd = prediction.Date.ToUniversalTime() >= seasonEndDate;
            //        embed.AddField(x =>
            //        {
            //            x.Name = $"Occurrence {occurrences}{(isPastSeasonEnd ? $" {Emotes.Warning}" : "")}";
            //            x.Value = $"{prediction.WitchQueenMission} on {TimestampTag.FromDateTime(prediction.Date, TimestampTagStyles.ShortDate)}";
            //            x.IsInline = false;
            //        });
            //        occurrences++;
            //    }

            //    if (embed.Fields.Any(x => x.Name.Contains(Emotes.Warning)))
            //        embed.Description +=
            //            $"\n\n*{Emotes.Warning} - Prediction is beyond the {ManifestHelper.CurrentSeason.DisplayProperties.Name} end date ({TimestampTag.FromDateTime(seasonEndDate, TimestampTagStyles.ShortDate)}) and is subject to change.*";

            //    embed.ThumbnailUrl =
            //        "https://www.bungie.net/common/destiny2_content/icons/e17d13013bad7d53c47b0231b9784e1e.png";
            //    await RespondAsync(embed: embed.Build());
            //}
        }

        [SlashCommand("rank", "Display a Destiny 2 leaderboard of choice.")]
        public async Task Rank([Summary("leaderboard", "Specific leaderboard to display."),
            Choice("Season Pass Level", 0), Choice("Longest XP Logging Session", 1), Choice("Most Logged XP Per Hour", 2),
            Choice("Total XP Logging Time", 3), Choice("Equipped Power Level", 4)] int ArgLeaderboard,
            [Summary("season", "Season of the specific leaderboard. Defaults to the current season."),
            Choice("Season of the Lost", 15), Choice("Season of the Risen", 16), Choice("Season of the Haunted", 17),
            Choice("Season of Plunder", 18), Choice("Season of the Seraph", 19), Choice("Season of Defiance", 20),
            Choice("Season of the Deep", 21)] int Season = 21)
        {
            Leaderboard LeaderboardType = (Leaderboard)ArgLeaderboard;

            EmbedBuilder embed = new();
            switch (LeaderboardType)
            {
                case Leaderboard.Level:
                    {
                        string json = null;
                        switch (Season)
                        {
                            case 15: json = File.ReadAllText(LevelData.FilePathS15); break;
                            case 16: json = File.ReadAllText(LevelData.FilePathS16); break;
                            case 17: json = File.ReadAllText(LevelData.FilePathS17); break;
                            case 18: json = File.ReadAllText(LevelData.FilePathS18); break;
                            case 19: json = File.ReadAllText(LevelData.FilePathS19); break;
                            case 20: json = File.ReadAllText(LevelData.FilePath); break;
                            default: await RespondAsync($"Issue with Season number argument."); return;
                        }
                        LevelData ld = JsonConvert.DeserializeObject<LevelData>(json);

                        embed = LeaderboardHelper.GetLeaderboardEmbed(ld.GetSortedLevelData(), Context.User, Season);
                        break;
                    }
                case Leaderboard.LongestSession:
                    {
                        string json = null;
                        switch (Season)
                        {
                            case 15: json = File.ReadAllText(LongestSessionData.FilePathS15); break;
                            case 16: json = File.ReadAllText(LongestSessionData.FilePathS16); break;
                            case 17: json = File.ReadAllText(LongestSessionData.FilePathS17); break;
                            case 18: json = File.ReadAllText(LongestSessionData.FilePathS18); break;
                            case 19: json = File.ReadAllText(LongestSessionData.FilePathS19); break;
                            case 20: json = File.ReadAllText(LongestSessionData.FilePath); break;
                            default: await RespondAsync($"Issue with Season number argument."); return;
                        }
                        LongestSessionData lsd = JsonConvert.DeserializeObject<LongestSessionData>(json);

                        embed = LeaderboardHelper.GetLeaderboardEmbed(lsd.GetSortedLevelData(), Context.User, Season);
                        break;
                    }
                case Leaderboard.XPPerHour:
                    {
                        string json = null;
                        switch (Season)
                        {
                            case 15: json = File.ReadAllText(XPPerHourData.FilePathS15); break;
                            case 16: json = File.ReadAllText(XPPerHourData.FilePathS16); break;
                            case 17: json = File.ReadAllText(XPPerHourData.FilePathS17); break;
                            case 18: json = File.ReadAllText(XPPerHourData.FilePathS18); break;
                            case 19: json = File.ReadAllText(XPPerHourData.FilePathS19); break;
                            case 20: json = File.ReadAllText(XPPerHourData.FilePath); break;
                            default: await RespondAsync($"Issue with Season number argument."); return;
                        }
                        XPPerHourData xph = JsonConvert.DeserializeObject<XPPerHourData>(json);

                        embed = LeaderboardHelper.GetLeaderboardEmbed(xph.GetSortedLevelData(), Context.User, Season);
                        break;
                    }
                case Leaderboard.MostXPLoggingTime:
                    {
                        string json = null;
                        switch (Season)
                        {
                            case 15: json = File.ReadAllText(MostXPLoggingTimeData.FilePathS15); break;
                            case 16: json = File.ReadAllText(MostXPLoggingTimeData.FilePathS16); break;
                            case 17: json = File.ReadAllText(MostXPLoggingTimeData.FilePathS17); break;
                            case 18: json = File.ReadAllText(MostXPLoggingTimeData.FilePathS18); break;
                            case 19: json = File.ReadAllText(MostXPLoggingTimeData.FilePathS19); break;
                            case 20: json = File.ReadAllText(MostXPLoggingTimeData.FilePath); break;
                            default: await RespondAsync($"Issue with Season number argument."); return;
                        }
                        MostXPLoggingTimeData mttd = JsonConvert.DeserializeObject<MostXPLoggingTimeData>(json);

                        embed = LeaderboardHelper.GetLeaderboardEmbed(mttd.GetSortedLevelData(), Context.User, Season);
                        break;
                    }
                case Leaderboard.PowerLevel:
                    {
                        string json = null;
                        switch (Season)
                        {
                            case 15: json = File.ReadAllText(PowerLevelData.FilePathS15); break;
                            case 16: json = File.ReadAllText(PowerLevelData.FilePathS16); break;
                            case 17: json = File.ReadAllText(PowerLevelData.FilePathS17); break;
                            case 18: json = File.ReadAllText(PowerLevelData.FilePathS18); break;
                            case 19: json = File.ReadAllText(PowerLevelData.FilePathS19); break;
                            case 20: json = File.ReadAllText(PowerLevelData.FilePath); break;
                            default: await RespondAsync($"Issue with Season number argument."); return;
                        }
                        PowerLevelData pld = JsonConvert.DeserializeObject<PowerLevelData>(json);

                        embed = LeaderboardHelper.GetLeaderboardEmbed(pld.GetSortedLevelData(), Context.User, Season);
                        break;
                    }
            }

            await RespondAsync($"", embed: embed.Build());
        }

        [RequireBungieOauth]
        [SlashCommand("unlink", "Unlink your Bungie tag from your Discord account through Levante.")]
        public async Task Unlink([Summary("delete-leaderboards", "Delete your leaderboard stats when you unlink. This is true by default.")] bool RemoveLeaderboard = true)
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
    }
}
