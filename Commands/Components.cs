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
        public async Task DeleteChannel() => await (Context.Channel as SocketGuildChannel).DeleteAsync();

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
                await RespondAsync($"You are not registered! Use \"/link [YOUR BUNGIE TAG] <PLATFORM>\" to register.", ephemeral: true);
                return;
            }

            if (ActiveConfig.IsExistingActiveUser(user.Id))
            {
                await RespondAsync($"You are already actively using our logging feature.", ephemeral: true);
                return;
            }

            await DeferAsync(ephemeral: true);

            string memId = DataConfig.GetLinkedUser(user.Id).BungieMembershipID;
            string memType = DataConfig.GetLinkedUser(user.Id).BungieMembershipType;

            int userLevel = DataConfig.GetAFKValues(user.Id, out int lvlProg, out PrivacySetting fireteamPrivacy, out string CharacterId, out string errorStatus);

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

            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Getting things ready..."; });
            string uniqueName = DataConfig.GetLinkedUser(user.Id).UniqueBungieName;

            var userLogChannel = guild.CreateTextChannelAsync($"{uniqueName.Replace('#', '-')}").Result;
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
            switch (fireteamPrivacy)
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
            string recommend = fireteamPrivacy == PrivacySetting.Open || fireteamPrivacy == PrivacySetting.ClanAndFriendsOnly || fireteamPrivacy == PrivacySetting.FriendsOnly ? $" It is recommended to change your privacy to prevent people from joining you. {user.Mention}" : "";
            await LogHelper.Log(userLogChannel, $"{uniqueName} has fireteam on {privacy}.{recommend}");

            ActiveConfig.AddActiveUserToConfig(newUser);
            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "'s" : "s'";
            await Context.Client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers} Player{s} XP", ActivityType.Watching));

            await LogHelper.Log(userLogChannel, "User is subscribed to our Bungie API refreshes. Waiting for next refresh...");
            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Your logging channel has been successfully created! Access it here: {userLogChannel.Mention}!"; });
            LogHelper.ConsoleLog($"Started XP logging for {newUser.UniqueBungieName}.");
        }

        [ComponentInteraction("stopXPAFK")]
        public async Task StopAFK()
        {
            var user = Context.User;
            if (!DataConfig.IsExistingLinkedUser(user.Id))
            {
                await RespondAsync($"You are not registered! Use \"/link [YOUR BUNGIE TAG] <PLATFORM>\" to register.", ephemeral: true);
                return;
            }

            if (!ActiveConfig.IsExistingActiveUser(user.Id))
            {
                await RespondAsync($"You are not actively using our logging feature.", ephemeral: true);
                return;
            }

            var aau = ActiveConfig.GetActiveAFKUser(user.Id);

            await LogHelper.Log(Context.Client.GetChannelAsync(aau.DiscordChannelID).Result as ITextChannel, $"<@{user.Id}>: Logging terminated by user. Here is your session summary:", Embed: XPLoggingHelper.GenerateSessionSummary(aau, Context.Client.CurrentUser.GetAvatarUrl()), CB: XPLoggingHelper.GenerateDeleteChannelButton());
            await LogHelper.Log(user.CreateDMChannelAsync().Result, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(aau.TimeStarted)}.", XPLoggingHelper.GenerateSessionSummary(aau, Context.Client.CurrentUser.GetAvatarUrl()));

            await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(aau));
            ActiveConfig.DeleteActiveUserFromConfig(user.Id);
            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "'s" : "s'";
            await Context.Client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers} Player{s} XP", ActivityType.Watching));
            await RespondAsync($"Stopped AFK logging for {aau.UniqueBungieName}.", ephemeral: true);
            LogHelper.ConsoleLog($"Stopped logging for {aau.UniqueBungieName} via user request.");
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
                Text = $"Powered by {BotConfig.AppName}"
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

        // Old buttons.
        [ComponentInteraction("startAFK")]
        public async Task OldStartAFK() => await OldThrallwayButton();

        [ComponentInteraction("stopAFK")]
        public async Task OldStopAFK() => await OldThrallwayButton();

        [ComponentInteraction("viewHelp")]
        public async Task OldThrallwayButton()
        {
            await RespondAsync($"Hey! This button and hub are outdated and requires the new and improved XP hub! Ask a server admin to run the command '/create-hub' to get this sorted!\n" +
                $"*This warning will be removed in April 2022.*", ephemeral: true);
        }
    }
}
