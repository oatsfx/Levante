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
using DestinyUtility.Configs;
using System.Net.Http;
using DestinyUtility.Helpers;
using System.ComponentModel;
using System.Threading;
using Discord.Rest;
using Discord.Net;
using DestinyUtility.Leaderboards;
using DestinyUtility.Rotations;

namespace DestinyUtility
{
    public sealed class DestinyUtilityCord
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        public static readonly string LostSectorTrackingConfigPath = @"Configs/lostSectorTrackingConfigPath.json";

        public static readonly string LevelDataPath = @"Data/levelData.json";
        public static readonly string XPPerHourDataPath = @"Data/xpPerHourData.json";
        public static readonly string MostThrallwayTimeDataPath = @"Data/mostThrallwayTimeData.json";
        public static readonly string LongestSessionDataPath = @"Data/longestSessionData.json";

        private Timer DailyResetTimer;

        public DestinyUtilityCord()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection()
                .AddSingleton(_client)
                //.AddSingleton<InteractiveService>()
                .BuildServiceProvider();
        }

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            string ASCIIName = @"
   ___          __  _           __  ____  _ ___ __      
  / _ \___ ___ / /_(_)__  __ __/ / / / /_(_) (_) /___ __
 / // / -_|_-</ __/ / _ \/ // / /_/ / __/ / / / __/ // /
/____/\__/___/\__/_/_//_/\_, /\____/\__/_/_/_/\__/\_, / 
    - dev. by @OatsFX   /___/                    /___/  
";
            Console.WriteLine(ASCIIName);
            
            new DestinyUtilityCord().StartAsync().GetAwaiter().GetResult();
        }

        public async Task StartAsync()
        {
            if (!ConfigHelper.CheckAndLoadConfigFiles())
                return;

            if (!CheckAndLoadDataFiles())
                return;

            Console.Title = $"DestinyUtility v{BotConfig.Version}";
            Console.WriteLine($"Current Bot Version: v{BotConfig.Version}");
            Console.WriteLine($"Current Developer Note: {BotConfig.Note}");

            Console.WriteLine($"Legend Lost Sector: {LostSectorRotation.GetLostSectorString(LostSectorRotation.CurrentLegendLostSector)} ({LostSectorRotation.CurrentLegendArmorDrop})");
            Console.WriteLine($"Master Lost Sector: {LostSectorRotation.GetLostSectorString(LostSectorRotation.GetMasterLostSector())} ({LostSectorRotation.GetMasterLostSectorArmorDrop()})");

            _client.Log += log =>
            {
                Console.WriteLine(log.ToString());
                return Task.CompletedTask;
            };

            Timer timer = new Timer(TimerCallback, null, 25000, 240000);

            if (DateTime.Now.Hour >= 10) // after daily reset
            {
                SetUpTimer(new DateTime(DateTime.Today.AddDays(1).Year, DateTime.Today.AddDays(1).Month, DateTime.Today.AddDays(1).Day, 10, 0, 0));
            }
            else
            {
                SetUpTimer(new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 10, 0, 0));
            }

            await InitializeCommands().ConfigureAwait(false);

            await _client.LoginAsync(TokenType.Bot, BotConfig.DiscordToken).ConfigureAwait(false);
            await _client.StartAsync().ConfigureAwait(false);

            await UpdateBotActivity();

            await Task.Delay(-1);
        }
        
        private async Task UpdateBotActivity()
        {
            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "" : "s";
            await _client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumThrallwayUsers} Thrallway Farmer{s}", ActivityType.Watching));
        }

        private void SetUpTimer(DateTime alertTime)
        {
            // this is called to get the timer set up to run at every daily reset
            TimeSpan timeToGo = new TimeSpan(alertTime.Ticks - DateTime.Now.Ticks);
            Console.WriteLine($"Time until Daily Reset: {timeToGo.Days:00}:{timeToGo.Hours:00}:{timeToGo.Minutes:00}:{timeToGo.Seconds:00}");
            DailyResetTimer = new Timer(DailyResetChanges, null, (long)timeToGo.TotalMilliseconds, Timeout.Infinite);
        }

        private async Task PostDailyResetUpdate()
        {
            foreach (ulong ChannelID in LostSectorRotation.AnnounceLostSectorUpdates)
            {
                var channel = _client.GetChannel(ChannelID) as SocketTextChannel;

                await channel.SendMessageAsync($"", embed: CurrentRotations.DailyResetEmbed(_client).Result.Build());
            }
        }

        public async void DailyResetChanges(Object o = null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nDaily Reset Occurred.");
            LostSectorRotation.LostSectorChange();

            Console.WriteLine($"Legend Lost Sector: {LostSectorRotation.GetLostSectorString(LostSectorRotation.CurrentLegendLostSector)} ({LostSectorRotation.CurrentLegendArmorDrop})");
            Console.WriteLine($"Master Lost Sector: {LostSectorRotation.GetLostSectorString(LostSectorRotation.GetMasterLostSector())} ({LostSectorRotation.GetMasterLostSectorArmorDrop()})");

            // Soon to be depreciated.
            //await LostSectorTrackingConfig.PostLostSectorUpdate(_client);

            // Taking over lost sector updates.
            await PostDailyResetUpdate();

            // Send users their tracking if applicable.
            List<LostSectorRotation.LostSectorLink> temp = new List<LostSectorRotation.LostSectorLink>();
            foreach (var LSL in LostSectorRotation.LostSectorLinks)
            {
                bool addBack = true;
                if (LostSectorRotation.CurrentLegendLostSector == LSL.LostSector && LSL.Difficulty == LostSectorDifficulty.Legend)
                {
                    if (LostSectorRotation.CurrentLegendArmorDrop == LSL.ArmorDrop)
                    {
                        IUser user;
                        if (_client.GetUser(LSL.DiscordID) == null)
                        {
                            var _rClient = _client.Rest;
                            user =  _rClient.GetUserAsync(LSL.DiscordID).Result;
                        }
                        else
                        {
                            user = _client.GetUser(LSL.DiscordID);
                        }

                        await user.SendMessageAsync($"Hey {user.Mention}!\n" +
                            $"{LostSectorRotation.GetLostSectorString(LSL.LostSector)} ({LSL.Difficulty}) is dropping {LSL.ArmorDrop} today! I have removed your tracking, good luck!");
                        addBack = false;
                    }
                }
                else if (LostSectorRotation.GetMasterLostSector() == LSL.LostSector && LSL.Difficulty == LostSectorDifficulty.Master)
                {
                    if (LostSectorRotation.GetMasterLostSectorArmorDrop() == LSL.ArmorDrop)
                    {
                        IUser user;
                        if (_client.GetUser(LSL.DiscordID) == null)
                        {
                            var _rClient = _client.Rest;
                            user = _rClient.GetUserAsync(LSL.DiscordID).Result;
                        }
                        else
                        {
                            user = _client.GetUser(LSL.DiscordID);
                        }

                        await user.SendMessageAsync($"Hey {user.Mention}!\n" +
                            $"{LostSectorRotation.GetLostSectorString(LSL.LostSector)} ({LSL.Difficulty}) is dropping {LSL.ArmorDrop} today! I have removed your tracking, good luck!");
                        addBack = false;
                    }
                }
                if (addBack)
                    temp.Add(LSL);
            }

            string json = File.ReadAllText(LostSectorTrackingConfigPath);
            LostSectorRotation lstConfig = JsonConvert.DeserializeObject<LostSectorRotation>(json);

            LostSectorRotation.UpdateLostSectorsList();
            LostSectorRotation.LostSectorLinks = temp;
            string output = JsonConvert.SerializeObject(lstConfig, Formatting.Indented);
            File.WriteAllText(LostSectorTrackingConfigPath, output);

            // start the next timer
            SetUpTimer(new DateTime(DateTime.Today.AddDays(1).Year, DateTime.Today.AddDays(1).Month, DateTime.Today.AddDays(1).Day, 10, 0, 0));
            Console.ForegroundColor = ConsoleColor.Cyan;
        }

        private async void TimerCallback(Object o) => await RefreshBungieAPI();

        private bool IsBungieAPIDown()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);
                var response = client.GetAsync("https://www.bungie.net/platform/Destiny2/SearchDestinyPlayer/3/" + Uri.EscapeDataString("OatsFX#5630")).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                if (item == null) return true;

                string status = item.ErrorStatus;

                return !status.Equals("Success");
            }
        }

        #region ThrallwayLogging
        private async Task RefreshBungieAPI()
        {
            if (ActiveConfig.ActiveAFKUsers.Count <= 0)
            {
                Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Skipping refresh, no active AFK users...");
                await LoadLeaderboards();
                return;
            }

            if (IsBungieAPIDown())
            {
                Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Skipping refresh, Bungie API is down...");
                foreach (ActiveConfig.ActiveAFKUser aau in ActiveConfig.ActiveAFKUsers)
                {
                    await LogHelper.Log(_client.GetChannelAsync(aau.DiscordChannelID).Result as ITextChannel, $"Refresh denied. Bungie API is temporarily down.");
                }
                return;
            }

            Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Refreshing Bungie API...");
            List<ActiveConfig.ActiveAFKUser> temp = new List<ActiveConfig.ActiveAFKUser>();
            List<ActiveConfig.ActiveAFKUser> refreshAgainList = new List<ActiveConfig.ActiveAFKUser>();
            try // thrallway
            {
                Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Refreshing Thrallway Users...");
                foreach (ActiveConfig.ActiveAFKUser aau in ActiveConfig.ActiveAFKUsers)
                {
                    ActiveConfig.ActiveAFKUser tempAau = aau;
                    int updatedLevel = DataConfig.GetAFKValues(tempAau.DiscordID, out int updatedProgression, out bool isInShatteredThrone);
                    bool addBack = true;

                    Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Checking user: {tempAau.UniqueBungieName}");

                    if (!isInShatteredThrone)
                    {
                        string uniqueName = tempAau.UniqueBungieName;

                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Player {uniqueName} is no longer in Shattered Throne.");
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"<@{tempAau.DiscordID}>: Logging terminated by automation. Here is your session summary:", GenerateSessionSummary(tempAau).Result, GenerateDeleteChannelButton());

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
                        await LogHelper.Log(user.CreateDMChannelAsync().Result, $"<@{tempAau.DiscordID}>: Player {uniqueName} is no longer in Shattered Throne. Logging will be terminated for {uniqueName}.");
                        //await (_client.GetChannel(tempAau.DiscordChannelID) as SocketGuildChannel).DeleteAsync();

                        Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Stopped logging for {tempAau.UniqueBungieName}.");

                        await Task.Run(() => CheckLeaderboardData(tempAau));
                        addBack = false;
                    }
                    else if (updatedLevel > tempAau.LastLoggedLevel)
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Level up detected. {tempAau.LastLoggedLevel} -> {updatedLevel}");

                        tempAau.LastLoggedLevel = updatedLevel;
                        tempAau.LastLevelProgress = updatedProgression;

                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, 
                            $"Start: {tempAau.StartLevel} ({String.Format("{0:n0}", tempAau.StartLevelProgress)}/100,000 XP). Now: {tempAau.LastLoggedLevel} ({String.Format("{0:n0}", tempAau.LastLevelProgress)}/100,000 XP)");
                    }
                    else if (updatedProgression <= tempAau.LastLevelProgress)
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"No XP change detected, attempting to refresh API again...");
                        tempAau = await RefreshSpecificUser(tempAau).ConfigureAwait(false);
                        if (tempAau == null) addBack = false;
                    }
                    else
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Refreshed! Progress for {tempAau.UniqueBungieName} (Level: {updatedLevel}): {String.Format("{0:n0}", tempAau.LastLevelProgress)} XP -> {String.Format("{0:n0}", updatedProgression)} XP");

                        tempAau.LastLoggedLevel = updatedLevel;
                        tempAau.LastLevelProgress = updatedProgression;
                    }
                    if (addBack)
                        temp.Add(tempAau);

                    await Task.Delay(3500); // we dont want to spam API if we have a ton of AFK subscriptions
                }

                string json = File.ReadAllText(ActiveConfig.FilePath);
                ActiveConfig aConfig = JsonConvert.DeserializeObject<ActiveConfig>(json);

                ActiveConfig.UpdateActiveAFKUsersList();
                ActiveConfig.ActiveAFKUsers = temp;
                string output = JsonConvert.SerializeObject(aConfig, Formatting.Indented);
                File.WriteAllText(ActiveConfig.FilePath, output);

                await UpdateBotActivity();
                Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Bungie API Refreshed!");
            }
            catch (Exception x)
            {
                Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Refresh failed, trying again! Reason: {x.Message}");
                await Task.Delay(8000);
                await RefreshBungieAPI();
            }

            // data loading
            await Task.Delay(25000); // wait to prevent numerous API calls
            await LoadLeaderboards();
        }

        private void CheckLeaderboardData(ActiveConfig.ActiveAFKUser AAU)
        {
            
            // Generate a Leaderboard entry, and overwrite if the existing one is worse.
            if (XPPerHourData.IsExistingLinkedEntry(AAU.UniqueBungieName))
            {
                var entry = XPPerHourData.GetExistingLinkedEntry(AAU.UniqueBungieName);

                int xpPerHour = 0;
                if ((DateTime.Now - AAU.TimeStarted).TotalHours >= 1)
                    xpPerHour = (int)Math.Floor((((AAU.LastLoggedLevel - AAU.StartLevel) * 100000) - AAU.StartLevelProgress + AAU.LastLevelProgress) / (DateTime.Now - AAU.TimeStarted).TotalHours);
                
                // Only add back if the entry is better than their previous.
                if (xpPerHour > entry.XPPerHour)
                {
                    XPPerHourData.DeleteEntryFromConfig(AAU.UniqueBungieName);
                    XPPerHourData.AddEntryToConfig(new XPPerHourData.XPPerHourEntry()
                    {
                        XPPerHour = xpPerHour,
                        UniqueBungieName = AAU.UniqueBungieName
                    });
                }
            }
            else
            {
                int xpPerHour = 0;
                if ((DateTime.Now - AAU.TimeStarted).TotalHours >= 1)
                    xpPerHour = (int)Math.Floor((((AAU.LastLoggedLevel - AAU.StartLevel) * 100000) - AAU.StartLevelProgress + AAU.LastLevelProgress) / (DateTime.Now - AAU.TimeStarted).TotalHours);

                XPPerHourData.AddEntryToConfig(new XPPerHourData.XPPerHourEntry()
                {
                    XPPerHour = xpPerHour,
                    UniqueBungieName = AAU.UniqueBungieName
                });
            }

            if (LongestSessionData.IsExistingLinkedEntry(AAU.UniqueBungieName))
            {
                var entry = LongestSessionData.GetExistingLinkedEntry(AAU.UniqueBungieName);

                var sessionTime = DateTime.Now - AAU.TimeStarted;

                // Only add back if the entry is better than their previous.
                if (sessionTime > entry.Time)
                {
                    LongestSessionData.DeleteEntryFromConfig(AAU.UniqueBungieName);
                    LongestSessionData.AddEntryToConfig(new LongestSessionData.LongestSessionEntry()
                    {
                        Time = sessionTime,
                        UniqueBungieName = AAU.UniqueBungieName
                    });
                }
            }
            else
            {
                LongestSessionData.AddEntryToConfig(new LongestSessionData.LongestSessionEntry()
                {
                    Time = DateTime.Now - AAU.TimeStarted,
                    UniqueBungieName = AAU.UniqueBungieName
                });
            }

            if (MostThrallwayTimeData.IsExistingLinkedEntry(AAU.UniqueBungieName))
            {
                var entry = MostThrallwayTimeData.GetExistingLinkedEntry(AAU.UniqueBungieName);

                var newTotalTime = (DateTime.Now - AAU.TimeStarted) + entry.Time;

                // Overwrite the existing entry with new data.
                MostThrallwayTimeData.DeleteEntryFromConfig(AAU.UniqueBungieName);
                MostThrallwayTimeData.AddEntryToConfig(new MostThrallwayTimeData.MostThrallwayTimeEntry()
                { 
                    Time = newTotalTime,
                    UniqueBungieName = AAU.UniqueBungieName
                });
            }
            else
            {
                MostThrallwayTimeData.AddEntryToConfig(new MostThrallwayTimeData.MostThrallwayTimeEntry()
                {
                    Time = DateTime.Now - AAU.TimeStarted,
                    UniqueBungieName = AAU.UniqueBungieName
                });
            }
        }

        private async Task LoadLeaderboards()
        {
            try
            {
                Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Pulling data for leaderboards...");
                LevelData.LevelDataEntries.Clear();
                foreach (var link in DataConfig.DiscordIDLinks) // USE THIS FOREACH LOOP TO POPULATE FUTURE LEADERBOARDS (that use API calls)
                {
                    // populate list
                    LevelData.LevelDataEntries.Add(new LevelData.LevelDataEntry()
                    {
                        LastLoggedLevel = DataConfig.GetUserSeasonPassLevel(link.DiscordID, out _),
                        UniqueBungieName = link.UniqueBungieName,
                    });
                    await Task.Delay(150);
                }
                LevelData.UpdateEntriesConfig();
                Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Data pulling complete!");
            }
            catch
            {
                Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Error while updating leaderboards, trying again at next refresh.");
            }
        }

        private async Task<ActiveConfig.ActiveAFKUser> RefreshSpecificUser(ActiveConfig.ActiveAFKUser aau)
        {
            await Task.Delay(15000);
            Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Refreshing Bungie API specifically for {aau.UniqueBungieName}.");
            List<ActiveConfig.ActiveAFKUser> temp = new List<ActiveConfig.ActiveAFKUser>();
            ActiveConfig.ActiveAFKUser tempAau = new ActiveConfig.ActiveAFKUser();
            try
            {
                tempAau = aau;
                int updatedLevel = DataConfig.GetUserSeasonPassLevel(tempAau.DiscordID, out int updatedProgression);

                if (updatedLevel > aau.LastLoggedLevel)
                {
                    await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Level up detected. {tempAau.LastLoggedLevel} -> {updatedLevel}");

                    tempAau.LastLoggedLevel = updatedLevel;
                    tempAau.LastLevelProgress = updatedProgression;

                    await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel,
                            $"Start: {tempAau.StartLevel} ({String.Format("{0:n0}", tempAau.StartLevelProgress)}/100,000 XP). Now: {tempAau.LastLoggedLevel} ({String.Format("{0:n0}", tempAau.LastLevelProgress)}/100,000 XP)");
                }
                else if (updatedProgression == aau.LastLevelProgress)
                {
                    await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Potential wipe detected.");
                    await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"<@{tempAau.DiscordID}>: Logging terminated by automation. Here is your session summary:", GenerateSessionSummary(tempAau).Result, GenerateDeleteChannelButton());

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
                    await LogHelper.Log(user.CreateDMChannelAsync().Result, $"Here is the session summary, beginning on {tempAau.TimeStarted:G} (UTC-7).", GenerateSessionSummary(tempAau).Result);

                    Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Stopped logging for {tempAau.UniqueBungieName}.");

                    await Task.Run(() => CheckLeaderboardData(tempAau));
                    return null;
                }
                else if (updatedProgression < aau.LastLevelProgress)
                {
                    await LogHelper.Log(_client.GetChannelAsync(aau.DiscordChannelID).Result as ITextChannel, $"API backstepped. Waiting for next refresh.");
                    return tempAau;
                }
                else
                {
                    await LogHelper.Log(_client.GetChannelAsync(aau.DiscordChannelID).Result as ITextChannel, $"Refreshed! Progress for {tempAau.UniqueBungieName} (Level: {updatedLevel}): {String.Format("{0:n0}", tempAau.LastLevelProgress)} XP -> {String.Format("{0:n0}", updatedProgression)} XP");
                    tempAau.LastLoggedLevel = updatedLevel;
                    tempAau.LastLevelProgress = updatedProgression;
                }

                Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] API Refreshed for {tempAau.UniqueBungieName}!");

                return tempAau;
            }
            catch (Exception x)
            {
                Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Refresh for {tempAau.UniqueBungieName} failed!");
                await LogHelper.Log(_client.GetChannelAsync(aau.DiscordChannelID).Result as ITextChannel, $"Exception found: {x}");
                return null;
            }
        }

        private ComponentBuilder GenerateDeleteChannelButton()
        {
            Emoji deleteEmote = new Emoji("⛔");

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Delete Log Channel", customId: $"deleteChannel", ButtonStyle.Secondary, deleteEmote, row: 0);

            return buttonBuilder;
        }

        private async Task<EmbedBuilder> GenerateSessionSummary(ActiveConfig.ActiveAFKUser aau)
        {
            var app = await _client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Session Summary: {aau.UniqueBungieName}",
                IconUrl = app.IconUrl,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Thrallway Session Summary"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };
            int levelsGained = aau.LastLoggedLevel - aau.StartLevel;
            long xpGained = (levelsGained * 100000) - aau.StartLevelProgress + aau.LastLevelProgress;
            var timeSpan = DateTime.Now - aau.TimeStarted;
            string timeString = $"{(Math.Floor(timeSpan.TotalHours) > 0 ? $"{Math.Floor(timeSpan.TotalHours)}h " : "")}" +
                    $"{(timeSpan.Minutes > 0 ? $"{timeSpan.Minutes:00}m " : "")}" +
                    $"{timeSpan.Seconds:00}s";
            int xpPerHour = 0;
            if ((DateTime.Now - aau.TimeStarted).TotalHours >= 1)
                xpPerHour = (int)Math.Floor(xpGained / (DateTime.Now - aau.TimeStarted).TotalHours);
            embed.Description =
                $"Levels Gained: {levelsGained}\n" +
                $"XP Gained: {String.Format("{0:n0}", xpGained)}\n" +
                $"Time: {timeString}\n" +
                $"XP Per Hour: {String.Format("{0:n0}", xpPerHour)}";

            return embed;
        }
        #endregion

        private async Task InitializeCommands()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.MessageReceived += HandleMessageAsync;
            //_client.InteractionCreated += HandleInteraction;

            //_client.Ready += InitializeSlashCommands;
            _client.SlashCommandExecuted += SlashCommandHandler;

            _client.ButtonExecuted += ButtonHandler;
            //_client.SelectMenuExecuted += SelectMenuHandler;
        }

        private async Task InitializeSlashCommands()
        {
            var guild = _client.GetGuild(600548936062730259);
            //await guild.DeleteApplicationCommandsAsync();
            var cmds = await _client.Rest.GetGlobalApplicationCommands();

            /*foreach (var cmd in cmds)
            {
                if (cmd.Name.Equals("nextarmor"))
                {
                    await cmd.DeleteAsync();
                }
            }*/

            var lostSectorAlertCommand = new SlashCommandBuilder();
            lostSectorAlertCommand.WithName("lostsectoralert");
            lostSectorAlertCommand.WithDescription("Be notified when a Lost Sector comes into Rotation");
            var scobA = new SlashCommandOptionBuilder()
                .WithName("lost-sector")
                .WithDescription("The Lost Sector you want to be Notified for")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (LostSector LS in Enum.GetValues(typeof(LostSector)))
            {
                scobA.AddChoice($"{LostSectorRotation.GetLostSectorString(LS)}", (int)LS);
            }

            var scobB = new SlashCommandOptionBuilder()
                .WithName("armor-drop")
                .WithDescription("The Exotic Armor the Lost Sector should drop")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (ExoticArmorType EAT in Enum.GetValues(typeof(ExoticArmorType)))
            {
                scobB.AddChoice($"{EAT}", (int)EAT);
            }

            lostSectorAlertCommand.AddOption(scobA)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("difficulty")
                    .WithDescription("Desired Difficulty of the Lost Sector")
                    .WithRequired(true)
                    .AddChoice("Legend", 0)
                    .AddChoice("Master", 1)
                    .WithType(ApplicationCommandOptionType.Integer))
                .AddOption(scobB);

            // ==============================================

            var lostSectorInfoCommand = new SlashCommandBuilder();
            lostSectorInfoCommand.WithName("lostsectorinfo");
            lostSectorInfoCommand.WithDescription("Get Info on a Lost Sector based on Difficulty");
            var scobC = new SlashCommandOptionBuilder()
                .WithName("lost-sector")
                .WithDescription("The Lost Sector you want Information on")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (LostSector LS in Enum.GetValues(typeof(LostSector)))
            {
                scobC.AddChoice($"{LostSectorRotation.GetLostSectorString(LS)}", (int)LS);
            }

            lostSectorInfoCommand.AddOption(scobC)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("difficulty")
                    .WithDescription("The Difficulty of the Lost Sector")
                    .WithRequired(true)
                    .AddChoice("Legend", 0)
                    .AddChoice("Master", 1)
                    .WithType(ApplicationCommandOptionType.Integer));

            // ==============================================

            var nextOccuranceCommand = new SlashCommandBuilder();
            nextOccuranceCommand.WithName("next");
            nextOccuranceCommand.WithDescription("Find out when the next occurance of a lost sector and/or Exotic Armor type is");

            var scobD = new SlashCommandOptionBuilder()
                .WithName("lost-sector")
                .WithDescription("The Lost Sector you want me to get the occurance of")
                .WithRequired(false)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (LostSector LS in Enum.GetValues(typeof(LostSector)))
            {
                scobD.AddChoice($"{LostSectorRotation.GetLostSectorString(LS)}", (int)LS);
            }

            var scobE = new SlashCommandOptionBuilder()
                .WithName("armor-drop")
                .WithDescription("The Exotic Armor you want me to get the occurance of")
                .WithRequired(false)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (ExoticArmorType EAT in Enum.GetValues(typeof(ExoticArmorType)))
            {
                scobE.AddChoice($"{EAT}", (int)EAT);
            }

            nextOccuranceCommand.AddOption(scobD).AddOption(scobE);

            //  ==============================================

            var removeAlertCommand = new SlashCommandBuilder();
            removeAlertCommand.WithName("removealert");
            removeAlertCommand.WithDescription("Remove an active tracking alert");

            // ==============================================

            var rankCommand = new SlashCommandBuilder();
            rankCommand.WithName("rank");
            rankCommand.WithDescription("Display a Destiny 2 leaderboard of choice");

            var scobF = new SlashCommandOptionBuilder()
                .WithName("leaderboard")
                .WithDescription("Specific leaderboard to display")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (Leaderboard LB in Enum.GetValues(typeof(Leaderboard)))
            {
                scobF.AddChoice($"{LeaderboardHelper.GetLeaderboardString(LB)}", (int)LB);
            }

            rankCommand.AddOption(scobF);

            // ==============================================

            var alertCommand = new SlashCommandBuilder();
            alertCommand.WithName("alert");
            alertCommand.WithDescription("Set up announcements for Daily/Weekly Reset");

            var scobG = new SlashCommandOptionBuilder()
                .WithName("type")
                .WithDescription("Choose between Daily or Weekly Reset")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);

            scobG.AddChoice($"Daily", 0);
            scobG.AddChoice($"Weekly", 1);

            alertCommand.AddOption(scobG);

            try
            {
                //await guild.CreateApplicationCommandAsync(globalCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(lostSectorAlertCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(lostSectorInfoCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(nextOccuranceCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(removeAlertCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(rankCommand.Build());
                await _client.CreateGlobalApplicationCommandAsync(alertCommand.Build());
            }
            catch (HttpException exception)
            {
                Console.WriteLine(exception);
            }
        }

        /*private async Task SelectMenuHandler(SocketMessageComponent interaction)
        {
            // Will be used for implementation at some point.
            // Said implementation will be for daily/weekly reset notification setup.
            // A slash command will be called and it will respond with a select menu containing Daily and Weekly.
        }*/

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.Data.Name.Equals("lostsectoralert"))
            {
                if (LostSectorRotation.IsExistingLinkedLostSectorsTracking(command.User.Id))
                {
                    await command.RespondAsync($"You already have an active alert set up!", ephemeral: true);
                    return;
                }
                else
                {
                    LostSector LS = 0;
                    LostSectorDifficulty LSD = 0;
                    ExoticArmorType EAT = 0;

                    foreach (var option in command.Data.Options)
                    {
                        if (option.Name.Equals("lost-sector"))
                            LS = (LostSector)Convert.ToInt32(option.Value);
                        else if (option.Name.Equals("difficulty"))
                            LSD = (LostSectorDifficulty)Convert.ToInt32(option.Value);
                        else if (option.Name.Equals("armor-drop"))
                            EAT = (ExoticArmorType)Convert.ToInt32(option.Value);
                    }

                    LostSectorRotation.AddLostSectorsTrackingToConfig(command.User.Id, LS, LSD, EAT);

                    await command.RespondAsync($"I will remind you when {LostSectorRotation.GetLostSectorString(LS)} ({LSD}) is dropping {EAT}, which will be on " +
                        $"{(LSD == LostSectorDifficulty.Legend ? $"{TimestampTag.FromDateTime(DateTime.Now.AddDays(LostSectorRotation.DaysUntilNextOccurance(LS, EAT)).Date.AddHours(10), TimestampTagStyles.ShortDate)}" : $"{TimestampTag.FromDateTime(DateTime.Now.AddDays(1 + LostSectorRotation.DaysUntilNextOccurance(LS, EAT)).Date.AddHours(10), TimestampTagStyles.ShortDate)}")}.",
                        ephemeral: true);
                    return;
                }
            }
            else if (command.Data.Name.Equals("removealert"))
            {
                if (!LostSectorRotation.IsExistingLinkedLostSectorsTracking(command.User.Id))
                {
                    await command.RespondAsync($"You don't have an active Lost Sector tracker.", ephemeral: true);
                    return;
                }

                await command.RespondAsync($"Removed your tracking for {LostSectorRotation.GetLostSectorString(LostSectorRotation.GetLostSectorsTracking(command.User.Id).LostSector)}" +
                    $" ({LostSectorRotation.GetLostSectorsTracking(command.User.Id).Difficulty}).", ephemeral: true);
                LostSectorRotation.DeleteLostSectorsTrackingFromConfig(command.User.Id);
                
            }
            else if (command.Data.Name.Equals("lostsectorinfo"))
            {
                LostSector LS = 0;
                LostSectorDifficulty LSD = 0;

                foreach (var option in command.Data.Options)
                {
                    if (option.Name.Equals("lost-sector"))
                        LS = (LostSector)Convert.ToInt32(option.Value);
                    else if (option.Name.Equals("difficulty"))
                        LSD = (LostSectorDifficulty)Convert.ToInt32(option.Value);
                }

                await command.RespondAsync($"", embed: LostSectorRotation.GetLostSectorEmbed(LS, LSD).Build());
                return;
            }
            else if (command.Data.Name.Equals("next"))
            {
                LostSector? LS = null;
                ExoticArmorType? EAT = null;

                foreach (var option in command.Data.Options)
                {
                    if (option.Name.Equals("lost-sector"))
                        LS = (LostSector)Convert.ToInt32(option.Value);
                    else if (option.Name.Equals("armor-drop"))
                        EAT = (ExoticArmorType)Convert.ToInt32(option.Value);
                }
                
                if (LS == null && EAT == null)
                {
                    var auth = new EmbedAuthorBuilder()
                    {
                        Name = $"Tomorrow's Lost Sectors",
                    };
                    var foot = new EmbedFooterBuilder()
                    {
                        Text = $"This is a prediction and is subject to change"
                    };
                    var embed = new EmbedBuilder()
                    {
                        Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                        Author = auth,
                        Footer = foot,
                    };

                    embed.Description = $"Legend: **{LostSectorRotation.GetLostSectorString(LostSectorRotation.GetPredictedLegendLostSector(1))}** dropping **{LostSectorRotation.GetPredictedLegendLostSectorArmorDrop(1)}**\n" +
                        $"Master: **{LostSectorRotation.GetLostSectorString(LostSectorRotation.CurrentLegendLostSector)}** dropping **{LostSectorRotation.CurrentLegendArmorDrop}**";

                    await command.RespondAsync($"", embed: embed.Build());
                    return;
                }

                var legendDate = DateTime.Now.AddDays(LostSectorRotation.DaysUntilNextOccurance(LS, EAT)).Date.AddHours(10);
                var masterDate = DateTime.Now.AddDays(1 + LostSectorRotation.DaysUntilNextOccurance(LS, EAT)).Date.AddHours(10);

                if (LS == null)
                {
                    var auth = new EmbedAuthorBuilder()
                    {
                        Name = $"Next Occurance of {EAT}",
                    };
                    var foot = new EmbedFooterBuilder()
                    {
                        Text = $"This is a prediction and is subject to change"
                    };
                    var embed = new EmbedBuilder()
                    {
                        Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                        Author = auth,
                        Footer = foot,
                    };

                    embed.Description = $"Lost Sector: {LostSectorRotation.GetLostSectorString(LostSectorRotation.GetPredictedLegendLostSector(LostSectorRotation.DaysUntilNextOccurance(LS, EAT)))}\n" +
                        $"Legend: {TimestampTag.FromDateTime(legendDate, TimestampTagStyles.ShortDate)}\n" +
                        $"Master: {TimestampTag.FromDateTime(masterDate, TimestampTagStyles.ShortDate)}";

                    await command.RespondAsync($"", embed: embed.Build());
                    return;
                }
                else if (EAT == null)
                {
                    var auth = new EmbedAuthorBuilder()
                    {
                        Name = $"Next occurance of {LostSectorRotation.GetLostSectorString((LostSector)LS)}",
                    };
                    var foot = new EmbedFooterBuilder()
                    {
                        Text = $"This is a prediction and is subject to change"
                    };
                    var embed = new EmbedBuilder()
                    {
                        Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                        Author = auth,
                        Footer = foot,
                    };

                    embed.Description = $"Exotic Drop: **{LostSectorRotation.GetPredictedLegendLostSectorArmorDrop(LostSectorRotation.DaysUntilNextOccurance(LS, EAT))}**\n" +
                        $"Legend: {TimestampTag.FromDateTime(legendDate, TimestampTagStyles.ShortDate)}\n" +
                        $"Master: {TimestampTag.FromDateTime(masterDate, TimestampTagStyles.ShortDate)}";

                    await command.RespondAsync($"", embed: embed.Build());
                    return;
                }
                else
                {
                    var auth = new EmbedAuthorBuilder()
                    {
                        Name = $"Next occurance of {LostSectorRotation.GetLostSectorString((LostSector)LS)} dropping {EAT}",
                    };
                    var foot = new EmbedFooterBuilder()
                    {
                        Text = $"This is a prediction and is subject to change"
                    };
                    var embed = new EmbedBuilder()
                    {
                        Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                        Author = auth,
                        Footer = foot,
                    };

                    embed.Description = $"Legend: {TimestampTag.FromDateTime(legendDate, TimestampTagStyles.ShortDate)}\n" +
                        $"Master: {TimestampTag.FromDateTime(masterDate, TimestampTagStyles.ShortDate)}";

                    await command.RespondAsync($"", embed: embed.Build());
                    return;
                }
            }
            else if (command.Data.Name.Equals("rank"))
            {
                Leaderboard LeaderboardType = 0;
                // Using this foreach Loop for future parameters, such as Seasonal leaderboards because we will be resetting the leaderboards each season.
                foreach (var option in command.Data.Options)
                {
                    if (option.Name.Equals("leaderboard"))
                    {
                        // Using Leaderboard enum Values to determine what kind of Leaderboard we should generate.
                        LeaderboardType = (Leaderboard)Convert.ToInt32(option.Value);
                    }
                }

                EmbedBuilder embed = new EmbedBuilder();
                switch (LeaderboardType)
                {
                    case Leaderboard.Level: embed = LeaderboardHelper.GetLeaderboardEmbed(LevelData.GetSortedLevelData(), command.User); break;
                    case Leaderboard.LongestSession: embed = LeaderboardHelper.GetLeaderboardEmbed(LongestSessionData.GetSortedLevelData(), command.User); break;
                    case Leaderboard.XPPerHour: embed = LeaderboardHelper.GetLeaderboardEmbed(XPPerHourData.GetSortedLevelData(), command.User); break;
                    case Leaderboard.MostThrallwayTime: embed = LeaderboardHelper.GetLeaderboardEmbed(MostThrallwayTimeData.GetSortedLevelData(), command.User); break;
                }

                await command.RespondAsync($"", embed: embed.Build());
            }
            else if (command.Data.Name.Equals("alert"))
            {
                bool IsDaily = false;
                foreach (var option in command.Data.Options)
                    if (option.Name.Equals("type"))
                        IsDaily = Convert.ToInt32(option.Value) == 0;

                if (DataConfig.IsExistingLinkedChannel(command.Channel.Id, IsDaily))
                {
                    await command.RespondAsync($"This channel already has {(IsDaily ? "Daily" : "Weekly")} reset posts set up!", ephemeral: true);
                    return;
                }
                else
                {
                    DataConfig.AddChannelToRotationConfig(command.Channel.Id, IsDaily);

                    await command.RespondAsync($"This channel is now successfully subscribed to {(IsDaily ? "Daily" : "Weekly")} reset posts.", ephemeral: true);
                    return;
                }
            }
        }

        private async Task ButtonHandler(SocketMessageComponent interaction)
        {
            // Get the custom ID 
            var customId = interaction.Data.CustomId;
            // Get the user
            var user = (SocketGuildUser)interaction.User;
            // Get the guild
            var guild = user.Guild;
            // Get the channel
            var channel = interaction.Channel;

            // Respond with the update message. This edits the message which this component resides.
            //await interaction.UpdateAsync(msgProps => msgProps.Content = $"Clicked {interaction.Data.CustomId}!");

            // Also you can followup with a additional messages

            if (customId.Equals("viewHelp"))
            {
                var app = await _client.GetApplicationInfoAsync();
                var auth = new EmbedAuthorBuilder()
                {
                    Name = $"Thrallway Logger Help!",
                    IconUrl = app.IconUrl,
                };
                var foot = new EmbedFooterBuilder()
                {
                    Text = $"Powered by @OatsFX"
                };
                var ruleEmbed = new EmbedBuilder()
                {
                    Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                    Author = auth,
                    Footer = foot,
                };
                ruleEmbed.Description =
                    $"__Steps:__\n" +
                    $"1) Launch Shattered Throne with the your choice of \"Thrallway\" checkpoint.\n" +
                    $"2) Get into your desired setup and start your AFK script.\n" +
                    $"3) This is when you can subscribe to our logs using the \"Ready\" button.";

                Embed[] embeds = new Embed[1];
                embeds[0] = ruleEmbed.Build();

                await interaction.RespondAsync($"", embeds, false, ephemeral: true);
            }
            else if (customId.Contains("force"))
            {
                DailyResetChanges();
            }
            else if (customId.Contains($"startAFK"))
            {
                if (ActiveConfig.ActiveAFKUsers.Count >= ActiveConfig.MaximumThrallwayUsers)
                {
                    await interaction.RespondAsync($"Unfortunately, I am at the maximum number of users to watch ({ActiveConfig.MaximumThrallwayUsers}). Try again later.", ephemeral: true);
                    return;
                }

                if (IsBungieAPIDown())
                {
                    await interaction.RespondAsync($"Bungie API is temporarily down, therefore, I cannot enable our logging feature. Try again later.", ephemeral: true);
                    return;
                }

                if (!DataConfig.IsExistingLinkedUser(user.Id))
                {
                    await interaction.RespondAsync($"You are not registered! Use \"{BotConfig.DefaultCommandPrefix}linkHelp\" to learn how to register.", ephemeral: true);
                    return;
                }

                if (ActiveConfig.IsExistingActiveUser(user.Id))
                {
                    await interaction.RespondAsync($"You are already actively using our logging feature.", ephemeral: true);
                    return;
                }

                string memId = DataConfig.GetLinkedUser(user.Id).BungieMembershipID;
                string memType = DataConfig.GetLinkedUser(user.Id).BungieMembershipType;

                int userLevel = DataConfig.GetAFKValues(user.Id, out int lvlProg, out bool isInShatteredThrone);

                if (!isInShatteredThrone)
                {
                    await interaction.RespondAsync($"You are not in Shattered Throne. Launch Shattered Throne, get set up, and then click \"Ready\".", ephemeral: true);
                    return;
                }

                await interaction.RespondAsync($"Getting things ready...", ephemeral: true);
                string uniqueName = DataConfig.GetLinkedUser(user.Id).UniqueBungieName;

                ICategoryChannel cc = null;
                foreach (var categoryChan in guild.CategoryChannels)
                {
                    if (categoryChan.Name.Equals($"Thrallway Logger"))
                    {
                        cc = categoryChan;
                    }
                }

                if (cc == null)
                {
                    await interaction.RespondAsync($"No category by the name of \"Thrallway Logger\" was found, cancelling operation. Let a server admin know!", ephemeral: true);
                    return;
                }

                var userLogChannel = guild.CreateTextChannelAsync($"{uniqueName.Replace('#','-')}").Result;
                ActiveConfig.ActiveAFKUser newUser = new ActiveConfig.ActiveAFKUser
                {
                    DiscordID = user.Id,
                    BungieMembershipID = memId,
                    UniqueBungieName = uniqueName,
                    DiscordChannelID = userLogChannel.Id,
                    StartLevel = userLevel,
                    LastLoggedLevel = userLevel,
                    StartLevelProgress = lvlProg,
                    LastLevelProgress = lvlProg,
                    PrivacySetting = ActiveConfig.GetFireteamPrivacy(memId, memType)
                };

                await userLogChannel.ModifyAsync(x =>
                {
                    x.CategoryId = cc.Id;
                    x.Topic = $"{uniqueName} (Starting Level: {newUser.StartLevel} [{String.Format("{0:n0}", newUser.StartLevelProgress)}/100,000 XP]) - Time Started: {DateTime.UtcNow}-UTC";
                    x.PermissionOverwrites = new[]
                    {
                        new Overwrite(user.Id, PermissionTarget.User, new OverwritePermissions(sendMessages: PermValue.Allow, viewChannel: PermValue.Allow)),
                        new Overwrite(688535303148929027, PermissionTarget.Role, new OverwritePermissions(sendMessages: PermValue.Allow, viewChannel: PermValue.Allow)),
                        new Overwrite(guild.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)),
                    };
                });

                string privacy = "";
                switch (newUser.PrivacySetting)
                {
                    case PrivacySetting.Open: privacy = "Open"; break;
                    case PrivacySetting.ClanAndFriendsOnly: privacy = "Clan and Friends Only"; break;
                    case PrivacySetting.FriendsOnly: privacy = "Friends Only"; break;
                    case PrivacySetting.InvitationOnly: privacy = "Invite Only"; break;
                    case PrivacySetting.Closed: privacy = "Closed"; break;
                    default: break;
                }
                await LogHelper.Log(userLogChannel, $"{uniqueName} is starting at Level {newUser.LastLoggedLevel} ({String.Format("{0:n0}", newUser.LastLevelProgress)}/100,000 XP).");
                string recommend = newUser.PrivacySetting == PrivacySetting.Open || newUser.PrivacySetting == PrivacySetting.ClanAndFriendsOnly || newUser.PrivacySetting == PrivacySetting.FriendsOnly ? " It is recommended to change your privacy to prevent people from joining you." : "";
                await LogHelper.Log(userLogChannel, $"{uniqueName} has fireteam on {privacy}.{recommend}");

                ActiveConfig.AddActiveUserToConfig(newUser);
                await UpdateBotActivity();

                await LogHelper.Log(userLogChannel, "User is subscribed to our Bungie API refreshes. Waiting for next refresh...");
            }
            else if (customId.Contains($"stopAFK"))
            {
                if (!DataConfig.IsExistingLinkedUser(user.Id))
                {
                    await interaction.RespondAsync($"You are not registered! Use \"{BotConfig.DefaultCommandPrefix}linkHelp\" to learn how to register.", ephemeral: true);
                    return;
                }

                if (!ActiveConfig.IsExistingActiveUser(user.Id))
                {
                    await interaction.RespondAsync($"You are not actively using our logging feature.", ephemeral: true);
                    return;
                }

                var aau = ActiveConfig.GetActiveAFKUser(user.Id);

                await LogHelper.Log(_client.GetChannelAsync(aau.DiscordChannelID).Result as ITextChannel, $"<@{user.Id}>: Logging terminated by user. Here is your session summary:", Embed: GenerateSessionSummary(aau).Result, CB: GenerateDeleteChannelButton());
                await LogHelper.Log(user.CreateDMChannelAsync().Result, $"Here is the session summary, beginning on {aau.TimeStarted:G} (UTC-7).", GenerateSessionSummary(aau).Result);

                await Task.Run(() => CheckLeaderboardData(aau));
                ActiveConfig.DeleteActiveUserFromConfig(user.Id);
                await UpdateBotActivity();
                await interaction.RespondAsync($"Stopped AFK logging for {aau.UniqueBungieName}.", ephemeral: true);
            }
            else if (customId.Contains("deleteChannel"))
            {
                await (channel as SocketGuildChannel).DeleteAsync();
            }
        }

        private async Task HandleMessageAsync(SocketMessage arg)
        {
            if (arg.Author.IsWebhook || arg.Author.IsBot) return; // Return if message is from a Webhook or Bot user
            if (arg.ToString().Length < 0) return; // Return of the message has no text
            if (arg.Author.Id == _client.CurrentUser.Id) return; // Return of the message is from itself

            int argPos = 0; // Position to check for command arguments

            string prefix = BotConfig.DefaultCommandPrefix;

            var msg = arg as SocketUserMessage;
            if (msg == null) return;

            if (msg.HasStringPrefix(prefix, ref argPos))
            {
                if (arg.Channel.GetType() == typeof(SocketDMChannel) && arg.Author.Id != 261121732704862208) // Send message if received via a DM
                {
                    await arg.Channel.SendMessageAsync($"I do not accept commands through Direct Messages.");
                    return;
                }

                /*if (CheckIfDisabledCommand(arg, prefix)) // Check for disabled command
                {
                    await arg.Channel.SendMessageAsync($"That command is disabled. Try again later.");
                    return;
                }*/

                var handled = await TryHandleCommandAsync(msg, argPos).ConfigureAwait(false);
                if (handled) return;
            }
        }

        private async Task<bool> TryHandleCommandAsync(SocketUserMessage msg, int argPos)
        {
            var context = new SocketCommandContext(_client, msg);

            var result = await _commands.ExecuteAsync(context, argPos, _services);

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
                    string commandNew = msg.Content.Substring(argPos).ToLowerInvariant().Trim();
                    string commandName;
                    if (commandNew.Contains(" "))
                    {
                        int index = commandNew.IndexOf(" ");
                        commandName = commandNew.Substring(0, index);
                    }
                    else
                    {
                        commandName = commandNew;
                    }
                    await context.Channel.SendMessageAsync($"[{error}]: Command is missing some arguments.").ConfigureAwait(false);
                }
            }
            return true;
        }

        private bool CheckAndLoadDataFiles()
        {
            LevelData ld;
            XPPerHourData xph;
            LongestSessionData ls;
            MostThrallwayTimeData mtt;

            bool closeProgram = false;
            if (File.Exists(LevelDataPath))
            {
                string json = File.ReadAllText(LevelDataPath);
                ld = JsonConvert.DeserializeObject<LevelData>(json);
            }
            else
            {
                ld = new LevelData();
                File.WriteAllText(LevelDataPath, JsonConvert.SerializeObject(ld, Formatting.Indented));
                Console.WriteLine($"No levelData.json file detected. A new one has been created and the program has stopped.");
                closeProgram = true;
            }

            if (File.Exists(XPPerHourDataPath))
            {
                string json = File.ReadAllText(XPPerHourDataPath);
                xph = JsonConvert.DeserializeObject<XPPerHourData>(json);
            }
            else
            {
                xph = new XPPerHourData();
                File.WriteAllText(XPPerHourDataPath, JsonConvert.SerializeObject(xph, Formatting.Indented));
                Console.WriteLine($"No xpPerHourData.json file detected. A new one has been created and the program has stopped.");
                closeProgram = true;
            }

            if (File.Exists(LongestSessionDataPath))
            {
                string json = File.ReadAllText(LongestSessionDataPath);
                ls = JsonConvert.DeserializeObject<LongestSessionData>(json);
            }
            else
            {
                ls = new LongestSessionData();
                File.WriteAllText(LongestSessionDataPath, JsonConvert.SerializeObject(ls, Formatting.Indented));
                Console.WriteLine($"No longestSessionData.json file detected. A new one has been created and the program has stopped.");
                closeProgram = true;
            }

            if (File.Exists(MostThrallwayTimeDataPath))
            {
                string json = File.ReadAllText(MostThrallwayTimeDataPath);
                mtt = JsonConvert.DeserializeObject<MostThrallwayTimeData>(json);
            }
            else
            {
                mtt = new MostThrallwayTimeData();
                File.WriteAllText(MostThrallwayTimeDataPath, JsonConvert.SerializeObject(mtt, Formatting.Indented));
                Console.WriteLine($"No mostThrallwayTimeData.json file detected. A new one has been created and the program has stopped.");
                closeProgram = true;
            }

            if (closeProgram == true) return false;
            return true;
        }

        private bool CheckAndLoadLostSectorData()
        {
            LostSectorRotation lstConfig;

            bool closeProgram = false;
            if (File.Exists(LostSectorTrackingConfigPath))
            {
                string json = File.ReadAllText(LostSectorTrackingConfigPath);
                lstConfig = JsonConvert.DeserializeObject<LostSectorRotation>(json);
            }
            else
            {
                lstConfig = new LostSectorRotation();
                File.WriteAllText(LostSectorTrackingConfigPath, JsonConvert.SerializeObject(lstConfig, Formatting.Indented));
                Console.WriteLine($"No lostSectorTrackingConfig.json file detected. A new one has been created and the program has stopped. Go and change API tokens and other items.");
                closeProgram = true;
            }

            if (closeProgram == true) return false;
            return true;
        }
    }
}
