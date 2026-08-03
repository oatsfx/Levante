using Discord;
using System.Threading.Tasks;
using Discord.Interactions;
using System;
using Levante.Util;
using Fergun.Interactive;
using Levante.Services;
using System.Linq;
using Levante.Configs;
using Levante.Structs;

namespace Levante.Commands
{
    public class Admin : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly GuildService Guilds;

        [DefaultMemberPermissions(GuildPermission.ManageChannels)]
        [Group("alert", "Set up announcements for Daily/Weekly Reset and Emblem Offers.")]
        public class Alert : InteractionModuleBase<ShardedInteractionContext>
        {
            private readonly GuildService Guilds;
            private readonly NewsService News;

            public Alert(GuildService guilds, NewsService news)
            {
                Guilds = guilds;
                News = news;
            }

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

                var guild = Guilds.GetGuild(Context.Guild.Id);

                if (guild.GetEmblemChannels().Any(x => x.ChannelId == Context.Channel.Id))
                {
                    Guilds.RemoveEmblemChannel(guild.GuildId, Context.Channel.Id);

                    await RespondAsync($"This channel will no longer receive Emblem Offer posts. Run this command to re-subscribe to them!", ephemeral: true);
                    return;
                }
                else
                {
                    foreach (var channel in Context.Guild.TextChannels)
                    {
                        if (guild.IsExistingEmblemChannel(channel.Id))
                        {
                            await RespondAsync($"This guild already has Emblem Offer posts set up in {channel.Mention}.", ephemeral: true);
                            return;
                        }
                    }

                    var randomOffer = EmblemOffer.GetRandomOffer();
                    
                    try
                    {
                        if (randomOffer == null)
                        {
                            var errEmbed = Embeds.GetErrorEmbed();
                            errEmbed.Description = $"There aren't any emblem offers as this time, but fret not, I've still set up alerts in this channel!";
                            await RespondAsync(embed: errEmbed.Build(), ephemeral: true);
                        }
                        else
                        {
                            await Context.Channel.SendMessageAsync(embed: randomOffer.BuildEmbed().Build(), components: randomOffer.BuildExternalButton().Build());
                        }
                        
                    }
                    catch
                    {
                        var errEmbed = Embeds.GetErrorEmbed();
                        errEmbed.Description = $"Something went wrong when trying to send an emblem offer post, do I have permissions to send messages into this channel?";
                        await RespondAsync(embed: errEmbed.Build(), ephemeral: true);
                        return;
                    }

                    if (RoleToPing != null)
                        Guilds.AddEmblemChannel(Context.Guild.Id, Context.Channel.Id, RoleToPing.Id);
                    else
                        Guilds.AddEmblemChannel(Context.Guild.Id, Context.Channel.Id);

                    await RespondAsync($"This channel is now successfully subscribed to Emblem Offer posts. Run this command again to remove this type of alert!", ephemeral: true);
                    return;
                }
            }

            [SlashCommand("news", "Set up announcements for News posts. Use this in the channel you want this set up in.")]
            public async Task NewsAlert([Summary("role", "Add a role to be pinged when a News post is posted.")] IRole RoleToPing = null)
            {
                if (Context.Channel.GetChannelType() == ChannelType.DM)
                {
                    var errEmbed = Embeds.GetErrorEmbed();
                    errEmbed.Description = $"I only allow alerts like these to be made in servers!";
                    await RespondAsync(embed: errEmbed.Build(), ephemeral: true);
                    return;
                }

                var guild = Guilds.GetGuild(Context.Guild.Id);

                if (guild.IsExistingNewsChannel(Context.Channel.Id))
                {
                    Guilds.RemoveNewsChannel(guild.GuildId, Context.Channel.Id);

                    await RespondAsync($"This channel will no longer receive News posts. Run this command to re-subscribe to them!", ephemeral: true);
                    return;
                }
                else
                {
                    foreach (var channel in Context.Guild.TextChannels)
                    {
                        if (guild.IsExistingNewsChannel(channel.Id))
                        {
                            await RespondAsync($"This guild already has News posts set up in {channel.Mention}.", ephemeral: true);
                            return;
                        }
                    }

                    var newsEmbed = News.GetRecentNewsEmbed();

                    try
                    {
                        await Context.Channel.SendMessageAsync($"{RoleToPing.Mention}", embed: newsEmbed);
                    }
                    catch
                    {
                        var errEmbed = Embeds.GetErrorEmbed();
                        errEmbed.Description = $"Something went wrong when trying to send a news post, do I have permissions to send messages into this channel?";
                        await RespondAsync(embed: errEmbed.Build(), ephemeral: true);
                        return;
                    }

                    if (RoleToPing != null)
                        Guilds.AddNewsChannel(Context.Guild.Id, Context.Channel.Id, RoleToPing.Id);
                    else
                        Guilds.AddNewsChannel(Context.Guild.Id, Context.Channel.Id);

                    await RespondAsync($"This channel is now successfully subscribed to News posts. Run this command again to remove this type of alert!", ephemeral: true);
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
                Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
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

            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Hub created at {hubChannel.Mention}. Feel free to move that Category anywhere! Do not remove the \"XP Logging\" string from the category name - that's how I know where to place channels!"; });
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
                Text = $"Powered by {AppConfig.App.Name} v{AppConfig.App.Version}",
            };
            var embed = new EmbedBuilder
            {
                Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                Author = auth,
                Footer = foot,
                Description = $"\n",
            };

            ulong emblemAnnounceId = 0,
                emblemAnnounceRoleId = 0,
                newsAnnounceId = 0,
                newsAnnounceRoleId = 0;

            var guild = Guilds.GetGuild(Context.Guild.Id);

            foreach (var Channel in Context.Guild.TextChannels)
            {
                if (emblemAnnounceId == 0 && guild.GetEmblemChannels().Exists(x => x.ChannelId == Channel.Id))
                {
                    emblemAnnounceId = Channel.Id;
                    emblemAnnounceRoleId = guild.GetEmblemChannels().Find(x => x.ChannelId == Channel.Id).RoleId;
                }

                if (newsAnnounceId == 0 && guild.GetNewsChannels().Exists(x => x.ChannelId == Channel.Id))
                {
                    newsAnnounceId = Channel.Id;
                    newsAnnounceRoleId = guild.GetNewsChannels().Find(x => x.ChannelId == Channel.Id).RoleId;
                }
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
                x.Name = $"> {AppConfig.App.Name} Integrations";
                x.Value = $"Bot Joined: {TimestampTag.FromDateTime(Context.Guild.GetUser(Context.Client.CurrentUser.Id).JoinedAt.Value.DateTime, TimestampTagStyles.ShortDateTime)}\n" +
                    $"Emblem Announce Channel: {(emblemAnnounceId == 0 ? "None." : $"<#{emblemAnnounceId}>")}{(emblemAnnounceRoleId != 0 ? $" and tags role: <@&{emblemAnnounceRoleId}>" : "")}\n" +
                    $"News Announce Channel: {(newsAnnounceId == 0 ? "None." : $"<#{newsAnnounceId}>")}{(newsAnnounceRoleId != 0 ? $" and tags role: <@&{newsAnnounceRoleId}>" : "")}\n";
                x.IsInline = false;
            });

            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }
    }
}
