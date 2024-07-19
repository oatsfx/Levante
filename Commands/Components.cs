using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Levante.Configs;
using Levante.Helpers;
using Levante.Rotations;
using Levante.Services;
using Levante.Util;
using Levante.Util.Attributes;
using Serilog;
using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Levante.Commands
{
    public class Components : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly LoggingService Logging;

        public Components(LoggingService loggingService)
        {
            Logging = loggingService;
        }

        [RequireOwner]
        [ComponentInteraction("dailyForce")]
        public async Task DailyForce()
        {
            await RespondAsync("Forcing daily...");
            CurrentRotations.DailyRotation();
            await Task.Run(CountdownConfig.CheckCountdowns);
            await Task.Run(EmblemOffer.CheckEmblemOffers);
            await DataConfig.PostDailyResetUpdate(Context.Client);
            await CurrentRotations.CheckUsersDailyTracking(Context.Client);
            await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = "Forced Daily Reset!"; });
        }

        [RequireOwner]
        [ComponentInteraction("weeklyForce")]
        public async Task WeeklyForce()
        {
            await RespondAsync("Forcing weekly...");
            CurrentRotations.WeeklyRotation();
            await Task.Run(CountdownConfig.CheckCountdowns);
            await Task.Run(EmblemOffer.CheckEmblemOffers);
            await DataConfig.PostDailyResetUpdate(Context.Client);
            await DataConfig.PostWeeklyResetUpdate(Context.Client);
            await CurrentRotations.CheckUsersDailyTracking(Context.Client);
            await CurrentRotations.CheckUsersWeeklyTracking(Context.Client);
            await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = "Forced Weekly Reset!"; });
        }

        [ComponentInteraction("deleteChannel")]
        public async Task DeleteChannel() => await (Context.Channel as SocketGuildChannel).DeleteAsync(options: new RequestOptions { AuditLogReason = $"XP Logging Delete Channel (User: {Context.User.Username})"});

        [RequireBungieOauth]
        [ComponentInteraction("startXPAFK")]
        public async Task StartAFK()
        {
            if (Logging.IsExistingActiveUser(Context.User.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You are already actively using my logging feature.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            var loggingOverrides = Logging.GetLoggingOverrides();
            if (loggingOverrides.Any())
            {
                try
                {
                    var menuBuilder = new SelectMenuBuilder()
                        .WithPlaceholder("Select the type of logging you want")
                        .WithCustomId("startLoggingOverride:false")
                        .WithMinValues(1)
                        .WithMaxValues(1);

                    foreach (var logOverride in loggingOverrides)
                    {
                        menuBuilder.AddOption(logOverride.ShortName, $"{logOverride.Hash}", logOverride.Name, String.IsNullOrEmpty(logOverride.DiscordEmote) ? null : Emote.Parse(logOverride.DiscordEmote));
                    }

                    var xEmote = new Emoji("❌");

                    var builder = new ComponentBuilder()
                        .WithSelectMenu(menuBuilder, row: 1).WithButton("Use Default Logging", customId: $"startLogging:false,0", ButtonStyle.Primary, row: 0);

                    await RespondAsync($"There are multiple logging options available, select one.\n" +
                        $"-# If you want to use original XP logging, hit the \"Use Default Logging\" button.", components: builder.Build(), ephemeral: true);
                }
                catch (Exception x)
                {
                    Log.Warning($"{x}");
                }
            }
        }

        [ComponentInteraction("stopXPAFK")]
        public async Task StopAFK()
        {
            var user = Context.User;
            if (!DataConfig.IsExistingLinkedUser(user.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You are not linked! Use the `/link` command to begin the linking process.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            if (!Logging.IsExistingActiveUser(user.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You aren't using my logging feature. Hit the \"Ready\" button to get started!";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            var aau = Logging.GetLoggingUser(user.Id);

            var channel = Context.Client.GetChannel(aau.DiscordChannelID);
            if (channel == null)
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"I could not find your logging channel, did it get deleted? I have removed you from my logging feature.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                Logging.DeleteActiveUserFromConfig(user.Id);
                Logging.UpdateUsersConfig();
                return;
            }

            await LogHelper.Log(Context.Client.GetChannel(aau.DiscordChannelID) as ITextChannel, $"<@{user.Id}>: Logging terminated by user. Here is your session summary:", Embed: Logging.GenerateSessionSummary(aau), CB: LoggingService.GenerateChannelButtons(aau.DiscordID));
            await LogHelper.Log(user.CreateDMChannelAsync().Result, $"Here is the session summary from <#{aau.DiscordChannelID}>, beginning on {TimestampTag.FromDateTime(aau.Start.Timestamp)}.", Logging.GenerateSessionSummary(aau));

            await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(aau));
            Logging.DeleteActiveUserFromConfig(user.Id);
            Logging.UpdateUsersConfig();

            await RespondAsync($"Stopped XP logging for {aau.UniqueBungieName}.", ephemeral: true);
            Log.Information("[{Type}] Stopped XP logging for {User} via user request.", "Logging", aau.UniqueBungieName);
        }

        [ComponentInteraction("viewXPHelp")]
        public async Task ViewHelp()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Logging Help!",
                IconUrl = app.IconUrl,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {AppConfig.App.Name} v{AppConfig.App.Version}"
            };
            var helpEmbed = new EmbedBuilder
            {
                Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                Author = auth,
                Footer = foot,
                Description =
                    $"## Steps:\n" +
                    $"1) Launch Destiny 2.\n" +
                    $"2) Hit the \"Ready\" button and start getting those XP gains in.\n" +
                    $"3) I will keep track of your gains in a personalized channel for you.",
            };

            await RespondAsync($"", embed: helpEmbed.Build(), ephemeral: true);
        }

        [RequireBungieOauth]
        [ComponentInteraction("restartLogging:*")]
        public async Task RestartLogging(ulong DiscordID)
        {
            if (Logging.IsExistingActiveUser(Context.User.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You are already actively using my logging feature.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            var loggingOverrides = Logging.GetLoggingOverrides();
            if (loggingOverrides.Any())
            {
                try
                {
                    var menuBuilder = new SelectMenuBuilder()
                        .WithPlaceholder("Select the type of logging you want")
                        .WithCustomId("startLoggingOverride:false")
                        .WithMinValues(1)
                        .WithMaxValues(1);

                    foreach (var logOverride in loggingOverrides)
                    {
                        menuBuilder.AddOption(logOverride.ShortName, $"{logOverride.Hash}", logOverride.Name, String.IsNullOrEmpty(logOverride.DiscordEmote) ? null : Emote.Parse(logOverride.DiscordEmote));
                    }

                    var xEmote = new Emoji("❌");

                    var builder = new ComponentBuilder()
                        .WithSelectMenu(menuBuilder, row: 1).WithButton("Use Default Logging", customId: $"startLogging:false,0", ButtonStyle.Primary, row: 0);

                    await RespondAsync($"There are multiple logging options available, select one.\n" +
                        $"-# If you want to use original XP logging, hit the \"Use Default Logging\" button.", components: builder.Build(), ephemeral: true);
                }
                catch (Exception x)
                {
                    Log.Warning($"{x}");
                }
            }

            if (Context.User.Id != DiscordID)
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You do not have permission to perform this action.";
                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            var user = Context.User;
            var guild = Context.Guild;

            if (!AppConfig.IsSupporter(Context.User.Id) && Logging.IsXpLoggingFull())
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"Unfortunately, we've hit the maximum amount of users ({Logging.GetMaxLoggingUsers()}) we are allowing to log XP. We understand that this may be frustrating; you'll have to wait for the amount of users to drop.\n" +
                    $"Want to bypass this limit? Support us at https://donate.{AppConfig.App.Website} / and let us know on Discord: https://support. {AppConfig.App.Website}/.\n" +
                    $"Use the `/support` command for more info!";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            if (!DataConfig.IsExistingLinkedUser(user.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You are not linked! Use the `/link` command to begin the linking process.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            if (Logging.IsExistingActiveUser(user.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You are already actively using my logging feature.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            await RespondAsync("Getting things ready...", ephemeral:true);
            var dil = DataConfig.GetLinkedUser(user.Id);

            string memId = dil.BungieMembershipID;
            string memType = dil.BungieMembershipType;

            var loggingValues = new XpLoggingValueResponse(user.Id);
            var userLevel = loggingValues.CurrentLevel;
            var userExtraLevel = loggingValues.CurrentExtraLevel;
            var lvlProg = loggingValues.XpProgress;
            var powerBonus = loggingValues.PowerBonus;
            var fireteamPrivacy = loggingValues.FireteamPrivacy;
            var characterId = loggingValues.CharacterId;
            var errorStatus = loggingValues.ErrorStatus;
            var activityHash = loggingValues.ActivityHash;
            var nextLevelAt = loggingValues.NextLevelAt;

            if (!errorStatus.Equals("Success"))
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"A Bungie API error has occurred. Reason: {errorStatus}"; });
                return;
            }

            string uniqueName = dil.UniqueBungieName;
            var userLogChannel = Context.Channel as SocketTextChannel;

            try
            {
                LoggingUser newUser = new()
                {
                    DiscordID = user.Id,
                    UniqueBungieName = uniqueName,
                    DiscordChannelID = userLogChannel.Id,
                    Start = new()
                    {
                        Level = userLevel,
                        ExtraLevel = userExtraLevel,
                        LevelProgress = lvlProg,
                        PowerBonus = powerBonus,
                        NextLevelAt = nextLevelAt,
                    },
                    Last = new()
                    {
                        Level = userLevel,
                        ExtraLevel = userExtraLevel,
                        LevelProgress = lvlProg,
                        PowerBonus = powerBonus,
                        NextLevelAt = nextLevelAt,
                    },
                    ActivityHash = activityHash,
                };

                await userLogChannel.ModifyAsync(x =>
                {
                    x.Topic = $"{uniqueName.Split('#')[0]} (Starting Level: {newUser.Start.Level}{(newUser.Last.ExtraLevel > 0 ? $" (+{newUser.Last.ExtraLevel})" : "")} [{newUser.Start.LevelProgress:n0}/{nextLevelAt:n0} XP] | Starting Power Bonus: +{newUser.Start.PowerBonus}) - Time Started: {TimestampTag.FromDateTime(newUser.Start.Timestamp)}";
                }, options: new RequestOptions() { AuditLogReason = "XP Logging Session Channel Edit" }).ConfigureAwait(false);

                string privacy = "";
                switch (fireteamPrivacy)
                {
                    case PrivacySetting.Open: privacy = "Open"; break;
                    case PrivacySetting.ClanAndFriendsOnly: privacy = "Clan and Friends Only"; break;
                    case PrivacySetting.FriendsOnly: privacy = "Friends Only"; break;
                    case PrivacySetting.InvitationOnly: privacy = "Invite Only"; break;
                    case PrivacySetting.Closed: privacy = "Closed"; break;
                    default: break;
                }

                LoggingType logType = LoggingType.Basic;
                if (AppConfig.IsSupporter(user.Id))
                    logType = LoggingType.Priority;

                var menuBuilder = new SelectMenuBuilder()
                    .WithPlaceholder("Add a user to the logging channel")
                    .WithCustomId("addUserToXpChannel")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .WithType(ComponentType.UserSelect);

                var builder = new ComponentBuilder()
                    .WithSelectMenu(menuBuilder);

                var guardian = new Guardian(newUser.UniqueBungieName, memId, memType, characterId, dil);
                await LogHelper.Log(userLogChannel, $"{uniqueName} is starting at Level {newUser.Last.Level}{(userExtraLevel > 0 ? $" (+{userExtraLevel})" : "")} ({newUser.Last.LevelProgress:n0}/{nextLevelAt:n0} XP) and Power Bonus +{newUser.Last.PowerBonus}. You are subscribed to our Bungie API refreshes. Waiting for next refresh... {(logType == LoggingType.Priority ? " *You are in the priority logging list; thank you for your generous support!*" : "")}", guardian.GetGuardianEmbed(), builder);
                //string recommend = fireteamPrivacy == PrivacySetting.Open || fireteamPrivacy == PrivacySetting.ClanAndFriendsOnly || fireteamPrivacy == PrivacySetting.FriendsOnly ? $" It is recommended to change your privacy to prevent people from joining you. {user.Mention}" : "";
                //await LogHelper.Log(userLogChannel, $"{uniqueName} has fireteam on {privacy}.{recommend}");

                Logging.AddUserToConfig(newUser, logType);
                Logging.UpdateUsersConfig();

                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Your logging has been successfully restarted! Reminder that the previous session did not continue; a new session was created without making a new channel!"; });
                Log.Information("[{Type}] Started XP logging for {User}.", "XP Sessions", newUser.UniqueBungieName);
            }
            catch
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"We had trouble gathering your game data, please try again!"; });
                return;
            }
        }

        [RequireBotPermission(ChannelPermission.ManageChannels)]
        [ComponentInteraction("addUserToXpChannel")]
        public async Task SelectMenu(IUser[] selectedUsers)
        {
            if (Context.Channel is not SocketGuildChannel guildChannel)
            {
                await RespondAsync("Channel is not part of a server! Cannot edit permissions.", ephemeral: true);
                return;
            }

            var activeUser = Logging.GetXpLoggingUsers().Find(x => x.DiscordChannelID == Context.Channel.Id);
            activeUser ??= Logging.GetPriorityXpLoggingUsers().Find(x => x.DiscordChannelID == Context.Channel.Id);

            if (activeUser == null)
            {
                await RespondAsync("Channel is currently not active.", ephemeral: true);
                return;
            }

            if (activeUser.DiscordID != Context.User.Id)
            {
                await RespondAsync("This channel is not yours, only the channel owner can add users to the channel.", ephemeral: true);
                return;
            }

            var usersAdded = string.Empty;
            foreach (var user in selectedUsers)
            {
                if (guildChannel.PermissionOverwrites.Any(x => x.TargetId == user.Id && (x.Permissions.SendMessages == PermValue.Allow && x.Permissions.ViewChannel == PermValue.Allow )))
                {
                    continue;
                }

                await guildChannel.AddPermissionOverwriteAsync(user, new OverwritePermissions(sendMessages: PermValue.Allow, viewChannel: PermValue.Allow), new RequestOptions{ AuditLogReason = "XP Logging User Added by Channel Owner" });
                usersAdded += $"{user.Mention}, ";
            }

            if (string.IsNullOrEmpty(usersAdded))
            {
                await RespondAsync($"No users added. The selected users can, most likely, already see this channel.", ephemeral: true);
                return;
            }

            var menuBuilder = new SelectMenuBuilder()
                .WithPlaceholder("Add another user to the logging channel")
                .WithCustomId("addUserToXpChannel")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithType(ComponentType.UserSelect);

            var builder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder);

            await RespondAsync($"{usersAdded.Remove(usersAdded.Length - 2)} can now see this channel.", components: builder.Build());
        }

        [RequireBotPermission(ChannelPermission.ManageChannels)]
        [RequireBungieOauth]
        [ComponentInteraction("startLoggingOverride:*")]
        public async Task StartLoggingOverride(bool isRestarted, string[] overrideHash)
        {
            Log.Debug($"{overrideHash}");
            var result = long.TryParse(overrideHash.First(), out var hash);
            if (!result)
            {
                await RespondAsync($"Something went wrong with the selection you made, please try again.", ephemeral: true);
                return;
            }
            await StartLogging(isRestarted, hash);
        }

        [RequireBotPermission(ChannelPermission.ManageChannels)]
        [RequireBungieOauth]
        [ComponentInteraction("startLogging:*,*")]
        public async Task StartLogging(bool isRestarted, long overrideHash = 0)
        {
            var user = Context.User;
            var guild = Context.Guild;

            if (isRestarted)
            {
                Log.Debug($"Restarted Logging");
            }

            if (!AppConfig.IsSupporter(Context.User.Id) && Logging.IsXpLoggingFull())
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"Unfortunately, we've hit the maximum amount of users ({Logging.GetMaxLoggingUsers()}) we are allowing to log XP. We understand that this may be frustrating; you'll have to wait for the amount of users to drop.\n" +
                    $"Want to bypass this limit? Support us at https://donate.{AppConfig.App.Website}/ and let us know on Discord: https://support.{AppConfig.App.Website}/.\n" +
                    $"Use the `/support` command for more info!";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            if (!DataConfig.IsExistingLinkedUser(user.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You are not linked! Use the `/link` command to begin the linking process.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            if (Logging.IsExistingActiveUser(user.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You are already actively using my logging feature.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            await RespondAsync("Getting things ready...", ephemeral: true);
            var dil = DataConfig.GetLinkedUser(user.Id);

            string memId = dil.BungieMembershipID;
            string memType = dil.BungieMembershipType;

            var loggingOverride = Logging.GetLoggingOverrides().FirstOrDefault(x => x.Hash == overrideHash);

            var loggingValues = new XpLoggingValueResponse(user.Id, loggingOverride);
            var userLevel = loggingValues.CurrentLevel;
            var userExtraLevel = loggingValues.CurrentExtraLevel;
            var lvlProg = loggingValues.XpProgress;
            var powerBonus = loggingValues.PowerBonus;
            var fireteamPrivacy = loggingValues.FireteamPrivacy;
            var characterId = loggingValues.CharacterId;
            var errorStatus = loggingValues.ErrorStatus;
            var activityHash = loggingValues.ActivityHash;
            var nextLevelAt = loggingValues.NextLevelAt;
            var overrideValue = loggingValues.OverrideValue;
            var overrideInvList = loggingValues.OverrideInventoryList;

            if (!errorStatus.Equals("Success"))
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"A Bungie API error has occurred. Reason: {errorStatus}"; });
                return;
            }

            ICategoryChannel cc = null;
            foreach (var categoryChan in guild.CategoryChannels)
            {
                if (categoryChan.Name.Contains($"XP Logging"))
                {
                    if (categoryChan.Channels.Count >= 50)
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"The \"{categoryChan.Name}\" category is full. You may have to invite me to another server and use this feature there if this persists."; });
                        return;

                        // Remove oldest channel testing feature.
                        //var oldestTime = DateTime.MinValue;
                        //var oldestChannel = categoryChan.Channels.FirstOrDefault();
                        //foreach (var channel in categoryChan.Channels)
                        //{
                        //    // Even though this category isn't supposed to have non-text channels, check anyway.
                        //    if (channel is not SocketTextChannel textChannel)
                        //    {
                        //        continue;
                        //    }

                        //    // Don't delete the XP Hub channel.
                        //    if (textChannel.Name.Contains("xp-hub") || (textChannel.Topic != null && textChannel.Topic.Contains("XP Hub:")))
                        //    {
                        //        var msgs = await (channel as SocketTextChannel).GetMessagesAsync().FlattenAsync();
                        //        if (msgs.Any(x => x.Author.Id == Context.Client.CurrentUser.Id && x.Components.Count == 1 && x.Embeds.Count == 1))
                        //        {
                        //            Log.Debug("Found the XP Hub channel.");
                        //            continue;
                        //        }
                        //    }

                        //    var msg = await textChannel.GetMessagesAsync(1).FlattenAsync();
                        //    if (!msg.Any())
                        //    {
                        //        await channel.DeleteAsync(options: new RequestOptions { AuditLogReason = "XP Logging Channel Deleted. Reason: No Messages" });
                        //        continue;
                        //    }

                        //    Log.Debug($"{msg.FirstOrDefault().CreatedAt}");
                        //    if (msg.FirstOrDefault().CreatedAt == oldestTime)
                        //    {
                        //        oldestChannel = channel;
                        //    }
                        //}

                        //await oldestChannel.DeleteAsync(options: new RequestOptions { AuditLogReason = "XP Logging Channel Deleted. Reason: Oldest Logging Channel" });
                    }

                    cc = categoryChan;
                }
            }

            if (cc == null)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"No category by the name of \"XP Logging\" was found, cancelling operation. Let a server admin know!"; });
                return;
            }

            string uniqueName = dil.UniqueBungieName;
            var userLogChannel = await guild.CreateTextChannelAsync($"{uniqueName.Split('#')[0]}", options: new RequestOptions { AuditLogReason = "XP Logging Session Create" },
                func: x =>
                {
                    x.CategoryId = cc.Id;
                    x.Topic = $"{uniqueName.Split('#')[0]} (Starting Level: {userLevel}{(userExtraLevel > 0 ? $" (+{userExtraLevel})" : "")} [{lvlProg:n0}/{nextLevelAt:n0} XP] | Starting Power Bonus: +{powerBonus}) - Time Started: {TimestampTag.FromDateTime(DateTime.Now)}";
                    x.PermissionOverwrites = new[]
                    {
                        new Overwrite(user.Id, PermissionTarget.User, new OverwritePermissions(sendMessages: PermValue.Allow, viewChannel: PermValue.Allow)),
                        new Overwrite(Context.Client.CurrentUser.Id, PermissionTarget.User, new OverwritePermissions(sendMessages: PermValue.Allow, viewChannel: PermValue.Allow)),
                        new Overwrite(guild.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)),
                    };
                });


            try
            {
                LoggingUser newUser = new()
                {
                    DiscordID = user.Id,
                    UniqueBungieName = uniqueName,
                    DiscordChannelID = userLogChannel.Id,
                    Start = new()
                    {
                        Level = userLevel,
                        ExtraLevel = userExtraLevel,
                        LevelProgress = lvlProg,
                        PowerBonus = powerBonus,
                        NextLevelAt = nextLevelAt,
                        OverrideValue = overrideValue,
                        OverrideInventoryList = overrideInvList,
                    },
                    Last = new()
                    {
                        Level = userLevel,
                        ExtraLevel = userExtraLevel,
                        LevelProgress = lvlProg,
                        PowerBonus = powerBonus,
                        NextLevelAt = nextLevelAt,
                        OverrideValue = overrideValue,
                        OverrideInventoryList = overrideInvList,
                    },
                    ActivityHash = activityHash,
                    OverrideHash = loggingOverride == null ? 0 : loggingOverride.Hash,
                };

                string privacy = "";
                switch (fireteamPrivacy)
                {
                    case PrivacySetting.Open: privacy = "Open"; break;
                    case PrivacySetting.ClanAndFriendsOnly: privacy = "Clan and Friends Only"; break;
                    case PrivacySetting.FriendsOnly: privacy = "Friends Only"; break;
                    case PrivacySetting.InvitationOnly: privacy = "Invite Only"; break;
                    case PrivacySetting.Closed: privacy = "Closed"; break;
                    default: break;
                }

                LoggingType logType = LoggingType.Basic;
                if (AppConfig.IsSupporter(user.Id))
                    logType = LoggingType.Priority;

                var menuBuilder = new SelectMenuBuilder()
                    .WithPlaceholder("Add a user to your logging channel")
                    .WithCustomId("addUserToXpChannel")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .WithType(ComponentType.UserSelect);

                var builder = new ComponentBuilder()
                    .WithSelectMenu(menuBuilder);

                var guardian = new Guardian(newUser.UniqueBungieName, memId, memType, characterId, dil);
                await LogHelper.Log(userLogChannel, $"{uniqueName} is starting at Level {newUser.Last.Level}{(userExtraLevel > 0 ? $" (+{userExtraLevel})" : "")} ({newUser.Last.LevelProgress:n0}/{nextLevelAt:n0} XP) and Power Bonus +{newUser.Last.PowerBonus}." +
                    $"You are subscribed to our Bungie API refreshes. Waiting for next refresh..." +
                    $"{(logType == LoggingType.Priority ? " *You are in the priority logging list; thank you for your generous support!*" : "")}" +
                    $"{(loggingOverride != null ? $"\n-# You have selected a Logging Override: {loggingOverride.Name}" : "")}", guardian.GetGuardianEmbed(), builder);
                //string recommend = fireteamPrivacy == PrivacySetting.Open || fireteamPrivacy == PrivacySetting.ClanAndFriendsOnly || fireteamPrivacy == PrivacySetting.FriendsOnly ? $" It is recommended to change your privacy to prevent people from joining you. {user.Mention}" : "";
                //await LogHelper.Log(userLogChannel, $"{uniqueName} has fireteam on {privacy}.{recommend}");

                if (Logging.IsExistingActiveUser(user.Id))
                {
                    await userLogChannel.DeleteAsync();

                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"You are already actively using my logging feature. I did create a channel for you, but I deleted it because you're logging with me already."; });
                    return;
                }

                Logging.AddUserToConfig(newUser, logType);
                Logging.UpdateUsersConfig();

                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Your logging channel has been successfully created! Access it here: {userLogChannel.Mention}!"; });
                Log.Information("[{Type}] Started XP logging for {User}.", "XP Sessions", newUser.UniqueBungieName);
            }
            catch
            {
                await userLogChannel.DeleteAsync();
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"We had trouble gathering your game data, please try again!"; });
                return;
            }
        }
    }
}
