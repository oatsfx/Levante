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
using Levante.Services;
using System.Collections.Generic;

namespace Levante
{
    public static class LevanteCordInstance
    {
        public static DiscordShardedClient Client;
    }

    public sealed class LevanteCord
    {
        private readonly DiscordShardedClient _client;
        private readonly CommandService _commands;
        private readonly InteractionService _interaction;
        private readonly IServiceProvider _services;

        private Timer DailyResetTimer;
        private Timer _xpTimer;
        private Timer _leaderboardTimer;
        private Timer CheckBungieAPI;

        public LevanteCord()
        {
            var config = new DiscordSocketConfig
            {
                TotalShards = BotConfig.DiscordShards,
                GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildInvites & ~GatewayIntents.GuildScheduledEvents,
            };

            _client = new DiscordShardedClient(config);
            _commands = new CommandService();
            _interaction = new InteractionService(_client);
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton<InteractiveService>()
                .AddSingleton<InteractionService>(p => new InteractionService(p.GetRequiredService<DiscordShardedClient>()))
                .AddSingleton<CreationsService>()
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
            if (!ConfigHelper.CheckAndLoadConfigFiles())
                return;
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
            await Task.Run(() => Console.Title = $"{BotConfig.AppName} v{BotConfig.Version}");

            if (!LeaderboardHelper.CheckAndLoadDataFiles())
                return;

            CurrentRotations.CreateJSONs();

            API.FetchManifest();
            ManifestHelper.LoadManifestDictionaries();

            EmblemOffer.LoadCurrentOffers();
            CurrentRotations.UpdateRotationsJSON();

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
            var client = _services.GetRequiredService<DiscordShardedClient>();
            var commands = _services.GetRequiredService<InteractionService>();
            var creations = _services.GetRequiredService<CreationsService>();

            client.Log += LogAsync;

            client.EntitlementCreated += HandleEntitlementCreated;

            await _client.LoginAsync(TokenType.Bot, BotConfig.DiscordToken);
            await _client.StartAsync();

            Log.Information("[{Type}] Bot successfully started! v{@Version}",
                "Startup", BotConfig.Version);
            await Task.Delay(-1);

            await _xpTimer.DisposeAsync();
            await _leaderboardTimer.DisposeAsync();
            await DailyResetTimer.DisposeAsync();
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
            if (SetRNG > 0 && SetRNG < RNGMax)
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
                    await _client.SetActivityAsync(new Game($"{BotConfig.PlayingStatuses[rand.Next(0, BotConfig.PlayingStatuses.Count)]} | v{BotConfig.Version}", ActivityType.Playing)); break;
                case 2:
                    await _client.SetActivityAsync(new Game($"{_client.Guilds.Count:n0} Servers | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 3:
                    await _client.SetActivityAsync(new Game($"{_client.Guilds.Sum(x => x.MemberCount):n0} Users | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 4:
                    await _client.SetActivityAsync(new Game($"{DataConfig.DiscordIDLinks.Count:n0} Linked Users | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 5:
                    await _client.SetActivityAsync(new Game($"{CurrentRotations.GetTotalLinks()} Rotation Trackers | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 6:
                    await _client.SetActivityAsync(new Game($"{BotConfig.Website} | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 7:
                    await _client.SetActivityAsync(new Game($"{BotConfig.Twitter} on Twitter | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 8:
                    await _client.SetActivityAsync(new Game($"{EmblemOffer.CurrentOffers.Count} Available Emblems", ActivityType.Watching)); break;
                case 9:
                    await _client.SetActivityAsync(new Game($"{_client.Shards.Count} Shards | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 10:
                    await _client.SetActivityAsync(new Game($"{BotConfig.WatchingStatuses[rand.Next(0, BotConfig.WatchingStatuses.Count)]} | v{BotConfig.Version}", ActivityType.Watching)); break;
                default:
                    await _client.SetActivityAsync(new CustomStatusGame("If you haven't used me after August 15th, 2023, relink using the /link command.")); break;
            }
            return;
        }

        private void SetUpTimer(DateTime alertTime)
        {
            // this is called to get the timer set up to run at every daily reset
            TimeSpan timeToGo = new(alertTime.Ticks - DateTime.UtcNow.Ticks);
            Log.Debug("Reset in: {Time}.", $"{timeToGo.Hours:00}:{timeToGo.Minutes:00}:{timeToGo.Seconds:00}");
            DailyResetTimer = new Timer(DailyResetChanges, null, (long)timeToGo.TotalMilliseconds, Timeout.Infinite);
        }

        public async void DailyResetChanges(Object o = null)
        {
            await Task.Run(CountdownConfig.CheckCountdowns);
            await Task.Run(EmblemOffer.CheckEmblemOffers);
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

        private async void LeaderboardTimerCallback(Object o) => await Task.Run(() => LoadLeaderboards()).ConfigureAwait(false);

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
                    actualUser ??= ActiveConfig.PriorityActiveAFKUsers.FirstOrDefault(x => x.DiscordChannelID == tempAau.DiscordChannelID);

                    IUser user = _client.GetUser(tempAau.DiscordID);
                    if (user == null)
                    {
                        var _rClient = _client.Rest;
                        user = await _rClient.GetUserAsync(tempAau.DiscordID);
                    }

                    var logChannel = _client.GetChannel(tempAau.DiscordChannelID) as ITextChannel;
                    var dmChannel = user.CreateDMChannelAsync().Result;

                    if (logChannel == null)
                    {
                        await LogHelper.Log(dmChannel, $"<@{tempAau.DiscordID}>: Refresh unsuccessful. Reason: LoggingChannelNotFound. Logging will be terminated for {tempAau.UniqueBungieName}.");
                        await LogHelper.Log(dmChannel, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.Start.Timestamp)}.", XPLoggingHelper.GenerateSessionSummary(tempAau));

                        Log.Information("[{Type}] Stopped logging for {User} via automation.", "XP Sessions", tempAau.UniqueBungieName);

                        ActiveConfig.DeleteActiveUserFromConfig(tempAau.DiscordID);
                        //ActiveConfig.ActiveAFKUsers.Remove(ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordChannelID == tempAau.DiscordChannelID));
                        await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau));
                    }

                    //int updatedLevel = DataConfig.GetAFKValues(tempAau.DiscordID, out int updatedProgression, out int powerBonus, out string errorStatus, out long activityHash);
                    var loggingValues = new XpLoggingValueResponse(tempAau.DiscordID);
                    var updatedLevel = loggingValues.CurrentLevel;
                    var updatedExtraLevel = loggingValues.CurrentExtraLevel;
                    var updatedProgression = loggingValues.XpProgress;
                    var powerBonus = loggingValues.PowerBonus;
                    var errorStatus = loggingValues.ErrorStatus;
                    var activityHash = loggingValues.ActivityHash;
                    var nextLevelAt = loggingValues.NextLevelAt;
                    var levelCap = loggingValues.LevelCap;

                    var combinedLevel = updatedLevel + updatedExtraLevel;
                    var lastCombinedLevel = aau.Last.Level + aau.Last.ExtraLevel;

                    if (!errorStatus.Equals("Success"))
                    {
                        if (tempAau.NoXPGainRefreshes >= ActiveConfig.RefreshesBeforeKick)
                        {
                            string uniqueName = tempAau.UniqueBungieName;

                            await LogHelper.Log(logChannel, $"Refresh unsuccessful. Reason: {errorStatus}.");
                            await LogHelper.Log(logChannel, $"<@{tempAau.DiscordID}>: Refresh unsuccessful. Reason: {errorStatus}. Here is your session summary:", XPLoggingHelper.GenerateSessionSummary(tempAau), XPLoggingHelper.GenerateChannelButtons(tempAau.DiscordID));

                            //await LogHelper.Log(dmChannel, $"<@{tempAau.DiscordID}>: Refresh unsuccessful. Reason: {errorStatus}. Logging will be terminated for {uniqueName}.");
                            await LogHelper.Log(dmChannel, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.Start.Timestamp)}.", XPLoggingHelper.GenerateSessionSummary(tempAau));

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

                    if (powerBonus > tempAau.Last.PowerBonus)
                    {
                        await LogHelper.Log(logChannel, $"Power bonus increase!: {tempAau.Last.PowerBonus} -> {powerBonus} (Start: {tempAau.Start.PowerBonus}).");

                        actualUser.Last.PowerBonus = powerBonus;
                    }
                   
                    /*if (activityHash != tempAau.ActivityHash)
                    {
                        string uniqueName = tempAau.UniqueBungieName;

                        await LogHelper.Log(logChannel, $"<@{tempAau.DiscordID}>: Player activity has changed. Logging terminated by automation. Here is your session summary:", XPLoggingHelper.GenerateSessionSummary(tempAau), XPLoggingHelper.GenerateChannelButtons(tempAau.DiscordID));

                        //await LogHelper.Log(dmChannel, $"<@{tempAau.DiscordID}>: Player has been determined as inactive. Logging will be terminated for {uniqueName}.");
                        await LogHelper.Log(dmChannel, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.Start.Timestamp)}.", XPLoggingHelper.GenerateSessionSummary(tempAau));

                        Log.Information("[{Type}] Stopped logging for {User} via automation.", "XP Sessions", tempAau.UniqueBungieName);
                        ActiveConfig.DeleteActiveUserFromConfig(tempAau.DiscordID);
                        await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau)).ConfigureAwait(false);
                    }
                    else*/ if (combinedLevel > lastCombinedLevel)
                    {
                        await LogHelper.Log(logChannel, $"Level up! Now: {tempAau.Last.Level}{(tempAau.Last.ExtraLevel > 0 ? $" (+{tempAau.Last.ExtraLevel})" : "")}" +
                            $" -> {updatedLevel}{(updatedExtraLevel > 0 ? $" (+{updatedExtraLevel})" : "")} " +
                            $"({updatedProgression:n0}/{nextLevelAt:n0} XP). " +
                            $"Start: {tempAau.Start.Level}{(tempAau.Start.ExtraLevel > 0 ? $" (+{tempAau.Start.ExtraLevel})" : "")} ({tempAau.Start.LevelProgress:n0}/{tempAau.Start.NextLevelAt:n0} XP).");

                        actualUser.Last.Level = updatedLevel;
                        actualUser.Last.ExtraLevel = updatedExtraLevel;
                        actualUser.Last.LevelProgress = updatedProgression;
                        actualUser.Last.NextLevelAt = nextLevelAt;
                        actualUser.NoXPGainRefreshes = 0;
                        actualUser.Last.Timestamp = DateTime.Now;
                    }
                    else if (updatedProgression <= tempAau.Last.LevelProgress)
                    {
                        if (tempAau.NoXPGainRefreshes >= ActiveConfig.RefreshesBeforeKick)
                        {
                            string uniqueName = tempAau.UniqueBungieName;

                            await LogHelper.Log(logChannel, $"<@{tempAau.DiscordID}>: Player has been determined as inactive. Logging terminated by automation. Here is your session summary:", XPLoggingHelper.GenerateSessionSummary(tempAau), XPLoggingHelper.GenerateChannelButtons(tempAau.DiscordID));

                            //await LogHelper.Log(dmChannel, $"<@{tempAau.DiscordID}>: Player has been determined as inactive. Logging will be terminated for {uniqueName}.");
                            await LogHelper.Log(dmChannel, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.Start.Timestamp)}.", XPLoggingHelper.GenerateSessionSummary(tempAau));

                            Log.Information("[{Type}] Stopped logging for {User} via automation.", "XP Sessions", tempAau.UniqueBungieName);
                            //listOfRemovals.Add(tempAau);

                            ActiveConfig.DeleteActiveUserFromConfig(tempAau.DiscordID);
                            //ActiveConfig.ActiveAFKUsers.Remove(ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordChannelID == tempAau.DiscordChannelID));
                            await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau)).ConfigureAwait(false);
                        }
                        else
                        {
                            actualUser.NoXPGainRefreshes = tempAau.NoXPGainRefreshes + 1;
                            await LogHelper.Log(logChannel, $"No XP change detected, waiting for next refresh... Warning {tempAau.NoXPGainRefreshes} of {ActiveConfig.RefreshesBeforeKick}.");
                        }
                    }
                    else
                    {
                        int levelsGained = (aau.Last.Level - aau.Start.Level) + (aau.Last.ExtraLevel - aau.Start.ExtraLevel);
                        long xpGained = 0;
                        // If the user hit the cap during the session, calculate gained XP differently.
                        if (aau.Start.Level < levelCap && aau.Last.Level >= levelCap)
                        {
                            int levelsToCap = levelCap - aau.Start.Level;
                            int levelsPastCap = levelsGained - levelsToCap;
                            // StartNextLevelAt should be the 100,000 or whatever Bungie decides to change it to later on.
                            xpGained = ((levelsToCap) * aau.Start.NextLevelAt) + (levelsPastCap * nextLevelAt) - aau.Start.LevelProgress + updatedProgression;
                        }
                        else // The nextLevelAt stays the same because it did not change mid-logging session.
                        {
                            xpGained = (levelsGained * nextLevelAt) - aau.Start.LevelProgress + updatedProgression;
                        }

                        long xpGainedInOneRefresh = updatedProgression - tempAau.Last.LevelProgress;

                        await LogHelper.Log(logChannel, $"{tempAau.Last.LevelProgress:n0} (+{xpGainedInOneRefresh:n0}) -> {updatedProgression:n0} XP" +
                            $" | Level: {updatedLevel}{(updatedExtraLevel > 0 ? $" (+{updatedExtraLevel})" : "")}" +
                            $" | Power: +{powerBonus}" +
                            $" | Rate: {(int)Math.Floor(xpGained / (DateTime.Now - aau.Start.Timestamp).TotalHours):n0} XP/Hour" +
                            $" | Inst. Rate: {(int)Math.Floor(xpGainedInOneRefresh / (DateTime.Now - aau.Last.Timestamp).TotalHours):n0} XP/Hour");

                        actualUser.Last.Level = updatedLevel;
                        actualUser.Last.ExtraLevel = updatedExtraLevel;
                        actualUser.Last.LevelProgress = updatedProgression;
                        actualUser.Last.NextLevelAt = nextLevelAt;
                        actualUser.NoXPGainRefreshes = 0;
                        actualUser.Last.Timestamp = DateTime.Now;
                    }
                }
                ActiveConfig.UpdateActiveAFKUsersConfig();

                // In place if the experimental function decides to break!
                if (ActiveConfig.RefreshScaling >= 0)
                {
                    int msTilNext = (int)Math.Floor((-((ActiveConfig.ActiveAFKUsers.Count + ActiveConfig.PriorityActiveAFKUsers.Count) * ActiveConfig.RefreshScaling) + ActiveConfig.TimeBetweenRefresh) * 60000);
                    msTilNext = msTilNext < 0 ? 0 : msTilNext;
                    double minutesTilNext = msTilNext / (double)60000;
                    
                    _xpTimer.Change(msTilNext, msTilNext);
                    Log.Debug("Refreshing in {Time} ms.", msTilNext);
                    Log.Information("[{Type}] Bungie API Refreshed! Next refresh in: {Time} minute(s).", "XP Sessions", minutesTilNext);
                }
                else
                {
                    _xpTimer.Change(ActiveConfig.TimeBetweenRefresh * 60000, ActiveConfig.TimeBetweenRefresh * 60000);
                    Log.Information("[{Type}] Bungie API Refreshed! Next refresh in: {Time} minute(s).", "XP Sessions", ActiveConfig.TimeBetweenRefresh);
                }
                
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
                bool updateConfig = false;
                foreach (var link in DataConfig.DiscordIDLinks.ToList()) // USE THIS FOREACH LOOP TO POPULATE FUTURE LEADERBOARDS (that use API calls)
                {
                    var user = link;
                    int Level = 0;
                    int PowerLevel = -1;
                    using var client = new HttpClient();
                    string errorReason = "ResponseError";
                    if (user.RefreshExpiration < DateTime.Now)
                    {
                        errorReason = "RefreshExpired";
                        Log.Warning("[{Type}] Refresh expired, removing this user from my data.", "Leaderboards", user.DiscordID, user.UniqueBungieName, errorReason);
                        DataConfig.DeleteUserFromConfig(user.DiscordID);
                        continue;
                    }

                    try
                    {
                        if (user.AccessExpiration < DateTime.Now)
                        {
                            user = DataConfig.RefreshCode(user);
                            DataConfig.DiscordIDLinks.FirstOrDefault(x => x.DiscordID == user.DiscordID).AccessExpiration = user.AccessExpiration;
                            DataConfig.DiscordIDLinks.FirstOrDefault(x => x.DiscordID == user.DiscordID).AccessToken = user.AccessToken;
                            DataConfig.DiscordIDLinks.FirstOrDefault(x => x.DiscordID == user.DiscordID).RefreshExpiration = user.RefreshExpiration;
                            DataConfig.DiscordIDLinks.FirstOrDefault(x => x.DiscordID == user.DiscordID).RefreshToken = user.RefreshToken;
                            DataConfig.UpdateConfig();
                            updateConfig = true;
                        }

                        client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {user.AccessToken}");

                        var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + user.BungieMembershipType + "/Profile/" + user.BungieMembershipID + "/?components=100,200,202").Result;
                        var content = response.Content.ReadAsStringAsync().Result;
                        dynamic item = JsonConvert.DeserializeObject(content);

                        errorReason = item.ErrorStatus;
                        // System to update names in case players do change name.
                        string name = $"{item.Response.profile.data.userInfo.bungieGlobalDisplayName}";
                        string nameCode = int.Parse($"{item.Response.profile.data.userInfo.bungieGlobalDisplayNameCode}").ToString().PadLeft(4, '0');
                        if (!user.UniqueBungieName.Equals($"{name}#{nameCode}"))
                        {
                            DataConfig.DiscordIDLinks.FirstOrDefault(x => x.DiscordID == user.DiscordID).UniqueBungieName = $"{name}#{nameCode}";
                            DataConfig.UpdateConfig();
                            updateConfig = true;
                        }

                        if (item.Response.profile.privacy != 1) continue;
                        if (item.Response.characters.privacy != 1) continue;

                        if (item.Response.profile.data.characterIds.Count <= 0)
                            continue;

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
                            UniqueBungieName = user.UniqueBungieName,
                        });

                        if (item.Response.characterProgressions.data == null) continue;
                        if (item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{BotConfig.Hashes.First100Ranks}"].level == ManifestHelper.CurrentLevelCap)
                        {
                            int extraLevel = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{BotConfig.Hashes.Above100Ranks}"].level;
                            Level = ManifestHelper.CurrentLevelCap + extraLevel;
                        }
                        else
                        {
                            Level = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"{BotConfig.Hashes.First100Ranks}"].level;
                        }

                        tempLevelData.LevelDataEntries.Add(new LevelData.LevelDataEntry()
                        {
                            Level = Level,
                            UniqueBungieName = user.UniqueBungieName,
                        });
                        Log.Debug($"{name}#{nameCode} - Level: {Level} - Power: {PowerLevel}");
                    }
                    catch
                    {
                        // Continue with the rest of the linked users. Don't want to stop the populating for one problematic account.
                        //string discordTag = $"{(_client.GetUser(link.DiscordID)).Username}#{(_client.GetUser(link.DiscordID)).Discriminator}";
                        Log.Warning("[{Type}] Error while pulling data for user: {DiscordTag} linked with {BungieTag}. Reason: {Reason}.", "Leaderboards", user.DiscordID, user.UniqueBungieName, errorReason);
                        continue;
                    }
                }
                if (updateConfig)
                    DataConfig.UpdateConfig();

                tempLevelData.UpdateEntriesConfig();
                tempPowerLevelData.UpdateEntriesConfig();
                Log.Information("[{Type}] Data pulling complete!", "Leaderboards");
                await Task.Delay(0);
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

            int readyShards = 0;
            _client.InteractionCreated += HandleInteraction;
            _interaction.SlashCommandExecuted += SlashCommandExecuted;
            _interaction.ComponentCommandExecuted += ComponentCommandExecuted;
            _client.MessageReceived += HandleMessageAsync;
            _client.EntitlementCreated += HandleEntitlementCreated;

            _client.ShardReady += async shard =>
            {
                //var guild = _client.GetGuild(915020047154565220);
                //await guild.DeleteApplicationCommandsAsync();
                //await _client.Rest.DeleteAllGlobalCommandsAsync();

                shard.Ready += async () =>
                {
                    await Task.Run(() =>
                    {
                        Log.Information("[{Type}] Shard {ShardID} connected and ready.", "Discord", shard.ShardId);
                    });
                };

                //try
                //{
                //    await shard.GetEntitlementsAsync();
                //    await shard.CreateTestEntitlementAsync(1252458418653106337, 915020047154565220, SubscriptionOwnerType.Guild);
                //}
                //catch (Exception x)
                //{
                //    Log.Debug($"{x}");
                //}
                //var entitlements = shard.Entitlements;

                readyShards++;
                if (readyShards == _client.Shards.Count)
                {
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

                    _xpTimer = new Timer(XPTimerCallback, null, 20000, ActiveConfig.TimeBetweenRefresh * 60000);
                    _leaderboardTimer = new Timer(LeaderboardTimerCallback, null, 30000, 3600000);
                    Log.Information("[{Type}] Continued XP logging for {Count} (+{PriorityCount}) Users.",
                        "XP Sessions", ActiveConfig.ActiveAFKUsers.Count, ActiveConfig.PriorityActiveAFKUsers.Count);

                    await UpdateBotActivity(12);
                }
            };

            LevanteCordInstance.Client = _client;
        }

        private async Task HandleEntitlementCreated(SocketEntitlement entitlement)
        {
            // Guild Entitlement
            if (entitlement.Guild != null)
            {
                Log.Debug($"{entitlement.Guild}");
            }
            
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
                            embed.AddField(x =>
                            {
                                x.Name = "Error";
                                x.Value = $"`{result.Error}: {result.ErrorReason}`";
                            });
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

        private async Task HandleMessageAsync(SocketMessage arg)
        {
            if (arg.Author.IsWebhook || arg.Author.IsBot) return; // Return if message is from a Webhook or Bot user
            if (arg.ToString().Length < 0) return; // Return of the message has no text
            if (arg.Author.Id == _client.CurrentUser.Id) return; // Return of the message is from itself

            int argPos = 0; // Position to check for command arguments

            var msg = arg as SocketUserMessage;
            if (msg == null) return;

            if (msg.MentionedUsers.Any(x => x.Id == _client.CurrentUser.Id))
            {
                if (msg.Content.ToLower().Contains("help"))
                {
                    await msg.ReplyAsync(embed: Embeds.GetHelpEmbed().Build());
                }
                
                return;
            }
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
                var ctx = new ShardedInteractionContext(_client, arg);
                await _interaction.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                var ctx = new ShardedInteractionContext(_client, arg);
                if (arg.Type == InteractionType.ApplicationCommand)
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }
}