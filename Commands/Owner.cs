using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DestinyUtility.Configs;

namespace DestinyUtility.Commands
{
    public class Owner : ModuleBase<SocketCommandContext>
    {
        [Command("createHub", RunMode = RunMode.Async)]
        [Summary("Creates the post with buttons so people can start their logs.")]
        [RequireOwner]
        public async Task Buttons()
        {
            ICategoryChannel cc = null;
            foreach (var categoryChan in Context.Guild.CategoryChannels)
            {
                if (categoryChan.Name.Equals($"Thrallway Logger"))
                {
                    cc = categoryChan;
                }
            }

            if (cc == null)
            {
                cc = await Context.Guild.CreateCategoryChannelAsync($"Thrallway Logger");
            }

            await cc.ModifyAsync(x =>
            {
                x.Position = Context.Guild.CategoryChannels.Count - 2;
                x.PermissionOverwrites = new[]
                {
                    new Overwrite(Context.Guild.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny))
                };
            });

            OverwritePermissions perms = new OverwritePermissions(sendMessages: PermValue.Deny);
            var hubChannel = Context.Guild.CreateTextChannelAsync($"thrallway-hub").Result;
            await hubChannel.ModifyAsync(x =>
            {
                x.CategoryId = cc.Id;
                x.Topic = $"Thrallway Hub: Start your logging here.";
                x.PermissionOverwrites = new[]
                {
                    new Overwrite(Context.Guild.Id, PermissionTarget.Role, new OverwritePermissions(sendMessages: PermValue.Deny, viewChannel: PermValue.Allow))
                };
            });
            var app = await Context.Client.GetApplicationInfoAsync();
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
                $"Click the \"Ready\" button and we'll start logging your progress and even DM you if we think you wiped!\n" +
                $"When you are done, click the \"Stop\" button and we'll shut your logging down.";

            Emoji sleepyEmote = new Emoji("😴");
            Emoji helpEmote = new Emoji("❔");
            Emoji stopEmote = new Emoji("🛑");

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Ready", customId: $"startAFK", ButtonStyle.Secondary, sleepyEmote, row: 0)
                .WithButton("Stop", customId: $"stopAFK", ButtonStyle.Secondary, stopEmote, row: 0)
                .WithButton("Help", customId: $"viewHelp", ButtonStyle.Secondary, helpEmote, row: 0);

            await hubChannel.SendMessageAsync($"", false, embed.Build(), component: buttonBuilder.Build());

            await ReplyAsync($"{Context.User.Mention}: Hub created at {hubChannel.Mention}");
        }
    }
}
