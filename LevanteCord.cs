using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Levante.Commands;
using Levante.Configs;
using Levante.Helpers;
using Levante.Services;
using Levante.Structs;
using Levante.Util;
using Levante.Util.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly UserService _users;
        private readonly GuildService _guilds;
        private readonly LoggingService _logging;
        private readonly IServiceProvider _services;

        private Timer DailyResetTimer;
        private Timer _xpTimer;
        private Timer _leaderboardTimer;

        public LevanteCord()
        {
            var config = new DiscordSocketConfig
            {
                TotalShards = AppConfig.Discord.Shards,
                GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildInvites & ~GatewayIntents.GuildScheduledEvents,
            };

            _client = new DiscordShardedClient(config);
            _commands = new CommandService();
            _interaction = new InteractionService(_client);
            _users = new UserService();
            _guilds = new GuildService();
            _logging = new LoggingService(_users);
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton<InteractiveService>()
                .AddSingleton<InteractionService>(p => new InteractionService(p.GetRequiredService<DiscordShardedClient>()))
                .AddSingleton<NewsService>()
                .AddSingleton<LoggingService>(_logging)
                .AddSingleton<UserService>(_users)
                .AddSingleton<GuildService>(_guilds)
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
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.DiscordLogSink()
                .Enrich.FromLogContext()
                .CreateLogger();

            new LevanteCord().StartAsync().GetAwaiter().GetResult();
        }

        public async Task StartAsync()
        {
            await Task.Run(() => Console.Title = $"{AppConfig.App.Name} v{AppConfig.App.Version}");

            var currentTime = DateTime.UtcNow;
            SetUpTimer(currentTime.Hour >= 17 ? new DateTime(currentTime.AddDays(1).Year, currentTime.AddDays(1).Month, currentTime.AddDays(1).Day, 17, 0, 5) : new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 17, 0, 5));

            ManifestHelper.LoadManifestDictionaries();
            EmblemOffer.LoadCurrentOffers();

            if (!AppConfig.IsDebug)
            {
                //var oauthManager = new OAuthHelper();
            }
            else
            {
                Log.Debug("[{Type}] Debugging.", "Startup");
            }

            await InitializeListeners().ConfigureAwait(false);
            var client = _services.GetRequiredService<DiscordShardedClient>();
            var commands = _services.GetRequiredService<InteractionService>();
            var news = _services.GetRequiredService<NewsService>();

            client.Log += LogAsync;

            client.EntitlementCreated += HandleEntitlementCreated;

            await _client.LoginAsync(TokenType.Bot, AppConfig.Discord.Token);
            await _client.StartAsync();

            Log.Information("[{Type}] Bot successfully started! v{@Version}",
                "Startup", AppConfig.App.Version);

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
                    var normalCount = _logging.GetXpLoggingUserCount();
                    var priorityCount = _logging.GetPriorityXpLoggingUserCount();
                    var maxCount = _logging.GetMaxLoggingUsers();
                    string s = normalCount == 1 ? "'s" : "s'";
                    string p = normalCount != 0 ? $" (+{priorityCount})" : "";
                    await _client.SetActivityAsync(new Game($"{normalCount}/{maxCount}{p} User{s} Game", ActivityType.Watching)); break;
                case 1:
                    await _client.SetActivityAsync(new Game($"{AppConfig.Discord.PlayingStatuses[rand.Next(0, AppConfig.Discord.PlayingStatuses.Count)]} | v{AppConfig.App.Version}", ActivityType.Playing)); break;
                case 2:
                    await _client.SetActivityAsync(new Game($"{_client.Guilds.Count:n0} Servers | v{AppConfig.App.Version}", ActivityType.Watching)); break;
                case 3:
                    await _client.SetActivityAsync(new Game($"{_client.Guilds.Sum(x => x.MemberCount):n0} Users | v{AppConfig.App.Version}", ActivityType.Watching)); break;
                case 4:
                    await _client.SetActivityAsync(new Game($"{_users.GetUsers().Count:n0} Linked Users | v{AppConfig.App.Version}", ActivityType.Watching)); break;
                case 6:
                    await _client.SetActivityAsync(new Game($"{AppConfig.App.Website} | v{AppConfig.App.Version}", ActivityType.Watching)); break;
                case 7:
                    await _client.SetActivityAsync(new Game($"{AppConfig.App.Twitter} on Twitter | v{AppConfig.App.Version}", ActivityType.Watching)); break;
                case 8:
                    await _client.SetActivityAsync(new Game($"{EmblemOffer.CurrentOffers.Count} Available Emblems", ActivityType.Watching)); break;
                case 9:
                    await _client.SetActivityAsync(new Game($"{_client.Shards.Count} Shards | v{AppConfig.App.Version}", ActivityType.Watching)); break;
                case 10:
                    await _client.SetActivityAsync(new Game($"{AppConfig.Discord.WatchingStatuses[rand.Next(0, AppConfig.Discord.WatchingStatuses.Count)]} | v{AppConfig.App.Version}", ActivityType.Watching)); break;
                case 11:
                    await _client.SetActivityAsync(new Game($"{_client.GetApplicationInfoAsync().Result.ApproximateUserInstallCount:n0} User Installs | v{AppConfig.App.Version}", ActivityType.Watching)); break;
                default:
                    break;
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

            // Start the next timer.
            SetUpTimer(new DateTime(DateTime.UtcNow.AddDays(1).Year, DateTime.UtcNow.AddDays(1).Month, DateTime.UtcNow.AddDays(1).Day, 17, 0, 5));
        }

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
                        Log.Information("[{Type}] Shard {ShardID} connected and ready. {}", "Discord", shard.ShardId);
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
                    if (AppConfig.IsDebug)
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
                            await _interaction.AddModulesToGuildAsync(_client.GetGuild(AppConfig.Discord.DevServerId), true, m);
                            break;
                        }
                    }

                    AppConfig.Discord.LoggingChannel = _client.GetChannel(AppConfig.Discord.LogChannelId) as SocketTextChannel;
                    AppConfig.Discord.CreationsLogChannel = _client.GetChannel(AppConfig.Discord.CommunityCreationsLogChannelId) as SocketTextChannel;

                    await UpdateBotActivity();
                    await EmoteConfig.RefreshEmotes();
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