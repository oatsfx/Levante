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
using System.Threading;
using Discord.Net;
using DestinyUtility.Leaderboards;
using DestinyUtility.Rotations;
using DestinyUtility.Util;

namespace DestinyUtility
{
    public sealed class DestinyUtilityCord
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

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

            await Task.Run(() => Console.Title = $"DestinyUtility v{BotConfig.Version}");

            if (!LeaderboardHelper.CheckAndLoadDataFiles())
                return;

            CurrentRotations.CreateJSONs();
            
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
            Console.WriteLine($"Altar Weapon: {EmpireHuntRotation.GetHuntNameString(CurrentRotations.EmpireHunt)}");
            Console.WriteLine();
            Console.WriteLine($"Nightmare Hunts: {CurrentRotations.NightmareHunts[0]}/{CurrentRotations.NightmareHunts[1]}/{CurrentRotations.NightmareHunts[2]}");
            Console.WriteLine();

            _client.Log += log =>
            {
                Console.WriteLine(log.ToString());
                return Task.CompletedTask;
            };

            Timer timer = new Timer(TimerCallback, null, 25000, 240000);

            if (DateTime.Now.Hour >= 10) // after daily reset
                SetUpTimer(new DateTime(DateTime.Today.AddDays(1).Year, DateTime.Today.AddDays(1).Month, DateTime.Today.AddDays(1).Day, 10, 0, 0));
            else
                SetUpTimer(new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 10, 0, 0));

            await InitializeListeners().ConfigureAwait(false);

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
                        await LogHelper.Log(user.CreateDMChannelAsync().Result, $"<@{tempAau.DiscordID}>: Player is no longer in Shattered Throne. Logging will be terminated for {uniqueName}.");
                        await LogHelper.Log(user.CreateDMChannelAsync().Result, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.TimeStarted)}.", GenerateSessionSummary(tempAau).Result);

                        LogHelper.ConsoleLog($"Stopped logging for {tempAau.UniqueBungieName} via automation.");
                        listOfRemovals.Add(tempAau);
                        await Task.Run(() => CheckLeaderboardData(tempAau));
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

                await UpdateBotActivity();
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
                LogHelper.ConsoleLog($"Pulling data for leaderboards...");
                LevelData.LevelDataEntries.Clear();
                PowerLevelData.PowerLevelDataEntries.Clear();
                foreach (var link in DataConfig.DiscordIDLinks) // USE THIS FOREACH LOOP TO POPULATE FUTURE LEADERBOARDS (that use API calls)
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
                    // populate list
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
                    await LogHelper.Log(user.CreateDMChannelAsync().Result, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.TimeStarted)}.", GenerateSessionSummary(tempAau).Result);

                    LogHelper.ConsoleLog($"Stopped logging for {tempAau.UniqueBungieName} via automation.");

                    await Task.Run(() => CheckLeaderboardData(tempAau));
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

        private async Task InitializeListeners()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.MessageReceived += HandleMessageAsync;
            //_client.InteractionCreated += HandleInteraction;

            //_client.Ready += InitializeSlashCommands;
            _client.SlashCommandExecuted += SlashCommandHandler;

            _client.ButtonExecuted += ButtonHandler;
            _client.SelectMenuExecuted += SelectMenuHandler;
        }

        private async Task InitializeSlashCommands()
        {
            var guild = _client.GetGuild(397846250797662208);
            await guild.DeleteApplicationCommandsAsync();
            var cmds = await _client.Rest.GetGlobalApplicationCommands();

            foreach (var cmd in cmds)
            {
                await cmd.DeleteAsync();
            }

            // ==============================================

            var altarsScob = new SlashCommandOptionBuilder()
                    .WithName("weapon")
                    .WithDescription("Altars of Sorrow weapon.")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer);
            foreach (AltarsOfSorrow AOS in Enum.GetValues(typeof(AltarsOfSorrow)))
            {
                altarsScob.AddChoice($"{AltarsOfSorrowRotation.GetWeaponNameString(AOS)} ({AOS})", (int)AOS);
            }

            // ---

            var ascentantScob = new SlashCommandOptionBuilder()
                .WithName("ascendant-challenge")
                .WithDescription("Ascendant Challenge name.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (AscendantChallenge AC in Enum.GetValues(typeof(AscendantChallenge)))
            {
                ascentantScob.AddChoice($"{AscendantChallengeRotation.GetChallengeNameString(AC)} ({AscendantChallengeRotation.GetChallengeLocationString(AC)})", (int)AC);
            }

            // ---

            var curseWeekScob = new SlashCommandOptionBuilder()
                .WithName("strength")
                .WithDescription("Curse Week strength.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (CurseWeek CW in Enum.GetValues(typeof(CurseWeek)))
            {
                curseWeekScob.AddChoice($"{CW}", (int)CW);
            }

            // ---

            var dscScob = new SlashCommandOptionBuilder()
                .WithName("challenge")
                .WithDescription("Deep Stone Crypt challenge.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (DeepStoneCryptEncounter DSCE in Enum.GetValues(typeof(DeepStoneCryptEncounter)))
            {
                dscScob.AddChoice($"{DeepStoneCryptRotation.GetEncounterString(DSCE)} ({DeepStoneCryptRotation.GetChallengeString(DSCE)})", (int)DSCE);
            }

            // ---

            var empireHuntScob = new SlashCommandOptionBuilder()
                .WithName("empire-hunt")
                .WithDescription("Empire Hunt boss.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (EmpireHunt EH in Enum.GetValues(typeof(EmpireHunt)))
            {
                empireHuntScob.AddChoice($"{EmpireHuntRotation.GetHuntBossString(EH)}", (int)EH);
            }

            // ---

            var gosScob = new SlashCommandOptionBuilder()
                .WithName("challenge")
                .WithDescription("Garden of Salvation challenge.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (GardenOfSalvationEncounter GOSE in Enum.GetValues(typeof(GardenOfSalvationEncounter)))
            {
                gosScob.AddChoice($"{GardenOfSalvationRotation.GetEncounterString(GOSE)} ({GardenOfSalvationRotation.GetChallengeString(GOSE)})", (int)GOSE);
            }

            // ---

            var lwScob = new SlashCommandOptionBuilder()
                .WithName("challenge")
                .WithDescription("Last Wish challenge.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (LastWishEncounter LWE in Enum.GetValues(typeof(LastWishEncounter)))
            {
                lwScob.AddChoice($"{LastWishRotation.GetEncounterString(LWE)} ({LastWishRotation.GetChallengeString(LWE)})", (int)LWE);
            }

            // ---

            var lostSectorScob = new SlashCommandOptionBuilder()
                .WithName("lost-sector")
                .WithDescription("Lost Sector name.")
                .WithRequired(false) // False so people don't need to track the other option.
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (LostSector LS in Enum.GetValues(typeof(LostSector)))
            {
                lostSectorScob.AddChoice($"{LostSectorRotation.GetLostSectorString(LS)}", (int)LS);
            }

            var difficultyScob = new SlashCommandOptionBuilder()
                    .WithName("difficulty")
                    .WithDescription("Lost Sector difficulty.")
                    .WithRequired(false) // False so people don't need to track the other option.
                    .AddChoice("Legend", 0)
                    .AddChoice("Master", 1)
                    .WithType(ApplicationCommandOptionType.Integer);

            var armorScob = new SlashCommandOptionBuilder()
                .WithName("armor-drop")
                .WithDescription("Lost Sector Exotic armor drop.")
                .WithRequired(false) // False so people don't need to track the other option.
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (ExoticArmorType EAT in Enum.GetValues(typeof(ExoticArmorType)))
            {
                armorScob.AddChoice($"{EAT}", (int)EAT);
            }

            // ---

            var nightfallScob = new SlashCommandOptionBuilder()
                .WithName("nightfall")
                .WithDescription("Nightfall Strike.")
                .WithRequired(false) // False so people don't need to track the other option.
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (Nightfall NF in Enum.GetValues(typeof(Nightfall)))
            {
                nightfallScob.AddChoice($"{NightfallRotation.GetStrikeNameString(NF)}", (int)NF);
            }

            var nightfallWeaponScob = new SlashCommandOptionBuilder()
                .WithName("weapon")
                .WithDescription("Nightfall Strike Weapon drop.")
                .WithRequired(false) // False so people don't need to track the other option.
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (NightfallWeapon NFW in Enum.GetValues(typeof(NightfallWeapon)))
            {
                nightfallWeaponScob.AddChoice($"{NightfallRotation.GetWeaponString(NFW)}", (int)NFW);
            }

            // ---

            var nightmareHuntScob = new SlashCommandOptionBuilder()
                .WithName("nightmare-hunt")
                .WithDescription("Nightmare Hunt boss.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (NightmareHunt NH in Enum.GetValues(typeof(NightmareHunt)))
            {
                nightmareHuntScob.AddChoice($"{NightmareHuntRotation.GetHuntNameString(NH)} ({NightmareHuntRotation.GetHuntBossString(NH)})", (int)NH);
            }

            // ---

            var vogScob = new SlashCommandOptionBuilder()
                .WithName("challenge")
                .WithDescription("Vault of Glass challenge.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (VaultOfGlassEncounter VOGE in Enum.GetValues(typeof(VaultOfGlassEncounter)))
            {
                vogScob.AddChoice($"{VaultOfGlassRotation.GetEncounterString(VOGE)} ({VaultOfGlassRotation.GetChallengeString(VOGE)})", (int)VOGE);
            }

            // ---

            var notifyCommand = new SlashCommandBuilder()
                .WithName("notify")
                .WithDescription("Be notified when a specific rotation is active.")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("altars-of-sorrow")
                    .WithDescription("Be notified when an Altars of Sorrow weapon is active.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(altarsScob))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("ascendant-challenge")
                    .WithDescription("Be notified when an Ascendant Challenge is active.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(ascentantScob))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("curse-week")
                    .WithDescription("Be notified when a Curse Week strength is active.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(curseWeekScob))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("deep-stone-crypt")
                    .WithDescription("Be notified when a Deep Stone Crypt challenge is active.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(dscScob))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("empire-hunt")
                    .WithDescription("Be notified when an Empire Hunt is active.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(empireHuntScob))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("garden-of-salvation")
                    .WithDescription("Be notified when a Garden of Salvation challenge is active.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(gosScob))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("last-wish")
                    .WithDescription("Be notified when a Last Wish challenge is active.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(lwScob))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("lost-sector")
                    .WithDescription("Be notified when a Lost Sector and/or Armor Drop is active.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(lostSectorScob)
                    .AddOption(difficultyScob)
                    .AddOption(armorScob))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("nightfall")
                    .WithDescription("Be notified when a Nightfall and/or Weapon is active.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(nightfallScob)
                    .AddOption(nightfallWeaponScob))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("nightmare-hunt")
                    .WithDescription("Be notified when a Nightmare Hunt is active.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(nightmareHuntScob))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("vault-of-glass")
                    .WithDescription("Be notified when a Vault of Glass challenge is active.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(vogScob))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("remove")
                    .WithDescription("Remove an active tracking notification.")
                    .WithType(ApplicationCommandOptionType.SubCommand));

            // ==============================================

            var raidCommand = new SlashCommandBuilder()
                .WithName("raid")
                .WithDescription("Display Raid information.")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("raid")
                    .WithDescription("Raid name.")
                    .WithType(ApplicationCommandOptionType.Integer)
                    .AddChoice("Last Wish", 0)
                    .AddChoice("Garden of Salvation", 1)
                    .AddChoice("Deep Stone Crypt", 2)
                    .AddChoice("Vault of Glass", 3));

            // ==============================================

            var nightfallScob2 = new SlashCommandOptionBuilder()
                .WithName("nightfall")
                .WithDescription("Nightfall Strike.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (Nightfall NF in Enum.GetValues(typeof(Nightfall)))
            {
                nightfallScob2.AddChoice($"{NightfallRotation.GetStrikeNameString(NF)}", (int)NF);
            }

            var nightfallCommand = new SlashCommandBuilder()
                .WithName("nightfall")
                .WithDescription("Display Nightfall information.")
                .AddOption(nightfallScob2);

            // ==============================================

            var patrolCommand = new SlashCommandBuilder()
                .WithName("patrol")
                .WithDescription("Display Patrol information.")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("location")
                    .WithDescription("Patrol location.")
                    .WithType(ApplicationCommandOptionType.Integer)
                    .AddChoice("The Dreaming City", 0)
                    .AddChoice("The Moon", 1)
                    .AddChoice("Europa", 2));

            // ==============================================

            var freeEmblemsCommand = new SlashCommandBuilder();
            freeEmblemsCommand.WithName("free-emblems");
            freeEmblemsCommand.WithDescription("Display a list of universal emblem codes.");

            // ==============================================

            var dailyCommand = new SlashCommandBuilder();
            dailyCommand.WithName("daily");
            dailyCommand.WithDescription("Display Daily reset information.");

            // ==============================================

            var weeklyCommand = new SlashCommandBuilder();
            weeklyCommand.WithName("weekly");
            weeklyCommand.WithDescription("Display Weekly reset information.");

            // ==============================================

            var rankCommand = new SlashCommandBuilder();
            rankCommand.WithName("rank");
            rankCommand.WithDescription("Display a Destiny 2 leaderboard of choice.");

            var scobF = new SlashCommandOptionBuilder()
                .WithName("leaderboard")
                .WithDescription("Specific leaderboard to display.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (Leaderboard LB in Enum.GetValues(typeof(Leaderboard)))
            {
                scobF.AddChoice($"{LeaderboardHelper.GetLeaderboardString(LB)}", (int)LB);
            }

            rankCommand.AddOption(scobF);

            // ==============================================

            var lostSectorInfoCommand = new SlashCommandBuilder();
            lostSectorInfoCommand.WithName("lost-sector");
            lostSectorInfoCommand.WithDescription("Get Info on a Lost Sector based on Difficulty.");
            var scobC = new SlashCommandOptionBuilder()
                .WithName("lost-sector")
                .WithDescription("The Lost Sector you want Information on.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (LostSector LS in Enum.GetValues(typeof(LostSector)))
            {
                scobC.AddChoice($"{LostSectorRotation.GetLostSectorString(LS)}", (int)LS);
            }

            lostSectorInfoCommand.AddOption(scobC)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("difficulty")
                    .WithDescription("The Difficulty of the Lost Sector.")
                    .WithRequired(true)
                    .AddChoice("Legend", 0)
                    .AddChoice("Master", 1)
                    .WithType(ApplicationCommandOptionType.Integer));

            // ==============================================

            var alertCommand = new SlashCommandBuilder();
            alertCommand.WithName("alert");
            alertCommand.WithDescription("Set up announcements for Daily/Weekly Reset.");

            var scobG = new SlashCommandOptionBuilder()
                .WithName("type")
                .WithDescription("Choose between Daily or Weekly Reset.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);

            scobG.AddChoice($"Daily", 0);
            scobG.AddChoice($"Weekly", 1);

            alertCommand.AddOption(scobG);

            try
            {
                //await guild.CreateApplicationCommandAsync(notifyCommand.Build());

                //await _client.CreateGlobalApplicationCommandAsync(notifyCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(raidCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(nightfallCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(patrolCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(freeEmblemsCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(rankCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(lostSectorInfoCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(alertCommand.Build());

                //await _client.CreateGlobalApplicationCommandAsync(dailyCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(weeklyCommand.Build());
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
        }
        
        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.Data.Name.Equals("lost-sector"))
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
                    case Leaderboard.PowerLevel: embed = LeaderboardHelper.GetLeaderboardEmbed(PowerLevelData.GetSortedLevelData(), command.User); break;
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
                    DataConfig.DeleteChannelFromRotationConfig(command.Channel.Id, IsDaily);

                    await command.RespondAsync($"This channel will no longer receive {(IsDaily ? "Daily" : "Weekly")} reset posts. Run this command to re-subscribe to them!", ephemeral: true);
                    return;
                }
                else
                {
                    DataConfig.AddChannelToRotationConfig(command.Channel.Id, IsDaily);

                    await command.RespondAsync($"This channel is now successfully subscribed to {(IsDaily ? "Daily" : "Weekly")} reset posts.", ephemeral: true);
                    return;
                }
            }
            else if (command.Data.Name.Equals("notify"))
            {
                var notifyType = command.Data.Options.First().Name;

                if (notifyType.Equals("altars-of-sorrow"))
                {
                    if (AltarsOfSorrowRotation.GetUserTracking(command.User.Id, out var Weapon) != null)
                    {
                        await command.RespondAsync($"You already have tracking for Altars of Sorrow. I am watching for {AltarsOfSorrowRotation.GetWeaponNameString(Weapon)} ({Weapon}).", ephemeral: true);
                        return;
                    }
                    foreach (var option in command.Data.Options.First().Options)
                        if (option.Name.Equals("weapon"))
                            Weapon = (AltarsOfSorrow)Convert.ToInt32(option.Value);

                    AltarsOfSorrowRotation.AddUserTracking(command.User.Id, Weapon);
                    await command.RespondAsync($"I will remind you when {AltarsOfSorrowRotation.GetWeaponNameString(Weapon)} ({Weapon}) is in rotation, which will be on {TimestampTag.FromDateTime(AltarsOfSorrowRotation.DatePrediction(Weapon), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    return;
                }
                else if (notifyType.Equals("ascendant-challenge"))
                {
                    if (AscendantChallengeRotation.GetUserTracking(command.User.Id, out var AscendantChallenge) != null)
                    {
                        await command.RespondAsync($"You already have tracking for Ascendant Challenges. I am watching for {AscendantChallengeRotation.GetChallengeNameString(AscendantChallenge)} ({AscendantChallengeRotation.GetChallengeLocationString(AscendantChallenge)}).", ephemeral: true);
                        return;
                    }
                    foreach (var option in command.Data.Options.First().Options)
                        if (option.Name.Equals("ascendant-challenge"))
                            AscendantChallenge = (AscendantChallenge)Convert.ToInt32(option.Value);

                    AscendantChallengeRotation.AddUserTracking(command.User.Id, AscendantChallenge);
                    await command.RespondAsync($"I will remind you when {AscendantChallengeRotation.GetChallengeNameString(AscendantChallenge)} ({AscendantChallengeRotation.GetChallengeLocationString(AscendantChallenge)}) is in rotation, which will be on {TimestampTag.FromDateTime(AscendantChallengeRotation.DatePrediction(AscendantChallenge), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    return;
                }
                else if (notifyType.Equals("curse-week"))
                {
                    if (CurseWeekRotation.GetUserTracking(command.User.Id, out var CurseWeek) != null)
                    {
                        await command.RespondAsync($"You already have tracking for Curse Weeks. I am watching for {CurseWeek} Strength.", ephemeral: true);
                        return;
                    }
                    foreach (var option in command.Data.Options.First().Options)
                        if (option.Name.Equals("strength"))
                            CurseWeek = (CurseWeek)Convert.ToInt32(option.Value);

                    CurseWeekRotation.AddUserTracking(command.User.Id, CurseWeek);
                    await command.RespondAsync($"I will remind you when {CurseWeek} Strength is in rotation, which will be on {TimestampTag.FromDateTime(CurseWeekRotation.DatePrediction(CurseWeek), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    return;
                }
                else if (notifyType.Equals("deep-stone-crypt"))
                {
                    if (DeepStoneCryptRotation.GetUserTracking(command.User.Id, out var Encounter) != null)
                    {
                        await command.RespondAsync($"You already have tracking for Deep Stone Crypt challenges. I am watching for {DeepStoneCryptRotation.GetEncounterString(Encounter)} ({DeepStoneCryptRotation.GetChallengeString(Encounter)}).", ephemeral: true);
                        return;
                    }
                    foreach (var option in command.Data.Options.First().Options)
                        if (option.Name.Equals("challenge"))
                            Encounter = (DeepStoneCryptEncounter)Convert.ToInt32(option.Value);

                    DeepStoneCryptRotation.AddUserTracking(command.User.Id, Encounter);
                    await command.RespondAsync($"I will remind you when {DeepStoneCryptRotation.GetEncounterString(Encounter)} ({DeepStoneCryptRotation.GetChallengeString(Encounter)}) is in rotation, which will be on {TimestampTag.FromDateTime(DeepStoneCryptRotation.DatePrediction(Encounter), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    return;
                }
                else if (notifyType.Equals("empire-hunt"))
                {
                    if (EmpireHuntRotation.GetUserTracking(command.User.Id, out var Hunt) != null)
                    {
                        await command.RespondAsync($"You already have tracking for Empire Hunts. I am watching for {EmpireHuntRotation.GetHuntBossString(Hunt)}.", ephemeral: true);
                        return;
                    }
                    foreach (var option in command.Data.Options.First().Options)
                        if (option.Name.Equals("empire-hunt"))
                            Hunt = (EmpireHunt)Convert.ToInt32(option.Value);

                    EmpireHuntRotation.AddUserTracking(command.User.Id, Hunt);
                    await command.RespondAsync($"I will remind you when {EmpireHuntRotation.GetHuntBossString(Hunt)} is in rotation, which will be on {TimestampTag.FromDateTime(EmpireHuntRotation.DatePrediction(Hunt), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    return;
                }
                else if (notifyType.Equals("garden-of-salvation"))
                {
                    if (GardenOfSalvationRotation.GetUserTracking(command.User.Id, out var Encounter) != null)
                    {
                        await command.RespondAsync($"You already have tracking for Garden of Salvation challenges. I am watching for {GardenOfSalvationRotation.GetEncounterString(Encounter)} ({GardenOfSalvationRotation.GetChallengeString(Encounter)}).", ephemeral: true);
                        return;
                    }
                    foreach (var option in command.Data.Options.First().Options)
                        if (option.Name.Equals("challenge"))
                            Encounter = (GardenOfSalvationEncounter)Convert.ToInt32(option.Value);

                    GardenOfSalvationRotation.AddUserTracking(command.User.Id, Encounter);
                    await command.RespondAsync($"I will remind you when {GardenOfSalvationRotation.GetEncounterString(Encounter)} ({GardenOfSalvationRotation.GetChallengeString(Encounter)}) is in rotation, which will be on {TimestampTag.FromDateTime(GardenOfSalvationRotation.DatePrediction(Encounter), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    return;
                }
                else if (notifyType.Equals("last-wish"))
                {
                    if (LastWishRotation.GetUserTracking(command.User.Id, out var Encounter) != null)
                    {
                        await command.RespondAsync($"You already have tracking for Last Wish challenges. I am watching for {LastWishRotation.GetEncounterString(Encounter)} ({LastWishRotation.GetChallengeString(Encounter)}).", ephemeral: true);
                        return;
                    }
                    foreach (var option in command.Data.Options.First().Options)
                        if (option.Name.Equals("challenge"))
                            Encounter = (LastWishEncounter)Convert.ToInt32(option.Value);

                    LastWishRotation.AddUserTracking(command.User.Id, Encounter);
                    await command.RespondAsync($"I will remind you when {LastWishRotation.GetEncounterString(Encounter)} ({LastWishRotation.GetChallengeString(Encounter)}) is in rotation, which will be on {TimestampTag.FromDateTime(LastWishRotation.DatePrediction(Encounter), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    return;
                }
                else if (notifyType.Equals("lost-sector"))
                {
                    if (LostSectorRotation.GetUserTracking(command.User.Id, out var LS, out var LSD, out var EAT) != null)
                    {
                        if (LS == null && LSD == null && EAT == null)
                            await command.RespondAsync($"An error has occurred.", ephemeral: true);
                        else if (LS != null && LSD == null && EAT == null)
                            await command.RespondAsync($"You already have tracking for Lost Sectors. I am watching for {LostSectorRotation.GetLostSectorString((LostSector)LS)}.", ephemeral: true);
                        else if (LS != null && LSD != null && EAT == null)
                            await command.RespondAsync($"You already have tracking for Lost Sectors. I am watching for {LostSectorRotation.GetLostSectorString((LostSector)LS)} ({LSD}).", ephemeral: true);
                        else if (LS == null && LSD == null && EAT != null)
                            await command.RespondAsync($"You already have tracking for Lost Sectors. I am watching for {EAT} armor drop.", ephemeral: true);
                        else if (LS == null && LSD != null && EAT != null)
                            await command.RespondAsync($"You already have tracking for Lost Sectors. I am watching for {LSD} {EAT} armor drop.", ephemeral: true);
                        else if (LS != null && LSD != null && EAT != null)
                            await command.RespondAsync($"You already have tracking for Lost Sectors. I am watching for {LostSectorRotation.GetLostSectorString((LostSector)LS)} ({LSD}) dropping {EAT}.", ephemeral: true);

                        return;
                    }
                    foreach (var option in command.Data.Options.First().Options)
                    {
                        if (option.Name.Equals("lost-sector"))
                            LS = (LostSector)Convert.ToInt32(option.Value);
                        else if (option.Name.Equals("difficulty"))
                            LSD = (LostSectorDifficulty)Convert.ToInt32(option.Value);
                        else if (option.Name.Equals("armor-drop"))
                            EAT = (ExoticArmorType)Convert.ToInt32(option.Value);
                    }

                    if (LS == null && LSD != null && EAT == null)
                    {
                        await command.RespondAsync($"I cannot track a difficulty, they are always active.", ephemeral: true);
                        return;
                    }

                    LostSectorRotation.AddUserTracking(command.User.Id, LS, LSD, EAT);
                    if (LS == null && LSD == null && EAT == null)
                        await command.RespondAsync($"An error has occurred.", ephemeral: true);
                    else if (LS != null && LSD == null && EAT == null)
                        await command.RespondAsync($"I will remind you when {LostSectorRotation.GetLostSectorString((LostSector)LS)} is in rotation, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, LSD, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    else if (LS != null && LSD != null && EAT == null)
                        await command.RespondAsync($"I will remind you when {LostSectorRotation.GetLostSectorString((LostSector)LS)} ({LSD}) is in rotation, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, LSD, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    else if (LS == null && LSD == null && EAT != null)
                        await command.RespondAsync($"I will remind you when Lost Sectors are dropping {EAT}, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, LSD, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    else if (LS == null && LSD != null && EAT != null)
                        await command.RespondAsync($"I will remind you when {LSD} Lost Sectors are dropping {EAT}, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, LSD, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    else if (LS != null && LSD != null && EAT != null)
                        await command.RespondAsync($"I will remind you when {LostSectorRotation.GetLostSectorString((LostSector)LS)} ({LSD}) is dropping {EAT}, which will be on {TimestampTag.FromDateTime(LostSectorRotation.DatePrediction(LS, LSD, EAT), TimestampTagStyles.ShortDate)}.", ephemeral: true);

                    return;
                }
                else if (notifyType.Equals("nightfall"))
                {
                    if (NightfallRotation.GetUserTracking(command.User.Id, out var NF, out var Weapon) != null)
                    {
                        if (NF == null && Weapon == null)
                            await command.RespondAsync($"An error has occurred.", ephemeral: true);
                        else if (NF != null && Weapon == null)
                            await command.RespondAsync($"You already have tracking for Nightfalls. I am watching for {NightfallRotation.GetStrikeNameString((Nightfall)NF)}.", ephemeral: true);
                        else if (NF == null && Weapon != null)
                            await command.RespondAsync($"You already have tracking for Nightfalls. I am watching for {NightfallRotation.GetWeaponString((NightfallWeapon)Weapon)} weapon drops.", ephemeral: true);
                        else if (NF != null && Weapon != null)
                            await command.RespondAsync($"You already have tracking for Nightfalls. I am watching for {NightfallRotation.GetStrikeNameString((Nightfall)NF)} with {NightfallRotation.GetWeaponString((NightfallWeapon)Weapon)} weapon drops.", ephemeral: true);
                        return;
                    }
                    foreach (var option in command.Data.Options.First().Options)
                    {
                        if (option.Name.Equals("nightfall"))
                            NF = (Nightfall)Convert.ToInt32(option.Value);
                        else if (option.Name.Equals("weapon"))
                            Weapon = (NightfallWeapon)Convert.ToInt32(option.Value);
                    }

                    NightfallRotation.AddUserTracking(command.User.Id, NF, Weapon);
                    if (NF == null && Weapon == null)
                        await command.RespondAsync($"An error has occurred.", ephemeral: true);
                    else if (NF != null && Weapon == null)
                        await command.RespondAsync($"I will remind you when {NightfallRotation.GetStrikeNameString((Nightfall)NF)} is in rotation, which will be on {TimestampTag.FromDateTime(NightfallRotation.DatePrediction(NF, Weapon), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    else if (NF == null && Weapon != null)
                        await command.RespondAsync($"I will remind you when {NightfallRotation.GetWeaponString((NightfallWeapon)Weapon)} is in rotation, which will be on {TimestampTag.FromDateTime(NightfallRotation.DatePrediction(NF, Weapon), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    else if (NF != null && Weapon != null)
                        await command.RespondAsync($"I will remind you when {NightfallRotation.GetStrikeNameString((Nightfall)NF)} is dropping {NightfallRotation.GetWeaponString((NightfallWeapon)Weapon)}, which will be on {TimestampTag.FromDateTime(NightfallRotation.DatePrediction(NF, Weapon), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    return;
                }
                else if (notifyType.Equals("nightmare-hunt"))
                {
                    if (NightmareHuntRotation.GetUserTracking(command.User.Id, out var Hunt) != null)
                    {
                        await command.RespondAsync($"You already have tracking for Nightmare Hunts. I am watching for {NightmareHuntRotation.GetHuntNameString(Hunt)} ({NightmareHuntRotation.GetHuntBossString(Hunt)}).", ephemeral: true);
                        return;
                    }
                    foreach (var option in command.Data.Options.First().Options)
                        if (option.Name.Equals("nightmare-hunt"))
                            Hunt = (NightmareHunt)Convert.ToInt32(option.Value);

                    NightmareHuntRotation.AddUserTracking(command.User.Id, Hunt);
                    await command.RespondAsync($"I will remind you when {NightmareHuntRotation.GetHuntNameString(Hunt)} ({NightmareHuntRotation.GetHuntBossString(Hunt)}) is in rotation, which will be on {TimestampTag.FromDateTime(NightmareHuntRotation.DatePrediction(Hunt), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    return;
                }
                else if (notifyType.Equals("vault-of-glass"))
                {
                    if (VaultOfGlassRotation.GetUserTracking(command.User.Id, out var Encounter) != null)
                    {
                        await command.RespondAsync($"You already have tracking for Vault of Glass challenges. I am watching for {VaultOfGlassRotation.GetEncounterString(Encounter)} ({VaultOfGlassRotation.GetChallengeString(Encounter)}).", ephemeral: true);
                        return;
                    }
                    foreach (var option in command.Data.Options.First().Options)
                        if (option.Name.Equals("challenge"))
                            Encounter = (VaultOfGlassEncounter)Convert.ToInt32(option.Value);

                    VaultOfGlassRotation.AddUserTracking(command.User.Id, Encounter);
                    await command.RespondAsync($"I will remind you when {VaultOfGlassRotation.GetEncounterString(Encounter)} ({VaultOfGlassRotation.GetChallengeString(Encounter)}) is in rotation, which will be on {TimestampTag.FromDateTime(VaultOfGlassRotation.DatePrediction(Encounter), TimestampTagStyles.ShortDate)}.", ephemeral: true);
                    return;
                }
                else if (notifyType.Equals("remove"))
                {
                    // Build a selection menu with a list of all of the active trackings a user has.
                    var menuBuilder = new SelectMenuBuilder()
                        .WithPlaceholder("Select one of your active trackers")
                        .WithCustomId("notifyRemovalMenu")
                        .WithMinValues(1)
                        .WithMaxValues(1);

                    if (AltarsOfSorrowRotation.GetUserTracking(command.User.Id, out var Weapon) != null)
                        menuBuilder.AddOption("Altars of Sorrow", "altars-of-sorrow", $"{AltarsOfSorrowRotation.GetWeaponNameString(Weapon)} ({Weapon})");

                    if (AscendantChallengeRotation.GetUserTracking(command.User.Id, out var Challenge) != null)
                        menuBuilder.AddOption("Ascendant Challenge", "ascendant-challenge", $"{AscendantChallengeRotation.GetChallengeNameString(Challenge)} ({AscendantChallengeRotation.GetChallengeLocationString(Challenge)})");

                    if (CurseWeekRotation.GetUserTracking(command.User.Id, out var Strength) != null)
                        menuBuilder.AddOption("Curse Week", "curse-week", $"{Strength} Strength");

                    if (DeepStoneCryptRotation.GetUserTracking(command.User.Id, out var DSCEncounter) != null)
                        menuBuilder.AddOption("Deep Stone Crypt Challenge", "dsc-challenge", $"{DeepStoneCryptRotation.GetEncounterString(DSCEncounter)} ({DeepStoneCryptRotation.GetChallengeString(DSCEncounter)})");

                    if (EmpireHuntRotation.GetUserTracking(command.User.Id, out var EmpireHunt) != null)
                        menuBuilder.AddOption("Empire Hunt", "empire-hunt", $"{EmpireHuntRotation.GetHuntBossString(EmpireHunt)}");

                    if (GardenOfSalvationRotation.GetUserTracking(command.User.Id, out var GoSEncounter) != null)
                        menuBuilder.AddOption("Garden of Salvation Challenge", "gos-challenge", $"{GardenOfSalvationRotation.GetEncounterString(GoSEncounter)} ({GardenOfSalvationRotation.GetChallengeString(GoSEncounter)})");

                    if (LastWishRotation.GetUserTracking(command.User.Id, out var LWEncounter) != null)
                        menuBuilder.AddOption("Last Wish Challenge", "lw-challenge", $"{LastWishRotation.GetEncounterString(LWEncounter)} ({LastWishRotation.GetChallengeString(LWEncounter)})");

                    if (LostSectorRotation.GetUserTracking(command.User.Id, out var LS, out var LSD, out var EAT) != null)
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

                    if (NightfallRotation.GetUserTracking(command.User.Id, out var NF, out var NFWeapon) != null)
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

                    if (NightmareHuntRotation.GetUserTracking(command.User.Id, out var NightmareHunt) != null)
                        menuBuilder.AddOption("Nightmare Hunt", "nightmare-hunt", $"{NightmareHuntRotation.GetHuntNameString(NightmareHunt)} ({NightmareHuntRotation.GetHuntBossString(NightmareHunt)})");

                    if (VaultOfGlassRotation.GetUserTracking(command.User.Id, out var VoGEncounter) != null)
                        menuBuilder.AddOption("Vault of Glass Challenge", "vog-challenge", $"{VaultOfGlassRotation.GetEncounterString(VoGEncounter)} ({VaultOfGlassRotation.GetChallengeString(VoGEncounter)})");

                    var builder = new ComponentBuilder()
                        .WithSelectMenu(menuBuilder);

                    try
                    {
                        await command.RespondAsync($"Which rotation tracker did you want me to remove? Please dismiss this message after you are done.", ephemeral: true, components: builder.Build());
                    }
                    catch
                    {
                        await command.RespondAsync($"You do not have any active trackers. Use '/notify' to activate your first one!", ephemeral: true);
                    }
                }
            }
            else if (command.Data.Name.Equals("free-emblems"))
            {
                var auth = new EmbedAuthorBuilder()
                {
                    Name = $"Universal Emblem Codes",
                    IconUrl = _client.GetApplicationInfoAsync().Result.IconUrl,
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
                    $"*Redeem those codes [here](https://www.bungie.net/7/en/Codes/Redeem).*";
                embed.ThumbnailUrl = _client.GetApplicationInfoAsync().Result.IconUrl;

                await command.RespondAsync("", embed: embed.Build());
            }
            else if (command.Data.Name.Equals("raid"))
            {
                await command.RespondAsync($"Command is under construction! Wait for the next update.", ephemeral: true);
                return;
            }
            else if (command.Data.Name.Equals("patrol"))
            {
                await command.RespondAsync($"Command is under construction! Wait for the next update.", ephemeral: true);
                return;
            }
            else if (command.Data.Name.Equals("nightfall"))
            {
                await command.RespondAsync($"Command is under construction! Wait for the next update.", ephemeral: true);
                return;
            }
            else if (command.Data.Name.Equals("daily"))
            {
                await command.RespondAsync($"", embed: CurrentRotations.DailyResetEmbed().Build());
                return;
            }
            else if (command.Data.Name.Equals("weekly"))
            {
                await command.RespondAsync($"", embed: CurrentRotations.WeeklyResetEmbed().Build());
                return;
            }
            else
            {
                await command.RespondAsync($"Command is under construction! Wait for the next update.", ephemeral: true);
                return;
            }
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

        private async Task ButtonHandler(SocketMessageComponent interaction)
        {
            var customId = interaction.Data.CustomId;
            var user = (SocketGuildUser)interaction.User;
            var guild = user.Guild;
            var channel = interaction.Channel;

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

                int userLevel = DataConfig.GetAFKValues(user.Id, out int lvlProg, out bool isInShatteredThrone, out PrivacySetting fireteamPrivacy, out string CharacterId, out string errorStatus);

                if (!errorStatus.Equals("Success"))
                {
                    await interaction.RespondAsync($"Bungie API is temporarily down, therefore, I cannot enable our logging feature. Try again later. Reason: {errorStatus}", ephemeral: true);
                    return;
                }

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
                    PrivacySetting = fireteamPrivacy
                };

                await userLogChannel.ModifyAsync(x =>
                {
                    x.CategoryId = cc.Id;
                    x.Topic = $"{uniqueName} (Starting Level: {newUser.StartLevel} [{String.Format("{0:n0}", newUser.StartLevelProgress)}/100,000 XP]) - Time Started: {TimestampTag.FromDateTime(newUser.TimeStarted)}";
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
                var guardian = new Guardian(newUser.UniqueBungieName, newUser.BungieMembershipID, DataConfig.GetLinkedUser(user.Id).BungieMembershipType, CharacterId);
                await LogHelper.Log(userLogChannel, $"{uniqueName} is starting at Level {newUser.LastLoggedLevel} ({String.Format("{0:n0}", newUser.LastLevelProgress)}/100,000 XP).", guardian.GetGuardianEmbed());
                string recommend = newUser.PrivacySetting == PrivacySetting.Open || newUser.PrivacySetting == PrivacySetting.ClanAndFriendsOnly || newUser.PrivacySetting == PrivacySetting.FriendsOnly ? $" It is recommended to change your privacy to prevent people from joining you. {user.Mention}" : "";
                await LogHelper.Log(userLogChannel, $"{uniqueName} has fireteam on {privacy}.{recommend}");

                ActiveConfig.AddActiveUserToConfig(newUser);
                await UpdateBotActivity();

                await LogHelper.Log(userLogChannel, "User is subscribed to our Bungie API refreshes. Waiting for next refresh...");
                LogHelper.ConsoleLog($"Started logging for {newUser.UniqueBungieName}.");
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
                await LogHelper.Log(user.CreateDMChannelAsync().Result, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(aau.TimeStarted)}.", GenerateSessionSummary(aau).Result);

                await Task.Run(() => CheckLeaderboardData(aau));
                ActiveConfig.DeleteActiveUserFromConfig(user.Id);
                await UpdateBotActivity();
                await interaction.RespondAsync($"Stopped AFK logging for {aau.UniqueBungieName}.", ephemeral: true);
                LogHelper.ConsoleLog($"Stopped logging for {aau.UniqueBungieName} via user request.");
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
    }
}