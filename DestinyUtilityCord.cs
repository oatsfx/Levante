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

            _client.Log += log =>
            {
                Console.WriteLine(log.ToString());
                return Task.CompletedTask;
            };

            await InitializeCommands().ConfigureAwait(false);

            await _client.LoginAsync(TokenType.Bot, BotConfig.DiscordToken).ConfigureAwait(false);
            await _client.StartAsync().ConfigureAwait(false);

            await UpdateBotActivity();

            Timer timer = new Timer(TimerCallback, null, 25000, 240000);

            await Task.Delay(-1);
        }
        
        private async Task UpdateBotActivity()
        {
            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "" : "s";
            await _client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count} Thrallway Farmer{s}", ActivityType.Watching));
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
            ulong chanId = 0;
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
                await LogHelper.Log(_client.GetChannelAsync(chanId).Result as ITextChannel, $"Exception found: {x}");
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
                    //await (_client.GetChannel(tempAau.DiscordChannelID) as SocketGuildChannel).DeleteAsync();

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
            _client.InteractionCreated += HandleInteraction;
        }

        private async Task HandleInteraction(SocketInteraction interaction)
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
        }

        private async Task MyMessageComponentHandler(SocketMessageComponent interaction)
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
                    Text = $"-Thrallway Logger"
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

                await interaction.FollowupAsync($"", embeds, false, ephemeral: true);
            }
            else if (customId.Contains($"startAFK"))
            {
                if (IsBungieAPIDown())
                {
                    await interaction.FollowupAsync($"Bungie API is temporarily down, therefore, we cannot enable our logging feature. Try again later.", ephemeral: true);
                    return;
                }

                if (!DataConfig.IsExistingLinkedUser(user.Id))
                {
                    await interaction.FollowupAsync($"You are not registered! Use \"{BotConfig.DefaultCommandPrefix}linkHelp\" to learn how to register.", ephemeral: true);
                    return;
                }

                if (ActiveConfig.IsExistingActiveUser(user.Id))
                {
                    await interaction.FollowupAsync($"You are already actively using our logging feature.", ephemeral: true);
                    return;
                }

                string memId = DataConfig.GetLinkedUser(user.Id).BungieMembershipID;
                string memType = DataConfig.GetLinkedUser(user.Id).BungieMembershipType;

                if (!ActiveConfig.IsPlayerOnline(memId, memType))
                {
                    await interaction.FollowupAsync($"You are not currently playing Destiny 2. Launch Destiny 2 then launch Shattered Throne, get set up, and then click \"Ready\".", ephemeral: true);
                    return;
                }

                if (!ActiveConfig.IsInShatteredThrone(memId, memType))
                {
                    await interaction.FollowupAsync($"You are not in Shattered Throne. Launch Shattered Throne, get set up, and then click \"Ready\".", ephemeral: true);
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

                var userLogChannel = guild.CreateTextChannelAsync($"{uniqueName}").Result;

                await LogHelper.Log(userLogChannel, "Getting things ready...");

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
                    x.Topic = $"{uniqueName} (Starting Level: {newUser.StartLevel} [{newUser.StartLevelProgress}/100000 XP]) - Time Started: {DateTime.Now}-GMT";
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
                    await interaction.FollowupAsync($"You are not registered! Use \"{BotConfig.DefaultCommandPrefix}linkHelp\" to learn how to register.", ephemeral: true);
                    return;
                }

                if (!ActiveConfig.IsExistingActiveUser(user.Id))
                {
                    await interaction.FollowupAsync($"You are not actively using our logging feature.", ephemeral: true);
                    return;
                }

                var aau = ActiveConfig.GetActiveAFKUser(user.Id);

                Emoji deleteEmote = new Emoji("⛔");
                var buttonBuilder = new ComponentBuilder()
                            .WithButton("Delete Log Channel", customId: $"deleteChannel", ButtonStyle.Secondary, deleteEmote, row: 0);

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

                await LogHelper.Log(_client.GetChannelAsync(aau.DiscordChannelID).Result as ITextChannel, $"<@{user.Id}>: Logging terminated by user. Here is your session summary:", Embed: embed, CB: buttonBuilder);
                string uniqueName = DataConfig.GetUniqueBungieName(user.Id);

                ActiveConfig.DeleteActiveUserFromConfig(user.Id);
                await UpdateBotActivity();
                await interaction.FollowupAsync($"Stopped AFK logging for {uniqueName}.", ephemeral: true);
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
                    await context.Channel.SendMessageAsync($"[{error}]: {result.ErrorReason}*").ConfigureAwait(false);
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
            bool closeProgram = false;
            if (File.Exists(BotConfigPath))
            {
                string json = File.ReadAllText(BotConfigPath);
                bConfig = JsonConvert.DeserializeObject<BotConfig>(json);
            }
            else
            {
                bConfig = new BotConfig();
                File.WriteAllText(BotConfigPath, JsonConvert.SerializeObject(bConfig));
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
                File.WriteAllText(DataConfigPath, JsonConvert.SerializeObject(dConfig));
                Console.WriteLine($"No dataConfig.json file detected. A new one has been created and the program has stopped. No action is needed.");
                closeProgram = true;
            }

            if (File.Exists(ActiveConfigPath))
            {
                string json = File.ReadAllText(ActiveConfigPath);
                aConfig = JsonConvert.DeserializeObject<ActiveConfig>(json);

                foreach (ActiveConfig.ActiveAFKUser aau in ActiveConfig.ActiveAFKUsers)
                {
                    int updatedLevel = DataConfig.GetUserSeasonPassLevel(aau.DiscordID, out int updatedProgression);
                    aau.LastLevelProgress = updatedProgression;
                    aau.LastLoggedLevel = updatedLevel;
                }

                string output = JsonConvert.SerializeObject(aConfig, Formatting.Indented);
                File.WriteAllText(ActiveConfigPath, output);
            }
            else
            {
                aConfig = new ActiveConfig();
                File.WriteAllText(ActiveConfigPath, JsonConvert.SerializeObject(aConfig));
                Console.WriteLine($"No activeConfig.json file detected. A new one has been created and the program has stopped. Go and change API tokens and other items.");
                closeProgram = true;
            }

            if (closeProgram == true) return false;
            return true;
        }
    }
}
