using BungieSharper.Entities.Destiny;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Levante.Configs;
using Levante.Helpers;
using Levante.Services;
using Levante.Structs;
using Levante.Util;
using Levante.Util.Attributes;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Levante.Commands
{
    public class Destiny : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly InteractiveService Interactive;
        private readonly LoggingService Logging;
        private readonly UserService Users;
        private readonly GuildService Guilds;

        public Destiny(LoggingService xpLoggingService, InteractiveService interactive, UserService users, GuildService guilds)
        {
            Logging = xpLoggingService;
            Interactive = interactive;
            Users = users;
            Guilds = guilds;
        }

        [SlashCommand("artifact", "Project what level you need to hit for a specific Power bonus.")]
        public async Task ArtifactBonusPrediction([Summary("power-bonus", "Power Level to project. Range: 1-100")] int PowerBonus)
        {
            await DeferAsync();

            PowerBonus = PowerBonus switch
            {
                < 1 => 1,
                > 100 => 100,
                _ => PowerBonus,
            };

            for (int i = 0; i < PowerBonus; i++)
            {
                Log.Debug($"{i}, {GetXPForLevel(i)}, {GetXPForBoost(i)}");
            }

            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Power Bonus Projection",
                IconUrl = app.IconUrl
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {AppConfig.App.Name} v{AppConfig.App.Version}"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                Author = auth,
                Footer = foot,
            };
            embed.Title = $"Power Bonus +{PowerBonus} at Level ";
            int Level = 0, ExtraLevel = 0, XpProgress = 0, XpProgressCap = 0, OverflowXpProgressCap = 0, LevelCap = ManifestHelper.CurrentLevelCap;

            if (Users.IsExistingUser(Context.User.Id))
            {
                var dil = Users.GetUser(Context.User.Id);

                var content = await BungieApi.Query($"/Platform/Destiny2/{dil.BungieCredentials.MembershipType}/Profile/{dil.BungieCredentials.MembershipId}/?components=100,104,202", dil.BungieCredentials.AccessToken);
                dynamic item = JsonConvert.DeserializeObject(content);

                XpProgressCap = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.BaseRanks}"].nextLevelAt;
                OverflowXpProgressCap = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.ExtraRanks}"].nextLevelAt;

                if (item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.BaseRanks}"].level == LevelCap)
                {
                    Level = LevelCap;
                    ExtraLevel = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.ExtraRanks}"].level;
                    XpProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.ExtraRanks}"].progressToNextLevel;
                }
                else
                {
                    Level = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.BaseRanks}"].level;
                    XpProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.BaseRanks}"].progressToNextLevel;
                }
                int currentPowerBonus = item.Response.profileProgression.data.seasonalArtifact.powerBonus;
                double projectedSeasonRank = 0.0;

                if (currentPowerBonus >= PowerBonus)
                {
                    int xpNeeded = GetXPForBoost(PowerBonus);
                    var seasonRanksNeeded = 0.0;

                    if (Level < LevelCap)
                    {
                        var xpToCap = ((LevelCap - Level) * XpProgressCap) - XpProgress;
                        if (xpToCap < xpNeeded)
                        {
                            var overflowXpNeeded = xpNeeded - xpToCap;
                            var extraLevelsNeeded = (double)overflowXpNeeded / OverflowXpProgressCap;

                            projectedSeasonRank = Level + ((double)xpToCap / XpProgressCap) + extraLevelsNeeded;

                            var remainder = xpToCap % XpProgressCap;
                            if (remainder + XpProgress >= XpProgressCap)
                                projectedSeasonRank += 1;
                        }
                        else
                        {
                            seasonRanksNeeded = (xpNeeded - XpProgress) / XpProgressCap;
                            projectedSeasonRank = Level + seasonRanksNeeded;
                        }
                    }
                    else
                    {
                        seasonRanksNeeded = (xpNeeded - XpProgress) / OverflowXpProgressCap;
                        projectedSeasonRank = Level + ExtraLevel + seasonRanksNeeded;
                    }

                    embed.Title += $"{seasonRanksNeeded:0.00}";
                    embed.Description =
                        $"You already hit this Power bonus; I cannot provide a personalized projection.\n" +
                        $"> You will hit Power Bonus +{PowerBonus} at roughly Level **{seasonRanksNeeded:0.00}**.";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); });
                }
                else
                {
                    int progressToNextLevel = item.Response.profileProgression.data.seasonalArtifact.powerBonusProgression.progressToNextLevel;
                    int nextPowerLevelAt = item.Response.profileProgression.data.seasonalArtifact.powerBonusProgression.nextLevelAt;

                    int totalXp = (Level * XpProgressCap) + (ExtraLevel * OverflowXpProgressCap) + progressToNextLevel;
                    int xpNeeded = GetXPForBoost(PowerBonus) - totalXp;
                    var seasonRanksNeeded = 0.0;
                    if (Level < LevelCap)
                    {
                        var xpToCap = ((LevelCap - Level) * XpProgressCap) - XpProgress;
                        if (xpToCap < xpNeeded)
                        {
                            var overflowXpNeeded = xpNeeded - xpToCap;
                            var extraLevelsNeeded = (double)overflowXpNeeded / OverflowXpProgressCap;

                            projectedSeasonRank = Level + ((double)xpToCap / XpProgressCap) + extraLevelsNeeded;

                            var remainder = xpToCap % XpProgressCap;
                            if (remainder + XpProgress >= XpProgressCap)
                                projectedSeasonRank += 1;
                        }
                        else
                        {
                            seasonRanksNeeded = (double)xpNeeded / XpProgressCap;
                            projectedSeasonRank = Level + ((double)progressToNextLevel / XpProgressCap) + seasonRanksNeeded;
                        }
                    }
                    else
                    {
                        seasonRanksNeeded = (double)xpNeeded / OverflowXpProgressCap;
                        projectedSeasonRank = Level + ExtraLevel + ((double)progressToNextLevel / OverflowXpProgressCap) + seasonRanksNeeded;
                    }

                    embed.Title += $"{projectedSeasonRank:0.00}";
                    embed.Description =
                        $"> You will hit Power Bonus +{PowerBonus} at roughly Level **{projectedSeasonRank:0.00}** (Need {seasonRanksNeeded:0.00}).";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); });
                }
            }
            else
            {
                int xpNeeded = GetXPForBoost(PowerBonus);
                var seasonRanksNeeded = 0.0;
                XpProgressCap = ManifestHelper.BaseNextLevelAt;
                OverflowXpProgressCap = ManifestHelper.ExtraNextLevelAt;

                if (xpNeeded > XpProgressCap * LevelCap)
                {
                    var overflowXpNeeded = xpNeeded - (XpProgressCap * LevelCap);
                    seasonRanksNeeded = LevelCap + ((double)overflowXpNeeded / OverflowXpProgressCap);
                }
                else
                {
                    seasonRanksNeeded = ((double)xpNeeded / XpProgressCap);
                }

                embed.Title += $"{seasonRanksNeeded:0.00}";
                embed.Description =
                        $"You are not linked; I cannot provide a personalized projection.\n" +
                        $"> You will hit Power Bonus +{PowerBonus} at roughly Level **{seasonRanksNeeded:0.00}**.";
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); });
            }
        }

        [SlashCommand("countdowns", "Gives remaining time to Destiny 2 events and releases.")]
        public async Task Countdowns()
        {
            await DeferAsync();
            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Countdowns",
                IconUrl = app.IconUrl
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {AppConfig.App.Name} v{AppConfig.App.Version}"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                Author = auth,
                Footer = foot,
            };
            foreach (var Countdown in CountdownConfig.Countdowns.OrderBy(key => key.Value))
                embed.AddField(x =>
                {
                    x.Name = $"{Countdown.Key}";
                    x.Value = $"Starts {TimestampTag.FromDateTime(Countdown.Value, TimestampTagStyles.Relative)} ({TimestampTag.FromDateTime(Countdown.Value, TimestampTagStyles.ShortDate)})\n";
                    x.IsInline = false;
                });

            await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); });
        }

        [SlashCommand("current-offers", "Gives a list of emblem offers. If an emblem is provided, you'll get the specific offer.")]
        public async Task CurrentOffers([Summary("emblem-name", "Emblem name of an emblem that is a current offer."), Autocomplete(typeof(CurrentOfferAutocomplete))] string EmblemHash = null)
        {
            await DeferAsync();
            if (EmblemHash == null)
            {
                string bungieName = "";
                var emblemsUserMissing = new List<string>();
                if (Users.IsExistingUser(Context.User.Id))
                {
                    var linkedUser = Users.GetUser(Context.User.Id);

                    var content = await BungieApi.Query($"/Platform/Destiny2/{linkedUser.BungieCredentials.MembershipType}/Profile/{linkedUser.BungieCredentials.MembershipId}/?components=800", linkedUser.BungieCredentials.AccessToken);
                    dynamic item = JsonConvert.DeserializeObject(content);

                    foreach (var emblem in EmblemOffer.CurrentOffers)
                    {
                        var hash = ManifestHelper.Emblems.First(x => x.Value.Contains(emblem.OfferedEmblem.GetName())).Key;
                        var emblemCollectible = ManifestHelper.EmblemsCollectible[hash];
                        var hasEmblem = !((DestinyCollectibleState)item.Response.profileCollectibles.data.collectibles[$"{emblemCollectible}"].state).HasFlag(DestinyCollectibleState.NotAcquired);

                        if (!hasEmblem)
                            emblemsUserMissing.Add(emblem.OfferedEmblem.GetName());
                    }
                    bungieName = linkedUser.BungieCredentials.BungieName;
                }

                if (EmblemOffer.CurrentOffers.Count > 10)
                {
                    var paginator = new LazyPaginatorBuilder()
                        .AddUser(Context.User)
                        .WithPageFactory(GeneratePage)
                        .WithMaxPageIndex((int)Math.Ceiling(EmblemOffer.CurrentOffers.Count / (decimal)10) - 1)
                        .AddOption(new Emoji("◀"), PaginatorAction.Backward)
                        .AddOption(new Emoji("🔢"), PaginatorAction.Jump)
                        .AddOption(new Emoji("▶"), PaginatorAction.Forward)
                        .AddOption(new Emoji("🛑"), PaginatorAction.Exit)
                        .WithActionOnCancellation(ActionOnStop.DeleteInput)
                        .WithActionOnTimeout(ActionOnStop.DeleteInput)
                        .WithFooter(PaginatorFooter.None)
                        .Build();

                    await Interactive.SendPaginatorAsync(paginator, Context.Interaction, TimeSpan.FromSeconds(AppConfig.Discord.DurationToWaitForPaginator), InteractionResponseType.DeferredChannelMessageWithSource);

                    PageBuilder GeneratePage(int index)
                    {
                        var embed = EmblemOffer.GetOfferListEmbed(index, bungieName, emblemsUserMissing);
                        return new PageBuilder()
                            .WithAuthor(embed.Author)
                            .WithDescription(embed.Description)
                            .WithColor((Discord.Color)embed.Color)
                            .WithFooter(embed.Footer);
                    }
                }
                else
                {
                    // No need to paginate because we should be able to fit all (less than 10) offers.
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = EmblemOffer.GetOfferListEmbed(0, bungieName, emblemsUserMissing).Build(); });
                }

                return;
            }
            long EmblemHashCode = long.Parse(EmblemHash);
            var offer = EmblemOffer.GetSpecificOffer(EmblemHashCode);
            if (!EmblemOffer.HasExistingOffer(EmblemHashCode))
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Invalid search, please try again. Make sure to choose one of the autocomplete options!"; });
            else
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = offer.BuildEmbed().Build(); message.Components = offer.BuildExternalButton().Build(); });
        }

        //[RequireBungieOauth]
        //[SlashCommand("fishing", "Get a player's Destiny 2 fishing information.")]
        //public async Task Fishing([Summary("hide", "Hide this post from users except yourself. Default: false")] bool hide = false)
        //{
        //    var User = Context.User;

        //    await DeferAsync(ephemeral: hide);
        //    var dil = DataConfig.GetLinkedUser(User.Id);
        //    if (dil == null)
        //    {
        //        var errorEmbed = Embeds.GetErrorEmbed();
        //        errorEmbed.Description = "Unable to pull user data. I may have lost access to their information, likely, they'll have to link again.";
        //        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = errorEmbed.Build(); });
        //        return;
        //    }

        //    int TotalFish, ExoticHeld, LegendaryHeld, RareHeld, UncommonHeld, Bait, MaxBait;
        //    double LargestFish;

        //    // ???????????????????????????????????????????????????
        //    var exoticFishCharacter = new Dictionary<long, string>
        //    {
        //        // Exotic
        //        { 3215008487, "Aeonian Alpha-Betta" },
        //        { 3821744120, "Whispering Mothcarp" },
        //        { 4065264321, "Vexing Placoderm" },
        //    };

        //    var exoticFish = new Dictionary<long, string>
        //    {
        //        // Exotic
        //        { 3045091722, "Kheprian Axehead" },
        //    };

        //    var legendFish = new Dictionary<long, string>
        //    {
        //        // Legendary
        //        { 2295044628, "Drangelfish (Baroque)" },
        //        { 4013700867, "Salvager's Salmon" },
        //        { 729316630, "Gnawing Hun Gar" },
        //        { 307419853, "No Turning Jack" },
        //        { 938043848, "Cod Duello" },
        //        { 686316983, "Temptation's Haddock" },
        //        { 3824155546, "Ignition Toad" },
        //        { 2440190801, "Deafening Whisker" },
        //        { 2811057884, "Servant Lobster" },
        //        { 3982872811, "Galliard Trevally" },
        //    };

        //    var rareFish = new Dictionary<long, string>
        //    {
        //        // Rare
        //        { 3821517233, "Aachen Cichlid" },
        //        { 3420416378, "Chiron's Carp" },
        //        { 3914115223, "Koi Cirrus" },
        //        { 4165738920, "Madrugadan Mackerel" },
        //        { 583260973, "Golden Trevallyhoo" },
        //        { 3998588662, "Agronatlantic Salmon" },
        //        { 1504217571, "Traxian Toad" },
        //        { 2612359540, "Hardcase Haddock" },
        //        { 1099115065, "Lamian Lobster" },
        //        { 2310653154, "Azimuth Angelfish" },
        //        { 2468256932, "Cup Bearer Catfish" },
        //        { 305477331, "Allegrian Jack" },
        //        { 1820537638, "Cuboid Cod" },
        //        { 151832733, "Gusevian Gar" },
        //    };

        //    var uncommonFish = new Dictionary<long, string>
        //    {
        //        // Uncommon
        //        { 3775818187, "Cydonian Cichlid" },
        //        { 1136707388, "At Least It's a Carp" },
        //        { 1184450997, "Hadrian Koi" },
        //        { 3081348702, "Minueting Mackerel" },
        //    };

        //    using var client = new HttpClient();

        //    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);
        //    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {dil.AccessToken}");

        //    var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + dil.BungieMembershipType + "/Profile/" + dil.BungieMembershipID + "/?components=100,200,900,1100,1200").Result;
        //    var content = response.Content.ReadAsStringAsync().Result;
        //    dynamic item = JsonConvert.DeserializeObject(content);

        //    TotalFish = int.Parse($"{item.Response.metrics.data.metrics["24768693"].objectiveProgress.progress}");

        //    Bait = int.Parse($"{item.Response.profileStringVariables.data.integerValuesByHash["3758904843"]}");
        //    MaxBait = int.Parse($"{item.Response.profileStringVariables.data.integerValuesByHash["1523938882"]}");

        //    ExoticHeld = int.Parse($"{item.Response.profileStringVariables.data.integerValuesByHash["1423773299"]}");
        //    LegendaryHeld = int.Parse($"{item.Response.profileStringVariables.data.integerValuesByHash["3064136636"]}");
        //    RareHeld = int.Parse($"{item.Response.profileStringVariables.data.integerValuesByHash["9025129"]}");
        //    UncommonHeld = int.Parse($"{item.Response.profileStringVariables.data.integerValuesByHash["2934707299"]}");

        //    LargestFish = int.Parse($"{item.Response.metrics.data.metrics["2691615711"].objectiveProgress.progress}")/(double)100;

        //    var auth = new EmbedAuthorBuilder()
        //    {
        //        Name = $"{dil.UniqueBungieName} Fishing Stats",
        //        IconUrl = User.GetAvatarUrl(),
        //    };
        //    var foot = new EmbedFooterBuilder()
        //    {
        //        Text = "Powered by the Bungie API"
        //    };
        //    var embed = new EmbedBuilder()
        //    {
        //        Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
        //        Author = auth,
        //        Footer = foot,
        //    };

        //    embed.Description = $"Total Fish Caught: **{TotalFish:n0}**\n" +
        //                        $"Largest Fish Caught: **{LargestFish:0.00} kg**\n" +
        //                        $"{DestinyEmote.Bait} **{Bait}**/**{MaxBait}**{(Bait == MaxBait ? " (Go Fishing Guardian!)" : "")}";

        //    string heldFishString = "";

        //    if (ExoticHeld != 0)
        //        heldFishString += $"Exotic: **{ExoticHeld:n0}**\n";

        //    if (LegendaryHeld != 0)
        //        heldFishString += $"Legendary: **{LegendaryHeld:n0}**\n";

        //    if (RareHeld != 0)
        //        heldFishString += $"Rare: **{RareHeld:n0}**\n";

        //    if (UncommonHeld != 0)
        //        heldFishString += $"Uncommon: **{UncommonHeld:n0}**";

        //    embed.AddField(x =>
        //    {
        //        x.Name = "Fish Held";
        //        x.Value = String.IsNullOrEmpty(heldFishString) ? $"No fish held. Go catch some fish!{(Bait <= 0 ? "Wait... you have no bait... Go get bait first!" : "")}" : heldFishString;
        //        x.IsInline = true;
        //    });

        //    string json = File.ReadAllText(EmoteConfig.FilePath);
        //    var emoteCfg = JsonConvert.DeserializeObject<EmoteConfig>(json);

        //    List<string> charIds = new();
        //    for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
        //    {
        //        try
        //        {
        //            charIds.Add($"{item.Response.profile.data.characterIds[i]}");
        //        }
        //        catch (Exception x)
        //        {
        //            Console.WriteLine($"{x}");
        //        }
        //    }

        //    string exoticFishString = "";
        //    foreach (var fish in exoticFishCharacter)
        //    {
        //        var newFishString = fish.Value.Replace(" ", "").Replace("-", "").Replace("'", "").Replace("(", "").Replace(")", "");
        //        var fishItem = ManifestHelper.Fish.First(x => x.Value.DisplayProperties.Name.Equals(fish.Value));
        //        if (!emoteCfg.HasEmote(newFishString))
        //        {
        //            var byteArray = new HttpClient().GetByteArrayAsync($"https://bungie.net{fishItem.Value.DisplayProperties.Icon}").Result;
        //            Task.Run(() => emoteCfg.AddEmote(newFishString, new Discord.Image(new MemoryStream(byteArray)))).Wait();
        //        }
        //        // Yeah, let's just make these character records even though they are the same number regardless.
        //        var fishRecord = item.Response.characterRecords.data[charIds.First()].records[$"{fish.Key}"];
        //        exoticFishString += $"{emoteCfg.GetEmote(newFishString)}{fishRecord.intervalObjectives[0].progress} ";
        //    }

        //    foreach (var fish in exoticFish)
        //    {
        //        var newFishString = fish.Value.Replace(" ", "").Replace("-", "").Replace("'", "").Replace("(", "").Replace(")", "");
        //        var fishItem = ManifestHelper.Fish.First(x => x.Value.DisplayProperties.Name.Equals(fish.Value));
        //        if (!emoteCfg.HasEmote(newFishString))
        //        {
        //            var byteArray = new HttpClient().GetByteArrayAsync($"https://bungie.net{fishItem.Value.DisplayProperties.Icon}").Result;
        //            Task.Run(() => emoteCfg.AddEmote(newFishString, new Discord.Image(new MemoryStream(byteArray)))).Wait();
        //        }

        //        var fishRecord = item.Response.profileRecords.data.records[$"{fish.Key}"];
        //        exoticFishString += $"{emoteCfg.GetEmote(newFishString)}{fishRecord.intervalObjectives[0].progress}";
        //    }

        //    string legendFishString = "";
        //    foreach (var fish in legendFish)
        //    {
        //        var newFishString = fish.Value.Replace(" ", "").Replace("-", "").Replace("'", "").Replace("(", "").Replace(")", "");
        //        var fishItem = ManifestHelper.Fish.First(x => x.Value.DisplayProperties.Name.Equals(fish.Value));
        //        if (!emoteCfg.HasEmote(newFishString))
        //        {
        //            var byteArray = new HttpClient().GetByteArrayAsync($"https://bungie.net{fishItem.Value.DisplayProperties.Icon}").Result;
        //            Task.Run(() => emoteCfg.AddEmote(newFishString, new Discord.Image(new MemoryStream(byteArray)))).Wait();
        //        }

        //        var fishRecord = item.Response.profileRecords.data.records[$"{fish.Key}"];
        //        legendFishString += $"{emoteCfg.GetEmote(newFishString)}{fishRecord.intervalObjectives[0].progress} ";
        //    }

        //    string rareFishString = "";
        //    foreach (var fish in rareFish)
        //    {
        //        var newFishString = fish.Value.Replace(" ", "").Replace("-", "").Replace("'", "").Replace("(", "").Replace(")", "");
        //        var fishItem = ManifestHelper.Fish.First(x => x.Value.DisplayProperties.Name.Equals(fish.Value));
        //        if (!emoteCfg.HasEmote(newFishString))
        //        {
        //            var byteArray = new HttpClient().GetByteArrayAsync($"https://bungie.net{fishItem.Value.DisplayProperties.Icon}").Result;
        //            Task.Run(() => emoteCfg.AddEmote(newFishString, new Discord.Image(new MemoryStream(byteArray)))).Wait();
        //        }

        //        var fishRecord = item.Response.profileRecords.data.records[$"{fish.Key}"];
        //        rareFishString += $"{emoteCfg.GetEmote(newFishString)}{fishRecord.intervalObjectives[0].progress} ";
        //    }

        //    string uncommonFishString = "";
        //    foreach (var fish in uncommonFish)
        //    {
        //        var newFishString = fish.Value.Replace(" ", "").Replace("-", "").Replace("'", "").Replace("(", "").Replace(")", "");
        //        var fishItem = ManifestHelper.Fish.First(x => x.Value.DisplayProperties.Name.Equals(fish.Value));
        //        if (!emoteCfg.HasEmote(newFishString))
        //        {
        //            var byteArray = new HttpClient().GetByteArrayAsync($"https://bungie.net{fishItem.Value.DisplayProperties.Icon}").Result;
        //            Task.Run(() => emoteCfg.AddEmote(newFishString, new Discord.Image(new MemoryStream(byteArray)))).Wait();
        //        }

        //        var fishRecord = item.Response.profileRecords.data.records[$"{fish.Key}"];
        //        uncommonFishString += $"{emoteCfg.GetEmote(newFishString)}{fishRecord.intervalObjectives[0].progress} ";
        //    }
        //    emoteCfg.UpdateJSON();

        //    embed.AddField(x =>
        //    {
        //        x.Name = "Exotic Fish";
        //        x.Value = String.IsNullOrEmpty(exoticFishString) ? "No Exotic Fish." : exoticFishString;
        //        x.IsInline = false;
        //    }).AddField(x =>
        //    {
        //        x.Name = "Legendary Fish";
        //        x.Value = String.IsNullOrEmpty(legendFishString) ? "No Legendary Fish." : legendFishString;
        //        x.IsInline = false;
        //    }).AddField(x =>
        //    {
        //        x.Name = "Rare Fish";
        //        x.Value = String.IsNullOrEmpty(rareFishString) ? "No Rare Fish." : rareFishString;
        //        x.IsInline = false;
        //    }).AddField(x =>
        //    {
        //        x.Name = "Uncommon Fish";
        //        x.Value = String.IsNullOrEmpty(uncommonFishString) ? "No Uncommon Fish." : uncommonFishString;
        //        x.IsInline = false;
        //    });

        //    embed.ThumbnailUrl =
        //        "https://www.bungie.net/common/destiny2_content/icons/eefd1ff16bb5c84e188eccd1545084a4.jpg";

        //    await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); });
        //}

        [SlashCommand("free-emblems", "Display a list of universal emblem codes.")]
        public async Task FreeEmblems([Summary("only-show-missing", "Only show the emblem codes you are missing. Default: true")] bool onlyShowMissing = true, [Summary("hide", "Hide this post from users except yourself. Default: false")] bool hide = false)
        {
            await DeferAsync(ephemeral: hide);

            string bungieName = "";
            var emblemsUserMissing = new List<string>();
            if (Users.IsExistingUser(Context.User.Id))
            {
                var linkedUser = Users.GetUser(Context.User.Id);

                var content = await BungieApi.Query($"/Platform/Destiny2/{linkedUser.BungieCredentials.MembershipType}/Profile/{linkedUser.BungieCredentials.MembershipId}/?components=800", linkedUser.BungieCredentials.AccessToken);
                dynamic item = JsonConvert.DeserializeObject(content);

                foreach (var emblem in AppConfig.UniversalCodes)
                {
                    var hash = ManifestHelper.Emblems.First(x => x.Value.Contains(emblem.Name)).Key;
                    var emblemCollectible = ManifestHelper.EmblemsCollectible[hash];
                    var hasEmblem = !((DestinyCollectibleState)item.Response.profileCollectibles.data.collectibles[$"{emblemCollectible}"].state).HasFlag(DestinyCollectibleState.NotAcquired);

                    if (!hasEmblem)
                        emblemsUserMissing.Add(emblem.Name);
                }
                bungieName = linkedUser.BungieCredentials.BungieName;
            }

            var universalEmblems = AppConfig.UniversalCodes;

            if (onlyShowMissing)
                universalEmblems = universalEmblems.Where(x => emblemsUserMissing.Contains(x.Name)).ToList();

            if (universalEmblems.Count == 0)
                universalEmblems = AppConfig.UniversalCodes;

            var paginator = new LazyPaginatorBuilder()
                .AddUser(Context.User)
                .WithPageFactory(GeneratePage)
                .WithMaxPageIndex((int)Math.Ceiling(universalEmblems.Count / 10.0) - 1)
                .AddOption(new Emoji("◀"), PaginatorAction.Backward)
                .AddOption(new Emoji("🔢"), PaginatorAction.Jump)
                .AddOption(new Emoji("▶"), PaginatorAction.Forward)
                .AddOption(new Emoji("🛑"), PaginatorAction.Exit)
                .WithActionOnCancellation(ActionOnStop.DeleteInput)
                .WithActionOnTimeout(ActionOnStop.DeleteInput)
                .WithFooter(PaginatorFooter.None)
                .Build();

            await Interactive.SendPaginatorAsync(paginator, Context.Interaction, TimeSpan.FromSeconds(AppConfig.Discord.DurationToWaitForPaginator), responseType: InteractionResponseType.DeferredChannelMessageWithSource, ephemeral: hide);


            PageBuilder GeneratePage(int index)
            {
                var auth = new EmbedAuthorBuilder()
                {
                    Name = "Universal Emblem Codes",
                    IconUrl = Context.Client.GetApplicationInfoAsync().Result.IconUrl,
                };
                var foot = new EmbedFooterBuilder()
                {
                    Text = "These codes are not limited to one account and can be used by anyone."
                };
                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                    Author = auth,
                    Footer = foot,
                };

                foreach (var emblem in universalEmblems.GetRange(10 * index, (10 * index) + 10 > universalEmblems.Count ? universalEmblems.Count - (10 * index) : 10))
                {
                    embed.Description +=
                        $"> {(!String.IsNullOrEmpty(bungieName) && emblemsUserMissing.Contains(emblem.Name) ? $"{EmoteConfig.GetEmote("Warning")} " : "")}[{emblem.Name}]({BungieApi.API_BASE_URL}/7/en/Codes/Redeem?token={emblem.Code}): **{emblem.Code}**\n";
                }

                var count = universalEmblems.Count;
                var pageNumStr = $"Showing {(10 * index) + 1}-{((10 * index) + 10 > count ? count : (10 * index) + 10)} emblems of {count} total.";
                embed.Description +=
                    $"*Click the name to redeem.*\n{(!String.IsNullOrEmpty(bungieName) && emblemsUserMissing.Any() ? $"{EmoteConfig.GetEmote("Warning")} - Your linked account ({bungieName}) is missing this emblem.\n" : "")}\n{pageNumStr}";


                return new PageBuilder()
                    .WithAuthor(embed.Author)
                    .WithDescription(embed.Description)
                    .WithFields(embed.Fields)
                    .WithFooter(embed.Footer)
                    .WithColor((Discord.Color)embed.Color);
            }
        }

        [Group("guardian", "Display Guardian information.")]
        public class Guardians : InteractionModuleBase<ShardedInteractionContext>
        {
            private readonly InteractiveService Interactive;
            private readonly LoggingService Logging;
            private readonly UserService Users;
            private readonly GuildService Guilds;

            public Guardians(LoggingService xpLoggingService, InteractiveService interactive, UserService users, GuildService guilds)
            {
                Logging = xpLoggingService;
                Interactive = interactive;
                Users = users;
                Guilds = guilds;
            }

            [SlashCommand("linked-user", "Get Guardian information of a Linked User.")]
            public async Task LinkedUser([Summary("user", "User to get Guardian information for.")] IUser User,
                [Summary("class", "Guardian Class to get information for."), Choice("Titan", 0), Choice("Hunter", 1), Choice("Warlock", 2)] int ClassType,
                [Summary("platform", "Only needed if the user does not have Cross Save activated. This will be ignored otherwise."),
                Choice("Xbox", 1), Choice("PSN", 2), Choice("Steam", 3), Choice("Stadia", 5), Choice("Epic Games", 6)]int ArgPlatform = 0)
            {
                await DeferAsync();

                if (!Users.IsExistingUser(User.Id))
                {
                    var embed = Embeds.GetErrorEmbed();
                    embed.Description = $"User is not linked; tell them to link using '/link' or use the Bungie tag variant of this command.";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); });
                    return;
                }

                var linkedUser = Users.GetUser(User.Id);
                var Platform = (Guardian.Platform)ArgPlatform;

                var content = await BungieApi.Query($"/Platform/Destiny2/{linkedUser.BungieCredentials.MembershipType}/Profile/{linkedUser.BungieCredentials.MembershipId}?components=100,200");
                dynamic item = JsonConvert.DeserializeObject(content);

                if (item.ErrorStatus.Equals("Success"))
                {
                    var embed = Embeds.GetErrorEmbed();
                    embed.Description = $"Bungie API is temporary down, try again later.";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); });
                    return;
                }

                if (item.ErrorCode != 1)
                {
                    var embed = Embeds.GetErrorEmbed();
                    embed.Description = $"An error occured with that account. Is there a connected Destiny 2 account?";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); });
                    return;
                }

                List<Guardian> userGuardians = new();
                for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                {
                    try
                    {
                        string charId = $"{item.Response.profile.data.characterIds[i]}";
                        if ((int)item.Response.characters.data[$"{charId}"].classType == ClassType)
                            userGuardians.Add(new Guardian(linkedUser.BungieCredentials.BungieName, linkedUser.BungieCredentials.MembershipId, linkedUser.BungieCredentials.MembershipType, charId, linkedUser));
                    }
                    catch (Exception x)
                    {
                        var embed = Embeds.GetErrorEmbed();
                        embed.Description = $"`{x}`";
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); });
                        Log.Debug($"{x}");
                        return;
                    }
                }

                if (userGuardians.Count == 0)
                {
                    var embed = Embeds.GetErrorEmbed();
                    embed.Description = $"No guardians found for user.";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); });
                    return;
                }

                List<Embed> embeds = new();
                foreach (var guardian in userGuardians)
                    embeds.Add(guardian.GetGuardianEmbed().Build());

                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embeds = embeds.ToArray(); });
            }

            [SlashCommand("bungie-tag", "Get Guardian information of any player.")]
            public async Task BungieTag([Summary("player", "Player's Bungie tag to get Guardian information for."), Autocomplete(typeof(BungieTagAutocomplete))] string BungieTag,
                [Summary("class", "Guardian Class to get information for."), Choice("Titan", 0), Choice("Hunter", 1), Choice("Warlock", 2)] int ClassType,
                [Summary("platform", "Only needed if the user does not have Cross Save activated. This will be ignored otherwise."),
                Choice("Xbox", 1), Choice("PSN", 2), Choice("Steam", 3), Choice("Stadia", 5), Choice("Epic Games", 6)]int ArgPlatform = 0)
            {
                Guardian.Platform Platform = (Guardian.Platform)ArgPlatform;

                await DeferAsync();

                string MembershipType = null;
                string MembershipID = null;

                var content = await BungieApi.Query($"/Platform/Destiny2/SearchDestinyPlayer/-1/{Uri.EscapeDataString(BungieTag)}");
                dynamic item = JsonConvert.DeserializeObject(content);

                for (int i = 0; i < item.Response.Count; i++)
                {
                    string memId = item.Response[i].membershipId;
                    string memType = item.Response[i].membershipType;

                    var memContent = await BungieApi.Query("/platform/Destiny2/" + memType + "/Profile/" + memId + "/?components=100");
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

                if (MembershipType == null || MembershipID == null)
                {
                    var embed = Embeds.GetErrorEmbed();
                    embed.Description = $"An error occurred when retrieving that player's Guardians. Run the command again and specify their platform.";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); });
                    return;
                }

                content = await BungieApi.Query($"/Platform/Destiny2/{MembershipType}/Profile/{MembershipID}?components=100,200");
                item = JsonConvert.DeserializeObject(content);

                if (item.ErrorStatus.Equals("Success"))
                {
                    var embed = Embeds.GetErrorEmbed();
                    embed.Description = $"Bungie API is temporary down, try again later.";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); });
                    return;
                }

                if (item.ErrorCode != 1)
                {
                    var embed = Embeds.GetErrorEmbed();
                    embed.Description = $"An error occured with that account. Is there a connected Destiny 2 account?";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); });
                    return;
                }

                List<Guardian> userGuardians = [];
                for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                {
                    try
                    {
                        string charId = $"{item.Response.profile.data.characterIds[i]}";
                        if ((int)item.Response.characters.data[$"{charId}"].classType == ClassType)
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

                List<Embed> embeds = [];
                foreach (var guardian in userGuardians)
                    embeds.Add(guardian.GetGuardianEmbed().Build());

                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embeds = embeds.ToArray(); });
            }
        }

        [RequireBungieOauth]
        [SlashCommand("level", "Gets your Destiny 2 Season Pass Rank.")]
        public async Task GetLevel([Summary("user", "User you want the Season Pass rank of. Leave empty for your own.")] IUser User = null)
        {
            User ??= Context.User;

            if (!Users.IsExistingUser(User.Id))
            {
                await RespondAsync($"No account linked for {User.Mention}.", ephemeral: true);
                return;
            }
            await DeferAsync();
            try
            {
                var dil = Users.GetUser(User.Id);
                int Level = 0, ExtraLevel = 0, XpProgress = 0, XpProgressCap = 0, OverflowXpProgressCap = 0, LevelCap = ManifestHelper.CurrentLevelCap;
                
                dynamic item;
                var content = await BungieApi.Query($"/Platform/Destiny2/{dil.BungieCredentials.MembershipType}/Profile/{dil.BungieCredentials.MembershipId}/?components=100,104,202", dil.BungieCredentials.AccessToken);
                item = JsonConvert.DeserializeObject(content);

                if (item.Response.profile.privacy == 2)
                {
                    await RespondAsync($"{User.Mention} has their Destiny 2 stats on Private. Let's respect that.", ephemeral: true);
                    return;
                }

                XpProgressCap = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.BaseRanks}"].nextLevelAt;
                OverflowXpProgressCap = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.ExtraRanks}"].nextLevelAt;

                if (item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.BaseRanks}"].level == LevelCap)
                {
                    Level = LevelCap;
                    ExtraLevel = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.ExtraRanks}"].level;
                    XpProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.ExtraRanks}"].progressToNextLevel;
                }
                else
                {
                    Level = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.BaseRanks}"].level;
                    XpProgress = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{AppConfig.Hashes.BaseRanks}"].progressToNextLevel;
                }

                var auth = new EmbedAuthorBuilder()
                {
                    Name = $"Season {ManifestHelper.CurrentSeason.SeasonNumber}: {ManifestHelper.CurrentSeason.DisplayProperties.Name} Level and XP Info",
                    IconUrl = User.GetAvatarUrl(),
                };
                var foot = new EmbedFooterBuilder()
                {
                    Text = $"Powered by the Bungie API"
                };
                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                    Author = auth,
                    Footer = foot,
                    Description =
                        $"Player: **{dil.BungieCredentials.BungieName}**\n" +
                        $"Level: **{Level}**{(ExtraLevel > 0 ? $" (+**{ExtraLevel}**)" : "")}\n" +
                        $"Progress to Next Level: **{XpProgress:n0}/{(Level >= LevelCap ? OverflowXpProgressCap : XpProgressCap):n0}**",
                };

                int powerBonus = item.Response.profileProgression.data.seasonalArtifact.powerBonus;
                int totalXP = item.Response.profileProgression.data.seasonalArtifact.pointProgression.currentProgress;
                int dailyProgress = item.Response.profileProgression.data.seasonalArtifact.pointProgression.dailyProgress;
                int weeklyProgress = item.Response.profileProgression.data.seasonalArtifact.pointProgression.weeklyProgress;
                int progressToNextPowerLevel = item.Response.profileProgression.data.seasonalArtifact.pointProgression.progressToNextLevel;
                int nextPowerLevelAt = item.Response.profileProgression.data.seasonalArtifact.pointProgression.nextLevelAt;

                //int xpForNextBoost = GetXPForBoost(powerBonus + 1);
                int xpNeeded = nextPowerLevelAt - progressToNextPowerLevel;
                var projectedSeasonRank = 0.00;

                // Check if we're below the level cap in case we need to do different calculations for Power Bonus projection.
                if (Level < LevelCap)
                {
                    var xpToCap = ((LevelCap - Level) * XpProgressCap) - XpProgress;
                    if (xpToCap < xpNeeded)
                    {
                        var overflowXpNeeded = xpNeeded - xpToCap;
                        var extraLevelsNeeded = overflowXpNeeded / OverflowXpProgressCap;

                        projectedSeasonRank = Level + (xpToCap / XpProgressCap) + extraLevelsNeeded;

                        var remainder = xpToCap % XpProgressCap;
                        if (remainder + XpProgress >= XpProgressCap)
                            projectedSeasonRank += 1;
                    }
                    else
                    {
                        var seasonRanksNeeded = (double)(xpNeeded - XpProgress) / XpProgressCap;
                        projectedSeasonRank = Level + seasonRanksNeeded;
                    }
                }
                else
                {
                    var seasonRanksNeeded = (double)(xpNeeded - XpProgress) / OverflowXpProgressCap;
                    projectedSeasonRank = Level + ExtraLevel + seasonRanksNeeded;
                }

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
                    x.Value = $"Progress: {progressToNextPowerLevel:n0}/{nextPowerLevelAt:n0} XP\n" +
                        $"Next Level (+{powerBonus + 1}): {xpNeeded:n0} XP (At Rank: {projectedSeasonRank:0.00})";
                    x.IsInline = true;
                });

                await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); });
            }
            catch
            {
                await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = "An error has occurred, please try again later."; });
            }
        }

        private static int GetXPForBoost(int boostLevel)
        {
            var xp = 0;
            for (int i = 0; i <= boostLevel; i++)
            {
                xp += GetXPForLevel(i);
            }
            return xp;
        }

        private static int GetXPForLevel(int level)
        {
            if (level == 0)
            {
                return 0;
            }
            else
            {
                if (level > 20)
                {
                    level = 20;
                }
                return 55_000 * (level / 2) + GetXPForLevel(level - 1);
            }
        }

        [RequireBungieOauth]
        [SlashCommand("materials", "Gets your Destiny 2 material/currency counts.")]
        public async Task Materials([Summary("hide", "Hide this post from users except yourself. Default: false")] bool hide = false)
        {
            var User = Context.User;

            await DeferAsync(ephemeral: hide);
            var dil = Users.GetUser(User.Id);
            if (dil == null)
            {
                var errEmbed = Embeds.GetErrorEmbed();
                errEmbed.Description = $"Unable to pull user data. I may have lost access to their information, likely, they'll have to link again.";
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = errEmbed.Build(); });
                return;
            }

            int Glimmer = 0, LegendaryShards = 0, UpgradeModules = 0, EnhancementCores = 0, EnhancementPrisms = 0, AscendantShards = 0,
                SpoilsOfConquest = 0, RaidBanners = 0, BrightDust = 0, ResonantAlloy = 0, HarmonicAlloy = 0, AscendantAlloy = 0,
                StrangeCoins = 0, TreasureKeys = 0, ParaversalHauls = 0, TinctureOfQueensfoil = 0, PhantasmalFragments = 0, HerealwaysPieces = 0, StrandMeditations = 0,
                TerminalOverloadKeys = 0;

            // <Hash, Amount>
            var seasonalMats = new Dictionary<long, int>();

            var content = await BungieApi.Query($"/Platform/Destiny2/{dil.BungieCredentials.MembershipType}/Profile/{dil.BungieCredentials.MembershipId}/?components=100,102,103,201,1200", dil.BungieCredentials.AccessToken);
            dynamic item = JsonConvert.DeserializeObject(content);

            for (int i = 0; i < item.Response.profileInventory.data.items.Count; i++)
            {
                long hash = item.Response.profileInventory.data.items[i].itemHash;
                if (AppConfig.SeasonalCurrencyHashes.ContainsKey(hash))
                {
                    if (seasonalMats.ContainsKey(hash))
                        seasonalMats[hash] += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}");
                    else
                        seasonalMats.Add(hash, int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"));

                    continue;
                }

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

                    // Harmonic Alloy
                    case 2708128607: HarmonicAlloy += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                    // Ascendant Alloy
                    case 353704689: AscendantAlloy += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                    // Raid Banners
                    case 3282419336: RaidBanners += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                    // Tincture Of Queensfoil
                    case 2367713531: TinctureOfQueensfoil += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                    // Strange Coins
                    case 800069450: StrangeCoins += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                    // Treasure Keys
                    case 616392721: TreasureKeys += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                    // Paraversal Hauls
                    case 1116049802: ParaversalHauls += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                    // Phantasmal Fragments
                    case 443031982: PhantasmalFragments += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                    // Herealways Pieces
                    case 2993288448: HerealwaysPieces += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                    // Strand Meditations
                    case 1289622079: StrandMeditations += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;

                    // Terminal Overload Keys
                    case 1471199156: TerminalOverloadKeys += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}"); break;
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

                        // Harmonic Alloy
                        case 2708128607: HarmonicAlloy += int.Parse($"{item.Response.characterInventories.data[$"{charId}"].items[j].quantity}"); break;

                        // Ascendant Alloy
                        case 353704689: AscendantAlloy += int.Parse($"{item.Response.characterInventories.data[$"{charId}"].items[j].quantity}"); break;

                        // Strange Coins
                        case 800069450: StrangeCoins += int.Parse($"{item.Response.characterInventories.data[$"{charId}"].items[j].quantity}"); break;

                        // Terminal Overload Keys
                        case 1471199156: TerminalOverloadKeys += int.Parse($"{item.Response.characterInventories.data[$"{charId}"].items[j].quantity}"); break;
                    }
                }
            }

            Glimmer += int.Parse($"{item.Response.profileCurrencies.data.items[0].quantity}");
            LegendaryShards += int.Parse($"{item.Response.profileCurrencies.data.items[1].quantity}");
            BrightDust += int.Parse($"{item.Response.profileCurrencies.data.items[2].quantity}");

            var auth = new EmbedAuthorBuilder()
            {
                Name = $"{dil.BungieCredentials.BungieName} Material and Currency Count",
                IconUrl = User.GetAvatarUrl(),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by the Bungie API"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                Author = auth,
                Footer = foot,
            };

            embed.AddField(x =>
            {
                x.Name = "Currency";
                x.Value = $"{EmoteConfig.GetEmote("Glimmer")} {Glimmer:n0}\n" +
                    $"{EmoteConfig.GetEmote("LegendaryShards")} {LegendaryShards:n0}\n" +
                    $"{EmoteConfig.GetEmote("BrightDust")} {BrightDust:n0}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Masterwork";
                x.Value = $"{EmoteConfig.GetEmote("EnhancementCore")} {EnhancementCores:n0}\n" +
                    $"{EmoteConfig.GetEmote("EnhancementPrism")} {EnhancementPrisms:n0}\n" +
                    $"{EmoteConfig.GetEmote("AscendantShard")} {AscendantShards:n0}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Crafting";
                x.Value = $"{EmoteConfig.GetEmote("ResonantAlloy")} {ResonantAlloy:n0}\n" +
                    $"{EmoteConfig.GetEmote("HarmonicAlloy")} {HarmonicAlloy:n0}\n" +
                    $"{EmoteConfig.GetEmote("AscendantAlloy")} {AscendantAlloy:n0}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Campaign";
                x.Value = $"{EmoteConfig.GetEmote("TinctureOfQueensfoil")} {TinctureOfQueensfoil:n0}\n" +
                    $"{EmoteConfig.GetEmote("PhantasmalFragments")} {PhantasmalFragments:n0}\n" +
                    $"{EmoteConfig.GetEmote("HerealwaysPieces")} {HerealwaysPieces:n0}\n" +
                    $"{EmoteConfig.GetEmote("StrandMeditations")} {StrandMeditations:n0}\n" +
                    $"{EmoteConfig.GetEmote("TerminalOverloadKey")} {TerminalOverloadKeys:n0}\n";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Miscellaneous";
                x.Value = $"{EmoteConfig.GetEmote("UpgradeModule")} {UpgradeModules:n0}\n" +
                    $"{EmoteConfig.GetEmote("SpoilsOfConquest")} {SpoilsOfConquest:n0}\n" +
                    $"{EmoteConfig.GetEmote("RaidBanners")} {RaidBanners:n0}\n" +
                    $"{EmoteConfig.GetEmote("StrangeCoins")} {StrangeCoins:n0}\n" +
                    $"{EmoteConfig.GetEmote("ParaversalHauls")} {ParaversalHauls:n0}\n" +
                    $"{EmoteConfig.GetEmote("TreasureKeys")} {TreasureKeys:n0}";
                x.IsInline = true;
            });


            if (seasonalMats.Count > 0)
            {
                string result = "";
                foreach (var seasonalMat in seasonalMats)
                {
                    var formattedMat = AppConfig.SeasonalCurrencyHashes[seasonalMat.Key].Replace(" ", "").Replace("-", "").Replace("'", "");
                    if (!EmoteConfig.HasEmote(formattedMat))
                    {
                        content = await BungieApi.Query($"/Platform/Destiny2/Manifest/DestinyInventoryItemDefinition/{seasonalMat.Key}");
                        item = JsonConvert.DeserializeObject(content);
                        var byteArray = new HttpClient().GetByteArrayAsync($"https://bungie.net{item.Response.displayProperties.icon}").Result;
                        Task.Run(() => EmoteConfig.AddEmote(formattedMat, new Discord.Image(new MemoryStream(byteArray)))).Wait();
                    }
                    result += $"{EmoteConfig.GetEmote(formattedMat)} {seasonalMat.Value:n0}\n";
                }

                embed.AddField(x =>
                {
                    x.Name = "Seasonal";
                    x.Value = result;
                    x.IsInline = true;
                });
            }

            string engramCounts = "";
            foreach (var engram in AppConfig.EngramHashes)
            {
                engramCounts += $"{engram.Value} {item.Response.profileStringVariables.data.integerValuesByHash[$"{engram.Key}"]}\n";
            }

            if (!String.IsNullOrEmpty(engramCounts))
            {
                embed.AddField(x =>
                {
                    x.Name = "Vendor Engrams";
                    x.Value = engramCounts;
                    x.IsInline = true;
                });
            }

            await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); }); 
        }

        [SlashCommand("rarest-emblems", "Get your profile's rarest emblems.")]
        public async Task RarestEmblems([Summary("player", "Player's Bungie tag to get Guardian information for."), Autocomplete(typeof(BungieTagAutocomplete))] string BungieTag = "", [Summary("platform", "Only needed if the user does not have Cross Save activated. This will be ignored otherwise."),
                Choice("Xbox", 1), Choice("PSN", 2), Choice("Steam", 3), Choice("Stadia", 5), Choice("Epic Games", 6)]int ArgPlatform = 0,
            [Summary("count", "Number of rarest emblems to fetch. Default: 7. Range: 1-50.")] int count = 7,
            [Summary("hide", "Hide this post from users except yourself. Default: false")] bool hide = false)
        {
            await DeferAsync(ephemeral: hide);

            Guardian.Platform Platform = (Guardian.Platform)ArgPlatform;
            count = count switch
            {
                < 1 => 1,
                > 50 => 50,
                _ => count,
            };

            string MembershipType = null;
            string MembershipID = null;
            string authToken = null;
            if (!String.IsNullOrEmpty(BungieTag))
            {
                var bTagContent = await BungieApi.Query($"/Platform/Destiny2/SearchDestinyPlayer/-1/{Uri.EscapeDataString(BungieTag)}");
                dynamic bTagItem = JsonConvert.DeserializeObject(bTagContent);

                for (int i = 0; i < bTagItem.Response.Count; i++)
                {
                    string memId = bTagItem.Response[i].membershipId;
                    string memType = bTagItem.Response[i].membershipType;

                    var memContent = await BungieApi.Query($"/platform/Destiny2/{memType}/Profile/{memId}/?components=100");
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

                if (MembershipType == null || MembershipID == null)
                {
                    var errorEmbed = Embeds.GetErrorEmbed();
                    errorEmbed.Description = $"An error occurred when retrieving that player's Guardians. Run the command again and specify their platform.";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = errorEmbed.Build(); });
                    return;
                }
            }
            else
            {
                if (!Users.IsExistingUser(Context.User.Id))
                {
                    var errorEmbed = Embeds.GetErrorEmbed();
                    errorEmbed.Description = $"You don't have a Destiny 2 account linked, I need this information to get your data from Bungie's API. Get started with the `/link` command.";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = errorEmbed.Build(); });
                    return;
                }

                var dil = Users.GetUser(Context.User.Id);
                if (dil == null)
                {
                    var errorEmbed = Embeds.GetErrorEmbed();
                    errorEmbed.Description = $"There was an error with your linked account. Try relinking with the `/link` command.";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = errorEmbed.Build(); });
                    return;
                }

                MembershipID = dil.BungieCredentials.MembershipId;
                MembershipType = dil.BungieCredentials.MembershipType;
                authToken = dil.BungieCredentials.AccessToken;
                BungieTag = dil.BungieCredentials.BungieName;
            }
            var content = await BungieApi.Query($"/Platform/Destiny2/{MembershipType}/Profile/{MembershipID}/?components=800", authToken);
            dynamic item = JsonConvert.DeserializeObject(content);

            var collectibles = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(item.Response.profileCollectibles.data.collectibles.ToString()) as Dictionary<string, dynamic>;
            var filtered = collectibles.Where(x => ManifestHelper.EmblemsCollectible.ContainsValue(uint.Parse(x.Key)) && !((DestinyCollectibleState)x.Value.state)
                .HasFlag(DestinyCollectibleState.NotAcquired))
                .Select(x => long.Parse(x.Key));

            var acquisition = new EmblemReport(filtered, count);
            // In case we are given data that is less than what we asked for.
            count = acquisition.Data.Count;

            var paginator = new LazyPaginatorBuilder()
                .AddUser(Context.User)
                .WithStartPageIndex(0)
                .WithPageFactory(GeneratePage)
                .WithMaxPageIndex((int)Math.Ceiling(count / 10.0) - 1)
                .AddOption(new Emoji("◀"), PaginatorAction.Backward)
                .AddOption(new Emoji("🔢"), PaginatorAction.Jump)
                .AddOption(new Emoji("▶"), PaginatorAction.Forward)
                .AddOption(new Emoji("🛑"), PaginatorAction.Exit)
                .WithActionOnCancellation(ActionOnStop.DeleteInput)
                .WithActionOnTimeout(ActionOnStop.DeleteInput)
                .WithFooter(PaginatorFooter.None)
                .Build();

            await Interactive.SendPaginatorAsync(paginator, Context.Interaction, TimeSpan.FromSeconds(AppConfig.Discord.DurationToWaitForPaginator), ephemeral: hide, responseType: InteractionResponseType.DeferredChannelMessageWithSource);

            async Task<PageBuilder>  GeneratePage(int index)
            {
                string embedDesc = "";
                var startIndex = index * 10;
                var endIndex = ((10 * index) + 10 > count ? count : (10 * index) + 10);

                for (int i = startIndex; i < endIndex; i++)
                {
                    var emblem = acquisition.Data[i];
                    var invHash = ManifestHelper.EmblemsCollectible.FirstOrDefault(x => x.Value == emblem.CollectibleHash).Key;
                    var emblemName = ManifestHelper.Emblems[ManifestHelper.EmblemsCollectible.FirstOrDefault(x => x.Value == emblem.CollectibleHash).Key];
                    var formattedName = emblemName.Replace(" ", "").Replace("-", "").Replace("'", "");

                    var emote = EmoteConfig.GetEmote(formattedName);
                    if (EmoteConfig.HasEmote(formattedName))
                        emote = $"{EmoteConfig.GetEmote(formattedName)}";
                    else
                    {
                        var emblemDetails = new Emblem(invHash);
                        var byteArray = new HttpClient().GetByteArrayAsync($"{emblemDetails.GetIconUrl()}").Result;

                        emote = await EmoteConfig.AddEmote(formattedName, new Discord.Image(new MemoryStream(byteArray)));
                    }

                    embedDesc += $"> {(count > 10 ? $"{i + 1}" : $"{i + 1,2}")}. `{emblem.Acquisition,7}` - {emote} [{emblemName}](https://emblem.report/{invHash}) ({emblem.Percentage}%)\n";
                }

                var auth = new EmbedAuthorBuilder()
                {
                    Name = $"{BungieTag} Rarest Emblems",
                    Url = $"https://emblem.report/p/{MembershipType}/{MembershipID}",
                };
                var foot = new EmbedFooterBuilder()
                {
                    Text = $"Powered by emblem.report | Showing {startIndex + 1}-{endIndex} of {count}"
                };
                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                    Author = auth,
                    Footer = foot,
                    Description = embedDesc,
                };

                return new PageBuilder()
                    .WithAuthor(embed.Author)
                    .WithDescription(embed.Description)
                    .WithFields(embed.Fields)
                    .WithFooter(embed.Footer)
                    .WithColor((Discord.Color)embed.Color);
            }
            
        }

        [SlashCommand("seasonals", "View the current season's challenges, even ones not available yet.")]
        public async Task Seasonals([Summary("week", "Start at a specified week. Numbers outside of the bounds will default accordingly.")] int week = 1,
            [Summary("hide", "Hide this post from users except yourself. Default: false")] bool hide = false)
        {
            if (week > ManifestHelper.SeasonalChallenges.Count)
                week = ManifestHelper.SeasonalChallenges.Count;
            else if (week < 1)
                week = 1;

            var User = Context.User;
            var dil = Users.GetUser(Context.User.Id);

            bool showProgress = dil != null;

            await DeferAsync(ephemeral: hide);
            using (var client = new HttpClient())
            {
                dynamic item = "";
                string charId = "";

                if (showProgress)
                {
                    var content = await BungieApi.Query($"/Platform/Destiny2/{dil.BungieCredentials.MembershipType}/Profile/{dil.BungieCredentials.MembershipId}/?components=100,900,1200", dil.BungieCredentials.AccessToken);
                    item = JsonConvert.DeserializeObject(content);
                    charId = $"{item.Response.profile.data.characterIds[0]}";
                }

                var paginator = new LazyPaginatorBuilder()
                    .AddUser(Context.User)
                    .WithStartPageIndex(week - 1)
                    .WithPageFactory(GeneratePage)
                    .WithMaxPageIndex(ManifestHelper.SeasonalChallenges.Count - 1)
                    .AddOption(new Emoji("◀"), PaginatorAction.Backward)
                    .AddOption(new Emoji("🔢"), PaginatorAction.Jump)
                    .AddOption(new Emoji("▶"), PaginatorAction.Forward)
                    .AddOption(new Emoji("🛑"), PaginatorAction.Exit)
                    .WithActionOnCancellation(ActionOnStop.DeleteInput)
                    .WithActionOnTimeout(ActionOnStop.DeleteInput)
                    .WithFooter(PaginatorFooter.None)
                    .Build();

                await Interactive.SendPaginatorAsync(paginator, Context.Interaction, TimeSpan.FromSeconds(AppConfig.Discord.DurationToWaitForPaginator), responseType: InteractionResponseType.DeferredChannelMessageWithSource);

                PageBuilder GeneratePage(int index)
                {
                    var auth = new EmbedAuthorBuilder()
                    {
                        Name = $"{ManifestHelper.CurrentSeason.DisplayProperties.Name} Seasonal Challenges Week {index + 1} of {ManifestHelper.SeasonalChallenges.Count}",
                        IconUrl = $"{BungieApi.API_BASE_URL}{ManifestHelper.CurrentSeason.DisplayProperties.Icon}",
                    };

                var embed = new EmbedBuilder()
                {
                    Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                    Author = auth,
                };

                int classifiedCount = 0;
                foreach (var challenges in ManifestHelper.SeasonalChallenges[index])
                {
                    if (challenges.Value.Redacted)
                    {
                        classifiedCount++;
                        continue;
                    }

                        string progress = "";
                        if (showProgress)
                        {
                            for (int i = 0; i < item.Response.characterRecords.data[charId].records[$"{challenges.Key}"].objectives.Count; i++)
                            {
                                long hash = (long)item.Response.characterRecords.data[charId].records[$"{challenges.Key}"].objectives[i].objectiveHash;
                                int progressValue = item.Response.characterRecords.data[charId].records[$"{challenges.Key}"].objectives[i].progress;
                                int completionValue = item.Response.characterRecords.data[charId].records[$"{challenges.Key}"].objectives[i].completionValue;
                                if (!ManifestHelper.SeasonalObjectives.ContainsKey(hash)) continue;
                                progress += $"> {ManifestHelper.SeasonalObjectives[hash]}:" +
                                    $" {progressValue}/{completionValue} {(progressValue >= completionValue ? EmoteConfig.GetEmote("Yes") : "")}\n";
                            }
                        }

                        embed.AddField(x =>
                        {
                            x.Name = challenges.Value.DisplayProperties.Name;
                            x.Value = $"{DestinyEmote.ParseBungieText(DestinyEmote.ParseBungieVars(challenges.Value.DisplayProperties.Description.Split('\n').FirstOrDefault()))}\n{progress}";
                            x.IsInline = false;
                        });
                    }

                    var foot = new EmbedFooterBuilder()
                    {
                        Text = $"Powered by the Bungie API | Week {index + 1}/{ManifestHelper.SeasonalChallenges.Count}{(classifiedCount > 0 ? $" | Classified Records ({classifiedCount}/{ManifestHelper.SeasonalChallenges[index].Count}) are Hidden" : "")}"
                    };
                    embed.WithFooter(foot);

                    return new PageBuilder()
                        .WithAuthor(embed.Author)
                        .WithDescription(embed.Description)
                        .WithFields(embed.Fields)
                        .WithFooter(embed.Footer)
                        .WithColor((Discord.Color)embed.Color);
                }
            }
        }

        [SlashCommand("try-on", "Try on any emblem in the Bungie API.")]
        public async Task TryOut([Summary("emblem-name", "Emblem name of the Emblem you want to try on."), Autocomplete(typeof(EmblemAutocomplete))] string SearchQuery)
        {
            var linkedUser = Users.GetUser(Context.User.Id);

            if (!long.TryParse(SearchQuery, out long HashCode))
            {
                var errEmbed = Embeds.GetErrorEmbed();
                errEmbed.Description = "Invalid search, please try again. Make sure to choose one of the autocomplete options!";
                await RespondAsync(embed: errEmbed.Build(), ephemeral: true);
                return;
            }

            Emblem emblem;
            try
            {
                emblem = new Emblem(HashCode);
                var byteArray = new HttpClient().GetByteArrayAsync(emblem.GetBackgroundUrl()).Result;
            }
            catch (Exception)
            {
                var errEmbed = Embeds.GetErrorEmbed();
                errEmbed.Description = $"No emblem found for Hash Code: {HashCode}.";
                await RespondAsync(embed: errEmbed.Build(), ephemeral: true);
                return;
            }

            string name = linkedUser != null ? linkedUser.BungieCredentials.BungieName.Substring(0, linkedUser.BungieCredentials.BungieName.Length - 5) : Context.User.Username;

            var embed = Embeds.GetErrorEmbed();
            embed.Description =
                    $"This command is deprecated. Please use the [web version](https://winnow.oatsfx.com/#/emblem-try-on?emblem={HashCode}&name={name}) of this tool!";

            await RespondAsync(embed: embed.Build());
        }

        [Group("view", "Get details on in-game items.")]
        public class View : InteractionModuleBase<ShardedInteractionContext>
        {
            private readonly UserService Users;

            public View(UserService users)
            {
                Users = users;
            }

            [SlashCommand("consumable", "Get details on a consumable found via Bungie's API.")]
            public async Task ViewConsumable([Summary("name", "Name of the consumable you want details for."), Autocomplete(typeof(ConsumablesAutocomplete))] string SearchQuery)
            {
                if (!long.TryParse(SearchQuery, out long HashCode))
                {
                    var errEmbed = Embeds.GetErrorEmbed();
                    errEmbed.Description = $"Invalid search, please try again. Make sure to choose one of the autocomplete options!";
                    await RespondAsync($"", embed: errEmbed.Build(), ephemeral: true);
                    return;
                }

                await DeferAsync();

                Consumable consumable;
                try
                {
                    consumable = new Consumable(HashCode);
                }
                catch (Exception)
                {
                    var errEmbed = Embeds.GetErrorEmbed();
                    errEmbed.Description = $"No consumable found for Hash Code: {HashCode}.";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $""; message.Components = new ComponentBuilder().Build(); message.Embed = errEmbed.Build(); });
                    return;
                }

                if (!ManifestHelper.Consumables.ContainsKey(HashCode))
                {
                    var errEmbed = Embeds.GetErrorEmbed();
                    errEmbed.Description = $"Hash Code: {HashCode} is not a Consumable type.";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $""; message.Components = new ComponentBuilder().Build(); message.Embed = errEmbed.Build(); });
                    return;
                }

                var itemEmbed = consumable.GetEmbed();
                
                if (Users.IsExistingUser(Context.User.Id))
                {
                    var dil = Users.GetUser(Context.User.Id);
                    var content = await BungieApi.Query($"/Platform/Destiny2/{dil.BungieCredentials.MembershipType}/Profile/{dil.BungieCredentials.MembershipId}/?components=100,102,201", dil.BungieCredentials.AccessToken);
                    dynamic item = JsonConvert.DeserializeObject(content);

                    // Get the amount the user has on their account.
                    var invAmount = 0;
                    var vaultAmount = 0;
                    var postAmount = 0;
                    for (int i = 0; i < item.Response.profileInventory.data.items.Count; i++)
                    {
                        long hash = item.Response.profileInventory.data.items[i].itemHash;
                        if (hash == HashCode)
                        {
                            if (item.Response.profileInventory.data.items[i].bucketHash == 138197802)
                                vaultAmount += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}");
                            else
                                invAmount += int.Parse($"{item.Response.profileInventory.data.items[i].quantity}");
                        }
                    }

                    int numOfChars = item.Response.profile.data.characterIds.Count;
                    for (int i = 0; i < numOfChars; i++)
                    {
                        string charId = $"{item.Response.profile.data.characterIds[i]}";
                        for (int j = 0; j < item.Response.characterInventories.data[$"{charId}"].items.Count; j++)
                        {
                            long hash = item.Response.characterInventories.data[$"{charId}"].items[j].itemHash;
                            if (hash == HashCode)
                            {
                                postAmount += int.Parse($"{item.Response.characterInventories.data[$"{charId}"].items[j].quantity}");
                                break;
                            }
                        }
                    }

                    if (invAmount > 0 || vaultAmount > 0 || postAmount > 0)
                    {
                        itemEmbed.AddField(x =>
                        {
                            x.Name = "> You Have";
                            x.Value =
                                $"Inventory: **{invAmount:n0}**\n" +
                                $"Vault: **{vaultAmount:n0}**\n" +
                                $"Postmaster: **{postAmount:n0}**";
                            x.IsInline = true;
                        });
                    }
                }

                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = itemEmbed.Build(); message.Content = null; message.Components = new ComponentBuilder().Build(); });
            }

            [SlashCommand("emblem", "Get details on an emblem found via Bungie's API.")]
            public async Task ViewEmblem([Summary("name", "Name of the emblem you want details for."), Autocomplete(typeof(EmblemAutocomplete))] string SearchQuery,
                [Summary("show-wide-bg", "Show nameplate background or inventory menu background. Default: false (nameplate background)")] bool showWideBg = false)
            {
                if (!long.TryParse(SearchQuery, out long HashCode))
                {
                    var errEmbed = Embeds.GetErrorEmbed();
                    errEmbed.Description = $"Invalid search, please try again. Make sure to choose one of the autocomplete options!";
                    await RespondAsync($"", embed: errEmbed.Build(), ephemeral: true);
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

                if (emblem.GetItemType() != BungieSharper.Entities.Destiny.DestinyItemType.Emblem)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Hash Code: {HashCode} is not an Emblem type."; });
                    return;
                }

                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = emblem.GetEmbed(showWideBg).Build(); message.Content = null; message.Components = new ComponentBuilder().Build(); });
            }

            [SlashCommand("perk", "Get details on a weapon perk found via Bungie's API.")]
            public async Task ViewPerk([Summary("name", "Name of the perk you want details for."), Autocomplete(typeof(WeaponPerkAutocomplete))] string SearchQuery)
            {
                if (!long.TryParse(SearchQuery, out long HashCode))
                {
                    var errEmbed = Embeds.GetErrorEmbed();
                    errEmbed.Description = $"Invalid search, please try again. Make sure to choose one of the autocomplete options!";
                    await RespondAsync($"", embed: errEmbed.Build(), ephemeral: true);
                    return;
                }

                await DeferAsync();
                WeaponPerk perk;
                try
                {
                    perk = new WeaponPerk(HashCode);
                }
                catch (Exception)
                {
                    var errEmbed = Embeds.GetErrorEmbed();
                    errEmbed.Description = $"No perk found for Hash Code: {HashCode}.";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = errEmbed.Build(); });
                    return;
                }

                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = perk.GetEmbed().Build(); message.Content = null; message.Components = new ComponentBuilder().Build(); });
            }

            [SlashCommand("weapon", "Get details on a weapon found via Bungie's API.")]
            public async Task ViewWeapon([Summary("name", "Name of the weapon you want details for."), Autocomplete(typeof(WeaponAutocomplete))] string SearchQuery,
                [Summary("show-more-perks", "Show more perks like barrels and mags. Default: false (no perks)")] bool showMorePerks = false,
                [Summary("hide", "Hide this post from users except yourself. Default: false")] bool hide = false)
            {
                if (!long.TryParse(SearchQuery, out long HashCode))
                {
                    var errEmbed = Embeds.GetErrorEmbed();
                    errEmbed.Description = $"Invalid search, please try again. Make sure to choose one of the autocomplete options!";
                    await RespondAsync($"", embed: errEmbed.Build(), ephemeral: true);
                    return;
                }

                await DeferAsync(ephemeral: hide);
                Weapon weapon;
                try
                {
                    weapon = new Weapon(HashCode);
                }
                catch (Exception)
                {
                    var errEmbed = Embeds.GetErrorEmbed();
                    errEmbed.Description = $"No weapon found for Hash Code: {HashCode}.";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = errEmbed.Build(); });
                    return;
                }

                if (weapon.GetItemType() != BungieSharper.Entities.Destiny.DestinyItemType.Weapon)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Hash Code: {HashCode} is not a Weapon type."; });
                    return;
                }

                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = weapon.GetEmbed(showMorePerks).Build(); message.Content = null; message.Components = new ComponentBuilder().Build(); });
            }
        }
    }
}
