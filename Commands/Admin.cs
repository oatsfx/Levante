using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Levante.Configs;
using System.Drawing;
using System.IO;
using Levante.Util;
using System.Net;
using System.Collections.Generic;
using Fergun.Interactive;

namespace Levante.Commands
{
    public class Admin : ModuleBase<SocketCommandContext>
    {
        [Command("createHub", RunMode = RunMode.Async)]
        [Summary("Creates the post with buttons so people can start their logs.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task CreateHub()
        {
            var app = await Context.Client.GetApplicationInfoAsync();

            ICategoryChannel cc = null;
            foreach (var categoryChan in Context.Guild.CategoryChannels)
                if (categoryChan.Name.Contains($"Thrallway Logger"))
                {
                    cc = categoryChan;
                    // Remove an existing hub.
                    foreach (var channel in categoryChan.Channels)
                        if (channel.Name.ToLower().Contains("thrallway") && channel.Name.ToLower().Contains("hub"))
                            await channel.DeleteAsync();
                }

            // Create a category channel if there isn't one.
            if (cc == null)
            {
                cc = await Context.Guild.CreateCategoryChannelAsync($"Thrallway Logger");

                await cc.AddPermissionOverwriteAsync(Context.Guild.GetRole(Context.Guild.Id), new OverwritePermissions(sendMessages: PermValue.Deny));
                await cc.AddPermissionOverwriteAsync(Context.Client.GetUser(app.Id), new OverwritePermissions(sendMessages: PermValue.Allow, attachFiles: PermValue.Allow, embedLinks: PermValue.Allow, manageChannel: PermValue.Allow));
            }

            var hubChannel = Context.Guild.CreateTextChannelAsync($"thrallway-hub").Result;
            await hubChannel.ModifyAsync(x =>
            {
                x.CategoryId = cc.Id;
                x.Topic = $"Thrallway Hub: Start your logging here.";
                x.PermissionOverwrites = new[]
                {
                    new Overwrite(Context.Guild.Id, PermissionTarget.Role, new OverwritePermissions(sendMessages: PermValue.Deny, viewChannel: PermValue.Allow)),
                    new Overwrite(Context.Client.CurrentUser.Id, PermissionTarget.User, new OverwritePermissions(sendMessages: PermValue.Allow, attachFiles: PermValue.Allow, embedLinks: PermValue.Allow, manageChannel: PermValue.Allow))
                };
            });

            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Thrallway Hub",
                IconUrl = app.IconUrl,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Make sure you have your AFK setup running before clicking one of the buttons."
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };
            embed.Description =
                $"Are you getting ready to go AFK in Shattered Throne soon?\n" +
                $"Click the \"Ready\" button and I'll start logging your progress and even DM you if I think you wiped!\n" +
                $"When you are done, click the \"Stop\" button and I'll shut your logging down.";

            Emoji sleepyEmote = new Emoji("😴");
            Emoji helpEmote = new Emoji("❔");
            Emoji stopEmote = new Emoji("🛑");

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Ready", customId: $"startAFK", ButtonStyle.Secondary, sleepyEmote, row: 0)
                .WithButton("Stop", customId: $"stopAFK", ButtonStyle.Secondary, stopEmote, row: 0)
                .WithButton("Help", customId: $"viewHelp", ButtonStyle.Secondary, helpEmote, row: 0);

            await hubChannel.SendMessageAsync($"", false, embed.Build(), components: buttonBuilder.Build());

            await ReplyAsync($"{Context.User.Mention}: Hub created at {hubChannel.Mention}. Feel free to move that Category anywhere!");
        }
    }
}
