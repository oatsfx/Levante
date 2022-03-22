using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using Levante.Configs;
using System.Net.Http;
using Levante.Helpers;
using System.Threading;
using Levante.Leaderboards;
using Levante.Rotations;
using Levante.Util;
using Fergun.Interactive;
using Discord.Interactions;
using System.IO;
using System.Web;
using System.Net;
using APIHelper;

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

        private HttpListener _listener;

        public LevanteCord()
        {
            var scktConfig = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMessages | GatewayIntents.GuildMembers,
                //TotalShards = 2,
            };
            _client = new DiscordSocketClient(scktConfig);
            _commands = new CommandService();
            _interaction = new InteractionService(_client);
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton<InteractiveService>()
                .AddSingleton<InteractionService>()
                .BuildServiceProvider();

            _listener = new HttpListener();
            _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            //_listener.Prefixes.Add("https://*:42069/");
            _listener.Prefixes.Add("http://*:8080/");
            _listener.Prefixes.Add("https://oauth.levante.dev/");
            //_listener.Start();
            //_listener.BeginGetContext(new AsyncCallback(GetToken), _listener);
            Console.WriteLine($"[OAUTH 2.0] Listening...");
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

            await Task.Run(() => Console.Title = $"Levante v{BotConfig.Version:0.00}");

            if (!LeaderboardHelper.CheckAndLoadDataFiles())
                return;

            CurrentRotations.CreateJSONs();

            if (!API.FetchManifest(BotConfig.BungieApiKey))
            {
                // TODO: set up a task at a later date to refetch manifest
                // maybe make a boolean that disables manifest requiring commands if it's false
            }

            EmblemOffer.LoadCurrentOffers();

            Console.WriteLine($"[Startup] Current Bot Version: v{BotConfig.Version:0.00}");
            Console.WriteLine($"[Startup] Current Developer Note: {BotConfig.Note}");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[Rotations]");
            Console.WriteLine($"Legend/Master Lost Sector: {LostSectorRotation.GetLostSectorString(CurrentRotations.LostSector)} ({CurrentRotations.LostSectorArmorDrop})");
            Console.WriteLine($"Altar Weapon: {AltarsOfSorrowRotation.GetWeaponNameString(CurrentRotations.AltarWeapon)} ({CurrentRotations.AltarWeapon})");
            Console.WriteLine($"Wellspring ({WellspringRotation.GetWellspringTypeString(CurrentRotations.Wellspring)}): {WellspringRotation.GetWeaponNameString(CurrentRotations.Wellspring)} ({WellspringRotation.GetWellspringBossString(CurrentRotations.Wellspring)})");
            Console.WriteLine($"Last Wish Challenge: {LastWishRotation.GetEncounterString(CurrentRotations.LWChallengeEncounter)} ({LastWishRotation.GetChallengeString(CurrentRotations.LWChallengeEncounter)})");
            Console.WriteLine($"Garden of Salvation Challenge: {GardenOfSalvationRotation.GetEncounterString(CurrentRotations.GoSChallengeEncounter)} ({GardenOfSalvationRotation.GetChallengeString(CurrentRotations.GoSChallengeEncounter)})");
            Console.WriteLine($"Deep Stone Crypt Challenge: {DeepStoneCryptRotation.GetEncounterString(CurrentRotations.DSCChallengeEncounter)} ({DeepStoneCryptRotation.GetChallengeString(CurrentRotations.DSCChallengeEncounter)})");
            Console.WriteLine($"Vault of Glass Challenge: {VaultOfGlassRotation.GetEncounterString(CurrentRotations.VoGChallengeEncounter)} ({VaultOfGlassRotation.GetChallengeString(CurrentRotations.VoGChallengeEncounter)})");
            Console.WriteLine($"Vow of the Disiple Challenge: {VowOfTheDiscipleRotation.GetEncounterString(CurrentRotations.VowChallengeEncounter)} ({VowOfTheDiscipleRotation.GetChallengeString(CurrentRotations.VowChallengeEncounter)})");
            Console.WriteLine($"Curse Week: {CurrentRotations.CurseWeek}");
            Console.WriteLine($"Ascendant Challenge: {AscendantChallengeRotation.GetChallengeNameString(CurrentRotations.AscendantChallenge)} ({AscendantChallengeRotation.GetChallengeLocationString(CurrentRotations.AscendantChallenge)})");
            Console.WriteLine($"Nightfall: {NightfallRotation.GetStrikeNameString(CurrentRotations.Nightfall)} (dropping {NightfallRotation.GetWeaponString(CurrentRotations.NightfallWeaponDrop)})");
            Console.WriteLine($"Empire Hunt: {EmpireHuntRotation.GetHuntNameString(CurrentRotations.EmpireHunt)}");
            Console.WriteLine($"Nightmare Hunts: {CurrentRotations.NightmareHunts[0]}/{CurrentRotations.NightmareHunts[1]}/{CurrentRotations.NightmareHunts[2]}");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;

            var client = _services.GetRequiredService<DiscordSocketClient>();
            var commands = _services.GetRequiredService<InteractionService>();

            client.Log += log =>
            {
                Console.WriteLine(log.ToString());
                return Task.CompletedTask;
            };

            Timer xpTimer = new Timer(XPTimerCallback, null, 20000, BotConfig.TimeBetweenRefresh * 60000);
            Timer leaderboardTimer = new Timer(LeaderboardTimerCallback, null, 30000, 600000);

            if (DateTime.Now.Hour >= 10) // after daily reset
                SetUpTimer(new DateTime(DateTime.Today.AddDays(1).Year, DateTime.Today.AddDays(1).Month, DateTime.Today.AddDays(1).Day, 10, 0, 0));
            else
                SetUpTimer(new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 10, 0, 0));

            await InitializeListeners();

            await _client.LoginAsync(TokenType.Bot, BotConfig.DiscordToken);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task ListenLoop(HttpListener listener)
        {
            /*var context = await listener.GetContextAsync();
            var query = context.Request.QueryString;
            if (query != null && query.Count > 0)
            {
                if (!string.IsNullOrEmpty(query["code"]))
                {
                    Console.WriteLine($"{query["code"]}");
                }
                else if (!string.IsNullOrEmpty(query["error"]))
                {
                    Console.WriteLine(string.Format("{0}: {1}", query["error"], query["error_description"]));
                }
            }*/
            
        }

        public async void GetToken(IAsyncResult ar)
        {
            //var url = HttpUtility.UrlEncode(@"http://localhost:60403");
            //System.Diagnostics.Process.Start(url);
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            // Note: The GetContext method blocks while waiting for a request.
            HttpListenerContext context = _listener.EndGetContext(ar);

            var query = context.Request.QueryString;
            if (query != null && query.Count > 0)
            {
                if (!string.IsNullOrEmpty(query["code"]))
                {
                    Console.WriteLine($"{query["code"]}");
                }
                else if (!string.IsNullOrEmpty(query["error"]))
                {
                    Console.WriteLine(string.Format("{0}: {1}", query["error"], query["error_description"]));
                }
            }

            // Wait for next
            _listener.BeginGetContext(new AsyncCallback(GetToken), _listener);
            Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff") + " Handling request");

            HttpListenerRequest request = context.Request;
            // Obtain a response object.
            HttpListenerResponse response = context.Response;
            // Construct a response.
            string responseString = @"
   __                      __     
  / /  ___ _  _____ ____  / /____ 
 / /__/ -_) |/ / _ `/ _ \/ __/ -_)
/____/\__/|___/\_,_/_//_/\__/\__/   dev. by @OatsFX
            ";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            response.Redirect("https://www.levante.dev/");

            // simulate work
            Thread.Sleep(3000);

            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // You must close the output stream.
            output.Close();
            Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff") + " completed");
        }

        private async Task UpdateBotActivity(int SetRNG = -1)
        {
            int RNG = 0;
            int RNGMax = 10;
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
                    string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "'s" : "s'";
                    await _client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers} Player{s} XP", ActivityType.Watching)); break;
                case 1:
                    await _client.SetActivityAsync(new Game($"{BotConfig.Note} | v{String.Format("{0:0.00#}", BotConfig.Version)}", ActivityType.Playing)); break;
                case 2:
                    await _client.SetActivityAsync(new Game($"for /help | v{String.Format("{0:0.00#}", BotConfig.Version)}", ActivityType.Watching)); break;
                case 3:
                    await _client.SetActivityAsync(new Game($"{_client.Guilds.Count} Servers | v{String.Format("{0:0.00#}", BotConfig.Version)}", ActivityType.Watching)); break;
                case 4:
                    await _client.SetActivityAsync(new Game($"{_client.Guilds.Sum(x => x.MemberCount):n0} Users | v{String.Format("{0:0.00#}", BotConfig.Version)}", ActivityType.Watching)); break;
                case 5:
                    await _client.SetActivityAsync(new Game($"{DataConfig.DiscordIDLinks.Count} Linked Users | v{String.Format("{0:0.00#}", BotConfig.Version)}", ActivityType.Watching)); break;
                case 6:
                    await _client.SetActivityAsync(new Game($"{CurrentRotations.GetTotalLinks()} Rotation Trackers | v{String.Format("{0:0.00#}", BotConfig.Version)}", ActivityType.Watching)); break;
                case 7:
                    await _client.SetActivityAsync(new Game($"levante.dev | v{String.Format("{0:0.00#}", BotConfig.Version)}", ActivityType.Watching)); break;
                case 8:
                    await _client.SetActivityAsync(new Game($"@Levante_Bot on Twitter", ActivityType.Watching)); break;
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

        private async void XPTimerCallback(Object o) => await RefreshBungieAPI().ConfigureAwait(false);

        private async void LeaderboardTimerCallback(Object o) => await LoadLeaderboards().ConfigureAwait(false);

        #region XPLogging
        private async Task RefreshBungieAPI()
        {
            if (ActiveConfig.ActiveAFKUsers.Count <= 0)
            {
                LogHelper.ConsoleLog($"Skipping refresh, no active AFK users...");
                return;
            }

            LogHelper.ConsoleLog($"Refreshing Bungie API...");
            List<ActiveConfig.ActiveAFKUser> listOfRemovals = new List<ActiveConfig.ActiveAFKUser>();
            //List<ActiveConfig.ActiveAFKUser> newList = new List<ActiveConfig.ActiveAFKUser>();
            try // XP Logs
            {
                LogHelper.ConsoleLog($"Refreshing XP Logging Users...");
                foreach (ActiveConfig.ActiveAFKUser aau in ActiveConfig.ActiveAFKUsers.ToList())
                {
                    ActiveConfig.ActiveAFKUser tempAau = aau;
                    int updatedLevel = DataConfig.GetAFKValues(tempAau.DiscordID, out int updatedProgression, out bool isPlaying, out string errorStatus);

                    if (!errorStatus.Equals("Success") && !errorStatus.Equals("PlayerNotOnline"))
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Refresh unsuccessful. Reason: {errorStatus}.");
                        LogHelper.ConsoleLog($"Refresh unsuccessful for {tempAau.UniqueBungieName}. Reason: {errorStatus}.");
                        // Move onto the next user so everyone gets the message.
                        //newList.Add(tempAau);
                        continue;
                    }

                    LogHelper.ConsoleLog($"Checking {tempAau.UniqueBungieName}.");

                    if (!isPlaying)
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

                        LogHelper.ConsoleLog($"Stopped logging for {tempAau.UniqueBungieName} via automation.");
                        //listOfRemovals.Add(tempAau);
                        //ActiveConfig.DeleteActiveUserFromConfig(tempAau.DiscordID);
                        ActiveConfig.ActiveAFKUsers.Remove(ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID));
                        await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau));
                    }
                    else if (updatedLevel > tempAau.LastLoggedLevel)
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Level up detected: {tempAau.LastLoggedLevel} -> {updatedLevel}");

                        ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID).LastLoggedLevel = updatedLevel;
                        ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID).LastLevelProgress = updatedProgression;
                        ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID).NoXPGainRefreshes = 0;
                        //tempAau.LastLoggedLevel = updatedLevel;
                        //tempAau.LastLevelProgress = updatedProgression;
                        //tempAau.NoXPGainRefreshes = 0;
                        //newList.Add(tempAau);

                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, 
                            $"Start: {tempAau.StartLevel} ({String.Format("{0:n0}", tempAau.StartLevelProgress)}/100,000 XP). Now: {tempAau.LastLoggedLevel} ({String.Format("{0:n0}", tempAau.LastLevelProgress)}/100,000 XP)");
                    }
                    else if (updatedProgression <= tempAau.LastLevelProgress)
                    {
                        if (tempAau.NoXPGainRefreshes >= ActiveConfig.RefreshesBeforeKick)
                        {
                            string uniqueName = tempAau.UniqueBungieName;

                            await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Player has been determined as inactive.");
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
                            await LogHelper.Log(user.CreateDMChannelAsync().Result, $"<@{tempAau.DiscordID}>: Player has been determined as inactive. Logging will be terminated for {uniqueName}.");
                            await LogHelper.Log(user.CreateDMChannelAsync().Result, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.TimeStarted)}.", XPLoggingHelper.GenerateSessionSummary(tempAau, _client.CurrentUser.GetAvatarUrl()));

                            LogHelper.ConsoleLog($"Stopped logging for {tempAau.UniqueBungieName} via automation.");
                            //listOfRemovals.Add(tempAau);
                            // ***Change to remove it from list because file update is called at end of method.***
                            //ActiveConfig.DeleteActiveUserFromConfig(tempAau.DiscordID);
                            ActiveConfig.ActiveAFKUsers.Remove(ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID));
                            await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau));
                        }
                        else
                        {
                            ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID).NoXPGainRefreshes = tempAau.NoXPGainRefreshes + 1;
                            //tempAau.NoXPGainRefreshes = tempAau.NoXPGainRefreshes + 1;
                            //newList.Add(tempAau);
                            await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"No XP change detected, waiting for next refresh... Warning {tempAau.NoXPGainRefreshes} of {ActiveConfig.RefreshesBeforeKick}.");
                        }
                    }
                    else
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Refreshed! Progress for {tempAau.UniqueBungieName} (Level: {updatedLevel}): {String.Format("{0:n0}", tempAau.LastLevelProgress)} XP -> {String.Format("{0:n0}", updatedProgression)} XP");

                        ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID).LastLoggedLevel = updatedLevel;
                        ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID).LastLevelProgress = updatedProgression;
                        ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordID == tempAau.DiscordID).NoXPGainRefreshes = 0;
                        //tempAau.LastLoggedLevel = updatedLevel;
                        //tempAau.LastLevelProgress = updatedProgression;
                        //tempAau.NoXPGainRefreshes = 0;
                        //newList.Add(tempAau);
                    }
                    await Task.Delay(3250); // we dont want to spam API if we have a ton of AFK subscriptions
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

                LogHelper.ConsoleLog($"Bungie API Refreshed!");
            }
            catch (Exception x)
            {
                LogHelper.ConsoleLog($"Refresh failed, trying again! Reason: {x.Message} ({x.StackTrace})");
                await Task.Delay(8000);
                await RefreshBungieAPI().ConfigureAwait(false);
                return;
            }
            await UpdateBotActivity();
        }

        private async Task LoadLeaderboards()
        {
            try
            {
                LogHelper.ConsoleLog($"Pulling data for leaderboards...");
                var tempPowerLevelData = new PowerLevelData();
                var tempLevelData = new LevelData();
                bool nameChange = false;
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

                        //first 100 levels: 4095505052 (S15); 2069932355 (S16)
                        //anything after: 1531004716 (S15): 1787069365 (S16)
                        try
                        {
                            for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                            {
                                string charId = $"{item.Response.profile.data.characterIds[i]}";
                                int powerLevelComp = (int)item.Response.characters.data[charId].light;
                                if (PowerLevel <= powerLevelComp)
                                    PowerLevel = powerLevelComp;
                            }

                            if (item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"2069932355"].level == 100)
                            {
                                int extraLevel = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"1787069365"].level;
                                Level = 100 + extraLevel;
                            }
                            else
                            {
                                Level = item.Response.characterProgressions.data[$"{item.Response.profile.data.characterIds[0]}"].progressions[$"2069932355"].level;
                            }

                            // System to update names in case players do change name.
                            string name = $"{item.Response.profile.data.userInfo.bungieGlobalDisplayName}";
                            string nameCode = int.Parse($"{item.Response.profile.data.userInfo.bungieGlobalDisplayNameCode}").ToString().PadLeft(4, '0');
                            if (!link.UniqueBungieName.Equals($"{name}#{nameCode}"))
                            {
                                Console.WriteLine($"Name change detected: {link.UniqueBungieName} -> {name}#{nameCode}");
                                DataConfig.DiscordIDLinks.FirstOrDefault(x => x.DiscordID == link.DiscordID).UniqueBungieName = $"{name}#{nameCode}";
                                nameChange = true;
                            }
                        }
                        catch
                        {
                            // Continue with the rest of the linked users. Don't want to stop the populating for one problematic account.
                            LogHelper.ConsoleLog($"Error while pulling data for user: {_client.GetUserAsync(link.DiscordID).Result.Username}#{_client.GetUserAsync(link.DiscordID).Result.Discriminator} linked with {link.UniqueBungieName}.");
                            await Task.Delay(250);
                            continue;
                        }
                    }
                    // Populate List
                    tempLevelData.LevelDataEntries.Add(new LevelData.LevelDataEntry()
                    {
                        LastLoggedLevel = Level,
                        UniqueBungieName = link.UniqueBungieName,
                    });
                    tempPowerLevelData.PowerLevelDataEntries.Add(new PowerLevelData.PowerLevelDataEntry()
                    {
                        PowerLevel = PowerLevel,
                        UniqueBungieName = link.UniqueBungieName,
                    });
                    await Task.Delay(250);
                }
                if (nameChange)
                    DataConfig.UpdateConfig();

                tempLevelData.UpdateEntriesConfig();
                tempPowerLevelData.UpdateEntriesConfig();
                LogHelper.ConsoleLog($"Data pulling complete!");
            }
            catch
            {
                LogHelper.ConsoleLog($"Error while updating leaderboards, trying again at next refresh.");
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
                //await _interaction.RegisterCommandsToGuildAsync(397846250797662208, true);
                //var guild = _client.GetGuild(933971948965359626);
                //await guild.DeleteApplicationCommandsAsync();
                await _interaction.RegisterCommandsGloballyAsync(true);
                //await _client.Rest.DeleteAllGlobalCommandsAsync();
                await UpdateBotActivity(1);
            };

            _interaction.SlashCommandExecuted += SlashCommandExecuted;

            _client.SelectMenuExecuted += SelectMenuHandler;
            LevanteCordInstance.Client = _client;
        }

        private async Task SlashCommandExecuted(SlashCommandInfo info, IInteractionContext context, Discord.Interactions.IResult result)
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
            await UpdateBotActivity();
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
                if (LostSectorRotation.GetUserTracking(interaction.User.Id, out var LS, out var EAT) == null)
                {
                    await interaction.RespondAsync($"No Lost Sector tracking enabled.", ephemeral: true);
                    return;
                }
                LostSectorRotation.RemoveUserTracking(interaction.User.Id);
                if (LS == null && EAT == null)
                    await interaction.RespondAsync($"An error has occurred.", ephemeral: true);
                else if (LS != null && EAT == null)
                    await interaction.RespondAsync($"Removed your Lost Sector tracking, you will not be notified when {LostSectorRotation.GetLostSectorString((LostSector)LS)} is available.", ephemeral: true);
                else if (LS == null &&EAT != null)
                    await interaction.RespondAsync($"Removed your Lost Sector tracking, you will not be notified when Lost Sectors are dropping {EAT}.", ephemeral: true);
                else if (LS != null && EAT != null)
                    await interaction.RespondAsync($"Removed your Lost Sector tracking, you will not be notified when {LostSectorRotation.GetLostSectorString((LostSector)LS)} is dropping {EAT}.", ephemeral: true);
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

                if (BotConfig.BotStaffDiscordIDs.Contains(arg.Author.Id))
                {
                    var handled = await TryHandleCommandAsync(msg, argPos).ConfigureAwait(false);
                    if (handled) return;
                }
                else
                {
                    var embed = new EmbedBuilder()
                    .WithDescription($"All text-based commands, similar to this one, have been migrated to Slash Commands. This warning will be removed in April 2022.")
                    .WithColor(new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B));
                    await msg.ReplyAsync(embed: embed.Build());
                    return;
                }
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