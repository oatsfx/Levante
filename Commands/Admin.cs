using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Levante.Configs;

// ReSharper disable UnusedMember.Global

namespace Levante.Commands
{
    public class Admin : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("create-hub", "Creates a post with buttons so people can start their XP logs.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task CreateHub()
        {
            await DeferAsync(true);
            var app = await Context.Client.GetApplicationInfoAsync();

            ICategoryChannel cc = null;
            foreach (var categoryChan in Context.Guild.CategoryChannels)
                if (categoryChan.Name.Contains("XP Logging"))
                {
                    cc = categoryChan;
                    // Remove an existing hub.
                    foreach (var channel in categoryChan.Channels)
                        if (channel.Name.ToLower().Contains("xp") && channel.Name.ToLower().Contains("hub"))
                            await channel.DeleteAsync();
                }

            // Create a category channel if there isn't one.
            if (cc == null)
            {
                cc = await Context.Guild.CreateCategoryChannelAsync("XP Logging");

                await cc.AddPermissionOverwriteAsync(Context.Guild.GetRole(Context.Guild.Id),
                    new OverwritePermissions(sendMessages: PermValue.Deny));
                await cc.AddPermissionOverwriteAsync(Context.Client.GetUser(app.Id),
                    new OverwritePermissions(sendMessages: PermValue.Allow, attachFiles: PermValue.Allow,
                        embedLinks: PermValue.Allow, manageChannel: PermValue.Allow));
            }

            var hubChannel = Context.Guild.CreateTextChannelAsync("xp-hub").Result;
            await hubChannel.ModifyAsync(x =>
            {
                x.CategoryId = cc.Id;
                x.Topic = "XP Hub: Start your logging here.";
                x.PermissionOverwrites = new[]
                {
                    new Overwrite(Context.Guild.Id, PermissionTarget.Role,
                        new OverwritePermissions(sendMessages: PermValue.Deny, viewChannel: PermValue.Allow)),
                    new Overwrite(Context.Client.CurrentUser.Id, PermissionTarget.User,
                        new OverwritePermissions(sendMessages: PermValue.Allow, attachFiles: PermValue.Allow,
                            embedLinks: PermValue.Allow, manageChannel: PermValue.Allow))
                };
            });

            var auth = new EmbedAuthorBuilder
            {
                Name = "XP Hub",
                IconUrl = app.IconUrl
            };
            var foot = new EmbedFooterBuilder
            {
                Text = "Make sure you have your AFK setup running before clicking one of the buttons."
            };
            var embed = new EmbedBuilder
            {
                Color =
                    new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
                Description = "Are you ready to keep track of your XP gains?\n" +
                              "Click the \"Ready\" button and I'll start logging your progress!\n" +
                              "When you are done, click the \"Stop\" button and I'll shut your logging down."
            };

            var sleepyEmote = new Emoji("😴");
            var helpEmote = new Emoji("❔");
            var stopEmote = new Emoji("🛑");

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Ready", "startXPAFK", ButtonStyle.Secondary, sleepyEmote, row: 0)
                .WithButton("Stop", "stopXPAFK", ButtonStyle.Secondary, stopEmote, row: 0)
                .WithButton("Help", "viewXPHelp", ButtonStyle.Secondary, helpEmote, row: 0);

            await hubChannel.SendMessageAsync("", false, embed.Build(), components: buttonBuilder.Build());

            await Context.Interaction.ModifyOriginalResponseAsync(message =>
            {
                message.Content = $"Hub created at {hubChannel.Mention}. Feel free to move that Category anywhere!";
            });
        }

        [Group("alert", "Set up announcements for Daily/Weekly Reset and Emblem Offers.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public class Alert : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("emblem-offers", "Set up announcements for Emblem Offers.")]
            public async Task EmblemOffers(
                [Summary("role", "Add a role to be pinged when a new Emblem Offer is posted.")] IRole RoleToPing = null)
            {
                foreach (var channel in Context.Guild.TextChannels)
                {
                    if (!DataConfig.IsExistingEmblemLinkedChannel(channel.Id)) continue;
                    await RespondAsync($"This guild already has Emblem Offer posts set up in {channel.Mention}.",
                        ephemeral: true);
                    return;
                }

                if (DataConfig.IsExistingEmblemLinkedChannel(Context.Channel.Id))
                {
                    DataConfig.DeleteEmblemChannel(Context.Channel.Id);

                    await RespondAsync(
                        "This channel will no longer receive Emblem Offer reset posts. Run this command to re-subscribe to them!",
                        ephemeral: true);
                }
                else
                {
                    DataConfig.AddEmblemChannel(Context.Channel.Id, RoleToPing);

                    await RespondAsync(
                        "This channel is now successfully subscribed to Emblem Offer posts. Run this command again to remove this type of alert!",
                        ephemeral: true);
                }
            }

            [SlashCommand("resets", "Set up announcements for Daily/Weekly Reset.")]
            public async Task Resets(
                [Summary("reset-type", "Choose between Daily or Weekly Reset.")]
                [Choice("Daily", 0)]
                [Choice("Weekly", 1)]
                int ResetType)
            {
                var IsDaily = ResetType == 0;

                foreach (var channel in Context.Guild.TextChannels)
                {
                    if (!DataConfig.IsExistingLinkedChannel(channel.Id, IsDaily)) continue;
                    await RespondAsync(
                        $"This guild already has {(IsDaily ? "Daily" : "Weekly")} reset posts set up in {channel.Mention}.",
                        ephemeral: true);
                    return;
                }

                if (DataConfig.IsExistingLinkedChannel(Context.Channel.Id, IsDaily))
                {
                    DataConfig.DeleteChannelFromRotationConfig(Context.Channel.Id, IsDaily);

                    await RespondAsync(
                        $"This channel will no longer receive {(IsDaily ? "Daily" : "Weekly")} reset posts. Run this command to re-subscribe to them!",
                        ephemeral: true);
                }
                else
                {
                    DataConfig.AddChannelToRotationConfig(Context.Channel.Id, IsDaily);

                    await RespondAsync(
                        $"This channel is now successfully subscribed to {(IsDaily ? "Daily" : "Weekly")} reset posts. Run this command again to remove this type of alert!",
                        ephemeral: true);
                }
            }
        }
    }
}