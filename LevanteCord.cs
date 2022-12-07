using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using System.Linq;
using Levante.Configs;
using System.Net.Http;
using Levante.Helpers;
using System.Threading;
using Levante.Leaderboards;
using Levante.Rotations;
using Levante.Util;
using Fergun.Interactive;
using Discord.Interactions;
using APIHelper;
using Levante.Util.Attributes;
using Serilog;
using Serilog.Events;

namespace Levante
{
    public static class LevanteCordInstance
    {
        public static DiscordSocketClient Client;
    }

    public sealed class LevanteCord
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly InteractionService _interaction;
        private readonly IServiceProvider _services;

        private Timer DailyResetTimer;
        private Timer _xpTimer;
        private Timer _leaderboardTimer;
        private Timer CommunityCreationsTimer;
        private Timer CheckBungieAPI;

        public LevanteCord()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged
            };

            // TODO: Implement a sharded client.

            _client = new DiscordSocketClient(config);
            _commands = new CommandService();
            _interaction = new InteractionService(_client);
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton<InteractiveService>()
                .AddSingleton<InteractionService>()
                .BuildServiceProvider();
        }

        static void Main(string[] args)
        {
            string ASCIIName = @"
   __                      __     
  / /  ___ _  _____ ____  / /____ 
 / /__/ -_) |/ / _ `/ _ \/ __/ -_)
/____/\__/|___/\_,_/_//_/\__/\__/   dev. by @OatsFX
            ";
            Console.WriteLine(ASCIIName);
            //create the logger and setup your sinks, filters and properties
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}", theme: BotConfig.LevanteTheme)
                .WriteTo.DiscordLogSink()
                .Enrich.FromLogContext()
                .CreateLogger();

            new LevanteCord().StartAsync().GetAwaiter().GetResult();
        }

        public async Task StartAsync()
        {
            if (!ConfigHelper.CheckAndLoadConfigFiles())
                return;

            await Task.Run(() => Console.Title = $"Levante v{BotConfig.Version}");

            if (!LeaderboardHelper.CheckAndLoadDataFiles())
                return;

            CurrentRotations.CreateJSONs();

            API.FetchManifest();
            ManifestHelper.LoadManifestDictionaries();

            EmblemOffer.LoadCurrentOffers();
            Ada1Rotation.GetAda1Inventory();
            NightfallRotation.GetCurrentNightfall();
            CurrentRotations.UpdateRotationsJSON();

            //Console.ForegroundColor = ConsoleColor.Magenta;
            //Console.WriteLine($"[ROTATIONS]");
            //Console.WriteLine($"Legend/Master Lost Sector: {LostSectorRotation.GetLostSectorString(CurrentRotations.LostSector)} ({CurrentRotations.LostSectorArmorDrop})");
            //Console.WriteLine($"Altar Weapon: {AltarsOfSorrowRotation.GetWeaponNameString(CurrentRotations.AltarWeapon)} ({CurrentRotations.AltarWeapon})");
            //Console.WriteLine($"Wellspring ({WellspringRotation.GetWellspringTypeString(CurrentRotations.Wellspring)}): {WellspringRotation.GetWeaponNameString(CurrentRotations.Wellspring)} ({WellspringRotation.GetWellspringBossString(CurrentRotations.Wellspring)})");
            //Console.WriteLine($"Last Wish Challenge: {LastWishRotation.GetEncounterString(CurrentRotations.LWChallengeEncounter)} ({LastWishRotation.GetChallengeString(CurrentRotations.LWChallengeEncounter)})");
            //Console.WriteLine($"Garden of Salvation Challenge: {GardenOfSalvationRotation.GetEncounterString(CurrentRotations.GoSChallengeEncounter)} ({GardenOfSalvationRotation.GetChallengeString(CurrentRotations.GoSChallengeEncounter)})");
            //Console.WriteLine($"Deep Stone Crypt Challenge: {DeepStoneCryptRotation.GetEncounterString(CurrentRotations.DSCChallengeEncounter)} ({DeepStoneCryptRotation.GetChallengeString(CurrentRotations.DSCChallengeEncounter)})");
            //Console.WriteLine($"Vault of Glass Challenge: {VaultOfGlassRotation.GetEncounterString(CurrentRotations.VoGChallengeEncounter)} ({VaultOfGlassRotation.GetChallengeString(CurrentRotations.VoGChallengeEncounter)})");
            //Console.WriteLine($"Vow of the Disciple Challenge: {VowOfTheDiscipleRotation.GetEncounterString(CurrentRotations.VowChallengeEncounter)} ({VowOfTheDiscipleRotation.GetChallengeString(CurrentRotations.VowChallengeEncounter)})");
            //Console.WriteLine($"Curse Week: {CurrentRotations.CurseWeek}");
            //Console.WriteLine($"Ascendant Challenge: {AscendantChallengeRotation.GetChallengeNameString(CurrentRotations.AscendantChallenge)} ({AscendantChallengeRotation.GetChallengeLocationString(CurrentRotations.AscendantChallenge)})");
            //Console.WriteLine($"Nightfall: {NightfallRotation.GetStrikeNameString(CurrentRotations.Nightfall)} (dropping {NightfallRotation.GetWeaponString(CurrentRotations.NightfallWeaponDrop)})");
            //Console.WriteLine($"Empire Hunt: {EmpireHuntRotation.GetHuntNameString(CurrentRotations.EmpireHunt)}");
            //Console.WriteLine($"Nightmare Hunts: {CurrentRotations.NightmareHunts[0]}/{CurrentRotations.NightmareHunts[1]}/{CurrentRotations.NightmareHunts[2]}");
            //Console.WriteLine();

            var currentTime = DateTime.UtcNow;
            SetUpTimer(currentTime.Hour >= 17 ? new DateTime(currentTime.AddDays(1).Year, currentTime.AddDays(1).Month, currentTime.AddDays(1).Day, 17, 0, 5) : new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 17, 0, 5));

            if (!BotConfig.IsDebug)
            {
                var oauthManager = new OAuthHelper();
            }
            else
            {
                Log.Debug("[{Type}] Debugging.", "Startup");
            }

            await InitializeListeners();
            var client = _services.GetRequiredService<DiscordSocketClient>();
            var commands = _services.GetRequiredService<InteractionService>();

            client.Log += LogAsync;
            
            await _client.LoginAsync(TokenType.Bot, BotConfig.DiscordToken);
            await _client.StartAsync();

            _xpTimer = new Timer(XPTimerCallback, null, 20000, ActiveConfig.TimeBetweenRefresh * 60000);
            _leaderboardTimer = new Timer(LeaderboardTimerCallback, null, 30000, 3600000);
            Log.Information("[{Type}] Continued XP logging for {Count} (+{PriorityCount}) Users.",
                "XP Sessions", ActiveConfig.ActiveAFKUsers.Count, ActiveConfig.PriorityActiveAFKUsers.Count);

            Log.Information("[{Type}] Bot successfully started! v{@Version}",
                "Startup", BotConfig.Version);
            await Task.Delay(-1);

            await _xpTimer.DisposeAsync();
            await _leaderboardTimer.DisposeAsync();
            await DailyResetTimer.DisposeAsync();
            await CommunityCreationsTimer.DisposeAsync();
        }

        private static async Task LogAsync(LogMessage message)
        {
            var severity = message.Severity switch
            {
                LogSeverity.Critical => LogEventLevel.Fatal,
                LogSeverity.Error => LogEventLevel.Error,
                LogSeverity.Warning => LogEventLevel.Warning,
                LogSeverity.Info => LogEventLevel.Information,
                LogSeverity.Verbose => LogEventLevel.Verbose,
                LogSeverity.Debug => LogEventLevel.Debug,
                _ => LogEventLevel.Information
            };
            Log.Write(severity, message.Exception,"[{Source}] " + message.Message, message.Source);
            await Task.CompletedTask;
        }

        private async Task UpdateBotActivity(int SetRNG = -1)
        {
            int RNG = 0;
            int RNGMax = 35;
            Random rand = new();
            if (SetRNG != -1 && SetRNG < RNGMax)
                RNG = SetRNG;
            else
                RNG = rand.Next(0, RNGMax);

            switch (RNG)
            {
                case 0:
                    string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "'s" : "s'";
                    string p = ActiveConfig.PriorityActiveAFKUsers.Count != 0 ? $" (+{ActiveConfig.PriorityActiveAFKUsers.Count})" : "";
                    await _client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers}{p} User{s} XP", ActivityType.Watching)); break;
                case 1:
                    await _client.SetActivityAsync(new Game($"{BotConfig.Notes[rand.Next(0, BotConfig.Notes.Count)]} | v{BotConfig.Version}", ActivityType.Playing)); break;
                case 2:
                    await _client.SetActivityAsync(new Game($"for /help | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 3:
                    await _client.SetActivityAsync(new Game($"{_client.Guilds.Count:n0} Servers | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 4:
                    await _client.SetActivityAsync(new Game($"{_client.Guilds.Sum(x => x.MemberCount):n0} Users | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 5:
                    await _client.SetActivityAsync(new Game($"{DataConfig.DiscordIDLinks.Count} Linked Users | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 6:
                    await _client.SetActivityAsync(new Game($"{CurrentRotations.GetTotalLinks()} Rotation Trackers | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 7:
                    await _client.SetActivityAsync(new Game($"{BotConfig.Website} | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 8:
                    await _client.SetActivityAsync(new Game($"{BotConfig.Twitter} on Twitter", ActivityType.Watching)); break;
                case 9:
                    await _client.SetActivityAsync(new Game($"{EmblemOffer.CurrentOffers.Count} Available Emblems", ActivityType.Watching)); break;
                case 10:
                    await _client.SetActivityAsync(new Game($"this ratio", ActivityType.Watching)); break;
                case 11:
                    await _client.SetActivityAsync(new Game($"for /support | v{BotConfig.Version}", ActivityType.Watching)); break;
                default: break;
            }
            return;
        }

        private void SetUpTimer(DateTime alertTime)
        {
            // this is called to get the timer set up to run at every daily reset
            TimeSpan timeToGo = new(alertTime.Ticks - DateTime.UtcNow.Ticks);
            Log.Debug("Reset in: {Time}.", $"{timeToGo.Hours}:{timeToGo.Minutes}:{timeToGo.Seconds}");
            DailyResetTimer = new Timer(DailyResetChanges, null, (long)timeToGo.TotalMilliseconds, Timeout.Infinite);
        }

        public async void DailyResetChanges(Object o = null)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var devLinked = DataConfig.DiscordIDLinks.FirstOrDefault(x => x.DiscordID == BotConfig.BotDevDiscordIDs[0]);
                devLinked = DataConfig.RefreshCode(devLinked);

                var response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/" + devLinked.BungieMembershipType + "/Profile/" + devLinked.BungieMembershipID + "?components=100,200").Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                if (item.ErrorCode != 1)
                {
                    CheckBungieAPI = new Timer(DailyResetChanges, null, 60000, Timeout.Infinite);
                    Log.Warning("[{Type}] Reset Delayed. Reason: {ErrorStatus}.", "Reset", item.ErrorStatus);
                    return;
                }

                string charId = $"{item.Response.profile.data.characterIds[0]}";
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {devLinked.AccessToken}");

                response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + devLinked.BungieMembershipType + "/Profile/" + devLinked.BungieMembershipID + "/Character/" + charId + "/Vendors/350061650/?components=400,402").Result;
                content = response.Content.ReadAsStringAsync().Result;
                item = JsonConvert.DeserializeObject(content);

                // Check Vendors
                if (item.ErrorCode != 1)
                {
                    CheckBungieAPI = new Timer(DailyResetChanges, null, 60000, Timeout.Infinite);
                    Log.Warning("[{Type}] Reset Delayed. Reason: {ErrorStatus}.", "Reset", item.ErrorStatus);
                    return;
                }
            }
            try
            {
                if (DateTime.Today.DayOfWeek == DayOfWeek.Tuesday)
                {
                    CurrentRotations.WeeklyRotation();
                    Log.Information("[{Type}] Weekly Reset Occurred.", "Reset");
                }
                else
                {
                    CurrentRotations.DailyRotation();
                    Log.Information("[{Type}] Daily Reset Occurred.", "Reset");
                }
            }
            catch (Exception x)
            {
                CheckBungieAPI = new Timer(DailyResetChanges, null, 60000, Timeout.Infinite);
                Log.Warning("[{Type}] Reset Delayed. Reason: {Exception}.", "Reset", x);
                return;
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
            SetUpTimer(new DateTime(DateTime.UtcNow.AddDays(1).Year, DateTime.UtcNow.AddDays(1).Month, DateTime.UtcNow.AddDays(1).Day, 17, 0, 5));
        }

        private async void XPTimerCallback(Object o) => await RefreshBungieAPI().ConfigureAwait(false);

        private async void LeaderboardTimerCallback(Object o) => await LoadLeaderboards().ConfigureAwait(false);

        #region XPLogging
        private async Task RefreshBungieAPI()
        {
            if (ActiveConfig.ActiveAFKUsers.Count <= 0 && ActiveConfig.PriorityActiveAFKUsers.Count <= 0)
            {
                Log.Information("[{Type}] Skipping refresh, no active logging users...", "XP Sessions");
                return;
            }

            // Stop Timer
            _xpTimer.Change(Timeout.Infinite, Timeout.Infinite);
            //LogHelper.ConsoleLog($"[XP SESSIONS] Refreshing Bungie API...");
            //List<ActiveConfig.ActiveAFKUser> listOfRemovals = new List<ActiveConfig.ActiveAFKUser>();
            //List<ActiveConfig.ActiveAFKUser> newList = new List<ActiveConfig.ActiveAFKUser>();
            try // XP Logs
            {
                Log.Information("[{Type}] Refreshing XP Logging Users...", "XP Sessions");
                var combinedAFKUsers = ActiveConfig.PriorityActiveAFKUsers.Concat(ActiveConfig.ActiveAFKUsers);
                foreach (ActiveConfig.ActiveAFKUser aau in combinedAFKUsers.ToList())
                {
                    ActiveConfig.ActiveAFKUser tempAau = aau;

                    // If the user has removed themselves from logging while we are refreshing.
                    if (!(ActiveConfig.ActiveAFKUsers.Exists(x => x.DiscordChannelID == tempAau.DiscordChannelID) ||
                        ActiveConfig.PriorityActiveAFKUsers.Exists(x => x.DiscordChannelID == tempAau.DiscordChannelID)))
                        continue;

                    Log.Information("[{Type}] Checking {User}.", "XP Sessions", tempAau.UniqueBungieName);
                    var actualUser = ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordChannelID == tempAau.DiscordChannelID);
                    if (actualUser == null)
                    {
                        actualUser = ActiveConfig.PriorityActiveAFKUsers.FirstOrDefault(x => x.DiscordChannelID == tempAau.DiscordChannelID);
                    }

                    IUser user = _client.GetUser(tempAau.DiscordID);
                    if (user == null)
                    {
                        var _rClient = _client.Rest;
                        user = await _rClient.GetUserAsync(tempAau.DiscordID);
                    }

                    var logChannel = await _client.GetChannelAsync(tempAau.DiscordChannelID) as ITextChannel;
                    var dmChannel = user.CreateDMChannelAsync().Result;

                    if (logChannel == null)
                    {
                        await LogHelper.Log(dmChannel, $"<@{tempAau.DiscordID}>: Refresh unsuccessful. Reason: LoggingChannelNotFound. Logging will be terminated for {tempAau.UniqueBungieName}.");
                        await LogHelper.Log(dmChannel, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.TimeStarted)}.", XPLoggingHelper.GenerateSessionSummary(tempAau, _client.CurrentUser.GetAvatarUrl()));

                        Log.Information("[{Type}] Stopped logging for {User} via automation.", "XP Sessions", tempAau.UniqueBungieName);

                        ActiveConfig.DeleteActiveUserFromConfig(tempAau.DiscordID);
                        //ActiveConfig.ActiveAFKUsers.Remove(ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordChannelID == tempAau.DiscordChannelID));
                        await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau));
                    }

                    int updatedLevel = DataConfig.GetAFKValues(tempAau.DiscordID, out int updatedProgression, out int powerBonus, out string errorStatus);

                    if (!errorStatus.Equals("Success"))
                    {
                        if (tempAau.NoXPGainRefreshes >= ActiveConfig.RefreshesBeforeKick)
                        {
                            string uniqueName = tempAau.UniqueBungieName;

                            await LogHelper.Log(logChannel, $"Refresh unsuccessful. Reason: {errorStatus}.");
                            await LogHelper.Log(logChannel, $"<@{tempAau.DiscordID}>: Refresh unsuccessful. Reason: {errorStatus}. Here is your session summary:", XPLoggingHelper.GenerateSessionSummary(tempAau, _client.CurrentUser.GetAvatarUrl()), XPLoggingHelper.GenerateChannelButtons(tempAau.DiscordID));

                            //await LogHelper.Log(dmChannel, $"<@{tempAau.DiscordID}>: Refresh unsuccessful. Reason: {errorStatus}. Logging will be terminated for {uniqueName}.");
                            await LogHelper.Log(dmChannel, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.TimeStarted)}.", XPLoggingHelper.GenerateSessionSummary(tempAau, _client.CurrentUser.GetAvatarUrl()));

                            Log.Information("[{Type}] Stopped logging for {User} via automation.", "XP Sessions", tempAau.UniqueBungieName);
                            //listOfRemovals.Add(tempAau);
                            // ***Change to remove it from list because file update is called at end of method.***
                            ActiveConfig.DeleteActiveUserFromConfig(tempAau.DiscordID);
                            //ActiveConfig.ActiveAFKUsers.Remove(ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordChannelID == tempAau.DiscordChannelID));
                            await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau));
                        }
                        else
                        {
                            actualUser.NoXPGainRefreshes = tempAau.NoXPGainRefreshes + 1;
                            await LogHelper.Log(logChannel, $"Refresh unsuccessful. Reason: {errorStatus}. Warning {tempAau.NoXPGainRefreshes} of {ActiveConfig.RefreshesBeforeKick}.");
                            Log.Information("[{Type}] Refresh unsuccessful for {User}. Reason: {Reason}", "XP Sessions", tempAau.UniqueBungieName, errorStatus);
                            // Move onto the next user so everyone gets the message.
                            //newList.Add(tempAau);
                        }
                        continue;
                    }

                    if (powerBonus > tempAau.LastPowerBonus)
                    {
                        await LogHelper.Log(logChannel, $"Power bonus increase detected: {tempAau.LastPowerBonus} -> {powerBonus} (Start: {tempAau.StartPowerBonus}).");

                        actualUser.LastPowerBonus = powerBonus;
                    }

                    /*if (!isPlaying)
                    {
                        string uniqueName = tempAau.UniqueBungieName;

                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Player is no longer playing Destiny 2.");
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"<@{tempAau.DiscordID}>: Logging terminated by automation. Here is your session summary:", XPLoggingHelper.GenerateSessionSummary(tempAau, _client.CurrentUser.GetAvatarUrl()), XPLoggingHelper.GenerateDeleteChannelButton());

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
                        await LogHelper.Log(user.CreateDMChannelAsync().Result, $"<@{tempAau.DiscordID}>: Player is no longer playing Destiny 2. Logging will be terminated for {uniqueName}.");
                        await LogHelper.Log(user.CreateDMChannelAsync().Result, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.TimeStarted)}.", XPLoggingHelper.GenerateSessionSummary(tempAau, _client.CurrentUser.GetAvatarUrl()));

                        LogHelper.ConsoleLog($"[LOGGING] Stopped logging for {tempAau.UniqueBungieName} via automation.");
                        //listOfRemovals.Add(tempAau);
                        //ActiveConfig.DeleteActiveUserFromConfig(tempAau.DiscordID);
                        ActiveConfig.ActiveAFKUsers.Remove(ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID));
                        await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau));
                    }
                    else */
                    if (updatedLevel > tempAau.LastLevel)
                    {
                        await LogHelper.Log(logChannel, $"Level up detected: {tempAau.LastLevel} -> {updatedLevel}. " +
                            $"Start: {tempAau.StartLevel} ({String.Format("{0:n0}", tempAau.StartLevelProgress)}/100,000 XP). Now: {updatedLevel} ({String.Format("{0:n0}", updatedProgression)}/100,000 XP).");

                        actualUser.LastLevel = updatedLevel;
                        actualUser.LastLevelProgress = updatedProgression;
                        actualUser.NoXPGainRefreshes = 0;
                        //tempAau.LastLoggedLevel = updatedLevel;
                        //tempAau.LastLevelProgress = updatedProgression;
                        //tempAau.NoXPGainRefreshes = 0;
                        //newList.Add(tempAau);
                    }
                    else if (updatedProgression <= tempAau.LastLevelProgress)
                    {
                        if (tempAau.NoXPGainRefreshes >= ActiveConfig.RefreshesBeforeKick)
                        {
                            string uniqueName = tempAau.UniqueBungieName;

                            await LogHelper.Log(logChannel, $"<@{tempAau.DiscordID}>: Player has been determined as inactive. Logging terminated by automation. Here is your session summary:", XPLoggingHelper.GenerateSessionSummary(tempAau, _client.CurrentUser.GetAvatarUrl()), XPLoggingHelper.GenerateChannelButtons(tempAau.DiscordID));

                            //await LogHelper.Log(dmChannel, $"<@{tempAau.DiscordID}>: Player has been determined as inactive. Logging will be terminated for {uniqueName}.");
                            await LogHelper.Log(dmChannel, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.TimeStarted)}.", XPLoggingHelper.GenerateSessionSummary(tempAau, _client.CurrentUser.GetAvatarUrl()));

                            Log.Information("[{Type}] Stopped logging for {User} via automation.", "XP Sessions", tempAau.UniqueBungieName);
                            //listOfRemovals.Add(tempAau);
                            // ***Change to remove it from list because file update is called at end of method.***
                            ActiveConfig.DeleteActiveUserFromConfig(tempAau.DiscordID);
                            //ActiveConfig.ActiveAFKUsers.Remove(ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordChannelID == tempAau.DiscordChannelID));
                            await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau));
                        }
                        else
                        {
                            actualUser.NoXPGainRefreshes = tempAau.NoXPGainRefreshes + 1;
                            //tempAau.NoXPGainRefreshes = tempAau.NoXPGainRefreshes + 1;
                            //newList.Add(tempAau);
                            await LogHelper.Log(logChannel, $"No XP change detected, waiting for next refresh... Warning {tempAau.NoXPGainRefreshes} of {ActiveConfig.RefreshesBeforeKick}.");
                        }
                    }
                    else
                    {
                        await LogHelper.Log(logChannel, $"Refreshed! Progress for {tempAau.UniqueBungieName} (Level: {updatedLevel} | Power Bonus: +{powerBonus}): {String.Format("{0:n0}", tempAau.LastLevelProgress)} XP -> {String.Format("{0:n0}", updatedProgression)} XP.");

                        actualUser.LastLevel = updatedLevel;
                        actualUser.LastLevelProgress = updatedProgression;
                        actualUser.NoXPGainRefreshes = 0;
                        //tempAau.LastLoggedLevel = updatedLevel;
                        //tempAau.LastLevelProgress = updatedProgression;
                        //tempAau.NoXPGainRefreshes = 0;
                        //newList.Add(tempAau);
                    }
                    await Task.Delay(250);
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

                _xpTimer.Change(ActiveConfig.TimeBetweenRefresh * 60000, ActiveConfig.TimeBetweenRefresh * 60000);
                Log.Information("[{Type}] Bungie API Refreshed! Next refresh in: {Time} minute(s).", "XP Sessions", ActiveConfig.TimeBetweenRefresh);
            }
            catch (Exception x)
            {
                Log.Warning("[{Type}] Refresh failed, trying again! Reason: {Message} ({StackTrace})", "XP Sessions", x.Message, x.StackTrace);
                await Task.Delay(8000);
                await RefreshBungieAPI().ConfigureAwait(false);
                return;
            }
            await UpdateBotActivity();
        }

        private async Task LoadLeaderboards()
        {
            Log.Information("[{Type}] Pulling data for leaderboards...", "Leaderboards");
            try
            {
                var tempPowerLevelData = new PowerLevelData();
                var tempLevelData = new LevelData();
                bool nameChange = false;
                foreach (var link in DataConfig.DiscordIDLinks.ToList()) // USE THIS FOREACH LOOP TO POPULATE FUTURE LEADERBOARDS (that use API calls)
                {
                    string errorReason = "ResponseError";
                    int Level = 0;
                    int PowerLevel = -1;
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                        var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + link.BungieMembershipType + "/Profile/" + link.BungieMembershipID + "/?components=100,200,202").Result;
                        var content = response.Content.ReadAsStringAsync().Result;
                        dynamic item = JsonConvert.DeserializeObject(content);

                        //first 100 levels: 4095505052 (S15); 2069932355 (S16); 26079066 (S17)
                        //anything after: 1531004716 (S15); 1787069365 (S16); 482365574 (S17)
                        errorReason = item.ErrorStatus;
                        try
                        {
                            // System to update names in case players do change name.
                            string name = $"{item.Response.profile.data.userInfo.bungieGlobalDisplayName}";
                            string nameCode = int.Parse($"{item.Response.profile.data.userInfo.bungieGlobalDisplayNameCode}").ToString().PadLeft(4, '0');
                            if (!link.UniqueBungieName.Equals($"{name}#{nameCode}"))
                            {
                                DataConfig.DiscordIDLinks.FirstOrDefault(x => x.DiscordID == link.DiscordID).UniqueBungieName = $"{name}#{nameCode}";
                                nameChange = true;
                            }

                            if (item.Response.profile.privacy != 1) continue;
                            if (item.Response.characters.privacy != 1) continue;

                            for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                            {
                                string charId = $"{item.Response.profile.data.characterIds[i]}";
                                int powerLevelComp = (int)item.Response.characters.data[charId].light;
                                if (PowerLevel <= powerLevelComp)
                                    PowerLevel = powerLevelComp;
                            }

                            tempPowerLevelData.PowerLevelDataEntries.Add(new PowerLevelData.PowerLevelDataEntry()
                            {
                                PowerLevel = PowerLevel,
                                UniqueBungieName = link.UniqueBungieName,
                            });

                            if (item.Response.characterProgressions.data == null) continue;
                            if (item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{BotConfig.Hashes.First100Ranks}"].level == 100)
                            {
                                int extraLevel = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{BotConfig.Hashes.Above100Ranks}"].level;
                                Level = 100 + extraLevel;
                            }
                            else
                            {
                                Level = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{BotConfig.Hashes.First100Ranks}"].level;
                            }

                            tempLevelData.LevelDataEntries.Add(new LevelData.LevelDataEntry()
                            {
                                Level = Level,
                                UniqueBungieName = link.UniqueBungieName,
                            });
                            await Task.Delay(250);
                        }
                        catch
                        {
                            // Continue with the rest of the linked users. Don't want to stop the populating for one problematic account.
                            string discordTag = $"{(await _client.GetUserAsync(link.DiscordID)).Username}#{(await _client.GetUserAsync(link.DiscordID)).Discriminator}";
                            Log.Warning("[{Type}] Error while pulling data for user: {DiscordTag} linked with {BungieTag}. Reason: {Reason}.", "Leaderboards", discordTag, link.UniqueBungieName, errorReason);
                            await Task.Delay(250);
                            continue;
                        }
                    }
                }
                if (nameChange)
                    DataConfig.UpdateConfig();

                tempLevelData.UpdateEntriesConfig();
                tempPowerLevelData.UpdateEntriesConfig();
                Log.Information("[{Type}] Data pulling complete!", "Leaderboards");
            }
            catch (Exception x)
            {
                Log.Information("[{Type}] Error while updating leaderboards, trying again at next refresh. {Exception}", "Leaderboards", x);
            }
        }

        #endregion

        private async Task InitializeListeners()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _interaction.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.Ready += async () =>
            {
                //var guild = _client.GetGuild(915020047154565220);
                //await guild.DeleteApplicationCommandsAsync();
                //await _client.Rest.DeleteAllGlobalCommandsAsync();

                if (BotConfig.IsDebug)
                {
                    //397846250797662208
                    //915020047154565220
                    //1011700865087852585
                    await _interaction.RegisterCommandsToGuildAsync(1011700865087852585);
                }
                else
                {
                    await _interaction.RegisterCommandsGloballyAsync();
                }

                foreach (var m in _interaction.Modules)
                {
                    foreach (var a in m.Attributes)
                    {
                        if (a is not DevGuildOnlyAttribute support) continue;
                        await _interaction.AddModulesToGuildAsync(_client.GetGuild(BotConfig.DevServerID), true, m);
                        break;
                    }
                }
                BotConfig.LoggingChannel = _client.GetChannel(BotConfig.LogChannel) as SocketTextChannel;

                BotConfig.CreationsLogChannel = _client.GetChannel(BotConfig.CommunityCreationsLogChannel) as SocketTextChannel;
                var creationsManager = new CreationsHelper();
                CommunityCreationsTimer = new Timer(creationsManager.CheckCommunityCreationsCallback, null, 5000, 60000 * 2);

                await UpdateBotActivity(1);
            };

            _client.InteractionCreated += HandleInteraction;
            _interaction.SlashCommandExecuted += SlashCommandExecuted;
            _interaction.ComponentCommandExecuted += ComponentCommandExecuted;
            _client.SelectMenuExecuted += SelectMenuHandler;
            //_client.MessageReceived += HandleMessageAsync;
            LevanteCordInstance.Client = _client;
        }

        private async Task SlashCommandExecuted(SlashCommandInfo info, IInteractionContext context, Discord.Interactions.IResult result)
        {
            if (!result.IsSuccess)
            {
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        {
                            var embed = Embeds.GetErrorEmbed();
                            embed.Description = $"{result.ErrorReason}";
                            await context.Interaction.RespondAsync($"", ephemeral: true, embed: embed.Build());
                            break;
                        }
                    default:
                        {
                            var embed = Embeds.GetErrorEmbed();
                            var interData = (context.Interaction as SocketSlashCommand).Data;
                            embed.AddField(x =>
                            {
                                x.Name = "Error";
                                x.Value = $"`{result.Error}: {result.ErrorReason}`";
                            });
                            await context.Interaction.RespondAsync($"", ephemeral: true, embed: embed.Build());
                            break;
                        }
                }
            }
            await UpdateBotActivity();
            return;
        }

        private async Task ComponentCommandExecuted(ComponentCommandInfo info, IInteractionContext context, Discord.Interactions.IResult result)
        {
            if (!result.IsSuccess)
            {
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        var embed = Embeds.GetErrorEmbed();
                        embed.Description = $"{result.ErrorReason}";
                        await context.Interaction.RespondAsync($"", ephemeral: true, embed: embed.Build());
                        break;
                    default:
                        break;
                }
            }
            return;
        }

        private async Task SelectMenuHandler(SocketMessageComponent interaction)
        {
            string trackerType = interaction.Data.Values.First();

            if (trackerType.Equals("ada-1"))
            {
                if (Ada1Rotation.GetUserTracking(interaction.User.Id, out var ModHash) == null)
                {
                    await interaction.RespondAsync($"No Ada-1 tracking enabled.", ephemeral: true);
                    return;
                }
                Ada1Rotation.RemoveUserTracking(interaction.User.Id);
                await interaction.RespondAsync($"Removed your Ada-1 tracking, you will not be notified when {ManifestHelper.Ada1ArmorMods[ModHash]} is available.", ephemeral: true);
                return;
            }
            else if (trackerType.Equals("altars-of-sorrow"))
            {
                if (AltarsOfSorrowRotation.GetUserTracking(interaction.User.Id, out var Weapon) == null)
                {
                    await interaction.RespondAsync($"No Altars of Sorrow tracking enabled.", ephemeral: true);
                    return;
                }
                AltarsOfSorrowRotation.RemoveUserTracking(interaction.User.Id);
                await interaction.RespondAsync($"Removed your Altars of Sorrow tracking, you will not be notified when {AltarsOfSorrowRotation.GetWeaponNameString(Weapon)} ({Weapon}) is available.", ephemeral: true);
                return;
            }
            else if (trackerType.Equals("ascendant-challenge"))
            {
                if (AscendantChallengeRotation.GetUserTracking(interaction.User.Id, out var Challenge) == null)
                {
                    await interaction.RespondAsync($"No Ascendant Challenge tracking enabled.", ephemeral: true);
                    return;
                }
                AscendantChallengeRotation.RemoveUserTracking(interaction.User.Id);
                await interaction.RespondAsync($"Removed your Ascendant Challenge tracking, you will not be notified when {AscendantChallengeRotation.GetChallengeNameString(Challenge)} ({AscendantChallengeRotation.GetChallengeLocationString(Challenge)}) is available.", ephemeral: true);
                return;
            }
            else if (trackerType.Equals("curse-week"))
            {
                if (CurseWeekRotation.GetUserTracking(interaction.User.Id, out var Strength) == null)
                {
                    await interaction.RespondAsync($"No Curse Week tracking enabled.", ephemeral: true);
                    return;
                }
                CurseWeekRotation.RemoveUserTracking(interaction.User.Id);
                await interaction.RespondAsync($"Removed your Curse Week tracking, you will not be notified when {Strength} Strength is available.", ephemeral: true);
                return;
            }
            else if (trackerType.Equals("dsc-challenge"))
            {
                if (DeepStoneCryptRotation.GetUserTracking(interaction.User.Id, out var DSCEncounter) == null)
                {
                    await interaction.RespondAsync($"No Deep Stone Crypt challenges tracking enabled.", ephemeral: true);
                    return;
                }
                DeepStoneCryptRotation.RemoveUserTracking(interaction.User.Id);
                await interaction.RespondAsync($"Removed your Deep Stone Crypt challenges tracking, you will not be notified when {DeepStoneCryptRotation.GetEncounterString(DSCEncounter)} ({DeepStoneCryptRotation.GetChallengeString(DSCEncounter)}) is available.", ephemeral: true);
                return;
            }
            else if (trackerType.Equals("empire-hunt"))
            {
                if (EmpireHuntRotation.GetUserTracking(interaction.User.Id, out var EmpireHunt) == null)
                {
                    await interaction.RespondAsync($"No Empire Hunt tracking enabled.", ephemeral: true);
                    return;
                }
                EmpireHuntRotation.RemoveUserTracking(interaction.User.Id);
                await interaction.RespondAsync($"Removed your Empire Hunt tracking, you will not be notified when {EmpireHuntRotation.GetHuntBossString(EmpireHunt)} is available.", ephemeral: true);
                return;
            }
            else if (trackerType.Equals("featured-raid"))
            {
                if (FeaturedRaidRotation.GetUserTracking(interaction.User.Id, out var FeaturedRaid) == null)
                {
                    await interaction.RespondAsync($"No Featured Raid tracking enabled.", ephemeral: true);
                    return;
                }
                FeaturedRaidRotation.RemoveUserTracking(interaction.User.Id);
                await interaction.RespondAsync($"Removed your Featured Raid tracking, you will not be notified when {FeaturedRaidRotation.GetRaidString(FeaturedRaid)} is available.", ephemeral: true);
                return;
            }
            else if (trackerType.Equals("gos-challenge"))
            {
                if (GardenOfSalvationRotation.GetUserTracking(interaction.User.Id, out var GoSEncounter) == null)
                {
                    await interaction.RespondAsync($"No Garden of Salvation challenges tracking enabled.", ephemeral: true);
                    return;
                }
                GardenOfSalvationRotation.RemoveUserTracking(interaction.User.Id);
                await interaction.RespondAsync($"Removed your Garden of Salvation challenges tracking, you will not be notified when {GardenOfSalvationRotation.GetEncounterString(GoSEncounter)} ({GardenOfSalvationRotation.GetChallengeString(GoSEncounter)}) is available.", ephemeral: true);
                return;
            }
            else if (trackerType.Equals("kf-challenge"))
            {
                if (KingsFallRotation.GetUserTracking(interaction.User.Id, out var KFEncounter) == null)
                {
                    await interaction.RespondAsync($"No King's Fall challenges tracking enabled.", ephemeral: true);
                    return;
                }
                KingsFallRotation.RemoveUserTracking(interaction.User.Id);
                await interaction.RespondAsync($"Removed your King's Fall challenges tracking, you will not be notified when {KFEncounter} ({KingsFallRotation.GetChallengeString(KFEncounter)}) is available.", ephemeral: true);
                return;
            }
            else if (trackerType.Equals("lw-challenge"))
            {
                if (LastWishRotation.GetUserTracking(interaction.User.Id, out var LWEncounter) == null)
                {
                    await interaction.RespondAsync($"No Last Wish challenges tracking enabled.", ephemeral: true);
                    return;
                }
                LastWishRotation.RemoveUserTracking(interaction.User.Id);
                await interaction.RespondAsync($"Removed your Last Wish challenges tracking, you will not be notified when {LastWishRotation.GetEncounterString(LWEncounter)} ({LastWishRotation.GetChallengeString(LWEncounter)}) is available.", ephemeral: true);
                return;
            }
            else if (trackerType.Equals("lost-sector"))
            {
                if (LostSectorRotation.GetUserTracking(interaction.User.Id, out var LS, out var EAT) == null)
                {
                    await interaction.RespondAsync($"No Lost Sector tracking enabled.", ephemeral: true);
                    return;
                }
                LostSectorRotation.RemoveUserTracking(interaction.User.Id);
                if (LS == -1 && EAT == null)
                    await interaction.RespondAsync($"An error has occurred.", ephemeral: true);
                else if (LS != -1 && EAT == null)
                    await interaction.RespondAsync($"Removed your Lost Sector tracking, you will not be notified when {LostSectorRotation.LostSectors[LS].Name} is available.", ephemeral: true);
                else if (LS == -1 && EAT != null)
                    await interaction.RespondAsync($"Removed your Lost Sector tracking, you will not be notified when Lost Sectors are dropping {EAT}.", ephemeral: true);
                else if (LS != -1 && EAT != null)
                    await interaction.RespondAsync($"Removed your Lost Sector tracking, you will not be notified when {LostSectorRotation.LostSectors[LS].Name} is dropping {EAT}.", ephemeral: true);
                return;
            }
            else if (trackerType.Equals("nightfall"))
            {
                if (NightfallRotation.GetUserTracking(interaction.User.Id, out var NF, out var NFWeapon) == null)
                {
                    await interaction.RespondAsync($"No Nightfall tracking enabled.", ephemeral: true);
                    return;
                }
                NightfallRotation.RemoveUserTracking(interaction.User.Id);
                if (NF == null && NFWeapon == null)
                    await interaction.RespondAsync($"An error has occurred.", ephemeral: true);
                else if (NF != null && NFWeapon == null)
                    await interaction.RespondAsync($"Removed your Nightfall tracking, you will not be notified when {NightfallRotation.Nightfalls[(int)NF]} is available.", ephemeral: true);
                else if (NF == null && NFWeapon != null)
                    await interaction.RespondAsync($"Removed your Nightfall tracking, you will not be notified when {NightfallRotation.NightfallWeapons[(int)NFWeapon].Name} is available.", ephemeral: true);
                else if (NF != null && NFWeapon != null)
                    await interaction.RespondAsync($"Removed your Nightfall tracking, you will not be notified when {NightfallRotation.Nightfalls[(int)NF]} is dropping {NightfallRotation.NightfallWeapons[(int)NFWeapon].Name}.", ephemeral: true);
                return;
            }
            else if (trackerType.Equals("nightmare-hunt"))
            {
                if (NightmareHuntRotation.GetUserTracking(interaction.User.Id, out var NightmareHunt) == null)
                {
                    await interaction.RespondAsync($"No Nightmare Hunt tracking enabled.", ephemeral: true);
                    return;
                }
                NightmareHuntRotation.RemoveUserTracking(interaction.User.Id);
                await interaction.RespondAsync($"Removed your Nightmare Hunt tracking, you will not be notified when {NightmareHuntRotation.GetHuntNameString(NightmareHunt)} ({NightmareHuntRotation.GetHuntBossString(NightmareHunt)}) is available.", ephemeral: true);
                return;
            }
            else if (trackerType.Equals("vog-challenge"))
            {
                if (VaultOfGlassRotation.GetUserTracking(interaction.User.Id, out var VoGEncounter) == null)
                {
                    await interaction.RespondAsync($"No Vault of Glass challenges tracking enabled.", ephemeral: true);
                    return;
                }
                VaultOfGlassRotation.RemoveUserTracking(interaction.User.Id);
                await interaction.RespondAsync($"Removed your Vault of Glass challenges tracking, you will not be notified when {VaultOfGlassRotation.GetEncounterString(VoGEncounter)} ({VaultOfGlassRotation.GetChallengeString(VoGEncounter)}) is available.", ephemeral: true);
                return;
            }
            else if (trackerType.Equals("vow-challenge"))
            {
                if (VowOfTheDiscipleRotation.GetUserTracking(interaction.User.Id, out var VowEncounter) == null)
                {
                    await interaction.RespondAsync($"No Vow of the Disciple challenges tracking enabled.", ephemeral: true);
                    return;
                }
                VowOfTheDiscipleRotation.RemoveUserTracking(interaction.User.Id);
                await interaction.RespondAsync($"Removed your Vow of the Disciple challenges tracking, you will not be notified when {VowOfTheDiscipleRotation.GetEncounterString(VowEncounter)} ({VowOfTheDiscipleRotation.GetChallengeString(VowEncounter)}) is available.", ephemeral: true);
                return;
            }
            else if (trackerType.Equals("wellspring"))
            {
                if (WellspringRotation.GetUserTracking(interaction.User.Id, out var Wellspring) == null)
                {
                    await interaction.RespondAsync($"No Wellspring tracking enabled.", ephemeral: true);
                    return;
                }
                WellspringRotation.RemoveUserTracking(interaction.User.Id);
                await interaction.RespondAsync($"Removed your Wellspring tracking, you will not be notified when The Wellspring: {WellspringRotation.GetWellspringTypeString(Wellspring)} is dropping {WellspringRotation.GetWeaponNameString(Wellspring)}.", ephemeral: true);
                return;
            }
        }

        //private async Task HandleMessageAsync(SocketMessage arg)
        //{
        //    if (arg.Author.IsWebhook || arg.Author.IsBot) return; // Return if message is from a Webhook or Bot user
        //    if (arg.ToString().Length < 0) return; // Return of the message has no text
        //    if (arg.Author.Id == _client.CurrentUser.Id) return; // Return of the message is from itself

        //    int argPos = 0; // Position to check for command arguments

        //    var msg = arg as SocketUserMessage;
        //    if (msg == null) return;

        //    if (msg.MentionedUsers.Contains(_client.CurrentUser))
        //    {
        //        Console.WriteLine("Ping!");
        //    }
        //}

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
                var ctx = new SocketInteractionContext(_client, arg);
                if (arg.Type == InteractionType.ApplicationCommand)
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }
}