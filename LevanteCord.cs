using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Levante.Configs;
using Levante.Helpers;
using Levante.Leaderboards;
using Levante.Rotations;
using Levante.Util;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using IResult = Discord.Interactions.IResult;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace Levante
{
    public sealed class LevanteCord
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly InteractionService _interaction;
        private readonly IServiceProvider _services;

        // ReSharper disable once NotAccessedField.Local
        private Timer DailyResetTimer;

        public LevanteCord()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _interaction = new InteractionService(_client);
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton<InteractiveService>()
                .AddSingleton<InteractionService>()
                .BuildServiceProvider();
        }

        private static void Main()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            const string ASCIIName = @"    ______     ___      _ __       
   / ____/__  / (_)____(_) /___  __
  / /_  / _ \/ / / ___/ / __/ / / /
 / __/ /  __/ / / /__/ / /_/ /_/ / 
/_/    \___/_/_/\___/_/\__/\__, /  @OatsFX
                          /____/  @axsLeaf
            ";
            Console.WriteLine(ASCIIName);

            new LevanteCord().StartAsync().GetAwaiter().GetResult();
        }

        public async Task StartAsync()
        {
            if (!ConfigHelper.CheckAndLoadConfigFiles())
                return;

            await Task.Run(() => Console.Title = $"FelicityOne v{BotConfig.Version:0.00#}");

            if (!LeaderboardHelper.CheckAndLoadDataFiles())
                return;

            CurrentRotations.CreateJSONs();

            if (!Directory.Exists("Configs/EmblemOffers"))
                Directory.CreateDirectory("Configs/EmblemOffers");

            EmblemOffer.LoadCurrentOffers();

            Console.WriteLine($"Current Bot Version: v{BotConfig.Version:0.00#}");
            Console.WriteLine($"Current Developer Note: {BotConfig.Note}");

            Console.WriteLine(
                $"Legend/Master Lost Sector: {LostSectorRotation.GetLostSectorString(CurrentRotations.LostSector)} ({CurrentRotations.LostSectorArmorDrop})");
            Console.WriteLine();
            Console.WriteLine(
                $"Altar Weapon: {AltarsOfSorrowRotation.GetWeaponNameString(CurrentRotations.AltarWeapon)} ({CurrentRotations.AltarWeapon})");
            Console.WriteLine();
            Console.WriteLine(
                $"Last Wish Challenge: {LastWishRotation.GetEncounterString(CurrentRotations.LWChallengeEncounter)} ({LastWishRotation.GetChallengeString(CurrentRotations.LWChallengeEncounter)})");
            Console.WriteLine(
                $"Garden of Salvation Challenge: {GardenOfSalvationRotation.GetEncounterString(CurrentRotations.GoSChallengeEncounter)} ({GardenOfSalvationRotation.GetChallengeString(CurrentRotations.GoSChallengeEncounter)})");
            Console.WriteLine(
                $"Deep Stone Crypt Challenge: {DeepStoneCryptRotation.GetEncounterString(CurrentRotations.DSCChallengeEncounter)} ({DeepStoneCryptRotation.GetChallengeString(CurrentRotations.DSCChallengeEncounter)})");
            Console.WriteLine(
                $"Vault of Glass Challenge: {VaultOfGlassRotation.GetEncounterString(CurrentRotations.VoGChallengeEncounter)} ({VaultOfGlassRotation.GetChallengeString(CurrentRotations.VoGChallengeEncounter)})");
            Console.WriteLine();
            Console.WriteLine($"Curse Week: {CurrentRotations.CurseWeek}");
            Console.WriteLine(
                $"Ascendant Challenge: {AscendantChallengeRotation.GetChallengeNameString(CurrentRotations.AscendantChallenge)} ({AscendantChallengeRotation.GetChallengeLocationString(CurrentRotations.AscendantChallenge)})");
            Console.WriteLine();
            Console.WriteLine(
                $"Nightfall: {NightfallRotation.GetStrikeNameString(CurrentRotations.Nightfall)} ({NightfallRotation.GetWeaponString(CurrentRotations.NightfallWeaponDrops[0])}/{NightfallRotation.GetWeaponString(CurrentRotations.NightfallWeaponDrops[1])})");
            Console.WriteLine();
            Console.WriteLine($"Empire Hunt: {EmpireHuntRotation.GetHuntNameString(CurrentRotations.EmpireHunt)}");
            Console.WriteLine();
            Console.WriteLine(
                $"Nightmare Hunts: {CurrentRotations.NightmareHunts[0]}/{CurrentRotations.NightmareHunts[1]}/{CurrentRotations.NightmareHunts[2]}");
            Console.WriteLine();

            ManifestHelper.PrepManifest();

            var client = _services.GetRequiredService<DiscordSocketClient>();
            var commands = _services.GetRequiredService<InteractionService>();

            client.Log += log =>
            {
                Console.WriteLine(log.ToString());
                return Task.CompletedTask;
            };

            commands.Log += log =>
            {
                Console.WriteLine(log.ToString());
                return Task.CompletedTask;
            };

            // ReSharper disable once UnusedVariable
            var timer = new Timer(TimerCallback, null, 25000, BotConfig.TimeBetweenRefresh * 60000);

            SetUpTimer(DateTime.Now.Hour >= 19
                ? new DateTime(DateTime.Today.AddDays(1).Year, DateTime.Today.AddDays(1).Month,
                    DateTime.Today.AddDays(1).Day, 19, 0, 0)
                : new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 19, 0, 0));

            await InitializeListeners();

            await _client.LoginAsync(TokenType.Bot, BotConfig.DiscordToken);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task UpdateBotActivity(int SetRNG = -1)
        {
            int RNG;
            const int RNGMax = 10;
            if (SetRNG != -1 && SetRNG < RNGMax)
            {
                RNG = SetRNG;
            }
            else
            {
                var rand = new Random();
                RNG = rand.Next(0, RNGMax);
            }

            switch (RNG)
            {
                case 0:
                    var s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "'s" : "s'";
                    await _client.SetActivityAsync(new Game(
                        $"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers} Player{s} XP",
                        ActivityType.Watching));
                    break;
                case 1:
                    await _client.SetActivityAsync(new Game($"{BotConfig.Note} | v{BotConfig.Version:0.00#}"));
                    break;
                case 2:
                    await _client.SetActivityAsync(new Game($"for /help | v{BotConfig.Version:0.00#}",
                        ActivityType.Watching));
                    break;
                case 3:
                    await _client.SetActivityAsync(new Game(
                        $"{_client.Guilds.Count} Servers | v{BotConfig.Version:0.00#}", ActivityType.Watching));
                    break;
                case 4:
                    await _client.SetActivityAsync(new Game(
                        $"{_client.Guilds.Sum(x => x.MemberCount):n0} Users | v{BotConfig.Version:0.00#}",
                        ActivityType.Watching));
                    break;
                case 5:
                    await _client.SetActivityAsync(new Game(
                        $"{DataConfig.DiscordIDLinks.Count} Linked Users | v{BotConfig.Version:0.00#}",
                        ActivityType.Watching));
                    break;
                case 6:
                    await _client.SetActivityAsync(new Game(
                        $"{CurrentRotations.GetTotalLinks()} Rotation Trackers | v{BotConfig.Version:0.00#}",
                        ActivityType.Watching));
                    break;
                case 7:
                    await _client.SetActivityAsync(new Game($"Felicity | v{BotConfig.Version:0.00#}",
                        ActivityType.Watching));
                    break;
            }
        }

        private void SetUpTimer(DateTime alertTime)
        {
            // this is called to get the timer set up to run at every daily reset
            var timeToGo = new TimeSpan(alertTime.Ticks - DateTime.Now.Ticks);
            DailyResetTimer = new Timer(DailyResetChanges, null, (long) timeToGo.TotalMilliseconds, Timeout.Infinite);
        }

        public async void DailyResetChanges(object o = null)
        {
            Console.ForegroundColor = ConsoleColor.Green;

            if (DateTime.Today.DayOfWeek == DayOfWeek.Tuesday)
            {
                CurrentRotations.WeeklyRotation();
                Console.WriteLine("Weekly Reset Occurred.");
            }
            else
            {
                CurrentRotations.DailyRotation();
                Console.WriteLine("Daily Reset Occurred.");
            }

            // Send reset embeds if applicable.
            if (DateTime.Today.DayOfWeek == DayOfWeek.Tuesday)
                await DataConfig.PostWeeklyResetUpdate(_client);

            await DataConfig.PostDailyResetUpdate(_client);

            // Send users their tracking if applicable.
            if (DateTime.Today.DayOfWeek == DayOfWeek.Tuesday)
                await CurrentRotations.CheckUsersWeeklyTracking(_client);

            await CurrentRotations.CheckUsersDailyTracking(_client);

            // Start the next timer.
            SetUpTimer(new DateTime(DateTime.Today.AddDays(1).Year, DateTime.Today.AddDays(1).Month,
                DateTime.Today.AddDays(1).Day, 10, 0, 0));
            Console.ForegroundColor = ConsoleColor.Cyan;
        }

        private async void TimerCallback(object o)
        {
            await RefreshBungieAPI().ConfigureAwait(false);
        }

        private async Task InitializeListeners()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _interaction.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.MessageReceived += HandleMessageAsync;
            _client.InteractionCreated += HandleInteraction;

            // This tells us how to build slash commands.
            _client.Ready += async () =>
            {
                var guild = _client.GetGuild(764586645684355092);
                await guild.DeleteApplicationCommandsAsync();
                await _interaction.RegisterCommandsToGuildAsync(764586645684355092);

                // await _interaction.RegisterCommandsGloballyAsync();
                await _client.Rest.DeleteAllGlobalCommandsAsync();
                await UpdateBotActivity(1);
            };

            _interaction.SlashCommandExecuted += SlashCommandExecuted;

            _client.SelectMenuExecuted += SelectMenuHandler;
        }

        private async Task SlashCommandExecuted(SlashCommandInfo info, IInteractionContext context, IResult result)
        {
            if (!result.IsSuccess)
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                if (result.Error == InteractionCommandError.UnmetPrecondition)
                    await context.Interaction.RespondAsync(
                        "You do not have the necessary permissions to run this command.", ephemeral: true);
            await UpdateBotActivity();
        }

        private async Task SelectMenuHandler(SocketMessageComponent interaction)
        {
            var trackerType = interaction.Data.Values.First();

            switch (trackerType)
            {
                case "altars-of-sorrow":
                {
                    if (AltarsOfSorrowRotation.GetUserTracking(interaction.User.Id, out var Weapon) == null)
                    {
                        await interaction.RespondAsync("No Altars of Sorrow tracking enabled.", ephemeral: true);
                        return;
                    }

                    AltarsOfSorrowRotation.RemoveUserTracking(interaction.User.Id);
                    await interaction.RespondAsync(
                        $"Removed your Altars of Sorrow tracking, you will not be notified when {AltarsOfSorrowRotation.GetWeaponNameString(Weapon)} ({Weapon}) is available.",
                        ephemeral: true);
                    return;
                }
                case "ascendant-challenge":
                {
                    if (AscendantChallengeRotation.GetUserTracking(interaction.User.Id, out var Challenge) == null)
                    {
                        await interaction.RespondAsync("No Ascendant Challenge tracking enabled.", ephemeral: true);
                        return;
                    }

                    AscendantChallengeRotation.RemoveUserTracking(interaction.User.Id);
                    await interaction.RespondAsync(
                        $"Removed your Ascendant Challenge tracking, you will not be notified when {AscendantChallengeRotation.GetChallengeNameString(Challenge)} ({AscendantChallengeRotation.GetChallengeLocationString(Challenge)}) is available.",
                        ephemeral: true);
                    return;
                }
                case "curse-week":
                {
                    if (CurseWeekRotation.GetUserTracking(interaction.User.Id, out var Strength) == null)
                    {
                        await interaction.RespondAsync("No Curse Week tracking enabled.", ephemeral: true);
                        return;
                    }

                    CurseWeekRotation.RemoveUserTracking(interaction.User.Id);
                    await interaction.RespondAsync(
                        $"Removed your Curse Week tracking, you will not be notified when {Strength} Strength is available.",
                        ephemeral: true);
                    return;
                }
                case "dsc-challenge":
                {
                    if (DeepStoneCryptRotation.GetUserTracking(interaction.User.Id, out var DSCEncounter) == null)
                    {
                        await interaction.RespondAsync("No Deep Stone Crypt challenges tracking enabled.",
                            ephemeral: true);
                        return;
                    }

                    DeepStoneCryptRotation.RemoveUserTracking(interaction.User.Id);
                    await interaction.RespondAsync(
                        $"Removed your Deep Stone Crypt challenges tracking, you will not be notified when {DeepStoneCryptRotation.GetEncounterString(DSCEncounter)} ({DeepStoneCryptRotation.GetChallengeString(DSCEncounter)}) is available.",
                        ephemeral: true);
                    return;
                }
                case "empire-hunt":
                {
                    if (EmpireHuntRotation.GetUserTracking(interaction.User.Id, out var EmpireHunt) == null)
                    {
                        await interaction.RespondAsync("No Empire Hunt tracking enabled.", ephemeral: true);
                        return;
                    }

                    EmpireHuntRotation.RemoveUserTracking(interaction.User.Id);
                    await interaction.RespondAsync(
                        $"Removed your Empire Hunt tracking, you will not be notified when {EmpireHuntRotation.GetHuntBossString(EmpireHunt)} is available.",
                        ephemeral: true);
                    return;
                }
                case "gos-challenge":
                {
                    if (GardenOfSalvationRotation.GetUserTracking(interaction.User.Id, out var GoSEncounter) == null)
                    {
                        await interaction.RespondAsync("No Garden of Salvation challenges tracking enabled.",
                            ephemeral: true);
                        return;
                    }

                    GardenOfSalvationRotation.RemoveUserTracking(interaction.User.Id);
                    await interaction.RespondAsync(
                        $"Removed your Garden of Salvation challenges tracking, you will not be notified when {GardenOfSalvationRotation.GetEncounterString(GoSEncounter)} ({GardenOfSalvationRotation.GetChallengeString(GoSEncounter)}) is available.",
                        ephemeral: true);
                    return;
                }
                case "lw-challenge":
                {
                    if (LastWishRotation.GetUserTracking(interaction.User.Id, out var LWEncounter) == null)
                    {
                        await interaction.RespondAsync("No Last Wish challenges tracking enabled.", ephemeral: true);
                        return;
                    }

                    LastWishRotation.RemoveUserTracking(interaction.User.Id);
                    await interaction.RespondAsync(
                        $"Removed your Last Wish challenges tracking, you will not be notified when {LastWishRotation.GetEncounterString(LWEncounter)} ({LastWishRotation.GetChallengeString(LWEncounter)}) is available.",
                        ephemeral: true);
                    return;
                }
                case "lost-sector":
                {
                    if (LostSectorRotation.GetUserTracking(interaction.User.Id, out var LS, out var LSD, out var EAT) ==
                        null)
                    {
                        await interaction.RespondAsync("No Lost Sector tracking enabled.", ephemeral: true);
                        return;
                    }

                    LostSectorRotation.RemoveUserTracking(interaction.User.Id);
                    if (LS == null && LSD == null && EAT == null)
                        await interaction.RespondAsync("An error has occurred.", ephemeral: true);
                    else if (LS != null && LSD == null && EAT == null)
                        await interaction.RespondAsync(
                            $"Removed your Lost Sector tracking, you will not be notified when {LostSectorRotation.GetLostSectorString((LostSector) LS)} is available.",
                            ephemeral: true);
                    else if (LS != null && LSD != null && EAT == null)
                        await interaction.RespondAsync(
                            $"Removed your Lost Sector tracking, you will not be notified when {LostSectorRotation.GetLostSectorString((LostSector) LS)} ({LSD}) is available.",
                            ephemeral: true);
                    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                    else
                        switch (LS)
                        {
                            case null when LSD == null:
                                await interaction.RespondAsync(
                                    $"Removed your Lost Sector tracking, you will not be notified when Lost Sectors are dropping {EAT}.",
                                    ephemeral: true);
                                break;
                            case null when EAT != null:
                                await interaction.RespondAsync(
                                    $"Removed your Lost Sector tracking, you will not be notified when {LSD} Lost Sectors are dropping {EAT}.",
                                    ephemeral: true);
                                break;
                            default:
                            {
                                if (LS != null && LSD != null)
                                    await interaction.RespondAsync(
                                        $"Removed your Lost Sector tracking, you will not be notified when {LostSectorRotation.GetLostSectorString((LostSector) LS)} ({LSD}) is dropping {EAT}.",
                                        ephemeral: true);
                                break;
                            }
                        }

                    return;
                }
                case "nightfall":
                {
                    if (NightfallRotation.GetUserTracking(interaction.User.Id, out var NF, out var NFWeapon) == null)
                    {
                        await interaction.RespondAsync("No Nightfall tracking enabled.", ephemeral: true);
                        return;
                    }

                    NightfallRotation.RemoveUserTracking(interaction.User.Id);
                    if (NF == null && NFWeapon == null)
                        await interaction.RespondAsync("An error has occurred.", ephemeral: true);
                    else if (NF != null && NFWeapon == null)
                        await interaction.RespondAsync(
                            $"Removed your Nightfall tracking, you will not be notified when {NightfallRotation.GetStrikeNameString((Nightfall) NF)} is available.",
                            ephemeral: true);
                    else if (NF == null)
                        await interaction.RespondAsync(
                            $"Removed your Nightfall tracking, you will not be notified when {NightfallRotation.GetWeaponString((NightfallWeapon) NFWeapon)} is available.",
                            ephemeral: true);
                    else
                        await interaction.RespondAsync(
                            $"Removed your Nightfall tracking, you will not be notified when {NightfallRotation.GetStrikeNameString((Nightfall) NF)} is dropping {NightfallRotation.GetWeaponString((NightfallWeapon) NFWeapon)}.",
                            ephemeral: true);
                    return;
                }
                case "nightmare-hunt":
                {
                    if (NightmareHuntRotation.GetUserTracking(interaction.User.Id, out var NightmareHunt) == null)
                    {
                        await interaction.RespondAsync("No Nightmare Hunt tracking enabled.", ephemeral: true);
                        return;
                    }

                    NightmareHuntRotation.RemoveUserTracking(interaction.User.Id);
                    await interaction.RespondAsync(
                        $"Removed your Nightmare Hunt tracking, you will not be notified when {NightmareHuntRotation.GetHuntNameString(NightmareHunt)} ({NightmareHuntRotation.GetHuntBossString(NightmareHunt)}) is available.",
                        ephemeral: true);
                    return;
                }
                case "vog-challenge":
                {
                    if (VaultOfGlassRotation.GetUserTracking(interaction.User.Id, out var VoGEncounter) == null)
                    {
                        await interaction.RespondAsync("No Vault of Glass challenges tracking enabled.",
                            ephemeral: true);
                        return;
                    }

                    VaultOfGlassRotation.RemoveUserTracking(interaction.User.Id);
                    await interaction.RespondAsync(
                        $"Removed your Vault of Glass challenges tracking, you will not be notified when {VaultOfGlassRotation.GetEncounterString(VoGEncounter)} ({VaultOfGlassRotation.GetChallengeString(VoGEncounter)}) is available.",
                        ephemeral: true);
                    return;
                }
            }
        }

        private async Task HandleMessageAsync(SocketMessage arg)
        {
            if (arg.Author.IsWebhook || arg.Author.IsBot) return; // Return if message is from a Webhook or Bot user
            if (arg.Author.Id == _client.CurrentUser.Id) return; // Return of the message is from itself

            var argPos = 0;

            if (!(arg is SocketUserMessage msg)) return;

            if (msg.HasStringPrefix(BotConfig.DefaultCommandPrefix, ref argPos))
            {
                if (arg.Channel.GetType() == typeof(SocketDMChannel) &&
                    !BotConfig.BotStaffDiscordIDs.Contains(arg.Author.Id)) // Send message if received via a DM
                {
                    await arg.Channel.SendMessageAsync("I do not accept commands through Direct Messages.");
                    return;
                }

                if (BotConfig.BotStaffDiscordIDs.Contains(arg.Author.Id))
                {
                    var handled = await TryHandleCommandAsync(msg, argPos).ConfigureAwait(false);
                    // ReSharper disable once RedundantJumpStatement
                    if (handled)
                        return;
                }
                else
                {
                    var embed = new EmbedBuilder()
                        .WithDescription(
                            "All text-based commands, similar to this one, have been migrated to Slash Commands. This warning will be removed in April 2022.")
                        .WithColor(new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G,
                            BotConfig.EmbedColorGroup.B));
                    await msg.ReplyAsync(embed: embed.Build());
                }
            }
        }

        private async Task<bool> TryHandleCommandAsync(SocketUserMessage msg, int argPos)
        {
            var context = new SocketCommandContext(_client, msg);

            var result = await _commands.ExecuteAsync(context, argPos, _services);

            await UpdateBotActivity();

            if (!result.Error.HasValue) return true;

            var error = result.Error.Value;

            switch (error)
            {
                case CommandError.UnknownCommand:
                    return true;
                case CommandError.UnmetPrecondition:
                    await context.Channel.SendMessageAsync($"[{error}]: {result.ErrorReason}").ConfigureAwait(false);
                    break;
                case CommandError.BadArgCount:
                    await context.Channel.SendMessageAsync($"[{error}]: Command is missing some arguments.")
                        .ConfigureAwait(false);
                    break;
                default:
                    await context.Channel.SendMessageAsync($"[{error}]: {result.ErrorReason}").ConfigureAwait(false);
                    break;
            }

            return true;
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
                var ctx = new SocketInteractionContext(_client, arg);
                await _interaction.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (arg.Type == InteractionType.ApplicationCommand)
                    await arg.GetOriginalResponseAsync().ContinueWith(async msg => await msg.Result.DeleteAsync());
            }
        }

        #region XPLogging

        private async Task RefreshBungieAPI()
        {
            if (ActiveConfig.ActiveAFKUsers.Count <= 0)
            {
                LogHelper.ConsoleLog("Skipping refresh, no active AFK users...");
                await LoadLeaderboards();
                return;
            }

            LogHelper.ConsoleLog("Refreshing Bungie API...");

            // ReSharper disable once UnusedVariable
            var listOfRemovals = new List<ActiveConfig.ActiveAFKUser>();

            //List<ActiveConfig.ActiveAFKUser> newList = new List<ActiveConfig.ActiveAFKUser>();
            try // XP Logs
            {
                LogHelper.ConsoleLog("Refreshing XP Logging Users...");
                foreach (var aau in ActiveConfig.ActiveAFKUsers.ToList())
                {
                    var tempAau = aau;
                    var updatedLevel = DataConfig.GetAFKValues(tempAau.DiscordID, out var updatedProgression,
                        out var isPlaying, out var errorStatus);

                    if (!errorStatus.Equals("Success") && !errorStatus.Equals("PlayerNotOnline"))
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel,
                            $"Refresh unsuccessful. Reason: {errorStatus}.");
                        LogHelper.ConsoleLog(
                            $"Refresh unsuccessful for {tempAau.UniqueBungieName}. Reason: {errorStatus}.");
                        // Move onto the next user so everyone gets the message.
                        //newList.Add(tempAau);
                        continue;
                    }

                    LogHelper.ConsoleLog($"Checking {tempAau.UniqueBungieName}.");

                    if (!isPlaying)
                    {
                        var uniqueName = tempAau.UniqueBungieName;

                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel,
                            "Player is no longer playing Destiny 2.");
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel,
                            $"<@{tempAau.DiscordID}>: Logging terminated by automation. Here is your session summary:",
                            XPLoggingHelper.GenerateSessionSummary(tempAau, _client.CurrentUser.GetAvatarUrl()),
                            XPLoggingHelper.GenerateDeleteChannelButton());

                        IUser user;
                        if (_client.GetUser(tempAau.DiscordID) == null)
                        {
                            var _rClient = _client.Rest;
                            user = await _rClient.GetUserAsync(tempAau.DiscordID);
                        }
                        else
                        {
                            user = _client.GetUser(tempAau.DiscordID);
                        }

                        await LogHelper.Log(user.CreateDMChannelAsync().Result,
                            $"<@{tempAau.DiscordID}>: Player is no longer playing Destiny 2. Logging will be terminated for {uniqueName}.");
                        await LogHelper.Log(user.CreateDMChannelAsync().Result,
                            $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.TimeStarted)}.",
                            XPLoggingHelper.GenerateSessionSummary(tempAau, _client.CurrentUser.GetAvatarUrl()));

                        LogHelper.ConsoleLog($"Stopped logging for {tempAau.UniqueBungieName} via automation.");
                        //listOfRemovals.Add(tempAau);
                        ActiveConfig.DeleteActiveUserFromConfig(tempAau.DiscordID);
                        await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau));
                    }
                    else if (updatedLevel > tempAau.LastLoggedLevel)
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel,
                            $"Level up detected: {tempAau.LastLoggedLevel} -> {updatedLevel}");

                        ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID)!
                            .LastLoggedLevel = updatedLevel;
                        ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID)!
                            .LastLevelProgress = updatedProgression;
                        ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID)!
                            .NoXPGainRefreshes = 0;
                        //tempAau.LastLoggedLevel = updatedLevel;
                        //tempAau.LastLevelProgress = updatedProgression;
                        //tempAau.NoXPGainRefreshes = 0;
                        //newList.Add(tempAau);

                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel,
                            $"Start: {tempAau.StartLevel} ({tempAau.StartLevelProgress:n0}/100,000 XP). Now: {tempAau.LastLoggedLevel} ({tempAau.LastLevelProgress:n0}/100,000 XP)");
                    }
                    else if (updatedProgression <= tempAau.LastLevelProgress)
                    {
                        if (tempAau.NoXPGainRefreshes >= ActiveConfig.RefreshesBeforeKick)
                        {
                            var uniqueName = tempAau.UniqueBungieName;

                            await LogHelper.Log(
                                _client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel,
                                "Player has been determined as inactive.");
                            await LogHelper.Log(
                                _client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel,
                                $"<@{tempAau.DiscordID}>: Logging terminated by automation. Here is your session summary:",
                                XPLoggingHelper.GenerateSessionSummary(tempAau, _client.CurrentUser.GetAvatarUrl()),
                                XPLoggingHelper.GenerateDeleteChannelButton());

                            IUser user;
                            if (_client.GetUser(tempAau.DiscordID) == null)
                            {
                                var _rClient = _client.Rest;
                                user = await _rClient.GetUserAsync(tempAau.DiscordID);
                            }
                            else
                            {
                                user = _client.GetUser(tempAau.DiscordID);
                            }

                            await LogHelper.Log(user.CreateDMChannelAsync().Result,
                                $"<@{tempAau.DiscordID}>: Player has been determined as inactive. Logging will be terminated for {uniqueName}.");
                            await LogHelper.Log(user.CreateDMChannelAsync().Result,
                                $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.TimeStarted)}.",
                                XPLoggingHelper.GenerateSessionSummary(tempAau, _client.CurrentUser.GetAvatarUrl()));

                            LogHelper.ConsoleLog($"Stopped logging for {tempAau.UniqueBungieName} via automation.");
                            //listOfRemovals.Add(tempAau);
                            ActiveConfig.DeleteActiveUserFromConfig(tempAau.DiscordID);
                            await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau));
                        }
                        else
                        {
                            ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID)!
                                .NoXPGainRefreshes = tempAau.NoXPGainRefreshes + 1;
                            //tempAau.NoXPGainRefreshes = tempAau.NoXPGainRefreshes + 1;
                            //newList.Add(tempAau);
                            await LogHelper.Log(
                                _client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel,
                                $"No XP change detected, waiting for next refresh... Warning {tempAau.NoXPGainRefreshes} of {ActiveConfig.RefreshesBeforeKick}.");
                        }
                    }
                    else
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel,
                            $"Refreshed! Progress for {tempAau.UniqueBungieName} (Level: {updatedLevel}): {tempAau.LastLevelProgress:n0} XP -> {updatedProgression:n0} XP");

                        ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID)!
                            .LastLoggedLevel = updatedLevel;
                        ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID)!
                            .LastLevelProgress = updatedProgression;
                        ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID)!
                            .NoXPGainRefreshes = 0;
                        //tempAau.LastLoggedLevel = updatedLevel;
                        //tempAau.LastLevelProgress = updatedProgression;
                        //tempAau.NoXPGainRefreshes = 0;
                        //newList.Add(tempAau);
                    }

                    await Task.Delay(3250); // we don't want to spam API if we have a ton of AFK subscriptions
                }

                // Add in users that joined mid-refresh.
                /*foreach (var User in ActiveConfig.ActiveAFKUsers)
                {
                    bool isBeingRemoved = listOfRemovals.FirstOrDefault(x => x.DiscordID == User.DiscordID) != null;
                    bool isNotInNewList = newList.FirstOrDefault(x => x.DiscordID == User.DiscordID) == null;
                    if (!isBeingRemoved && isNotInNewList) // They weren't removed (prevents adding users that have been removed in this refresh), and they aren't in the newList.
                        newList.Add(User);
                }*/

                //ActiveConfig.ActiveAFKUsers = newList;
                ActiveConfig.UpdateActiveAFKUsersConfig();

                LogHelper.ConsoleLog("Bungie API Refreshed!");
            }
            catch (Exception x)
            {
                LogHelper.ConsoleLog($"Refresh failed, trying again! Reason: {x.Message} ({x.StackTrace})");
                await Task.Delay(8000);
                await RefreshBungieAPI().ConfigureAwait(false);
                return;
            }

            // data loading
            await Task.Delay(40000); // wait to prevent numerous API calls
            await LoadLeaderboards();
            await UpdateBotActivity();
        }

        private async Task LoadLeaderboards()
        {
            try
            {
                LogHelper.ConsoleLog("Pulling data for leaderboards...");
                var tempPowerLevelData = new PowerLevelData();
                var tempLevelData = new LevelData();
                foreach (var link in
                         DataConfig.DiscordIDLinks
                             .ToList()) // USE THIS FOREACH LOOP TO POPULATE FUTURE LEADERBOARDS (that use API calls)
                {
                    int Level;
                    var PowerLevel = -1;
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                        var response = client.GetAsync("https://www.bungie.net/Platform/Destiny2/" +
                                                       link.BungieMembershipType + "/Profile/" +
                                                       link.BungieMembershipID + "/?components=100,200,202").Result;
                        var content = response.Content.ReadAsStringAsync().Result;
                        dynamic item = JsonConvert.DeserializeObject(content);

                        //first 100 levels: 4095505052 (S15); 2069932355 (S16)
                        //anything after: 1531004716 (S15): 1787069365 (S16)

                        try
                        {
                            // ReSharper disable once PossibleNullReferenceException
                            for (var i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                            {
                                var charId = $"{item.Response.profile.data.characterIds[i]}";
                                var powerLevelComp = (int) item.Response.characters.data[charId].light;
                                if (PowerLevel <= powerLevelComp)
                                    PowerLevel = powerLevelComp;
                            }

                            if (item.Response.characterProgressions
                                    .data[$"{item.Response.profile.data.characterIds[0]}"].progressions["2069932355"]
                                    .level == 100)
                            {
                                int extraLevel = item.Response.characterProgressions
                                    .data[$"{item.Response.profile.data.characterIds[0]}"].progressions["1787069365"]
                                    .level;
                                Level = 100 + extraLevel;
                            }
                            else
                            {
                                Level = item.Response.characterProgressions
                                    .data[$"{item.Response.profile.data.characterIds[0]}"].progressions["2069932355"]
                                    .level;
                            }
                        }
                        catch
                        {
                            // Continue with the rest of the linked users. Don't want to stop the populating for one problematic account.
                            LogHelper.ConsoleLog(
                                $"Error while pulling data for user: {_client.GetUserAsync(link.DiscordID).Result.Username}#{_client.GetUserAsync(link.DiscordID).Result.Discriminator} linked with {link.UniqueBungieName}.");
                            await Task.Delay(250);
                            continue;
                        }
                    }

                    // Populate List
                    tempLevelData.LevelDataEntries.Add(new LevelData.LevelDataEntry
                    {
                        LastLoggedLevel = Level,
                        UniqueBungieName = link.UniqueBungieName
                    });
                    tempPowerLevelData.PowerLevelDataEntries.Add(new PowerLevelData.PowerLevelDataEntry
                    {
                        PowerLevel = PowerLevel,
                        UniqueBungieName = link.UniqueBungieName
                    });
                    await Task.Delay(250);
                }

                tempLevelData.UpdateEntriesConfig();
                tempPowerLevelData.UpdateEntriesConfig();
                LogHelper.ConsoleLog("Data pulling complete!");
            }
            catch
            {
                LogHelper.ConsoleLog("Error while updating leaderboards, trying again at next refresh.");
            }
        }

        #endregion
    }
}