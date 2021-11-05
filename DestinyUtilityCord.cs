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

namespace DestinyUtility
{
    public sealed class DestinyUtilityCord
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        public static readonly string BotConfigPath = @"Configs/botConfig.json";
        public static readonly string DataConfigPath = @"Configs/dataConfig.json";
        public static readonly string ActiveConfigPath = @"Configs/activeConfig.json";
        public static readonly string LostSectorTrackingConfigPath = @"Configs/lostSectorTrackingConfigPath.json";

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

        static void Main(string[] args) => new DestinyUtilityCord().StartAsync().GetAwaiter().GetResult();

        public async Task StartAsync()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;

            if (!CheckAndLoadConfigFiles())
                return;

            Console.WriteLine($"Current Bot Version: v{BotConfig.Version}");
            Console.WriteLine($"Current Developer Note: {BotConfig.Note}");

            Console.WriteLine($"Legend Lost Sector: {LostSectorTrackingConfig.GetLostSectorString(LostSectorTrackingConfig.CurrentLegendLostSector)} ({LostSectorTrackingConfig.CurrentLegendArmorDrop})");
            Console.WriteLine($"Master Lost Sector: {LostSectorTrackingConfig.GetLostSectorString(LostSectorTrackingConfig.GetMasterLostSector())} ({LostSectorTrackingConfig.GetMasterLostSectorArmorDrop()})");

            _client.Log += log =>
            {
                Console.WriteLine(log.ToString());
                return Task.CompletedTask;
            };

            await InitializeCommands().ConfigureAwait(false);

            await _client.LoginAsync(TokenType.Bot, BotConfig.DiscordToken).ConfigureAwait(false);
            await _client.StartAsync().ConfigureAwait(false);

            //await InitializeSlashCommands().ConfigureAwait(false);
            await UpdateBotActivity();

            Timer timer = new Timer(TimerCallback, null, 25000, 240000);

            if (DateTime.Now.Hour >= 10) // after daily reset
            {
                SetUpTimer(new DateTime(DateTime.Today.AddDays(1).Year, DateTime.Today.AddDays(1).Month, DateTime.Today.AddDays(1).Day, 10, 0, 0));
            }
            else
            {
                SetUpTimer(new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 10, 0, 0));
            }

            //SetUpTimer(new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 10, 14, 0));

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

        public async void DailyResetChanges(Object o = null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nDaily Reset Occurred.");
            LostSectorTrackingConfig.LostSectorChange();

            Console.WriteLine($"Legend Lost Sector: {LostSectorTrackingConfig.GetLostSectorString(LostSectorTrackingConfig.CurrentLegendLostSector)} ({LostSectorTrackingConfig.CurrentLegendArmorDrop})");
            Console.WriteLine($"Master Lost Sector: {LostSectorTrackingConfig.GetLostSectorString(LostSectorTrackingConfig.GetMasterLostSector())} ({LostSectorTrackingConfig.GetMasterLostSectorArmorDrop()})");

            await LostSectorTrackingConfig.PostLostSectorUpdate(_client);

            List<LostSectorTrackingConfig.LostSectorLink> temp = new List<LostSectorTrackingConfig.LostSectorLink>();
            foreach (var LSL in LostSectorTrackingConfig.LostSectorLinks)
            {
                bool addBack = true;
                if (LostSectorTrackingConfig.CurrentLegendLostSector == LSL.LostSector && LSL.Difficulty == LostSectorTrackingConfig.LostSectorDifficulty.Legend)
                {
                    if (LostSectorTrackingConfig.CurrentLegendArmorDrop == LSL.ArmorDrop)
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
                            $"{LostSectorTrackingConfig.GetLostSectorString(LSL.LostSector)} ({LSL.Difficulty}) is dropping {LSL.ArmorDrop} today! I have removed your tracking, good luck!");
                        addBack = false;
                    }
                }
                else if (LostSectorTrackingConfig.GetMasterLostSector() == LSL.LostSector && LSL.Difficulty == LostSectorTrackingConfig.LostSectorDifficulty.Master)
                {
                    if (LostSectorTrackingConfig.GetMasterLostSectorArmorDrop() == LSL.ArmorDrop)
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
                            $"{LostSectorTrackingConfig.GetLostSectorString(LSL.LostSector)} ({LSL.Difficulty}) is dropping {LSL.ArmorDrop} today! I have removed your tracking, good luck!");
                        addBack = false;
                    }
                }
                if (addBack)
                    temp.Add(LSL);
            }

            string json = File.ReadAllText(LostSectorTrackingConfigPath);
            LostSectorTrackingConfig lstConfig = JsonConvert.DeserializeObject<LostSectorTrackingConfig>(json);

            LostSectorTrackingConfig.UpdateLostSectorsList();
            LostSectorTrackingConfig.LostSectorLinks = temp;
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
            try
            {
                foreach (ActiveConfig.ActiveAFKUser aau in ActiveConfig.ActiveAFKUsers)
                {
                    ActiveConfig.ActiveAFKUser tempAau = aau;
                    int updatedLevel = DataConfig.GetUserSeasonPassLevel(tempAau.DiscordID, out int updatedProgression);
                    bool addBack = true;

                    if (!ActiveConfig.IsInShatteredThrone(DataConfig.GetLinkedUser(tempAau.DiscordID).BungieMembershipID, DataConfig.GetLinkedUser(tempAau.DiscordID).BungieMembershipType))
                    {
                        string uniqueName = DataConfig.GetUniqueBungieName(tempAau.DiscordID);

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

                        Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Stopped logging for {DataConfig.GetUniqueBungieName(aau.DiscordID)}.");
                        addBack = false;
                    }
                    else if (updatedLevel > tempAau.LastLoggedLevel)
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Level up detected. {tempAau.LastLoggedLevel} -> {updatedLevel}");

                        tempAau.LastLoggedLevel = updatedLevel;
                        tempAau.LastLevelProgress = updatedProgression;

                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, 
                            $"Start: {tempAau.StartLevel} ({tempAau.StartLevelProgress}/100000 XP). Now: {tempAau.LastLoggedLevel} ({tempAau.LastLevelProgress}/100000 XP)");
                    }
                    else if (updatedProgression <= tempAau.LastLevelProgress)
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"No XP change detected, attempting to refresh API again...");
                        tempAau = await RefreshSpecificUser(tempAau).ConfigureAwait(true);
                        if (tempAau == null) addBack = false;
                    }
                    else
                    {
                        await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Refreshed! Progress for {DataConfig.GetUniqueBungieName(tempAau.DiscordID)} (Level: {updatedLevel}): {tempAau.LastLevelProgress} XP -> {updatedProgression} XP");

                        tempAau.LastLoggedLevel = updatedLevel;
                        tempAau.LastLevelProgress = updatedProgression;
                    }
                    if (addBack)
                        temp.Add(tempAau);

                    await Task.Delay(2500); // we dont want to spam API if we have a ton of AFK subscriptions
                }

                string json = File.ReadAllText(ActiveConfigPath);
                ActiveConfig aConfig = JsonConvert.DeserializeObject<ActiveConfig>(json);

                ActiveConfig.UpdateActiveAFKUsersList();
                ActiveConfig.ActiveAFKUsers = temp;
                string output = JsonConvert.SerializeObject(aConfig, Formatting.Indented);
                File.WriteAllText(ActiveConfigPath, output);

                await UpdateBotActivity();
                Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Bungie API Refreshed!");
            }
            catch (Exception x)
            {
                Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Refresh failed, trying again!");
                await Task.Delay(8000);
                await RefreshBungieAPI();
            }
        }

        private async Task<ActiveConfig.ActiveAFKUser> RefreshSpecificUser(ActiveConfig.ActiveAFKUser aau)
        {
            await Task.Delay(20000);
            Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Refreshing Bungie API specifically for {DataConfig.GetUniqueBungieName(aau.DiscordID)}.");
            List<ActiveConfig.ActiveAFKUser> temp = new List<ActiveConfig.ActiveAFKUser>();
            ActiveConfig.ActiveAFKUser tempAau = new ActiveConfig.ActiveAFKUser();
            try
            {
                tempAau = aau;
                int updatedLevel = DataConfig.GetUserSeasonPassLevel(aau.DiscordID, out int updatedProgression);

                if (updatedLevel > aau.LastLoggedLevel)
                {
                    await LogHelper.Log(_client.GetChannelAsync(aau.DiscordChannelID).Result as ITextChannel, $"Level up detected. {aau.LastLoggedLevel} -> {updatedLevel}");

                    tempAau.LastLoggedLevel = updatedLevel;
                    tempAau.LastLevelProgress = updatedProgression;
                }
                else if (updatedProgression == aau.LastLevelProgress)
                {
                    await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"Potential wipe detected.");
                    await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"<@{aau.DiscordID}>: Logging terminated by automation. Here is your session summary:", GenerateSessionSummary(tempAau).Result, GenerateDeleteChannelButton());

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
                    string uniqueName = DataConfig.GetUniqueBungieName(tempAau.DiscordID);
                    await LogHelper.Log(user.CreateDMChannelAsync().Result, $"<@{aau.DiscordID}>: Potential wipe detected. Logging will be terminated for {uniqueName}.");
                    await LogHelper.Log(user.CreateDMChannelAsync().Result, $"Here is the session summary, beginning on {aau.TimeStarted:G} (UTC-7).", GenerateSessionSummary(aau).Result);

                    Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Stopped logging for {DataConfig.GetUniqueBungieName(aau.DiscordID)}.");

                    return null;
                }
                else if (updatedProgression < aau.LastLevelProgress)
                {
                    await LogHelper.Log(_client.GetChannelAsync(tempAau.DiscordChannelID).Result as ITextChannel, $"API backstepped. Waiting for next refresh.");
                    return tempAau;
                }
                else
                {
                    await LogHelper.Log(_client.GetChannelAsync(aau.DiscordChannelID).Result as ITextChannel, $"Refreshed! Progress for {DataConfig.GetUniqueBungieName(aau.DiscordID)} (Level: {updatedLevel}): {aau.LastLevelProgress} XP -> {updatedProgression} XP");
                    tempAau.LastLoggedLevel = updatedLevel;
                    tempAau.LastLevelProgress = updatedProgression;
                }

                Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] API Refreshed for {DataConfig.GetUniqueBungieName(aau.DiscordID)}!");

                return tempAau;
            }
            catch (Exception x)
            {
                Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Refresh for {DataConfig.GetUniqueBungieName(aau.DiscordID)} failed!");
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
                Name = $"Session Summary: {DataConfig.GetUniqueBungieName(aau.DiscordID)}",
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
            int xpPerHour = 0;
            if ((DateTime.Now - aau.TimeStarted).TotalHours >= 1)
                xpPerHour = (int)Math.Floor(xpGained / (DateTime.Now - aau.TimeStarted).TotalHours);
            embed.Description =
                $"Total Levels Gained: {levelsGained}\n" +
                $"Total XP Gained: {xpGained}\n" +
                $"Total Time: {String.Format("{0:0.00}", (DateTime.Now - aau.TimeStarted).TotalHours)} hours\n" +
                $"XP Per Hour: {xpPerHour}";

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
            _client.SelectMenuExecuted += SelectMenuHandler;
        }

        private async Task InitializeSlashCommands()
        {
            var guild = _client.GetGuild(600548936062730259);
            //await guild.DeleteApplicationCommandsAsync();
            //var cmds = await _client.Rest.GetGlobalApplicationCommands();

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
            foreach (LostSectorTrackingConfig.LostSector LS in Enum.GetValues(typeof(LostSectorTrackingConfig.LostSector)))
            {
                scobA.AddChoice($"{LostSectorTrackingConfig.GetLostSectorString(LS)}", (int)LS);
            }

            var scobB = new SlashCommandOptionBuilder()
                .WithName("armor-drop")
                .WithDescription("The Exotic Armor the Lost Sector should drop")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (LostSectorTrackingConfig.ExoticArmorType EAT in Enum.GetValues(typeof(LostSectorTrackingConfig.ExoticArmorType)))
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

            // 8==============================================D

            var lostSectorInfoCommand = new SlashCommandBuilder();
            lostSectorInfoCommand.WithName("lostsectorinfo");
            lostSectorInfoCommand.WithDescription("Get Info on a Lost Sector based on Difficulty");
            var scobC = new SlashCommandOptionBuilder()
                .WithName("lost-sector")
                .WithDescription("The Lost Sector you want Information on")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (LostSectorTrackingConfig.LostSector LS in Enum.GetValues(typeof(LostSectorTrackingConfig.LostSector)))
            {
                scobC.AddChoice($"{LostSectorTrackingConfig.GetLostSectorString(LS)}", (int)LS);
            }

            lostSectorInfoCommand.AddOption(scobC)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("difficulty")
                    .WithDescription("The Difficulty of the Lost Sector")
                    .WithRequired(true)
                    .AddChoice("Legend", 0)
                    .AddChoice("Master", 1)
                    .WithType(ApplicationCommandOptionType.Integer));

            // 8==============================================D

            var nextOccuranceCommand = new SlashCommandBuilder();
            nextOccuranceCommand.WithName("next");
            nextOccuranceCommand.WithDescription("Find out when the next occurance of a lost sector and/or Exotic Armor type is");

            var scobD = new SlashCommandOptionBuilder()
                .WithName("lost-sector")
                .WithDescription("The Lost Sector you want me to get the occurance of")
                .WithRequired(false)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (LostSectorTrackingConfig.LostSector LS in Enum.GetValues(typeof(LostSectorTrackingConfig.LostSector)))
            {
                scobD.AddChoice($"{LostSectorTrackingConfig.GetLostSectorString(LS)}", (int)LS);
            }

            var scobE = new SlashCommandOptionBuilder()
                .WithName("armor-drop")
                .WithDescription("The Exotic Armor you want me to get the occurance of")
                .WithRequired(false)
                .WithType(ApplicationCommandOptionType.Integer);
            foreach (LostSectorTrackingConfig.ExoticArmorType EAT in Enum.GetValues(typeof(LostSectorTrackingConfig.ExoticArmorType)))
            {
                scobE.AddChoice($"{EAT}", (int)EAT);
            }

            nextOccuranceCommand.AddOption(scobD).AddOption(scobE);

            //  8==============================================D

            var removeAlertCommand = new SlashCommandBuilder();
            removeAlertCommand.WithName("removealert");
            removeAlertCommand.WithDescription("Remove an active tracking alert");

            try
            {
                //await guild.CreateApplicationCommandAsync(globalCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(lostSectorAlertCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(lostSectorInfoCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(nextOccuranceCommand.Build());
                //await _client.CreateGlobalApplicationCommandAsync(removeAlertCommand.Build());
            }
            catch (ApplicationCommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
        }

        /*private async Task HandleInteraction(SocketInteraction interaction)
        {
            // Checking the type of this interaction
            switch (interaction)
            {
                // Slash commands
                case SocketSlashCommand commandInteraction:
                    break;

                // Button clicks/selection dropdowns
                case SocketMessageComponent componentInteraction:
                    await MyMessageComponentHandler(componentInteraction);
                    break;

                // Unused or Unknown/Unsupported
                default:
                    break;
            }
        }*/

        private async Task SelectMenuHandler(SocketMessageComponent interaction)
        {

        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.Data.Name.Equals("lostsectoralert"))
            {
                if (LostSectorTrackingConfig.IsExistingLinkedLostSectorsTracking(command.User.Id))
                {
                    await command.RespondAsync($"You already have an active alert set up!", ephemeral: true);
                    return;
                }
                else
                {
                    LostSectorTrackingConfig.LostSector LS = 0;
                    LostSectorTrackingConfig.LostSectorDifficulty LSD = 0;
                    LostSectorTrackingConfig.ExoticArmorType EAT = 0;

                    foreach (var option in command.Data.Options)
                    {
                        if (option.Name.Equals("lost-sector"))
                            LS = (LostSectorTrackingConfig.LostSector)Convert.ToInt32(option.Value);
                        else if (option.Name.Equals("difficulty"))
                            LSD = (LostSectorTrackingConfig.LostSectorDifficulty)Convert.ToInt32(option.Value);
                        else if (option.Name.Equals("armor-drop"))
                            EAT = (LostSectorTrackingConfig.ExoticArmorType)Convert.ToInt32(option.Value);
                    }

                    LostSectorTrackingConfig.AddLostSectorsTrackingToConfig(command.User.Id, LS, LSD, EAT);

                    await command.RespondAsync($"We will remind you when {LostSectorTrackingConfig.GetLostSectorString(LS)} ({LSD}) is dropping {EAT}, which will be on " +
                        $"{(LSD == LostSectorTrackingConfig.LostSectorDifficulty.Legend ? $"{DateTime.Now.AddDays(LostSectorTrackingConfig.DaysUntilNextOccurance(LS, EAT)).Date:d}" : $"{DateTime.Now.AddDays(1 + LostSectorTrackingConfig.DaysUntilNextOccurance(LS, EAT)).Date:d}")}.",
                        ephemeral: true);
                    return;
                }
            }
            else if (command.Data.Name.Equals("removealert"))
            {
                if (!LostSectorTrackingConfig.IsExistingLinkedLostSectorsTracking(command.User.Id))
                {
                    await command.RespondAsync($"You don't have an active Lost Sector tracker.", ephemeral: true);
                    return;
                }

                await command.RespondAsync($"Removed your tracking for {LostSectorTrackingConfig.GetLostSectorString(LostSectorTrackingConfig.GetLostSectorsTracking(command.User.Id).LostSector)}" +
                    $" ({LostSectorTrackingConfig.GetLostSectorsTracking(command.User.Id).Difficulty}).", ephemeral: true);
                LostSectorTrackingConfig.DeleteLostSectorsTrackingFromConfig(command.User.Id);
                
            }
            else if (command.Data.Name.Equals("lostsectorinfo"))
            {
                LostSectorTrackingConfig.LostSector LS = 0;
                LostSectorTrackingConfig.LostSectorDifficulty LSD = 0;

                foreach (var option in command.Data.Options)
                {
                    if (option.Name.Equals("lost-sector"))
                        LS = (LostSectorTrackingConfig.LostSector)Convert.ToInt32(option.Value);
                    else if (option.Name.Equals("difficulty"))
                        LSD = (LostSectorTrackingConfig.LostSectorDifficulty)Convert.ToInt32(option.Value);
                }

                await command.RespondAsync($"", embed: LostSectorTrackingConfig.GetLostSectorEmbed(LS, LSD).Build());
                return;
            }
            else if (command.Data.Name.Equals("next"))
            {
                LostSectorTrackingConfig.LostSector? LS = null;
                LostSectorTrackingConfig.ExoticArmorType? EAT = null;

                foreach (var option in command.Data.Options)
                {
                    if (option.Name.Equals("lost-sector"))
                        LS = (LostSectorTrackingConfig.LostSector)Convert.ToInt32(option.Value);
                    else if (option.Name.Equals("armor-drop"))
                        EAT = (LostSectorTrackingConfig.ExoticArmorType)Convert.ToInt32(option.Value);
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

                    embed.Description = $"Legend: **{LostSectorTrackingConfig.GetLostSectorString(LostSectorTrackingConfig.GetPredictedLegendLostSector(1))}** dropping **{LostSectorTrackingConfig.GetPredictedLegendLostSectorArmorDrop(1)}**\n" +
                        $"Master: **{LostSectorTrackingConfig.GetLostSectorString(LostSectorTrackingConfig.CurrentLegendLostSector)}** dropping **{LostSectorTrackingConfig.CurrentLegendArmorDrop}**";

                    await command.RespondAsync($"", embed: embed.Build());
                    return;
                }

                var legendDate = DateTime.Now.AddDays(LostSectorTrackingConfig.DaysUntilNextOccurance(LS, EAT)).Date;
                var masterDate = DateTime.Now.AddDays(1 + LostSectorTrackingConfig.DaysUntilNextOccurance(LS, EAT)).Date;

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

                    embed.Description = $"Lost Sector: {LostSectorTrackingConfig.GetLostSectorString(LostSectorTrackingConfig.GetPredictedLegendLostSector(LostSectorTrackingConfig.DaysUntilNextOccurance(LS, EAT)))}\n" +
                        $"Legend: **{legendDate.Date:d}**\n" +
                        $"Master: **{masterDate.Date:d}**";

                    await command.RespondAsync($"", embed: embed.Build());
                    return;
                }
                else if (EAT == null)
                {
                    var auth = new EmbedAuthorBuilder()
                    {
                        Name = $"Next occurance of {LostSectorTrackingConfig.GetLostSectorString((LostSectorTrackingConfig.LostSector)LS)}",
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

                    embed.Description = $"Exotic Drop: **{LostSectorTrackingConfig.GetPredictedLegendLostSectorArmorDrop(LostSectorTrackingConfig.DaysUntilNextOccurance(LS, EAT))}**\n" +
                        $"Legend: **{legendDate.Date:d}**\n" +
                        $"Master: **{masterDate.Date:d}**";

                    await command.RespondAsync($"", embed: embed.Build());
                    return;
                }
                else
                {
                    var auth = new EmbedAuthorBuilder()
                    {
                        Name = $"Next occurance of {LostSectorTrackingConfig.GetLostSectorString((LostSectorTrackingConfig.LostSector)LS)} dropping {EAT}",
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

                    embed.Description = $"Legend: **{legendDate.Date:d}**\n" +
                        $"Master: **{masterDate.Date:d}**";

                    await command.RespondAsync($"", embed: embed.Build());
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
                    await interaction.RespondAsync($"Unfortunately, we are at the maximum number of users to watch ({ActiveConfig.MaximumThrallwayUsers}). Try again later.", ephemeral: true);
                    return;
                }

                if (IsBungieAPIDown())
                {
                    await interaction.RespondAsync($"Bungie API is temporarily down, therefore, we cannot enable our logging feature. Try again later.", ephemeral: true);
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

                if (!ActiveConfig.IsPlayerOnline(memId, memType))
                {
                    await interaction.RespondAsync($"You are not currently playing Destiny 2. Launch Destiny 2 then launch Shattered Throne, get set up, and then click \"Ready\".", ephemeral: true);
                    return;
                }

                if (!ActiveConfig.IsInShatteredThrone(memId, memType))
                {
                    await interaction.RespondAsync($"You are not in Shattered Throne. Launch Shattered Throne, get set up, and then click \"Ready\".", ephemeral: true);
                    return;
                }

                string uniqueName = DataConfig.GetUniqueBungieName(user.Id);

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

                var userLogChannel = guild.CreateTextChannelAsync($"{uniqueName}").Result;
                await LogHelper.Log(userLogChannel, "Getting things ready...");
                await interaction.RespondAsync($"Your channel is setup! View it here: {userLogChannel.Mention}.", ephemeral: true);

                int userLevel = DataConfig.GetUserSeasonPassLevel(user.Id, out int lvlProg);
                ActiveConfig.ActiveAFKUser newUser = new ActiveConfig.ActiveAFKUser
                {
                    DiscordID = user.Id,
                    BungieMembershipID = memId,
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
                    x.Topic = $"{uniqueName} (Starting Level: {newUser.StartLevel} [{newUser.StartLevelProgress}/100000 XP]) - Time Started: {DateTime.UtcNow}-UTC";
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
                await LogHelper.Log(userLogChannel, $"{uniqueName} is starting at Level {newUser.LastLoggedLevel} ({newUser.LastLevelProgress}/100000 XP).");
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
                string uniqueName = DataConfig.GetUniqueBungieName(user.Id);

                ActiveConfig.DeleteActiveUserFromConfig(user.Id);
                await UpdateBotActivity();
                await interaction.RespondAsync($"Stopped AFK logging for {uniqueName}.", ephemeral: true);
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

        private bool CheckAndLoadConfigFiles()
        {
            BotConfig bConfig;
            DataConfig dConfig;
            ActiveConfig aConfig;
            LostSectorTrackingConfig lstConfig;

            bool closeProgram = false;
            if (File.Exists(BotConfigPath))
            {
                string json = File.ReadAllText(BotConfigPath);
                bConfig = JsonConvert.DeserializeObject<BotConfig>(json);
            }
            else
            {
                bConfig = new BotConfig();
                File.WriteAllText(BotConfigPath, JsonConvert.SerializeObject(bConfig, Formatting.Indented));
                Console.WriteLine($"No botConfig.json file detected. A new one has been created and the program has stopped. Go and change API tokens and other items.");
                closeProgram = true;
            }

            if (File.Exists(DataConfigPath))
            {
                string json = File.ReadAllText(DataConfigPath);
                dConfig = JsonConvert.DeserializeObject<DataConfig>(json);
            }
            else
            {
                dConfig = new DataConfig();
                File.WriteAllText(DataConfigPath, JsonConvert.SerializeObject(dConfig, Formatting.Indented));
                Console.WriteLine($"No dataConfig.json file detected. A new one has been created and the program has stopped. No action is needed.");
                closeProgram = true;
            }

            if (File.Exists(ActiveConfigPath))
            {
                string json = File.ReadAllText(ActiveConfigPath);
                aConfig = JsonConvert.DeserializeObject<ActiveConfig>(json);

                try
                {
                    foreach (ActiveConfig.ActiveAFKUser aau in ActiveConfig.ActiveAFKUsers)
                    {
                        int updatedLevel = DataConfig.GetUserSeasonPassLevel(aau.DiscordID, out int updatedProgression);
                        aau.LastLevelProgress = updatedProgression;
                        aau.LastLoggedLevel = updatedLevel;
                    }
                }
                catch (Exception x)
                {
                    DataConfig.UpdateUsersList();
                    Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Bungie API is down, loading stored data and continuing.");
                }
                

                string output = JsonConvert.SerializeObject(aConfig, Formatting.Indented);
                File.WriteAllText(ActiveConfigPath, output);
            }
            else
            {
                aConfig = new ActiveConfig();
                File.WriteAllText(ActiveConfigPath, JsonConvert.SerializeObject(aConfig, Formatting.Indented));
                Console.WriteLine($"No activeConfig.json file detected. A new one has been created and the program has stopped. Go and change API tokens and other items.");
                closeProgram = true;
            }

            if (File.Exists(LostSectorTrackingConfigPath))
            {
                string json = File.ReadAllText(LostSectorTrackingConfigPath);
                lstConfig = JsonConvert.DeserializeObject<LostSectorTrackingConfig>(json);
            }
            else
            {
                lstConfig = new LostSectorTrackingConfig();
                File.WriteAllText(LostSectorTrackingConfigPath, JsonConvert.SerializeObject(lstConfig, Formatting.Indented));
                Console.WriteLine($"No lostSectorTrackingConfig.json file detected. A new one has been created and the program has stopped. Go and change API tokens and other items.");
                closeProgram = true;
            }

            if (closeProgram == true) return false;
            return true;
        }
    }
}
