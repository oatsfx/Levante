using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Levante.Configs;
using Levante.Helpers;
using Levante.Rotations;
using Levante.Util;


namespace Levante.Commands
{
    public class Components : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction("force")]
        public async Task Force()
        {
            await RespondAsync("Forcing...");
            CurrentRotations.WeeklyRotation();
            await DataConfig.PostWeeklyResetUpdate(Context.Client);
            await DataConfig.PostDailyResetUpdate(Context.Client);
            await CurrentRotations.CheckUsersDailyTracking(Context.Client);
            await CurrentRotations.CheckUsersWeeklyTracking(Context.Client);
            await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Content = "Forced!"; });
        }

        [ComponentInteraction("deleteChannel")]
        public async Task DeleteChannel() => await (Context.Channel as SocketGuildChannel).DeleteAsync(options: new RequestOptions() { AuditLogReason = $"XP Logging Delete Channel (User: {Context.User.Username}#{Context.User.Discriminator})"});

        [ComponentInteraction("startXPAFK")]
        public async Task StartAFK()
        {
            var user = Context.User;
            var guild = Context.Guild;

            if (ActiveConfig.ActiveAFKUsers.Count >= ActiveConfig.MaximumLoggingUsers)
            {
                await RespondAsync($"Unfortunately, I am at the maximum number of users to watch ({ActiveConfig.MaximumLoggingUsers}). Try again later.", ephemeral: true);
                return;
            }

            if (!DataConfig.IsExistingLinkedUser(user.Id))
            {
                await RespondAsync($"You are not linked! Use \"/link\" to begin the linking process.", ephemeral: true);
                return;
            }

            if (ActiveConfig.IsExistingActiveUser(user.Id))
            {
                await RespondAsync($"You are already actively using my logging feature.", ephemeral: true);
                return;
            }

            await RespondAsync($"Getting things ready...", ephemeral: true);

            var dil = DataConfig.GetLinkedUser(user.Id);
            if (dil == null)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Unable to pull user data. I may have lost access to your information, likely, you'll have to link again."; });
                return;
            }

            string memId = dil.BungieMembershipID;
            string memType = dil.BungieMembershipType;
            int userLevel = DataConfig.GetAFKValues(user.Id, out int lvlProg, out int powerBonus, out PrivacySetting fireteamPrivacy, out string CharacterId, out string errorStatus);

            if (!errorStatus.Equals("Success"))
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"A Bungie API error has occurred. Reason: {errorStatus}"; });
                return;
            }

            ICategoryChannel cc = null;
            foreach (var categoryChan in guild.CategoryChannels)
                if (categoryChan.Name.Contains($"XP Logging"))
                    cc = categoryChan;

            if (cc == null)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"No category by the name of \"XP Logging\" was found, cancelling operation. Let a server admin know!"; });
                return;
            }

            string uniqueName = dil.UniqueBungieName;
            var userLogChannel = await guild.CreateTextChannelAsync($"{uniqueName.Replace('#', '-')}", options: new RequestOptions(){ AuditLogReason = "XP Logging Session Create" });

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
            };

            await userLogChannel.ModifyAsync(x =>
            {
                x.CategoryId = cc.Id;
                x.Topic = $"{uniqueName} (Starting Level: {newUser.StartLevel} [{String.Format("{0:n0}", newUser.StartLevelProgress)}/100,000 XP] | Starting Power Bonus: +{newUser.StartPowerBonus}) - Time Started: {TimestampTag.FromDateTime(newUser.TimeStarted)}";
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
            await LogHelper.Log(userLogChannel, $"{uniqueName} is starting at Level {newUser.LastLevel} ({String.Format("{0:n0}", newUser.LastLevelProgress)}/100,000 XP) and Power Bonus +{newUser.LastPowerBonus}.{(logType == LoggingType.Priority ? " *You are in the priority logging list; thank you for your generous support!*" : "")}", guardian.GetGuardianEmbed());
            string recommend = fireteamPrivacy == PrivacySetting.Open || fireteamPrivacy == PrivacySetting.ClanAndFriendsOnly || fireteamPrivacy == PrivacySetting.FriendsOnly ? $" It is recommended to change your privacy to prevent people from joining you. {user.Mention}" : "";
            await LogHelper.Log(userLogChannel, $"{uniqueName} has fireteam on {privacy}.{recommend}");

            ActiveConfig.AddActiveUserToConfig(newUser, logType);
            ActiveConfig.UpdateActiveAFKUsersConfig();
            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "'s" : "s'";
            string p = ActiveConfig.PriorityActiveAFKUsers.Count != 0 ? $" (+{ActiveConfig.PriorityActiveAFKUsers.Count})" : "";
            await Context.Client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers}{p} User{s} XP", ActivityType.Watching));

            await LogHelper.Log(userLogChannel, "User is subscribed to our Bungie API refreshes. Waiting for next refresh...");
            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Your logging channel has been successfully created! Access it here: {userLogChannel.Mention}!"; });
            LogHelper.ConsoleLog($"[LOGGING] Started XP logging for {newUser.UniqueBungieName}.");
        }

        [ComponentInteraction("stopXPAFK")]
        public async Task StopAFK()
        {
            var user = Context.User;
            if (!DataConfig.IsExistingLinkedUser(user.Id))
            {
                await RespondAsync($"You are not linked! Use \"/link\" to begin the linking process.", ephemeral: true);
                return;
            }

            if (!ActiveConfig.IsExistingActiveUser(user.Id))
            {
                await RespondAsync($"You are not actively using my logging feature.", ephemeral: true);
                return;
            }

            var aau = ActiveConfig.GetActiveAFKUser(user.Id);

            var channel = Context.Client.GetChannelAsync(aau.DiscordChannelID);
            if (channel.Result == null)
            {
                await RespondAsync($"I could not find your logging channel, did it get deleted? I have removed you from my logging feature.", ephemeral: true);
                ActiveConfig.DeleteActiveUserFromConfig(user.Id);
                ActiveConfig.UpdateActiveAFKUsersConfig();
                return;
            }

            await LogHelper.Log(Context.Client.GetChannelAsync(aau.DiscordChannelID).Result as ITextChannel, $"<@{user.Id}>: Logging terminated by user. Here is your session summary:", Embed: XPLoggingHelper.GenerateSessionSummary(aau, Context.Client.CurrentUser.GetAvatarUrl()), CB: XPLoggingHelper.GenerateChannelButtons());
            await LogHelper.Log(user.CreateDMChannelAsync().Result, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(aau.TimeStarted)}.", XPLoggingHelper.GenerateSessionSummary(aau, Context.Client.CurrentUser.GetAvatarUrl()));

            await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(aau));
            ActiveConfig.DeleteActiveUserFromConfig(user.Id);
            ActiveConfig.UpdateActiveAFKUsersConfig();
            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "'s" : "s'";
            string p = ActiveConfig.PriorityActiveAFKUsers.Count != 0 ? $" (+{ActiveConfig.PriorityActiveAFKUsers.Count})" : "";
            await Context.Client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers}{p} User{s} XP", ActivityType.Watching));
            await RespondAsync($"Stopped XP logging for {aau.UniqueBungieName}.", ephemeral: true);
            LogHelper.ConsoleLog($"[LOGGING] Stopped logging for {aau.UniqueBungieName} via user request.");
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
            var helpEmbed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };
            helpEmbed.Description =
                $"__Steps:__\n" +
                $"1) Launch Destiny 2.\n" +
                $"2) Hit the \"Ready\" button and start getting those XP gains in.\n" +
                $"3) I will keep track of your gains in a personalized channel for you.";

            await RespondAsync($"", embed: helpEmbed.Build(), ephemeral: true);
        }

        [ComponentInteraction("restartLogging")]
        public async Task RestartLogging()
        {
            var user = Context.User;
            var guild = Context.Guild;

            if (ActiveConfig.ActiveAFKUsers.Count >= ActiveConfig.MaximumLoggingUsers)
            {
                await RespondAsync($"Unfortunately, I am at the maximum number of users to watch ({ActiveConfig.MaximumLoggingUsers}). Try again later.", ephemeral: true);
                return;
            }

            if (!DataConfig.IsExistingLinkedUser(user.Id))
            {
                await RespondAsync($"You are not linked! Use \"/link\" to begin the linking process.", ephemeral: true);
                return;
            }

            if (ActiveConfig.IsExistingActiveUser(user.Id))
            {
                await RespondAsync($"You are already actively using my logging feature.", ephemeral: true);
                return;
            }

            await RespondAsync($"Getting things ready...", ephemeral: true);

            var dil = DataConfig.GetLinkedUser(user.Id);
            if (dil == null)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Unable to pull user data. I may have lost access to your information, likely, you'll have to link again."; });
                return;
            }

            string memId = dil.BungieMembershipID;
            string memType = dil.BungieMembershipType;
            int userLevel = DataConfig.GetAFKValues(user.Id, out int lvlProg, out int powerBonus, out PrivacySetting fireteamPrivacy, out string CharacterId, out string errorStatus);

            if (!errorStatus.Equals("Success"))
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"A Bungie API error has occurred. Reason: {errorStatus}"; });
                return;
            }

            ICategoryChannel cc = null;
            foreach (var categoryChan in guild.CategoryChannels)
                if (categoryChan.Name.Contains($"XP Logging"))
                    cc = categoryChan;

            if (cc == null)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"No category by the name of \"XP Logging\" was found, cancelling operation. Let a server admin know!"; });
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
            };

            await userLogChannel.ModifyAsync(x =>
            {
                x.CategoryId = cc.Id;
                x.Topic = $"{uniqueName} (Starting Level: {newUser.StartLevel} [{String.Format("{0:n0}", newUser.StartLevelProgress)}/100,000 XP] | Starting Power Bonus: +{newUser.StartPowerBonus}) - Time Started: {TimestampTag.FromDateTime(newUser.TimeStarted)}";
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
            await LogHelper.Log(userLogChannel, $"{uniqueName} is starting at Level {newUser.LastLevel} ({String.Format("{0:n0}", newUser.LastLevelProgress)}/100,000 XP) and Power Bonus +{newUser.LastPowerBonus}.{(logType == LoggingType.Priority ? " *You are in the priority logging list; thank you for your generous support!*" : "")}", guardian.GetGuardianEmbed());
            string recommend = fireteamPrivacy == PrivacySetting.Open || fireteamPrivacy == PrivacySetting.ClanAndFriendsOnly || fireteamPrivacy == PrivacySetting.FriendsOnly ? $" It is recommended to change your privacy to prevent people from joining you. {user.Mention}" : "";
            await LogHelper.Log(userLogChannel, $"{uniqueName} has fireteam on {privacy}.{recommend}");

            ActiveConfig.AddActiveUserToConfig(newUser, logType);
            ActiveConfig.UpdateActiveAFKUsersConfig();
            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "'s" : "s'";
            string p = ActiveConfig.PriorityActiveAFKUsers.Count != 0 ? $" (+{ActiveConfig.PriorityActiveAFKUsers.Count})" : "";
            await Context.Client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers}{p} User{s} XP", ActivityType.Watching));

            await LogHelper.Log(userLogChannel, "User is subscribed to our Bungie API refreshes. Waiting for next refresh...");
            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Your logging has been successfully restarted! Reminder that the previous session did not continue; a new session was created without making a new channel!"; });
            LogHelper.ConsoleLog($"[LOGGING] Started XP logging for {newUser.UniqueBungieName}.");
        }
    }
}
