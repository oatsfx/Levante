using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Levante.Configs;
using System.Drawing;
using System.IO;
using Levante.Util;
using System.Net;
using System.Collections.Generic;
using Fergun.Interactive;
using Discord.Interactions;
using Levante.Rotations;
using APIHelper;
using System.Linq;
using Levante.Helpers;
using BungieSharper;

namespace Levante.Commands
{
    public class Destiny : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractiveService Interactive { get; set; }

        [SlashCommand("artifact", "Project what level you need to hit for a specific Power bonus.")]
        public async Task ArtifactBonusPrediction([Summary("power-bonus", "Projected Power bonus you want to see what level it'll be hit at.")] int PowerBonus)
        {
            await DeferAsync();

            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Power Bonus Projection",
                IconUrl = app.IconUrl
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {BotConfig.AppName} v{String.Format("{0:0.00#}", BotConfig.Version)}"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };
            embed.Title = $"Power Bonus +{PowerBonus} at Level ";
            if (DataConfig.IsExistingLinkedUser(Context.User.Id))
            {
                var dil = DataConfig.GetLinkedUser(Context.User.Id);
                int Level;
                int XPProgress;
                dynamic item;
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {dil.AccessToken}");

                    var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + dil.BungieMembershipType + "/Profile/" + dil.BungieMembershipID + "/?components=100,104,202").Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    item = JsonConvert.DeserializeObject(content);

                    //first 100 levels: 4095505052 (S15); 2069932355 (S16); 26079066 (S17)
                    //anything after: 1531004716 (S15); 1787069365 (S16); 482365574 (S17)

                    if (item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"26079066"].level == 100)
                    {
                        int extraLevel = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"482365574"].level;
                        Level = 100 + extraLevel;
                        XPProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"482365574"].progressToNextLevel;
                    }
                    else
                    {
                        Level = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"26079066"].level;
                        XPProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"26079066"].progressToNextLevel;
                    }
                }
                int currentPowerBonus = item.Response.profileProgression.data.seasonalArtifact.powerBonus;

                if (currentPowerBonus >= PowerBonus)
                {
                    int xpNeeded = GetXPForBoost(PowerBonus);
                    var seasonRanksNeeded = (double)xpNeeded / 100000;
                    var remainder = xpNeeded % 100000;

                    embed.Title += $"{seasonRanksNeeded:.00}";
                    embed.Description =
                        $"You already hit this Power bonus; I cannot provide a personalized prediction.\n" +
                        $"> You will hit Power Bonus +{PowerBonus} at roughly Level **{seasonRanksNeeded:.00}**.";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); });
                }
                else
                {
                    int progressToNextLevel = item.Response.profileProgression.data.seasonalArtifact.powerBonusProgression.progressToNextLevel;
                    int xpForNextBoost = GetXPForBoost(PowerBonus);
                    int xpNeeded = xpForNextBoost - GetXPForBoost(currentPowerBonus) - progressToNextLevel;
                    var seasonRanksNeeded = (double)xpNeeded / 100000;
                    var remainder = xpNeeded % 100000;
                    if (remainder + XPProgress > 100000)
                        seasonRanksNeeded += 1;

                    embed.Title += $"{seasonRanksNeeded:.00}";
                    embed.Description =
                        $"> You will hit Power Bonus +{PowerBonus} at roughly Level **{seasonRanksNeeded:.00}** (Need {seasonRanksNeeded - (Level + ((double)XPProgress / 100000)):.00}).";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); });
                }
            }
            else
            {
                int xpNeeded = GetXPForBoost(PowerBonus);
                var seasonRanksNeeded = (double)xpNeeded / 100000;
                var remainder = xpNeeded % 100000;

                embed.Title += $"{seasonRanksNeeded:.00}";
                embed.Description =
                        $"You are not linked; I cannot provide a personalized prediction.\n" +
                        $"> You will hit Power Bonus +{PowerBonus} at roughly Level **{seasonRanksNeeded:.00}**.";
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); });
            }
        }

        [SlashCommand("current-offers", "Gives a list of emblem offers. If hash code provided, command will return with the specific offer.")]
        public async Task CurrentOffers([Summary("emblem-name", "Emblem name of an emblem that is a current offer."), Autocomplete(typeof(CurrentOfferAutocomplete))] string EmblemHash = null)
        {
            if (EmblemHash == null)
            {
                await RespondAsync(embed: EmblemOffer.GetOfferListEmbed().Build());
                return;
            }
            long EmblemHashCode = long.Parse(EmblemHash);
            if (!EmblemOffer.HasExistingOffer(EmblemHashCode))
                await RespondAsync("Are you sure you entered the correct hash code? Run this command without any arguments and try again.");
            else
                await RespondAsync(embed: EmblemOffer.GetSpecificOffer(EmblemHashCode).BuildEmbed().Build());
        }

        [SlashCommand("daily", "Display Daily reset information.")]
        public async Task Daily()
        {
            await RespondAsync(embed: CurrentRotations.DailyResetEmbed().Build());
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
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };
            embed.Title = "";
            foreach (var emblem in BotConfig.UniversalCodes)
            {
                embed.Description +=
                    $"[{emblem.Name}]({emblem.ImageUrl}): **{emblem.Code}**\n";
            }
            embed.Description +=
                $"*Redeem those codes [here](https://www.bungie.net/7/en/Codes/Redeem).*";

            await RespondAsync(embed: embed.Build());
            return;
        }

        [Group("guardian", "Display Guardian information.")]
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

                if (LinkedUser == null)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Unable to pull user data. I may have lost access to their information, likely, they'll have to link again."; });
                    return;
                }

                if (!DataConfig.IsExistingLinkedUser(LinkedUser.DiscordID))
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"User is not linked; tell them to link using '/link'."; });
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
            public async Task BungieTag([Summary("player", "Player's Bungie tag to get Guardian information for."), Autocomplete(typeof(BungieTagAutocomplete))] string BungieTag,
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

        [SlashCommand("level", "Gets your Destiny 2 Season Pass Rank.")]
        public async Task GetLevel([Summary("user", "User you want the Season Pass rank of. Leave empty for your own.")] IUser User = null)
        {
            if (User == null)
                User = Context.User;

            if (!DataConfig.IsExistingLinkedUser(User.Id))
            {
                await RespondAsync($"No account linked for {User.Mention}.", ephemeral: true);
                return;
            }
            await DeferAsync();
            try
            {
                string season = GetCurrentDestiny2Season(out int seasonNum);

                var dil = DataConfig.GetLinkedUser(Context.User.Id);
                int Level;
                int XPProgress;
                dynamic item;
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {dil.AccessToken}");

                    var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + dil.BungieMembershipType + "/Profile/" + dil.BungieMembershipID + "/?components=100,104,202").Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    item = JsonConvert.DeserializeObject(content);

                    if (item.Response.profile.privacy == 2)
                    {
                        await RespondAsync($"{User.Mention} has their Destiny 2 stats on Private. Let's respect that.", ephemeral: true);
                        return;
                    }

                    //first 100 levels: 4095505052 (S15); 2069932355 (S16); 26079066 (S17)
                    //anything after: 1531004716 (S15); 1787069365 (S16); 482365574 (S17)

                    if (item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"26079066"].level == 100)
                    {
                        int extraLevel = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"482365574"].level;
                        Level = 100 + extraLevel;
                        XPProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"482365574"].progressToNextLevel;
                    }
                    else
                    {
                        Level = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"26079066"].level;
                        XPProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"26079066"].progressToNextLevel;
                    }
                }

                var app = await Context.Client.GetApplicationInfoAsync();
                var auth = new EmbedAuthorBuilder()
                {
                    Name = $"Season {seasonNum}: {season} Level and XP Info",
                    IconUrl = User.GetAvatarUrl(),
                };
                var foot = new EmbedFooterBuilder()
                {
                    Text = $"Powered by the Bungie API"
                };
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                    Author = auth,
                    Footer = foot,
                };

                embed.Description =
                    $"Player: **{dil.UniqueBungieName}**\n" +
                    $"Level: **{Level}**\n" +
                    $"Progress to Next Level: **{XPProgress:n0}/100,000**";

                int powerBonus = item.Response.profileProgression.data.seasonalArtifact.powerBonus;
                int totalXP = item.Response.profileProgression.data.seasonalArtifact.powerBonusProgression.currentProgress;
                int dailyProgress = item.Response.profileProgression.data.seasonalArtifact.powerBonusProgression.dailyProgress;
                int weeklyProgress = item.Response.profileProgression.data.seasonalArtifact.powerBonusProgression.weeklyProgress;
                int progressToNextLevel = item.Response.profileProgression.data.seasonalArtifact.powerBonusProgression.progressToNextLevel;
                int nextLevelAt = item.Response.profileProgression.data.seasonalArtifact.powerBonusProgression.nextLevelAt;

                int xpForNextBoost = GetXPForBoost(powerBonus + 1);
                int xpNeeded = xpForNextBoost - GetXPForBoost(powerBonus) - progressToNextLevel;
                var seasonRanksNeeded = xpNeeded / 100000;
                var remainder = xpNeeded % 100000;
                if (remainder + XPProgress > 100000)
                    seasonRanksNeeded += 1;
                int projectedSeasonRank = Level + seasonRanksNeeded;

                embed.AddField(x =>
                {
                    x.Name = "Earned XP";
                    x.Value = $"Daily: {dailyProgress:n0}\n" +
                        $"Weekly: {weeklyProgress:n0}\n" +
                        $"Seasonal: {totalXP:n0}";
                    x.IsInline = true;
                }).AddField(x =>
                {
                    x.Name = $"Artifact Bonus (+{powerBonus})";
                    x.Value = $"Progress: {progressToNextLevel:n0}/{nextLevelAt:n0} XP\n" +
                        $"Next Level (+{powerBonus + 1}): {nextLevelAt - progressToNextLevel:n0} XP (At Rank: {projectedSeasonRank})";
                    x.IsInline = true;
                });

                await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); });
            }
            catch
            {
                await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = "An error has occurred, please try again later."; });
            }
        }

        private static int GetXPForBoost(int boostLevel) => 55000 * (boostLevel - 1) * (boostLevel - 1);

        [SlashCommand("lost-sector", "Get info on a Lost Sector based on Difficulty.")]
        public async Task LostSector([Summary("lost-sector", "Lost Sector name."),
                Choice("K1 Crew Quarters", 0), Choice("K1 Logistics", 1), Choice("K1 Revelation", 2),
                Choice("K1 Communion", 3), Choice("The Conflux", 4), Choice("Metamorphosis", 5),
                Choice("Sepulcher", 6), Choice("Extraction", 7), Choice("Excavation Site XII", 8),
                Choice("Skydock IV", 9), Choice("The Quarry", 10)] int ArgLS,
                [Summary("difficulty", "Lost Sector difficulty.")] LostSectorDifficulty ArgLSD)
        {
            //await RespondAsync($"Gathering data on new Lost Sectors. Check back later!", ephemeral: true);
            //return;
            LostSector LS = (LostSector)ArgLS;
            LostSectorDifficulty LSD = ArgLSD;

            await RespondAsync(embed: LostSectorRotation.GetLostSectorEmbed(LS, LSD).Build());
            return;
        }

        [SlashCommand("materials", "Gets your Destiny 2 material/currency counts.")]
        public async Task Materials()
        {
            var User = Context.User;

            if (!DataConfig.IsExistingLinkedUser(User.Id))
            {
                await RespondAsync($"No account linked for {User.Mention}.", ephemeral: true);
                return;
            }

            await DeferAsync();
            var dil = DataConfig.GetLinkedUser(User.Id);
            if (dil == null)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = $"Unable to pull user data. I may have lost access to their information, likely, they'll have to link again."; });
                return;
            }

            int Glimmer = 0, LegendaryShards = 0, UpgradeModules = 0, EnhancementCores = 0, EnhancementPrisms = 0, AscendantShards = 0,
                SpoilsOfConquest = 0, BrightDust = 0, ResonantElement = 0, ResonantAlloy = 0, DrownedAlloy = 0, AscendantAlloy = 0;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {dil.AccessToken}");

                var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + dil.BungieMembershipType + "/Profile/" + dil.BungieMembershipID + "/?components=100,102,103,201,1200").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                for (int i = 0; i < item.Response.profileInventory.data.items.Count; i++)
                {
                    long hash = item.Response.profileInventory.data.items[i].itemHash;
                    switch (hash)
                    {
                        // Upgrade Modules
                        case 2979281381: UpgradeModules += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                        // Enhancement Cores
                        case 3853748946: EnhancementCores += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                        // Enhancement Prisms
                        case 4257549984: EnhancementPrisms += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                        // Ascendant Shards
                        case 4257549985: AscendantShards += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                        // Spoils of Conquest
                        case 3702027555: SpoilsOfConquest += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                        // Resonant Alloy
                        case 2497395625: ResonantAlloy += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                        // Drowned Alloy
                        case 2708128607: DrownedAlloy += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                        // Ascendant Alloy
                        case 353704689: AscendantAlloy += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                        default: break;
                    }
                }

                for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                {
                    string charId = $"{item.Response.profile.data.characterIds[i]}";
                    for (int j = 0; j < item.Response.characterInventories.data[$"{charId}"].items.Count; j++)
                    {
                        long hash = item.Response.characterInventories.data[$"{charId}"].items[j].itemHash;
                        switch (hash)
                        {
                            // Upgrade Modules
                            case 2979281381: UpgradeModules += int.Parse($"{item.Response.characterInventories.data[$"{charId}"].items[j].quantity}"); break;

                            // Enhancement Cores
                            case 3853748946: EnhancementCores += int.Parse($"{item.Response.characterInventories.data[$"{charId}"].items[j].quantity}"); break;

                            // Enhancement Prisms
                            case 4257549984: EnhancementPrisms += int.Parse($"{item.Response.characterInventories.data[$"{charId}"].items[j].quantity}"); break;

                            // Ascendant Shards
                            case 4257549985: AscendantShards += int.Parse($"{item.Response.characterInventories.data[$"{charId}"].items[j].quantity}"); break;

                            // Spoils of Conquest
                            case 3702027555: SpoilsOfConquest += int.Parse($"{item.Response.characterInventories.data[$"{charId}"].items[j].quantity}"); break;

                            // Resonant Alloy
                            case 2497395625: ResonantAlloy += int.Parse($"{item.Response.characterInventories.data[$"{charId}"].items[j].quantity}"); break;

                            // Drowned Alloy
                            case 2708128607: DrownedAlloy += int.Parse($"{item.Response.characterInventories.data[$"{charId}"].items[j].quantity}"); break;

                            // Ascendant Alloy
                            case 353704689: AscendantAlloy += int.Parse($"{item.Response.characterInventories.data[$"{charId}"].items[j].quantity}"); break;

                            default: break;
                        }
                    }
                }

                Glimmer += int.Parse($"{item.Response.profileCurrencies.data.items[0].quantity}");
                LegendaryShards += int.Parse($"{item.Response.profileCurrencies.data.items[1].quantity}");
                BrightDust += int.Parse($"{item.Response.profileCurrencies.data.items[2].quantity}");

                ResonantElement += int.Parse($"{item.Response.profileStringVariables.data.integerValuesByHash["2747150405"]}");

                var app = await Context.Client.GetApplicationInfoAsync();
                var auth = new EmbedAuthorBuilder()
                {
                    Name = $"{dil.UniqueBungieName} Material and Currency Count",
                    IconUrl = User.GetAvatarUrl(),
                };
                var foot = new EmbedFooterBuilder()
                {
                    Text = $"Powered by the Bungie API"
                };
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                    Author = auth,
                    Footer = foot,
                };
                embed.Description = $"";

                embed.AddField(x =>
                {
                    x.Name = "Currency";
                    x.Value = $"{DestinyEmote.Glimmer} {Glimmer:n0}\n" +
                        $"{DestinyEmote.LegendaryShards} {LegendaryShards:n0}\n" +
                        $"{DestinyEmote.BrightDust} {BrightDust:n0}";
                    x.IsInline = true;
                }).AddField(x =>
                {
                    x.Name = "Masterwork";
                    x.Value = $"{DestinyEmote.EnhancementCore} {EnhancementCores:n0}\n" +
                        $"{DestinyEmote.EnhancementPrism} {EnhancementPrisms:n0}\n" +
                        $"{DestinyEmote.AscendantShard} {AscendantShards:n0}";
                    x.IsInline = true;
                }).AddField(x =>
                {
                    x.Name = "Crafting";
                    x.Value = $"{DestinyEmote.ResonantElement} {ResonantElement:n0}\n" +
                        $"{DestinyEmote.ResonantAlloy} {ResonantAlloy:n0}\n" +
                        $"{DestinyEmote.DrownedAlloy} {DrownedAlloy:n0}\n" +
                        $"{DestinyEmote.AscendantAlloy} {AscendantAlloy:n0}";
                    x.IsInline = true;
                })
                .AddField(x =>
                {
                    x.Name = "Miscellaneous";
                    x.Value = $"{DestinyEmote.UpgradeModule} {UpgradeModules:n0}\n" +
                        $"{DestinyEmote.SpoilsOfConquest} {SpoilsOfConquest:n0}";
                    x.IsInline = true;
                });

                await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); });
            }
        }

        //[SlashCommand("nightfall", "Display Nightfall information.")]
        //public async Task Nightfall([Summary("nightfall", "Nightfall Strike."),
        //        Choice("The Scarlet Keep", 0), Choice("The Arms Dealer", 1), Choice("The Lightblade", 2),
        //        Choice("The Glassway", 3), Choice("Fallen S.A.B.E.R.", 4), Choice("Birthplace of the Vile", 5)] int ArgNF)
        //{
        //    await RespondAsync($"Gathering data on new Nightfalls. Check back later!", ephemeral: true);
        //    return;
        //}

        //[SlashCommand("patrol", "Display Patrol information.")]
        //public async Task Patrol([Summary("location", "Patrol location."),
        //        Choice("The Dreaming City", 0), Choice("The Moon", 1), Choice("Europa", 2)] int ArgLocation)
        //{
        //    await RespondAsync($"Command is under construction! Wait for a future update.", ephemeral: true);
        //    return;
        //}

        //[SlashCommand("raid", "Display Raid information.")]
        //public async Task Raid([Summary("raid", "Raid name."),
        //        Choice("Last Wish", 0), Choice("Garden of Salvation", 1), Choice("Deep Stone Crypt", 2), Choice("Vault of Glass", 3)] int ArgRaid)
        //{
        //    await RespondAsync($"Command is under construction! Wait for a future update.", ephemeral: true);
        //    return;
        //}

        [SlashCommand("try-on", "Try on any emblem in the Bungie API.")]
        public async Task TryOut([Summary("emblem-name", "Emblem name of the Emblem you want to try on."), Autocomplete(typeof(EmblemAutocomplete))] string SearchQuery)
        {
            string name = "";
            var linkedUser = DataConfig.GetLinkedUser(Context.User.Id);
            if (linkedUser != null)
                name = linkedUser.UniqueBungieName.Substring(0, linkedUser.UniqueBungieName.Length - 5);
            else
                name = Context.User.Username;

            if (!long.TryParse(SearchQuery, out long HashCode))
            {
                await RespondAsync($"Invalid search, please try again. Make sure to choose one of the autocomplete options!", ephemeral: true);
                return;
            }

            Emblem emblem;
            Bitmap bitmap;
            try
            {
                emblem = new Emblem(HashCode);
                bitmap = (Bitmap)GetImageFromPicPath(emblem.GetBackgroundUrl()); //load the image file
            }
            catch (Exception)
            {
                await RespondAsync($"No emblem found for Hash Code: {HashCode}.");
                return;
            }

            await DeferAsync();

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                using (Font font = new Font("Neue Haas Grotesk Display Pro", 18, FontStyle.Bold))
                {
                    graphics.DrawString(name, font, Brushes.White, new PointF(83f, 7f));
                }

                using (Font font = new Font("Neue Haas Grotesk Display Pro", 14))
                {
                    graphics.DrawString($"Levante Bot{(BotConfig.IsSupporter(Context.User.Id) ? " Supporter" : "")} {(RequireBotStaff.IsBotStaff(Context.User.Id) ? " Staff" : "")}", font, new SolidBrush(System.Drawing.Color.FromArgb(128, System.Drawing.Color.White)), new PointF(84f, 37f));
                }
            }

            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                byte[] bytes = stream.ToArray();
                using (var fs = new FileStream(@"temp.png", FileMode.Create))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Close();
                }
            }

            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Emblem Testing",
                IconUrl = emblem.GetIconUrl(),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"This is not a perfect replica."
            };
            int[] emblemRGB = emblem.GetRGBAsIntArray();
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(emblemRGB[0], emblemRGB[1], emblemRGB[2]),
                Author = auth,
                Footer = foot
            };
            embed.Description =
                    $"This is probably what you'd look like in-game if you had **{emblem.GetName()}**.";
            embed.ImageUrl = @"attachment://temp.png";
            await Context.Interaction.FollowupWithFileAsync(filePath: "temp.png", embed: embed.Build());
        }

        [Group("view", "Get details on in-game items.")]
        public class View : InteractionModuleBase<SocketInteractionContext>
        {
            public InteractiveService Interactive { get; set; }

            [SlashCommand("emblem", "Get details on an emblem via its Hash Code found via Bungie's API.")]
            public async Task ViewEmblem([Summary("name", "Name of the emblem you want details for."), Autocomplete(typeof(EmblemAutocomplete))] string SearchQuery)
            {
                if (string.IsNullOrWhiteSpace(SearchQuery))
                {
                    await RespondAsync($"Invalid search, please try again.", ephemeral: true);
                    return;
                }

                if (!long.TryParse(SearchQuery, out long HashCode))
                {
                    await RespondAsync($"Invalid search, please try again. Make sure to choose one of the autocomplete options!", ephemeral: true);
                    return;
                }

                await DeferAsync();
                Emblem emblem;
                try
                {
                    emblem = new Emblem(HashCode);
                }
                catch (Exception)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"No emblem found for Hash Code: {HashCode}."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                    return;
                }

                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = emblem.GetEmbed().Build(); message.Content = null; message.Components = new ComponentBuilder().Build(); });
            }

            [SlashCommand("weapon", "Get details on a weapon via its Hash Code found via Bungie's API.")]
            public async Task ViewWeapon([Summary("name", "Name of the weapon you want details for."), Autocomplete(typeof(WeaponAutocomplete))] string SearchQuery)
            {
                if (string.IsNullOrWhiteSpace(SearchQuery))
                {
                    await RespondAsync($"Invalid search, please try again.", ephemeral: true);
                    return;
                }

                if (!long.TryParse(SearchQuery, out long HashCode))
                {
                    await RespondAsync($"Invalid search, please try again. Make sure to choose one of the autocomplete options!", ephemeral: true);
                    return;
                }

                await DeferAsync();
                Weapon weapon;
                try
                {
                    weapon = new Weapon(HashCode);
                }
                catch (Exception)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"No weapon found for Hash Code: {HashCode}."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                    return;
                }

                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = weapon.GetEmbed().Build(); message.Content = null; message.Components = new ComponentBuilder().Build(); });
            }
        }

        [SlashCommand("weekly", "Display Weekly reset information.")]
        public async Task Weekly()
        {
            await RespondAsync($"", embed: CurrentRotations.WeeklyResetEmbed().Build());
            return;
        }

        public class BungieTagAutocomplete : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                await Task.Delay(0);
                // Create a collection with suggestions for autocomplete
                List<AutocompleteResult> results = new();
                string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();
                if (String.IsNullOrWhiteSpace(SearchQuery))
                    return AutocompletionResult.FromSuccess();
                else if (SearchQuery.Contains('#'))
                {
                    foreach (var linkUser in DataConfig.DiscordIDLinks)
                        if (linkUser.UniqueBungieName.ToLower().Contains(SearchQuery.ToLower()))
                            results.Add(new AutocompleteResult($"{linkUser.UniqueBungieName}", $"{linkUser.UniqueBungieName}"));
                }
                else
                {
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);
                        // Attempt to use post, but results in Error Code 30.
                        //var values = new Dictionary<string, string>
                        //{
                        //    { "displayNamePrefix", SearchQuery },
                        //};
                        //var postContent = new FormUrlEncodedContent(values);

                        //var response = client.PostAsync("https://www.bungie.net/Platform/User/Search/GlobalName/0/", postContent).Result;
                        var response = client.GetAsync($"https://www.bungie.net/Platform/User/Search/Prefix/{SearchQuery}/0/").Result;
                        var content = response.Content.ReadAsStringAsync().Result;
                        dynamic item = JsonConvert.DeserializeObject(content);

                        foreach (var result in item.Response.searchResults)
                            results.Add(new AutocompleteResult($"{result.bungieGlobalDisplayName}#{$"{result.bungieGlobalDisplayNameCode}".PadLeft(4, '0')}",
                                $"{result.bungieGlobalDisplayName}#{$"{result.bungieGlobalDisplayNameCode}".PadLeft(4, '0')}"));
                    }
                }

                results = results.OrderBy(x => x.Name).ToList();

                // max - 25 suggestions at a time (API limit)
                return AutocompletionResult.FromSuccess(results.Take(25));
            }
        }

        public class EmblemAutocomplete : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                await Task.Delay(0);
                // Create a collection with suggestions for autocomplete
                List<AutocompleteResult> results = new();
                string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();
                if (String.IsNullOrWhiteSpace(SearchQuery))
                    for (int i = 0; i < 7; i++)
                    {
                        var emblem = ManifestHelper.Emblems.ElementAt(new Random().Next(0, ManifestHelper.Emblems.Count));
                        results.Add(new AutocompleteResult(emblem.Value, $"{emblem.Key}"));
                    }
                else
                    foreach (var Emblem in ManifestHelper.Emblems)
                        if (Emblem.Value.ToLower().Contains(SearchQuery.ToLower()))
                            results.Add(new AutocompleteResult(Emblem.Value, $"{Emblem.Key}"));

                results = results.OrderBy(x => x.Name).ToList();

                // max - 25 suggestions at a time (API limit)
                return AutocompletionResult.FromSuccess(results.Take(25));
            }
        }

        public class WeaponAutocomplete : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                await Task.Delay(0);
                // Create a collection with suggestions for autocomplete
                List<AutocompleteResult> results = new();
                string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();
                if (String.IsNullOrWhiteSpace(SearchQuery))
                    for (int i = 0; i < 7; i++)
                    {
                        var weapon = ManifestHelper.Weapons.ElementAt(new Random().Next(0, ManifestHelper.Emblems.Count));
                        results.Add(new AutocompleteResult(weapon.Value, $"{weapon.Key}"));
                    }
                else
                    foreach (var Weapon in ManifestHelper.Weapons)
                        if (Weapon.Value.ToLower().Contains(SearchQuery.ToLower()))
                            results.Add(new AutocompleteResult(Weapon.Value, $"{Weapon.Key}"));

                results = results.OrderBy(x => x.Name).ToList();

                // max - 25 suggestions at a time (API limit)
                return AutocompletionResult.FromSuccess(results.Take(25));
            }
        }

        public class CurrentOfferAutocomplete : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                await Task.Delay(0);
                // Create a collection with suggestions for autocomplete
                List<AutocompleteResult> results = new();
                string SearchQuery = autocompleteInteraction.Data.Current.Value.ToString();

                if (String.IsNullOrWhiteSpace(SearchQuery))
                    foreach (var Offer in EmblemOffer.CurrentOffers)
                        results.Add(new AutocompleteResult(Offer.OfferedEmblem.GetName(), $"{Offer.EmblemHashCode}"));
                else
                    foreach (var Offer in EmblemOffer.CurrentOffers)
                        if (Offer.OfferedEmblem.GetName().ToLower().Contains(SearchQuery.ToLower()))
                            results.Add(new AutocompleteResult(Offer.OfferedEmblem.GetName(), $"{Offer.EmblemHashCode}"));

                results = results.OrderBy(x => x.Name).ToList();

                // max - 25 suggestions at a time (API limit)
                return AutocompletionResult.FromSuccess(results.Take(25));
            }
        }

        private string GetCurrentDestiny2Season(out int SeasonNumber)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/3/Profile/4611686018471482002/?components=100").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                ulong seasonHash = item.Response.profile.data.currentSeasonHash;

                var response1 = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/Manifest/DestinySeasonDefinition/" + seasonHash + "/").Result;
                var content1 = response1.Content.ReadAsStringAsync().Result;
                dynamic item1 = JsonConvert.DeserializeObject(content1);

                SeasonNumber = item1.Response.seasonNumber;
                return $"{item1.Response.displayProperties.name}";
            }
        }

        public static System.Drawing.Image GetImageFromPicPath(string strUrl)
        {
            using (WebResponse wrFileResponse = WebRequest.Create(strUrl).GetResponse())
            {
                using (Stream objWebStream = wrFileResponse.GetResponseStream())
                {
                    MemoryStream ms = new MemoryStream();
                    objWebStream.CopyTo(ms, 8192);
                    return System.Drawing.Image.FromStream(ms);
                }
            }
        }
    }
}
