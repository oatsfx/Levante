using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Levante.Configs;
using Levante.Helpers;
using Levante.Rotations;
using Levante.Util;
using Levante.Util.Attributes;
using Serilog;

namespace Levante.Commands
{
    public class Components : InteractionModuleBase<ShardedInteractionContext>
    {
        [RequireOwner]
        [ComponentInteraction("dailyForce")]
        public async Task DailyForce()
        {
            await RespondAsync("Forcing daily...");
            CurrentRotations.DailyRotation();
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
            await DataConfig.PostDailyResetUpdate(Context.Client);
            await DataConfig.PostWeeklyResetUpdate(Context.Client);
            await CurrentRotations.CheckUsersDailyTracking(Context.Client);
            await CurrentRotations.CheckUsersWeeklyTracking(Context.Client);
            await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = "Forced Weekly Reset!"; });
        }

        [ComponentInteraction("deleteChannel")]
        public async Task DeleteChannel() => await (Context.Channel as SocketGuildChannel).DeleteAsync(options: new RequestOptions() { AuditLogReason = $"XP Logging Delete Channel (User: {Context.User.Username}#{Context.User.Discriminator})"});

        [RequireBungieOauth]
        [ComponentInteraction("startXPAFK")]
        public async Task StartAFK()
        {
            var user = Context.User;
            var guild = Context.Guild;

            if (!BotConfig.IsSupporter(Context.User.Id) && ActiveConfig.ActiveAFKUsers.Count >= ActiveConfig.MaximumLoggingUsers)
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"Unfortunately, we've hit the maximum amount of users ({ActiveConfig.MaximumLoggingUsers}) we are allowing to log XP. You'll have to wait for the amount of users to drop.\n" +
                    $"Want to bypass this limit? Support us at https://donate.levante.dev/ and let us know on Discord: https://support.levante.dev/.\n" +
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

            if (ActiveConfig.IsExistingActiveUser(user.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You are already actively using my logging feature.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            await RespondAsync($"Getting things ready...", ephemeral: true);
            var dil = DataConfig.GetLinkedUser(user.Id);

            string memId = dil.BungieMembershipID;
            string memType = dil.BungieMembershipType;
            int userLevel = DataConfig.GetAFKValues(user.Id, out int lvlProg, out int powerBonus, out PrivacySetting fireteamPrivacy, out string CharacterId, out string errorStatus, out long activityHash);

            if (!errorStatus.Equals("Success"))
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"A Bungie API error has occurred. Reason: {errorStatus}"; });
                return;
            }

            ICategoryChannel cc = null;
            foreach (var categoryChan in guild.CategoryChannels)
                if (categoryChan.Name.Contains($"XP Logging"))
                {
                    if (categoryChan.Channels.Count >= 50)
                    {
                        await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"The \"{categoryChan.Name}\" category is full. You may have to invite me to another server and use this feature there if this continues to occur."; });
                        return;
                    }

                    cc = categoryChan;
                }
                    

            if (cc == null)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"No category by the name of \"XP Logging\" was found, cancelling operation. Let a server admin know!"; });
                return;
            }

            string uniqueName = dil.UniqueBungieName;
            var userLogChannel = await guild.CreateTextChannelAsync($"{uniqueName.Split('#')[0]}", options: new RequestOptions(){ AuditLogReason = "XP Logging Session Create" });

            ActiveConfig.ActiveAFKUser newUser = new()
            {
                DiscordID = user.Id,
                UniqueBungieName = uniqueName,
                DiscordChannelID = userLogChannel.Id,
                StartLevel = userLevel,
                StartLevelProgress = lvlProg,
                StartPowerBonus = powerBonus,
                LastLevel = userLevel,
                LastLevelProgress = lvlProg,
                LastPowerBonus = powerBonus,
                ActivityHash = activityHash,
            };

            await userLogChannel.ModifyAsync(x =>
            {
                x.CategoryId = cc.Id;
                x.Topic = $"{uniqueName.Split('#')[0]} (Starting Level: {newUser.StartLevel} [{newUser.StartLevelProgress:n0}/100,000 XP] | Starting Power Bonus: +{newUser.StartPowerBonus}) - Time Started: {TimestampTag.FromDateTime(newUser.TimeStarted)}";
                x.PermissionOverwrites = new[]
                {
                        new Overwrite(user.Id, PermissionTarget.User, new OverwritePermissions(sendMessages: PermValue.Allow, viewChannel: PermValue.Allow)),
                        new Overwrite(Context.Client.CurrentUser.Id, PermissionTarget.User, new OverwritePermissions(sendMessages: PermValue.Allow, viewChannel: PermValue.Allow)),
                        new Overwrite(guild.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)),
                };
            }, options: new RequestOptions() { AuditLogReason = "XP Logging Session Channel Edit" });

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
            if (BotConfig.IsSupporter(user.Id))
                logType = LoggingType.Priority;

            var guardian = new Guardian(newUser.UniqueBungieName, memId, memType, CharacterId);
            await LogHelper.Log(userLogChannel, $"{uniqueName} is starting at Level {newUser.LastLevel} ({newUser.LastLevelProgress:n0}/100,000 XP) and Power Bonus +{newUser.LastPowerBonus}.{(logType == LoggingType.Priority ? " *You are in the priority logging list; thank you for your generous support!*" : "")}", guardian.GetGuardianEmbed());
            //string recommend = fireteamPrivacy == PrivacySetting.Open || fireteamPrivacy == PrivacySetting.ClanAndFriendsOnly || fireteamPrivacy == PrivacySetting.FriendsOnly ? $" It is recommended to change your privacy to prevent people from joining you. {user.Mention}" : "";
            //await LogHelper.Log(userLogChannel, $"{uniqueName} has fireteam on {privacy}.{recommend}");

            ActiveConfig.AddActiveUserToConfig(newUser, logType);
            ActiveConfig.UpdateActiveAFKUsersConfig();
            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "'s" : "s'";
            string p = ActiveConfig.PriorityActiveAFKUsers.Count != 0 ? $" (+{ActiveConfig.PriorityActiveAFKUsers.Count})" : "";
            await Context.Client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers}{p} User{s} XP", ActivityType.Watching));

            await LogHelper.Log(userLogChannel, "User is subscribed to our Bungie API refreshes. Waiting for next refresh...");
            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Your logging channel has been successfully created! Access it here: {userLogChannel.Mention}!"; });
            Log.Information("[{Type}] Started XP logging for {User}.", "XP Sessions", newUser.UniqueBungieName);
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

            if (!ActiveConfig.IsExistingActiveUser(user.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You are already using my logging feature.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            var aau = ActiveConfig.GetActiveAFKUser(user.Id);

            var channel = Context.Client.GetChannel(aau.DiscordChannelID);
            if (channel == null)
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"I could not find your logging channel, did it get deleted? I have removed you from my logging feature.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                ActiveConfig.DeleteActiveUserFromConfig(user.Id);
                ActiveConfig.UpdateActiveAFKUsersConfig();
                return;
            }

            await LogHelper.Log(Context.Client.GetChannel(aau.DiscordChannelID) as ITextChannel, $"<@{user.Id}>: Logging terminated by user. Here is your session summary:", Embed: XPLoggingHelper.GenerateSessionSummary(aau), CB: XPLoggingHelper.GenerateChannelButtons(aau.DiscordID));
            await LogHelper.Log(user.CreateDMChannelAsync().Result, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(aau.TimeStarted)}.", XPLoggingHelper.GenerateSessionSummary(aau));

            await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(aau));
            ActiveConfig.DeleteActiveUserFromConfig(user.Id);
            ActiveConfig.UpdateActiveAFKUsersConfig();
            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "'s" : "s'";
            string p = ActiveConfig.PriorityActiveAFKUsers.Count != 0 ? $" (+{ActiveConfig.PriorityActiveAFKUsers.Count})" : "";
            await Context.Client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers}{p} User{s} XP", ActivityType.Watching));
            await RespondAsync($"Stopped XP logging for {aau.UniqueBungieName}.", ephemeral: true);
            Log.Information("[{Type}] Stopped XP logging for {User} via user request.", "XP Sessions", aau.UniqueBungieName);
        }

        [ComponentInteraction("viewXPHelp")]
        public async Task ViewHelp()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"XP Logger Help!",
                IconUrl = app.IconUrl,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {BotConfig.AppName} v{BotConfig.Version}"
            };
            var helpEmbed = new EmbedBuilder
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = auth,
                Footer = foot,
                Description =
                    $"__Steps:__\n" +
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
            if (Context.User.Id != DiscordID)
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You do not have permission to perform this action.";
                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            var user = Context.User;
            var guild = Context.Guild;

            if (!BotConfig.IsSupporter(Context.User.Id) && ActiveConfig.ActiveAFKUsers.Count >= ActiveConfig.MaximumLoggingUsers)
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"Unfortunately, we've hit the maximum amount of users ({ActiveConfig.MaximumLoggingUsers}) we are allowing to log XP. You'll have to wait for the amount of users to drop.\n" +
                    $"Want to bypass this limit? Support us at https://donate.levante.dev/ and let us know on Discord: https://support.levante.dev/.\n" +
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

            if (ActiveConfig.IsExistingActiveUser(user.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You are already actively using my logging feature.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            await RespondAsync($"Getting things ready...", ephemeral: true);
            var dil = DataConfig.GetLinkedUser(user.Id);

            string memId = dil.BungieMembershipID;
            string memType = dil.BungieMembershipType;
            int userLevel = DataConfig.GetAFKValues(user.Id, out int lvlProg, out int powerBonus, out PrivacySetting fireteamPrivacy, out string CharacterId, out string errorStatus, out long activityHash);

            if (!errorStatus.Equals("Success"))
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"A Bungie API error has occurred. Reason: {errorStatus}"; });
                return;
            }

            string uniqueName = dil.UniqueBungieName;
            var userLogChannel = Context.Channel as SocketTextChannel;

            ActiveConfig.ActiveAFKUser newUser = new ActiveConfig.ActiveAFKUser
            {
                DiscordID = user.Id,
                UniqueBungieName = uniqueName,
                DiscordChannelID = userLogChannel.Id,
                StartLevel = userLevel,
                StartLevelProgress = lvlProg,
                StartPowerBonus = powerBonus,
                LastLevel = userLevel,
                LastLevelProgress = lvlProg,
                LastPowerBonus = powerBonus,
                ActivityHash = activityHash,
            };

            await userLogChannel.ModifyAsync(x =>
            {
                x.Topic = $"{uniqueName} (Starting Level: {newUser.StartLevel} [{newUser.StartLevelProgress:n0}/100,000 XP] | Starting Power Bonus: +{newUser.StartPowerBonus}) - Time Started: {TimestampTag.FromDateTime(newUser.TimeStarted)}";
            }, options: new RequestOptions() { AuditLogReason = "XP Logging Session Channel Edit" });

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
            if (BotConfig.IsSupporter(user.Id))
                logType = LoggingType.Priority;

            var guardian = new Guardian(newUser.UniqueBungieName, memId, memType, CharacterId);
            await LogHelper.Log(userLogChannel, $"{uniqueName} is starting at Level {newUser.LastLevel} ({newUser.LastLevelProgress:n0}/100,000 XP) and Power Bonus +{newUser.LastPowerBonus}.{(logType == LoggingType.Priority ? " *You are in the priority logging list; thank you for your generous support!*" : "")}", guardian.GetGuardianEmbed());
            //string recommend = fireteamPrivacy == PrivacySetting.Open || fireteamPrivacy == PrivacySetting.ClanAndFriendsOnly || fireteamPrivacy == PrivacySetting.FriendsOnly ? $" It is recommended to change your privacy to prevent people from joining you. {user.Mention}" : "";
            //await LogHelper.Log(userLogChannel, $"{uniqueName} has fireteam on {privacy}.{recommend}");

            ActiveConfig.AddActiveUserToConfig(newUser, logType);
            ActiveConfig.UpdateActiveAFKUsersConfig();
            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "'s" : "s'";
            string p = ActiveConfig.PriorityActiveAFKUsers.Count != 0 ? $" (+{ActiveConfig.PriorityActiveAFKUsers.Count})" : "";
            await Context.Client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers}{p} User{s} XP", ActivityType.Watching));

            await LogHelper.Log(userLogChannel, "User is subscribed to our Bungie API refreshes. Waiting for next refresh...");
            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Your logging has been successfully restarted! Reminder that the previous session did not continue; a new session was created without making a new channel!"; });
            Log.Information("[{Type}] Started XP logging for {User}.", "XP Sessions", newUser.UniqueBungieName);
        }
    }
}
