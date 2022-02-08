using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Levante.Configs;
using Levante.Helpers;
using Levante.Leaderboards;
using Levante.Rotations;
using Levante.Util;
using Newtonsoft.Json;


namespace Levante.Commands
{
    public class Components : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction("force")]
        public async Task Force()
        {
            CurrentRotations.WeeklyRotation();
            await RespondAsync(embed: CurrentRotations.WeeklyResetEmbed().Build());
        }

        [ComponentInteraction("deleteChannel")]
        public async Task DeleteChannel() => await (Context.Channel as SocketGuildChannel).DeleteAsync();

        [ComponentInteraction("startAFK")]
        public async Task StartAFK()
        {
            var user = Context.User;
            var guild = Context.Guild;
            if (ActiveConfig.ActiveAFKUsers.Count >= ActiveConfig.MaximumThrallwayUsers)
            {
                await RespondAsync($"Unfortunately, I am at the maximum number of users to watch ({ActiveConfig.MaximumThrallwayUsers}). Try again later.", ephemeral: true);
                return;
            }

            if (!DataConfig.IsExistingLinkedUser(user.Id))
            {
                await RespondAsync($"You are not registered! Use \"{BotConfig.DefaultCommandPrefix}link [YOUR BUNGIE TAG]\" to register.", ephemeral: true);
                return;
            }

            if (ActiveConfig.IsExistingActiveUser(user.Id))
            {
                await RespondAsync($"You are already actively using our logging feature.", ephemeral: true);
                return;
            }

            string memId = DataConfig.GetLinkedUser(user.Id).BungieMembershipID;
            string memType = DataConfig.GetLinkedUser(user.Id).BungieMembershipType;

            int userLevel = DataConfig.GetAFKValues(user.Id, out int lvlProg, out bool isInShatteredThrone, out PrivacySetting fireteamPrivacy, out string CharacterId, out string errorStatus);

            if (!errorStatus.Equals("Success"))
            {
                await RespondAsync($"An error has occurred. Reason: {errorStatus}", ephemeral: true);
                return;
            }

            ICategoryChannel cc = null;
            foreach (var categoryChan in guild.CategoryChannels)
            {
                if (categoryChan.Name.Contains($"Thrallway Logger"))
                {
                    cc = categoryChan;
                }
            }

            if (cc == null)
            {
                await RespondAsync($"No category by the name of \"Thrallway Logger\" was found, cancelling operation. Let a server admin know!", ephemeral: true);
                return;
            }

            await RespondAsync($"Getting things ready...", ephemeral: true);
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
            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "" : "s";
            await Context.Client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumThrallwayUsers} Thrallway Farmer{s}", ActivityType.Watching));

            await LogHelper.Log(userLogChannel, "User is subscribed to our Bungie API refreshes. Waiting for next refresh...");
            LogHelper.ConsoleLog($"Started logging for {newUser.UniqueBungieName}.");
        }

        [ComponentInteraction("stopAFK")]
        public async Task StopAFK()
        {
            var user = Context.User;
            if (!DataConfig.IsExistingLinkedUser(user.Id))
            {
                await RespondAsync($"You are not registered! Use \"{BotConfig.DefaultCommandPrefix}link [YOUR BUNGIE TAG]\" to register.", ephemeral: true);
                return;
            }

            if (!ActiveConfig.IsExistingActiveUser(user.Id))
            {
                await RespondAsync($"You are not actively using our logging feature.", ephemeral: true);
                return;
            }

            var aau = ActiveConfig.GetActiveAFKUser(user.Id);

            await LogHelper.Log(Context.Client.GetChannelAsync(aau.DiscordChannelID).Result as ITextChannel, $"<@{user.Id}>: Logging terminated by user. Here is your session summary:", Embed: ThrallwayHelper.GenerateSessionSummary(aau), CB: ThrallwayHelper.GenerateDeleteChannelButton());
            await LogHelper.Log(user.CreateDMChannelAsync().Result, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(aau.TimeStarted)}.", ThrallwayHelper.GenerateSessionSummary(aau));

            await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(aau));
            ActiveConfig.DeleteActiveUserFromConfig(user.Id);
            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "" : "s";
            await Context.Client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumThrallwayUsers} Thrallway Farmer{s}", ActivityType.Watching));
            await RespondAsync($"Stopped AFK logging for {aau.UniqueBungieName}.", ephemeral: true);
            LogHelper.ConsoleLog($"Stopped logging for {aau.UniqueBungieName} via user request.");
        }

        [ComponentInteraction("viewHelp")]
        public async Task ViewHelp()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Thrallway Logger Help!",
                IconUrl = app.IconUrl,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by @OatsFX"
            };
            var helpEmbed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };
            helpEmbed.Description =
                $"__Steps:__\n" +
                $"1) Launch Shattered Throne with the your choice of \"Thrallway\" checkpoint.\n" +
                $"2) Get into your desired setup and start your AFK script.\n" +
                $"3) This is when you can subscribe to our logs using the \"Ready\" button.";

            await RespondAsync($"", embed: helpEmbed.Build(), ephemeral: true);
        }
    }
}
