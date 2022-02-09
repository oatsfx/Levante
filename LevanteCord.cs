using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Levante.Configs;
using System.Net.Http;
using Levante.Helpers;
using System.Threading;
using Discord.Net;
using Levante.Leaderboards;
using Levante.Rotations;
using Levante.Util;
using Fergun.Interactive;
using Discord.Interactions;

namespace Levante
{
    public sealed class LevanteCord
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly InteractionService _interaction;
        private readonly IServiceProvider _services;

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

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            string ASCIIName = @"
   __                      __     
  / /  ___ _  _____ ____  / /____ 
 / /__/ -_) |/ / _ `/ _ \/ __/ -_)
/____/\__/|___/\_,_/_//_/\__/\__/   dev. by @OatsFX
            ";
            Console.WriteLine(ASCIIName);
            
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
            EmblemOffer.LoadCurrentOffers();
            
            Console.WriteLine($"Current Bot Version: v{BotConfig.Version}");
            Console.WriteLine($"Current Developer Note: {BotConfig.Note}");

            Console.WriteLine($"Legend Lost Sector: {LostSectorRotation.GetLostSectorString(CurrentRotations.LegendLostSector)} ({CurrentRotations.LegendLostSectorArmorDrop})");
            Console.WriteLine($"Master Lost Sector: {LostSectorRotation.GetLostSectorString(CurrentRotations.MasterLostSector)} ({CurrentRotations.MasterLostSectorArmorDrop})");
            Console.WriteLine();
            Console.WriteLine($"Altar Weapon: {AltarsOfSorrowRotation.GetWeaponNameString(CurrentRotations.AltarWeapon)} ({CurrentRotations.AltarWeapon})");
            Console.WriteLine();
            Console.WriteLine($"Last Wish Challenge: {LastWishRotation.GetEncounterString(CurrentRotations.LWChallengeEncounter)} ({LastWishRotation.GetChallengeString(CurrentRotations.LWChallengeEncounter)})");
            Console.WriteLine($"Garden of Salvation Challenge: {GardenOfSalvationRotation.GetEncounterString(CurrentRotations.GoSChallengeEncounter)} ({GardenOfSalvationRotation.GetChallengeString(CurrentRotations.GoSChallengeEncounter)})");
            Console.WriteLine($"Deep Stone Crypt Challenge: {DeepStoneCryptRotation.GetEncounterString(CurrentRotations.DSCChallengeEncounter)} ({DeepStoneCryptRotation.GetChallengeString(CurrentRotations.DSCChallengeEncounter)})");
            Console.WriteLine($"Vault of Glass Challenge: {VaultOfGlassRotation.GetEncounterString(CurrentRotations.VoGChallengeEncounter)} ({VaultOfGlassRotation.GetChallengeString(CurrentRotations.VoGChallengeEncounter)})");
            Console.WriteLine();
            Console.WriteLine($"Curse Week: {CurrentRotations.CurseWeek}");
            Console.WriteLine($"Ascendant Challenge: {AscendantChallengeRotation.GetChallengeNameString(CurrentRotations.AscendantChallenge)} ({AscendantChallengeRotation.GetChallengeLocationString(CurrentRotations.AscendantChallenge)})");
            Console.WriteLine();
            Console.WriteLine($"Nightfall: {NightfallRotation.GetStrikeNameString(CurrentRotations.Nightfall)} ({NightfallRotation.GetWeaponString(CurrentRotations.NightfallWeaponDrops[0])}/{NightfallRotation.GetWeaponString(CurrentRotations.NightfallWeaponDrops[1])})");
            Console.WriteLine();
            Console.WriteLine($"Empire Hunt: {EmpireHuntRotation.GetHuntNameString(CurrentRotations.EmpireHunt)}");
            Console.WriteLine();
            Console.WriteLine($"Nightmare Hunts: {CurrentRotations.NightmareHunts[0]}/{CurrentRotations.NightmareHunts[1]}/{CurrentRotations.NightmareHunts[2]}");
            Console.WriteLine();

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

            Timer timer = new Timer(TimerCallback, null, 25000, BotConfig.TimeBetweenRefresh * 60000);

            if (DateTime.Now.Hour >= 10) // after daily reset
                SetUpTimer(new DateTime(DateTime.Today.AddDays(1).Year, DateTime.Today.AddDays(1).Month, DateTime.Today.AddDays(1).Day, 10, 0, 0));
            else
                SetUpTimer(new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 10, 0, 0));

            await InitializeListeners();

            await _client.LoginAsync(TokenType.Bot, BotConfig.DiscordToken);
            await _client.StartAsync();

            await UpdateBotActivity(1);

            await Task.Delay(-1);
        }

        // Set to 0 for Watching X/Y Thrallway Users
        // Set to 1 for Playing {note} | v{version}
        // Set to 2 for Watching for {prefix}help | v{version}
        // Set to 3 for Watching X Servers | v{version}
        // Set to 4 for Watching @Levante_Bot on Twitter
        // Set to 5 for Watching X Linked Users | v{version}
        // Set to 6 for Watching X Rotation Trackers | v{version}
        private async Task UpdateBotActivity(int SetRNG = -1)
        {
            int RNG = 0;
            int RNGMax = 8;
            if (SetRNG != -1 && SetRNG < RNGMax)
                RNG = SetRNG;
            else
            {
                Random rand = new Random();
                RNG = rand.Next(0, RNGMax);
            }

            switch (RNG)
            {
                case 0:
                    string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "" : "s";
                    await _client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumThrallwayUsers} Thrallway Farmer{s}", ActivityType.Watching)); break;
                case 1:
                    await _client.SetActivityAsync(new Game($"{BotConfig.Note} | v{BotConfig.Version}", ActivityType.Playing)); break;
                case 2:
                    await _client.SetActivityAsync(new Game($"for {BotConfig.DefaultCommandPrefix}help | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 3:
                    await _client.SetActivityAsync(new Game($"{_client.Rest.GetGuildsAsync().Result.Count} Servers | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 4:
                    await _client.SetActivityAsync(new Game($"@Levante_Bot on Twitter", ActivityType.Watching)); break;
                case 5:
                    await _client.SetActivityAsync(new Game($"{DataConfig.DiscordIDLinks.Count} Linked Users | v{BotConfig.Version}", ActivityType.Watching)); break;
                case 6:
                    await _client.SetActivityAsync(new Game($"{CurrentRotations.GetTotalLinks()} Rotation Trackers | v{BotConfig.Version}", ActivityType.Watching)); break;
                default: break;
            }
            return;
        }

        private void SetUpTimer(DateTime alertTime)
        {
            // this is called to get the timer set up to run at every daily reset
            TimeSpan timeToGo = new TimeSpan(alertTime.Ticks - DateTime.Now.Ticks);
            DailyResetTimer = new Timer(DailyResetChanges, null, (long)timeToGo.TotalMilliseconds, Timeout.Infinite);
        }

        public async void DailyResetChanges(Object o = null)
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
            SetUpTimer(new DateTime(DateTime.Today.AddDays(1).Year, DateTime.Today.AddDays(1).Month, DateTime.Today.AddDays(1).Day, 10, 0, 0));
            Console.ForegroundColor = ConsoleColor.Cyan;
        }

        private async void TimerCallback(Object o) => await RefreshBungieAPI().ConfigureAwait(false);

        #region ThrallwayLogging
        private async Task RefreshBungieAPI()
        {
            if (ActiveConfig.ActiveAFKUsers.Count <= 0)
            {
                LogHelper.ConsoleLog($"Skipping refresh, no active AFK users...");
                await LoadLeaderboards();
                return;
            }

            LogHelper.ConsoleLog($"Refreshing Bungie API...");
            List<ActiveConfig.ActiveAFKUser> listOfRemovals = new List<ActiveConfig.ActiveAFKUser>();
            try // thrallway
            {
                LogHelper.ConsoleLog($"Refreshing Thrallway Users...");
                foreach (ActiveConfig.ActiveAFKUser aau in ActiveConfig.ActiveAFKUsers.ToList())
                {
                    ActiveConfig.ActiveAFKUser tempAau = aau;
                    int updatedLevel = DataConfig.GetAFKValues(tempAau.DiscordID, out int updatedProgression, out bool isInShatteredThrone, out string errorStatus);

                    if (!errorStatus.Equals("Success"))
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Refresh unsuccessful. Reason: {errorStatus}.");
                        LogHelper.ConsoleLog($"Refresh unsuccessful for {tempAau.UniqueBungieName}. Reason: {errorStatus}.");
                        // Move onto the next user so everyone gets the message.
                        continue;
                    }

                    LogHelper.ConsoleLog($"Checking {tempAau.UniqueBungieName}.");

                    if (!isInShatteredThrone)
                    {
                        string uniqueName = tempAau.UniqueBungieName;

                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Player is no longer in Shattered Throne.");
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"<@{tempAau.DiscordID}>: Logging terminated by automation. Here is your session summary:", ThrallwayHelper.GenerateSessionSummary(tempAau), ThrallwayHelper.GenerateDeleteChannelButton());

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
                        await LogHelper.Log(user.CreateDMChannelAsync().Result, $"<@{tempAau.DiscordID}>: Player is no longer in Shattered Throne. Logging will be terminated for {uniqueName}.");
                        await LogHelper.Log(user.CreateDMChannelAsync().Result, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.TimeStarted)}.", ThrallwayHelper.GenerateSessionSummary(tempAau));

                        LogHelper.ConsoleLog($"Stopped logging for {tempAau.UniqueBungieName} via automation.");
                        listOfRemovals.Add(tempAau);
                        await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau));
                    }
                    else if (updatedLevel > tempAau.LastLoggedLevel)
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Level up detected: {tempAau.LastLoggedLevel} -> {updatedLevel}");

                        tempAau.LastLoggedLevel = updatedLevel;
                        tempAau.LastLevelProgress = updatedProgression;

                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, 
                            $"Start: {tempAau.StartLevel} ({String.Format("{0:n0}", tempAau.StartLevelProgress)}/100,000 XP). Now: {tempAau.LastLoggedLevel} ({String.Format("{0:n0}", tempAau.LastLevelProgress)}/100,000 XP)");
                    }
                    else if (updatedProgression <= tempAau.LastLevelProgress)
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"No XP change detected, attempting to refresh API again...");
                        if (await RefreshSpecificUser(tempAau).ConfigureAwait(false) == null) listOfRemovals.Add(tempAau);
                    }
                    else
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Refreshed! Progress for {tempAau.UniqueBungieName} (Level: {updatedLevel}): {String.Format("{0:n0}", tempAau.LastLevelProgress)} XP -> {String.Format("{0:n0}", updatedProgression)} XP");

                        tempAau.LastLoggedLevel = updatedLevel;
                        tempAau.LastLevelProgress = updatedProgression;
                    }

                    await Task.Delay(3500); // we dont want to spam API if we have a ton of AFK subscriptions
                }

                foreach (var user in listOfRemovals)
                    if (ActiveConfig.GetActiveAFKUser(user.DiscordID) != null)
                        ActiveConfig.DeleteActiveUserFromConfig(user.DiscordID);

                ActiveConfig.UpdateActiveAFKUsersConfig();

                LogHelper.ConsoleLog($"Bungie API Refreshed!");
            }
            catch (Exception x)
            {
                LogHelper.ConsoleLog($"Refresh failed, trying again! Reason: {x.Message} ({x.StackTrace})");
                await Task.Delay(8000);
                await RefreshBungieAPI().ConfigureAwait(false);
                return;
            }

            // data loading
            await Task.Delay(45000); // wait to prevent numerous API calls
            await LoadLeaderboards();
        }

        private async Task LoadLeaderboards()
        {
            try
            {
                LogHelper.ConsoleLog($"Pulling data for leaderboards...");
                LevelData.LevelDataEntries.Clear();
                PowerLevelData.PowerLevelDataEntries.Clear();
                foreach (var link in DataConfig.DiscordIDLinks.ToList()) // USE THIS FOREACH LOOP TO POPULATE FUTURE LEADERBOARDS (that use API calls)
                {
                    int Level = 0;
                    int PowerLevel = -1;
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                        var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + link.BungieMembershipType + "/Profile/" + link.BungieMembershipID + "/?components=100,200,202").Result;
                        var content = response.Content.ReadAsStringAsync().Result;
                        dynamic item = JsonConvert.DeserializeObject(content);

                        //first 100 levels: 4095505052
                        //anything after: 1531004716
                        try
                        {
                            for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                            {
                                string charId = $"{item.Response.profile.data.characterIds[i]}";
                                int powerLevelComp = (int)item.Response.characters.data[charId].light;
                                if (PowerLevel <= powerLevelComp)
                                    PowerLevel = powerLevelComp;
                            }

                            if (item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"4095505052"].level == 100)
                            {
                                int extraLevel = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"1531004716"].level;
                                Level = 100 + extraLevel;
                            }
                            else
                            {
                                Level = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"4095505052"].level;
                            }
                        }
                        catch
                        {
                            // Continue with the rest of the linked users. Don't want to stop the populating for one problematic account.
                            LogHelper.ConsoleLog($"Error while pulling data for user: {_client.GetUserAsync(link.DiscordID).Result.Username}#{_client.GetUserAsync(link.DiscordID).Result.Discriminator} linked with {link.UniqueBungieName}.");
                            continue;
                        }
                    }
                    // Populate List
                    LevelData.LevelDataEntries.Add(new LevelData.LevelDataEntry()
                    {
                        LastLoggedLevel = Level,
                        UniqueBungieName = link.UniqueBungieName,
                    });
                    PowerLevelData.PowerLevelDataEntries.Add(new PowerLevelData.PowerLevelDataEntry()
                    {
                        PowerLevel = PowerLevel,
                        UniqueBungieName = link.UniqueBungieName,
                    });
                    await Task.Delay(250);
                }
                LevelData.UpdateEntriesConfig();
                LogHelper.ConsoleLog($"Data pulling complete!");
            }
            catch
            {
                LogHelper.ConsoleLog($"Error while updating leaderboards, trying again at next refresh.");
            }
        }

        private async Task<ActiveConfig.ActiveAFKUser> RefreshSpecificUser(ActiveConfig.ActiveAFKUser aau)
        {
            await Task.Delay(15000);
            LogHelper.ConsoleLog($"Refreshing Bungie API specifically for {aau.UniqueBungieName}.");
            ActiveConfig.ActiveAFKUser tempAau = aau;
            try
            {
                int updatedLevel = DataConfig.GetUserSeasonPassLevel(tempAau.DiscordID, out int updatedProgression);

                if (updatedLevel > aau.LastLoggedLevel)
                {
                    await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Level up detected: {tempAau.LastLoggedLevel} -> {updatedLevel}");

                    tempAau.LastLoggedLevel = updatedLevel;
                    tempAau.LastLevelProgress = updatedProgression;

                    await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel,
                            $"Start: {tempAau.StartLevel} ({String.Format("{0:n0}", tempAau.StartLevelProgress)}/100,000 XP). Now: {tempAau.LastLoggedLevel} ({String.Format("{0:n0}", tempAau.LastLevelProgress)}/100,000 XP).");
                }
                else if (updatedProgression == aau.LastLevelProgress)
                {
                    await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Potential wipe detected.");
                    await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"<@{tempAau.DiscordID}>: Logging terminated by automation. Here is your session summary:", ThrallwayHelper.GenerateSessionSummary(tempAau), ThrallwayHelper.GenerateDeleteChannelButton());

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
                    await LogHelper.Log(user.CreateDMChannelAsync().Result, $"<@{tempAau.DiscordID}>: Potential wipe detected. Logging will be terminated for {tempAau.UniqueBungieName}.");
                    await LogHelper.Log(user.CreateDMChannelAsync().Result, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.TimeStarted)}.", ThrallwayHelper.GenerateSessionSummary(tempAau));

                    LogHelper.ConsoleLog($"Stopped logging for {tempAau.UniqueBungieName} via automation.");

                    await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau));
                    await UpdateBotActivity(0);
                    return null;
                }
                else if (updatedProgression < aau.LastLevelProgress)
                {
                    await LogHelper.Log(_client.GetChannelAsync(aau.DiscordChannelID).Result as ITextChannel, $"XP Value was less than before, waiting for next refresh.");
                }
                else
                {
                    await LogHelper.Log(_client.GetChannelAsync(aau.DiscordChannelID).Result as ITextChannel, $"Refreshed! Progress for {tempAau.UniqueBungieName} (Level: {updatedLevel}): {String.Format("{0:n0}", tempAau.LastLevelProgress)} XP -> {String.Format("{0:n0}", updatedProgression)} XP");
                    tempAau.LastLoggedLevel = updatedLevel;
                    tempAau.LastLevelProgress = updatedProgression;
                }

                LogHelper.ConsoleLog($"API Refreshed for {tempAau.UniqueBungieName}!");

                return tempAau;
            }
            catch (Exception x)
            {
                LogHelper.ConsoleLog($"Refresh for {tempAau.UniqueBungieName} failed!");
                await LogHelper.Log(_client.GetChannelAsync(aau.DiscordChannelID).Result as ITextChannel, $"Exception found: {x}");
                return null;
            }
        }
        #endregion

        private async Task InitializeListeners()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _interaction.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.MessageReceived += HandleMessageAsync;
            _client.InteractionCreated += HandleInteraction;

            // This tells us how to build slash commands.
            _client.Ready += async () =>
            {
                //await _interaction.RegisterCommandsToGuildAsync(915020047154565220, true);
                var guild = _client.GetGuild(915020047154565220);
                await guild.DeleteApplicationCommandsAsync();
                //await _interaction.RegisterCommandsGloballyAsync(true);
            };

            _interaction.SlashCommandExecuted += SlashCommandExecuted;

            _client.SelectMenuExecuted += SelectMenuHandler;
        }

        private async Task SlashCommandExecuted(SlashCommandInfo info, Discord.IInteractionContext context, Discord.Interactions.IResult result)
        {
            if (!result.IsSuccess)
            {
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        await context.Interaction.RespondAsync("You do not have the necessary permissions to run this command.", ephemeral: true);
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

            if (trackerType.Equals("altars-of-sorrow"))
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
                if (LostSectorRotation.GetUserTracking(interaction.User.Id, out var LS, out var LSD, out var EAT) == null)
                {
                    await interaction.RespondAsync($"No Lost Sector tracking enabled.", ephemeral: true);
                    return;
                }
                LostSectorRotation.RemoveUserTracking(interaction.User.Id);
                if (LS == null && LSD == null && EAT == null)
                    await interaction.RespondAsync($"An error has occurred.", ephemeral: true);
                else if (LS != null && LSD == null && EAT == null)
                    await interaction.RespondAsync($"Removed your Lost Sector tracking, you will not be notified when {LostSectorRotation.GetLostSectorString((LostSector)LS)} is available.", ephemeral: true);
                else if (LS != null && LSD != null && EAT == null)
                    await interaction.RespondAsync($"Removed your Lost Sector tracking, you will not be notified when {LostSectorRotation.GetLostSectorString((LostSector)LS)} ({LSD}) is available.", ephemeral: true);
                else if (LS == null && LSD == null && EAT != null)
                    await interaction.RespondAsync($"Removed your Lost Sector tracking, you will not be notified when Lost Sectors are dropping {EAT}.", ephemeral: true);
                else if (LS == null && LSD != null && EAT != null)
                    await interaction.RespondAsync($"Removed your Lost Sector tracking, you will not be notified when {LSD} Lost Sectors are dropping {EAT}.", ephemeral: true);
                else if (LS != null && LSD != null && EAT != null)
                    await interaction.RespondAsync($"Removed your Lost Sector tracking, you will not be notified when {LostSectorRotation.GetLostSectorString((LostSector)LS)} ({LSD}) is dropping {EAT}.", ephemeral: true);
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
                    await interaction.RespondAsync($"Removed your Nightfall tracking, you will not be notified when {NightfallRotation.GetStrikeNameString((Nightfall)NF)} is available.", ephemeral: true);
                else if (NF == null && NFWeapon != null)
                    await interaction.RespondAsync($"Removed your Nightfall tracking, you will not be notified when {NightfallRotation.GetWeaponString((NightfallWeapon)NFWeapon)} is available.", ephemeral: true);
                else if (NF != null && NFWeapon != null)
                    await interaction.RespondAsync($"Removed your Nightfall tracking, you will not be notified when {NightfallRotation.GetStrikeNameString((Nightfall)NF)} is dropping {NightfallRotation.GetWeaponString((NightfallWeapon)NFWeapon)}.", ephemeral: true);
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
        }

        private async Task HandleMessageAsync(SocketMessage arg)
        {
            if (arg.Author.IsWebhook || arg.Author.IsBot) return; // Return if message is from a Webhook or Bot user
            if (arg.ToString().Length < 0) return; // Return of the message has no text
            if (arg.Author.Id == _client.CurrentUser.Id) return; // Return of the message is from itself

            int argPos = 0; // Position to check for command arguments

            var msg = arg as SocketUserMessage;
            if (msg == null) return;

            if (msg.HasStringPrefix(BotConfig.DefaultCommandPrefix, ref argPos))
            {
                if (arg.Channel.GetType() == typeof(SocketDMChannel) && !BotConfig.BotStaffDiscordIDs.Contains(arg.Author.Id)) // Send message if received via a DM
                {
                    await arg.Channel.SendMessageAsync($"I do not accept commands through Direct Messages.");
                    return;
                }

                var handled = await TryHandleCommandAsync(msg, argPos).ConfigureAwait(false);
                if (handled) return;
            }
        }

        private async Task<bool> TryHandleCommandAsync(SocketUserMessage msg, int argPos)
        {
            var context = new SocketCommandContext(_client, msg);

            var result = await _commands.ExecuteAsync(context, argPos, _services);

            await UpdateBotActivity();

            if (result.Error.HasValue)
            {
                CommandError error = result.Error.Value;

                if (error == CommandError.UnknownCommand)
                    return true;
                else if (error == CommandError.UnmetPrecondition)
                {
                    await context.Channel.SendMessageAsync($"[{error}]: {result.ErrorReason}").ConfigureAwait(false);
                }
                else if (error == CommandError.BadArgCount)
                {
                    await context.Channel.SendMessageAsync($"[{error}]: Command is missing some arguments.").ConfigureAwait(false);
                }
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
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }
}