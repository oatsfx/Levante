using Discord;
using System.Threading.Tasks;
using Levante.Configs;
using Discord.Interactions;
using System;
using Levante.Util;
using Levante.Rotations;

namespace Levante.Commands
{
    public class Admin : InteractionModuleBase<ShardedInteractionContext>
    {
        [DefaultMemberPermissions(GuildPermission.ManageChannels)]
        [Group("alert", "Set up announcements for Daily/Weekly Reset and Emblem Offers.")]
        public class Alert : InteractionModuleBase<ShardedInteractionContext>
        {
            [SlashCommand("emblem-offers", "Set up announcements for Emblem Offers. Use this in the channel you want this set up in.")]
            public async Task EmblemOffers([Summary("role", "Add a role to be pinged when a new Emblem Offer is posted.")] IRole RoleToPing = null)
            {
                if (Context.Channel.GetChannelType() == ChannelType.DM)
                {
                    var errEmbed = Embeds.GetErrorEmbed();
                    errEmbed.Description = $"I only allow alerts like these to be made in servers!";
                    await RespondAsync(embed: errEmbed.Build(), ephemeral: true);
                    return;
                }

                if (DataConfig.IsExistingEmblemLinkedChannel(Context.Channel.Id))
                {
                    DataConfig.DeleteEmblemChannel(Context.Channel.Id);

                    await RespondAsync($"This channel will no longer receive Emblem Offer reset posts. Run this command to re-subscribe to them!", ephemeral: true);
                    return;
                }
                else
                {
                    foreach (var channel in Context.Guild.TextChannels)
                    {
                        if (DataConfig.IsExistingEmblemLinkedChannel(channel.Id))
                        {
                            await RespondAsync($"This guild already has Emblem Offer posts set up in {channel.Mention}.", ephemeral: true);
                            return;
                        }
                    }

                    var randomOffer = EmblemOffer.GetRandomOffer();
                    try
                    {
                        await Context.Channel.SendMessageAsync(embed: randomOffer.BuildEmbed().Build(), components: randomOffer.BuildExternalButton().Build());
                    }
                    catch
                    {
                        var errEmbed = Embeds.GetErrorEmbed();
                        errEmbed.Description = $"Something went wrong when trying to send an emblem offer post, do I have permissions to send messages into this channel?";
                        await RespondAsync(embed: errEmbed.Build(), ephemeral: true);
                        return;
                    }

                    DataConfig.AddEmblemChannel(Context.Channel.Id, RoleToPing);

                    await RespondAsync($"This channel is now successfully subscribed to Emblem Offer posts. Run this command again to remove this type of alert!", ephemeral: true);
                    return;
                }
            }

            [SlashCommand("resets", "Set up announcements for Daily/Weekly Reset. Use this in the channel you want this set up in.")]
            public async Task Resets([Summary("reset-type", "Choose between Daily or Weekly Reset."),
                Choice("Daily", 0), Choice("Weekly", 1)] int ResetType)
            {
                if (Context.Channel.GetChannelType() == ChannelType.DM)
                {
                    var errEmbed = Embeds.GetErrorEmbed();
                    errEmbed.Description = $"I only allow alerts like these to be made in servers!";
                    await RespondAsync(embed: errEmbed.Build(), ephemeral: true);
                    return;
                }

                bool IsDaily = ResetType == 0;

                if (DataConfig.IsExistingLinkedChannel(Context.Channel.Id, IsDaily))
                {
                    DataConfig.DeleteChannelFromRotationConfig(Context.Channel.Id, IsDaily);

                    await RespondAsync($"This channel will no longer receive {(IsDaily ? "Daily" : "Weekly")} reset posts. Run this command to re-subscribe to them!", ephemeral: true);
                    return;
                }
                else
                {
                    foreach (var channel in Context.Guild.TextChannels)
                    {
                        if (DataConfig.IsExistingLinkedChannel(channel.Id, IsDaily))
                        {
                            await RespondAsync($"This guild already has {(IsDaily ? "Daily" : "Weekly")} reset posts set up in {channel.Mention}.", ephemeral: true);
                            return;
                        }
                    }

                    try
                    {
                        await Context.Channel.SendMessageAsync(embed: IsDaily ? CurrentRotations.DailyResetEmbed().Build() : CurrentRotations.WeeklyResetEmbed().Build());
                    }
                    catch
                    {
                        var errEmbed = Embeds.GetErrorEmbed();
                        errEmbed.Description = $"Something went wrong when trying to post the reset embed, do I have permissions to send messages into this channel?";
                        await RespondAsync(embed: errEmbed.Build(), ephemeral: true);
                        return;
                    }

                    DataConfig.AddChannelToRotationConfig(Context.Channel.Id, IsDaily);

                    await RespondAsync($"This channel is now successfully subscribed to {(IsDaily ? "Daily" : "Weekly")} reset posts. Run this command again to remove this type of alert!", ephemeral: true);
                    return;
                }
            }
        }

        [DefaultMemberPermissions(GuildPermission.ManageChannels)]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        [SlashCommand("create-hub", "Creates a post with XP logging buttons so people can start their XP logs.")]
        public async Task CreateHub()
        {
            if (Context.Channel.GetChannelType() == ChannelType.DM)
            {
                var errEmbed = Embeds.GetErrorEmbed();
                errEmbed.Description = $"Cannot make channels in Direct Messages!";
                await RespondAsync(embed: errEmbed.Build());
                return;
            }

            await DeferAsync(ephemeral: true);
            var app = await Context.Client.GetApplicationInfoAsync();

            ICategoryChannel cc = null;
            foreach (var categoryChan in Context.Guild.CategoryChannels)
                if (categoryChan.Name.Contains($"XP Logging"))
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
                cc = await Context.Guild.CreateCategoryChannelAsync($"XP Logging");

                await cc.AddPermissionOverwriteAsync(Context.Guild.GetRole(Context.Guild.Id), new OverwritePermissions(sendMessages: PermValue.Deny));
                await cc.AddPermissionOverwriteAsync(Context.Client.GetUser(app.Id), new OverwritePermissions(sendMessages: PermValue.Allow, attachFiles: PermValue.Allow, embedLinks: PermValue.Allow, manageChannel: PermValue.Allow));
            }

            var hubChannel = Context.Guild.CreateTextChannelAsync($"xp-hub").Result;
            await hubChannel.ModifyAsync(x =>
            {
                x.CategoryId = cc.Id;
                x.Topic = $"XP Hub: Start your logging here.";
                x.PermissionOverwrites = new[]
                {
                    new Overwrite(Context.Guild.Id, PermissionTarget.Role, new OverwritePermissions(sendMessages: PermValue.Deny, viewChannel: PermValue.Allow)),
                    new Overwrite(Context.Client.CurrentUser.Id, PermissionTarget.User, new OverwritePermissions(sendMessages: PermValue.Allow, attachFiles: PermValue.Allow, embedLinks: PermValue.Allow, manageChannel: PermValue.Allow))
                };
            });

            var auth = new EmbedAuthorBuilder()
            {
                Name = $"XP Hub",
                IconUrl = app.IconUrl,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"To get started, link using the command '/link'."
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = auth,
                Footer = foot,
            };
            embed.Description =
                $"Are you ready to keep track of your XP gains?\n" +
                $"Click the \"Ready\" button and I'll start logging your progress!\n" +
                $"When you are done, click the \"Stop\" button and I'll shut your logging down.";

            Emoji sleepyEmote = new Emoji("😴");
            Emoji helpEmote = new Emoji("❔");
            Emoji stopEmote = new Emoji("🛑");

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Ready", customId: $"startXPAFK", ButtonStyle.Secondary, sleepyEmote, row: 0)
                .WithButton("Stop", customId: $"stopXPAFK", ButtonStyle.Secondary, stopEmote, row: 0)
                .WithButton("Help", customId: $"viewXPHelp", ButtonStyle.Secondary, helpEmote, row: 0);

            await hubChannel.SendMessageAsync($"", false, embed.Build(), components: buttonBuilder.Build());

            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Hub created at {hubChannel.Mention}. Feel free to move that Category anywhere!"; });
        }

        [DefaultMemberPermissions(GuildPermission.ManageChannels)]
        [SlashCommand("server-info", "Gets general information about a server/guild along with Levante integrations.")]
        public async Task ServerInfo()
        {
            if (Context.Channel.GetChannelType() == ChannelType.DM)
            {
                var errEmbed = Embeds.GetErrorEmbed();
                errEmbed.Description = $"Cannot get server information in Direct Messages!";
                await RespondAsync(embed: errEmbed.Build());
                return;
            }

            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Server Information: {Context.Guild.Name}",
                IconUrl = Context.Guild.IconUrl,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {BotConfig.AppName} v{BotConfig.Version}",
            };
            var embed = new EmbedBuilder
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = auth,
                Footer = foot,
                Description = $"\n",
            };

            ulong emblemAnnounceId = 0,
                emblemAnnounceRoleId = 0,
                dailyAnnounceId = 0,
                weeklyAnnounceId = 0;

            foreach (var Channel in Context.Guild.TextChannels)
            {
                if (emblemAnnounceId == 0 && DataConfig.AnnounceEmblemLinks.Exists(x => x.ChannelID == Channel.Id))
                {
                    emblemAnnounceId = Channel.Id;
                    emblemAnnounceRoleId = DataConfig.AnnounceEmblemLinks.Find(x => x.ChannelID == Channel.Id).RoleID;
                }

                if (dailyAnnounceId == 0 && DataConfig.AnnounceDailyLinks.Contains(Channel.Id))
                    dailyAnnounceId = Channel.Id;

                if (weeklyAnnounceId == 0 && DataConfig.AnnounceWeeklyLinks.Contains(Channel.Id))
                    weeklyAnnounceId = Channel.Id;

            }

            embed.AddField(x =>
            {
                x.Name = $"> General Info";
                x.Value = $"Owner: {Context.Guild.Owner.Mention}{(Context.Guild.Owner.Id == Context.User.Id ? " (Hey! That's you!)" : "")}\n" +
                    $"Member Count: {Context.Guild.MemberCount}\n";
                x.IsInline = false;
            })
            .AddField(x =>
            {
                x.Name = $"> {BotConfig.AppName} Integrations";
                x.Value = $"Bot Joined: {TimestampTag.FromDateTime(Context.Guild.GetUser(Context.Client.CurrentUser.Id).JoinedAt.Value.DateTime, TimestampTagStyles.ShortDateTime)}\n" +
                    $"Emblem Announce Channel: {(emblemAnnounceId == 0 ? "None." : $"<#{emblemAnnounceId}>")}{(emblemAnnounceRoleId != 0 ? $" and tags role: <@&{emblemAnnounceRoleId}>" : "")}\n" +
                    $"Daily Reset Channel: {(dailyAnnounceId == 0 ? "None." : $"<#{dailyAnnounceId}>")}\n" +
                    $"Weekly Reset Channel: {(weeklyAnnounceId == 0 ? "None." : $"<#{weeklyAnnounceId}>")}\n";
                x.IsInline = false;
            });

            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }
    }
}
