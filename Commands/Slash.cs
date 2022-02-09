using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Levante.Configs;
using Levante.Helpers;
using Levante.Leaderboards;
using Levante.Rotations;
using Levante.Util;
using Newtonsoft.Json;

namespace Levante.Commands
{
    public class Slash : InteractionModuleBase<SocketInteractionContext>
    {
        [Group("alert", "Set up announcements for Daily/Weekly Reset and Emblem Offers.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public class Alert : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("emblem-offers", "Set up announcements for Emblem Offers.")]
            public async Task EmblemOffers([Summary("role", "Add a role to be pinged when a new Emblem Offer is posted.")]IRole RoleToPing = null)
            {
                if (DataConfig.IsExistingEmblemLinkedChannel(Context.Channel.Id))
                {
                    DataConfig.DeleteEmblemChannelFromRotationConfig(Context.Channel.Id);

                    await RespondAsync($"This channel will no longer receive Emblem Offer reset posts. Run this command to re-subscribe to them!", ephemeral: true);
                    return;
                }
                else
                {
                    DataConfig.AddEmblemChannel(Context.Channel.Id, RoleToPing);

                    await RespondAsync($"This channel is now successfully subscribed to Emblem Offer posts. Run this command again to remove this type of alert!", ephemeral: true);
                    return;
                }
            }

            [SlashCommand("resets", "Set up announcements for Daily/Weekly Reset.")]
            public async Task Resets([Summary("reset-type", "Choose between Daily or Weekly Reset."),
                Choice("Daily", 0), Choice("Weekly", 1)] int ResetType) 
            {
                bool IsDaily = ResetType == 0;

                if (DataConfig.IsExistingLinkedChannel(Context.Channel.Id, IsDaily))
                {
                    DataConfig.DeleteChannelFromRotationConfig(Context.Channel.Id, IsDaily);

                    await RespondAsync($"This channel will no longer receive {(IsDaily ? "Daily" : "Weekly")} reset posts. Run this command to re-subscribe to them!", ephemeral: true);
                    return;
                }
                else
                {
                    DataConfig.AddChannelToRotationConfig(Context.Channel.Id, IsDaily);

                    await RespondAsync($"This channel is now successfully subscribed to {(IsDaily ? "Daily" : "Weekly")} reset posts. Run this command again to remove this type of alert!", ephemeral: true);
                    return;
                }
            }
        }

        [SlashCommand("daily", "Display Daily reset information.")]
        public async Task Daily()
        {
            await RespondAsync($"", embed: CurrentRotations.DailyResetEmbed().Build());
            return;
        }

        [SlashCommand("free-emblems", "Display a list of universal emblem codes.")]
        public async Task FreeEmblems()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Universal Emblem Codes",
                IconUrl = Context.Client.GetApplicationInfoAsync().Result.IconUrl,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"These codes are not limited to one account and can be used by anyone."
            };
            var embed = new EmbedBuilder()
            {
                Color = new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };
            embed.Title = "";
            embed.Description =
                $"[The Visionary](https://www.bungie.net/common/destiny2_content/icons/65b4047b1b83aeeeb2e628305071fcea.jpg): **XFV-KHP-N97**\n" +
                $"[Cryonautics](https://www.bungie.net/common/destiny2_content/icons/6719dde48dca592addb4102cb747e097.jpg): **RA9-XPH-6KJ**\n" +
                $"[Galilean Excursion](https://bungie.net/common/destiny2_content/icons/3e99d575d00fb307c15fb5513dee13c6.jpg): **JYN-JAA-Y7D**\n" +
                $"[Future in Shadow](https://bungie.net/common/destiny2_content/icons/dd9af60ef15319ee986a1f6cc029fe71.jpg): **7LV-GTK-T7J**\n" +
                $"[Sequence Flourish](https://www.bungie.net/common/destiny2_content/icons/01e9b3863c14f9149ff4035b896ad5ed.jpg): **7D4-PKR-MD7**\n" +
                $"[A Classy Order](https://www.bungie.net/common/destiny2_content/icons/adaf0e2c15610cdfff750725701222ec.jpg): **YRC-C3D-YNC**\n" +
                $"[Be True](https://www.bungie.net/common/destiny2_content/icons/a6d9b66f124b25ac73969ebe4bc45b90.jpg): **ML3-FD4-ND9**\n" +
                $"[Heliotrope Warren](https://www.bungie.net/common/destiny2_content/icons/385c302dc22e6dafb8b50c253486d040.jpg): **L7T-CVV-3RD**\n" +
                $"[Shadow's Light](https://www.bungie.net//common/destiny2_content/icons/b296588f57aea1d15a04c3db6de98220.jpg): **F99-KPX-NCF**\n" +
                $"[Sneer of the Oni](https://www.bungie.net//common/destiny2_content/icons/bffe84c0efb9215dbdc8c4890c3e6234.jpg): **6LJ-GH7-TPA**\n" +
                $"[Countdown to Convergence](https://www.bungie.net//common/destiny2_content/icons/2560de3d4009044b291c6cfb69d11a7f.jpg): **PHV-6LF-9CP**\n" +
                $"[Liminal Nadir](https://www.bungie.net//common/destiny2_content/icons/4f9f612716a973ff03e5e17e9d7e7c91.jpg): **VA7-L7H-PNC**\n" +
                $"*Redeem those codes [here](https://www.bungie.net/7/en/Codes/Redeem).*";

            await RespondAsync(embed: embed.Build());
            return;
        }

        [Group("guardians", "Display Guardian information.")]
        public class Guardians : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("linked-user", "Get Guardian information of a Linked User.")]
            public async Task LinkedUser([Summary("user", "User to get Guardian information for.")] IUser User,
                [Summary("class", "Guardian Class to get information for.")] Guardian.Class ClassType,
                [Summary("platform", "Only needed if the user does not have Cross Save activated. This will be ignored otherwise."),
                Choice("Xbox", 1), Choice("PSN", 2), Choice("Steam", 3), Choice("Stadia", 5)]int ArgPlatform = 0)
            {
                DataConfig.DiscordIDLink LinkedUser = DataConfig.GetLinkedUser(User.Id);
                Guardian.Platform Platform = (Guardian.Platform)ArgPlatform;

                await DeferAsync();

                if (LinkedUser == null || !DataConfig.IsExistingLinkedUser(LinkedUser.DiscordID))
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"User is not linked; tell them to link using {BotConfig.DefaultCommandPrefix}link [THEIR BUNGIE TAG]."; });
                    return;
                }

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                    var response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/" + LinkedUser.BungieMembershipType + "/Profile/" + LinkedUser.BungieMembershipID + "?components=100,200").Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    dynamic item = JsonConvert.DeserializeObject(content);

                    if (DataConfig.IsBungieAPIDown(content))
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Bungie API is temporary down, try again later."; });
                        return;
                    }

                    if (item.ErrorCode != 1)
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"An error occured with that account. Is there a connected Destiny 2 account?"; });
                        return;
                    }

                    List<Guardian> userGuardians = new List<Guardian>();
                    for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                    {
                        try
                        {
                            string charId = $"{item.Response.profile.data.characterIds[i]}";
                            if ((Guardian.Class)item.Response.characters.data[$"{charId}"].classType == ClassType)
                                userGuardians.Add(new Guardian(LinkedUser.UniqueBungieName, LinkedUser.BungieMembershipID, LinkedUser.BungieMembershipType, charId));
                        }
                        catch (Exception x)
                        {
                            Console.WriteLine($"{x}");
                        }
                    }

                    if (userGuardians.Count == 0)
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"No guardian found."; });
                        return;
                    }

                    List<Embed> embeds = new List<Embed>();
                    foreach (var guardian in userGuardians)
                        embeds.Add(guardian.GetGuardianEmbed().Build());

                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embeds = embeds.ToArray(); });
                    return;
                }
            }

            [SlashCommand("bungie-tag", "Get Guardian information of any player.")]
            public async Task BungieTag([Summary("player", "Player's Bungie tag to get Guardian information for.")] string BungieTag,
                [Summary("class", "Guardian Class to get information for.")] Guardian.Class ClassType,
                [Summary("platform", "Only needed if the user does not have Cross Save activated. This will be ignored otherwise."),
                Choice("Xbox", 1), Choice("PSN", 2), Choice("Steam", 3), Choice("Stadia", 5)]int ArgPlatform = 0)
            {
                Guardian.Platform Platform = (Guardian.Platform)ArgPlatform;

                await DeferAsync();

                string MembershipType = null;
                string MembershipID = null;
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                    var response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/SearchDestinyPlayer/-1/" + Uri.EscapeDataString(BungieTag)).Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    dynamic item = JsonConvert.DeserializeObject(content);

                    string memId = "";
                    string memType = "";
                    for (int i = 0; i < item.Response.Count; i++)
                    {
                        memId = item.Response[i].membershipId;
                        memType = item.Response[i].membershipType;

                        var memResponse = client.GetAsync($"https://www.bungie.net/platform/Destiny2/" + memType + "/Profile/" + memId + "/?components=100").Result;
                        var memContent = memResponse.Content.ReadAsStringAsync().Result;
                        dynamic memItem = JsonConvert.DeserializeObject(memContent);

                        if (memItem.ErrorCode == 1)
                        {
                            if (((int)memItem.Response.profile.data.userInfo.crossSaveOverride == (int)memItem.Response.profile.data.userInfo.membershipType) ||
                                ((int)memItem.Response.profile.data.userInfo.crossSaveOverride == 0 && (int)memItem.Response.profile.data.userInfo.membershipType == ((int)Platform)))
                            {
                                MembershipType = memType;
                                MembershipID = memId;
                                break;
                            }
                        }
                    }
                }

                if (MembershipType == null || MembershipID == null)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"An error occurred when retrieving that player's Guardians. Run the command again and specify their platform."; });
                    return;
                }

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                    var response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/" + MembershipType + "/Profile/" + MembershipID + "?components=100,200").Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    dynamic item = JsonConvert.DeserializeObject(content);

                    if (DataConfig.IsBungieAPIDown(content))
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Bungie API is temporary down, try again later."; });
                        return;
                    }

                    if (item.ErrorCode != 1)
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"An error occured with that account. Is there a connected Destiny 2 account?"; });
                        return;
                    }

                    List<Guardian> userGuardians = new List<Guardian>();
                    for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                    {
                        try
                        {
                            string charId = $"{item.Response.profile.data.characterIds[i]}";
                            if ((Guardian.Class)item.Response.characters.data[$"{charId}"].classType == ClassType)
                                userGuardians.Add(new Guardian(BungieTag, MembershipID, MembershipType, charId));
                        }
                        catch (Exception x)
                        {
                            Console.WriteLine($"{x}");
                        }
                    }

                    if (userGuardians.Count == 0)
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "No guardian found."; });
                        return;
                    }

                    List<Embed> embeds = new List<Embed>();
                    foreach (var guardian in userGuardians)
                        embeds.Add(guardian.GetGuardianEmbed().Build());

                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embeds = embeds.ToArray(); });
                    return;
                }
            }
        }

        [SlashCommand("link", "Link your Bungie tag to your Discord account.")]
        public async Task Link([Summary("bungie-tag", "Your Bungie tag you wish to link with.")] string BungieTag,
            [Summary("platform", "Only needed if the user does not have Cross Save activated. This will be ignored otherwise."),
                Choice("Xbox", 1), Choice("PSN", 2), Choice("Steam", 3), Choice("Stadia", 5)]int ArgPlatform = 0)
        {
            if (DataConfig.IsExistingLinkedUser(Context.User.Id))
            {
                await RespondAsync($"You have an account linked already. Your linked account: {DataConfig.GetLinkedUser(Context.User.Id).UniqueBungieName}", ephemeral: true);
                return;
            }

            string memId = DataConfig.GetValidDestinyMembership(BungieTag, (Guardian.Platform)ArgPlatform, out string memType);

            if (memId == null && memType == null)
            {
                await RespondAsync($"Something went wrong. Is your Bungie Tag correct?", ephemeral: true);
                return;
            }

            if (!DataConfig.IsPublicAccount(BungieTag))
            {
                await RespondAsync($"Your account privacy is not set to public. I cannot access your information otherwise.", ephemeral: true);
                return;
            }

            DataConfig.AddUserToConfig(Context.User.Id, memId, memType, BungieTag);
            await RespondAsync($"Linked {Context.User.Mention} to {BungieTag}.", ephemeral: true);
        }

        [SlashCommand("lost-sector", "Get info on a Lost Sector based on Difficulty.")]
        public async Task LostSector([Summary("lost-sector", "Lost Sector name."),
                Choice("Bay of Drowned Wishes", 0), Choice("Chamber of Starlight", 1), Choice("Aphelion's Rest", 2),
                Choice("The Empty Tank", 3), Choice("K1 Logistics", 4), Choice("K1 Communion", 5),
                Choice("K1 Crew Quarters", 6), Choice("K1 Revelation", 7), Choice("Concealed Void", 8),
                Choice("Bunker E15", 9), Choice("Perdition", 10)] int ArgLS,
                [Summary("difficulty", "Lost Sector difficulty.")] LostSectorDifficulty ArgLSD)
        {
            LostSector LS = (LostSector)ArgLS;
            LostSectorDifficulty LSD = ArgLSD;

            await RespondAsync($"", embed: LostSectorRotation.GetLostSectorEmbed(LS, LSD).Build());
            return;
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
                Choice("Bay of Drowned Wishes", 0), Choice("Chamber of Starlight", 1), Choice("Aphelion's Rest", 2),
                Choice("The Empty Tank", 3), Choice("K1 Logistics", 4), Choice("K1 Communion", 5),
                Choice("K1 Crew Quarters", 6), Choice("K1 Revelation", 7), Choice("Concealed Void", 8),
                Choice("Bunker E15", 9), Choice("Perdition", 10)] int? ArgLS = null,
                [Summary("difficulty", "Lost Sector difficulty.")] LostSectorDifficulty? ArgLSD = null,
                [Summary("armor-drop", "Lost Sector Exotic armor drop.")] ExoticArmorType? ArgEAT = null)
            {
                if (LostSectorRotation.GetUserTracking(Context.User.Id, out var LS, out var LSD, out var EAT) != null)
                {
                    if (LS == null && LSD == null && EAT == null)
                        await RespondAsync($"An error has occurred.", ephemeral: true);
                    else if (LS != null && LSD == null && EAT == null)
                        await RespondAsync($"You already have tracking for Lost Sectors. I am watching for {LostSectorRotation.GetLostSectorString((LostSector)LS)}.", ephemeral: true);
                    else if (LS != null && LSD != null && EAT == null)
                        await RespondAsync($"You already have tracking for Lost Sectors. I am watching for {LostSectorRotation.GetLostSectorString((LostSector)LS)} ({LSD}).", ephemeral: true);
                    else if (LS == null && LSD == null && EAT != null)
                        await RespondAsync($"You already have tracking for Lost Sectors. I am watching for {EAT} armor drop.", ephemeral: true);
                    else if (LS == null && LSD != null && EAT != null)
                        await RespondAsync($"You already have tracking for Lost Sectors. I am watching for {LSD} {EAT} armor drop.", ephemeral: true);
                    else if (LS != null && LSD != null && EAT != null)
                        await RespondAsync($"You already have tracking for Lost Sectors. I am watching for {LostSectorRotation.GetLostSectorString((LostSector)LS)} ({LSD}) dropping {EAT}.", ephemeral: true);

                    return;
                }
                LS = (LostSector)ArgLS;
                LSD = ArgLSD;
                EAT = ArgEAT;

                if (LS == null && LSD != null && EAT == null)
                {
                    await RespondAsync($"I cannot track a difficulty, they are always active.", ephemeral: true);
                    return;
                }

                LostSectorRotation.AddUserTracking(Context.User.Id, LS, LSD, EAT);
                if (LS == null && LSD == null && EAT == null)
                    await RespondAsync($"An error has occurred.", ephemeral: true);
                else if (LS != null && LSD == null && EAT == null)
                    await RespondAsync($"I will remind you when {LostSectorRotation.GetLostSectorString((LostSector)LS)} is in rotation, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, LSD, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                else if (LS != null && LSD != null && EAT == null)
                    await RespondAsync($"I will remind you when {LostSectorRotation.GetLostSectorString((LostSector)LS)} ({LSD}) is in rotation, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, LSD, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                else if (LS == null && LSD == null && EAT != null)
                    await RespondAsync($"I will remind you when Lost Sectors are dropping {EAT}, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, LSD, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                else if (LS == null && LSD != null && EAT != null)
                    await RespondAsync($"I will remind you when {LSD} Lost Sectors are dropping {EAT}, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, LSD, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                else if (LS != null && LSD != null && EAT != null)
                    await RespondAsync($"I will remind you when {LostSectorRotation.GetLostSectorString((LostSector)LS)} ({LSD}) is dropping {EAT}, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, LSD, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);

                return;
            }

            [SlashCommand("nightfall", "Be notified when a Nightfall and/or Weapon is active.")]
            public async Task Nightfall([Summary("nightfall", "Nightfall Strike."),
                Choice("The Hollowed Lair", 0), Choice("Lake of Shadows", 1), Choice("Exodus Crash", 2),
                Choice("The Corrupted", 3), Choice("The Devils' Lair", 4), Choice("Proving Grounds", 5)] int? ArgNF = null,
                [Summary("weapon", "Nightfall Strike Weapon drop."),
                Choice("The Palindrome", 0), Choice("THE S.W.A.R.M.", 1), Choice("The Comedian", 2),
                Choice("Shadow Price", 3), Choice("Hung Jury SR4", 4), Choice("The Hothead", 5),
                Choice("PLUG ONE.1", 6), Choice("Uzume RR4", 7)] int? ArgWeapon = null)
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
                NF = (Nightfall)ArgNF;
                Weapon = (NightfallWeapon)ArgWeapon;

                NightfallRotation.AddUserTracking(Context.User.Id, NF, Weapon);
                if (NF == null && Weapon == null)
                    await RespondAsync($"An error has occurred.", ephemeral: true);
                else if (NF != null && Weapon == null)
                    await RespondAsync($"[{NightfallRotation.ActivityPrediction(NightfallRotation.DatePrediction(NF, Weapon), out NightfallWeapon[] WeaponDrops)} | Drops: {WeaponDrops[0]} {WeaponDrops[1]}]:I will remind you when {NightfallRotation.GetStrikeNameString((Nightfall)NF)} is in rotation, which will be on {TimestampTag.FromDateTime(NightfallRotation.DatePrediction(NF, Weapon), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                else if (NF == null && Weapon != null)
                    await RespondAsync($"I will remind you when {NightfallRotation.GetWeaponString((NightfallWeapon)Weapon)} is in rotation, which will be on {TimestampTag.FromDateTime(NightfallRotation.DatePrediction(NF, Weapon), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                else if (NF != null && Weapon != null)
                    await RespondAsync($"I will remind you when {NightfallRotation.GetStrikeNameString((Nightfall)NF)} is dropping {NightfallRotation.GetWeaponString((NightfallWeapon)Weapon)}, which will be on {TimestampTag.FromDateTime(NightfallRotation.DatePrediction(NF, Weapon), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                return;
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

                if (LostSectorRotation.GetUserTracking(Context.User.Id, out var LS, out var LSD, out var EAT) != null)
                {
                    if (LS == null && LSD == null && EAT == null)
                        menuBuilder.AddOption("Lost Sector", "remove-error", $"Nothing found");
                    else if (LS != null && LSD == null && EAT == null)
                        menuBuilder.AddOption("Lost Sector", "lost-sector", $"{LostSectorRotation.GetLostSectorString((LostSector)LS)}");
                    else if (LS != null && LSD != null && EAT == null)
                        menuBuilder.AddOption("Lost Sector", "lost-sector", $"{LostSectorRotation.GetLostSectorString((LostSector)LS)} ({LSD})");
                    else if (LS == null && LSD == null && EAT != null)
                        menuBuilder.AddOption("Lost Sector", "lost-sector", $"{EAT} Drop");
                    else if (LS == null && LSD != null && EAT != null)
                        menuBuilder.AddOption("Lost Sector", "lost-sector", $"{LSD} {EAT} Drop");
                    else if (LS != null && LSD != null && EAT != null)
                        menuBuilder.AddOption("Lost Sector", "lost-sector", $"{LostSectorRotation.GetLostSectorString((LostSector)LS)} ({LSD}) dropping {EAT}");
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

                var builder = new ComponentBuilder()
                    .WithSelectMenu(menuBuilder);

                try
                {
                    await RespondAsync($"Which rotation tracker did you want me to remove? Please dismiss this message after you are done.", ephemeral: true, components: builder.Build());
                }
                catch
                {
                    await RespondAsync($"You do not have any active trackers. Use '/notify' to activate your first one!", ephemeral: true);
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

            [SlashCommand("lost-sector", "Find out when a Lost Sector and/or Armor Drop is active next.")]
            public async Task LostSector([Summary("lost-sector", "Lost Sector name."),
                Choice("Bay of Drowned Wishes", 0), Choice("Chamber of Starlight", 1), Choice("Aphelion's Rest", 2),
                Choice("The Empty Tank", 3), Choice("K1 Logistics", 4), Choice("K1 Communion", 5),
                Choice("K1 Crew Quarters", 6), Choice("K1 Revelation", 7), Choice("Concealed Void", 8),
                Choice("Bunker E15", 9), Choice("Perdition", 10)] int? ArgLS = null,
                [Summary("difficulty", "Lost Sector difficulty.")] LostSectorDifficulty? ArgLSD = null,
                [Summary("armor-drop", "Lost Sector Exotic armor drop.")] ExoticArmorType? ArgEAT = null)
            {
                LostSector? LS = (LostSector)ArgLS;
                LostSectorDifficulty? LSD = ArgLSD;
                ExoticArmorType? EAT = ArgEAT;

                var predictedDate = LostSectorRotation.DatePrediction(LS, LSD, EAT);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Lost Sectors";
                embed.Description =
                    $"Next occurrance of {LostSectorRotation.GetLostSectorString((LostSector)LS)} {(LSD != LostSectorDifficulty.Legend ? $" (Master)" : " (Legend)")}" +
                        $"{(EAT != null ? $" dropping {EAT}" : "")} is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

                await RespondAsync($"", embed: embed.Build());
                return;
            }

            [SlashCommand("nightfall", "Find out when a Nightfall and/or Weapon is active next.")]
            public async Task Nightfall([Summary("nightfall", "Nightfall Strike."),
                Choice("The Hollowed Lair", 0), Choice("Lake of Shadows", 1), Choice("Exodus Crash", 2),
                Choice("The Corrupted", 3), Choice("The Devils' Lair", 4), Choice("Proving Grounds", 5)] int ArgNF,
                [Summary("weapon", "Nightfall Strike Weapon drop."),
                Choice("The Palindrome", 0), Choice("THE S.W.A.R.M.", 1), Choice("The Comedian", 2),
                Choice("Shadow Price", 3), Choice("Hung Jury SR4", 4), Choice("The Hothead", 5),
                Choice("PLUG ONE.1", 6), Choice("Uzume RR4", 7)] int? ArgWeapon = null)
            {
                Nightfall NF = (Nightfall)ArgNF;
                NightfallWeapon? Weapon = (NightfallWeapon)ArgWeapon;

                var predictedDate = NightfallRotation.DatePrediction(NF, Weapon);
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                };
                embed.Title = "Nightfall";
                embed.Description =
                    $"Next occurrance of {NightfallRotation.GetStrikeNameString(NF)}" +
                        $"{(Weapon != null ? $" dropping {NightfallRotation.GetWeaponString((NightfallWeapon)Weapon)}" : "")} is: {TimestampTag.FromDateTime(predictedDate, TimestampTagStyles.ShortDate)}.";

                await RespondAsync($"", embed: embed.Build());
                return;
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
        }

        [SlashCommand("nightfall", "Display Nightfall information.")]
        public async Task Nightfall([Summary("nightfall", "Nightfall Strike."),
                Choice("The Hollowed Lair", 0), Choice("Lake of Shadows", 1), Choice("Exodus Crash", 2),
                Choice("The Corrupted", 3), Choice("The Devils' Lair", 4), Choice("Proving Grounds", 5)] int ArgNF)
        {
            await RespondAsync($"Command is under construction! Wait for a future update.", ephemeral: true);
            return;
        }

        [SlashCommand("patrol", "Display Patrol information.")]
        public async Task Patrol([Summary("location", "Patrol location."),
                Choice("The Dreaming City", 0), Choice("The Moon", 1), Choice("Europa", 2)] int ArgLocation)
        {
            await RespondAsync($"Command is under construction! Wait for a future update.", ephemeral: true);
            return;
        }

        [SlashCommand("raid", "Display Raid information.")]
        public async Task Raid([Summary("raid", "Raid name."),
                Choice("Last Wish", 0), Choice("Garden of Salvation", 1), Choice("Deep Stone Crypt", 2), Choice("Vault of Glass", 3)] int ArgRaid)
        {
            await RespondAsync($"Command is under construction! Wait for a future update.", ephemeral: true);
            return;
        }

        [SlashCommand("rank", "Display a Destiny 2 leaderboard of choice.")]
        public async Task Rank([Summary("leaderboard", "Specific leaderboard to display."),
            Choice("Season Pass Level", 0), Choice("Longest Thrallway Session", 1), Choice("Most Thrallway XP Per Hour", 2),
            Choice("Total Thrallway Time", 3), Choice("Equipped Power Level", 3)] int ArgLeaderboard)
        {
            Leaderboard LeaderboardType = (Leaderboard)ArgLeaderboard;

            EmbedBuilder embed = new EmbedBuilder();
            switch (LeaderboardType)
            {
                case Leaderboard.Level: embed = LeaderboardHelper.GetLeaderboardEmbed(LevelData.GetSortedLevelData(), Context.User); break;
                case Leaderboard.LongestSession: embed = LeaderboardHelper.GetLeaderboardEmbed(LongestSessionData.GetSortedLevelData(), Context.User); break;
                case Leaderboard.XPPerHour: embed = LeaderboardHelper.GetLeaderboardEmbed(XPPerHourData.GetSortedLevelData(), Context.User); break;
                case Leaderboard.MostThrallwayTime: embed = LeaderboardHelper.GetLeaderboardEmbed(MostThrallwayTimeData.GetSortedLevelData(), Context.User); break;
                case Leaderboard.PowerLevel: embed = LeaderboardHelper.GetLeaderboardEmbed(PowerLevelData.GetSortedLevelData(), Context.User); break;
            }

            await RespondAsync($"", embed: embed.Build());
        }

        [SlashCommand("unlink", "Unlink your Bungie tag from your Discord account.")]
        public async Task Unlink()
        {
            if (!DataConfig.IsExistingLinkedUser(Context.User.Id))
            {
                await RespondAsync("You do not have a Bungie account linked. Use the command \"/link\" to link!", ephemeral: true);
                return;
            }

            var linkedUser = DataConfig.GetLinkedUser(Context.User.Id);
            DataConfig.DeleteUserFromConfig(Context.User.Id);
            await RespondAsync($"Your Bungie account: {linkedUser.UniqueBungieName} has been unlinked. Use the command \"/link\" if you want to re-link!", ephemeral: true);
        }

        [SlashCommand("weekly", "Display Weekly reset information.")]
        public async Task Weekly()
        {
            await RespondAsync($"", embed: CurrentRotations.WeeklyResetEmbed().Build());
            return;
        }
    }
}
