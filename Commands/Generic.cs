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
    public class Generic : ModuleBase<SocketCommandContext>
    {
        private CommandService _service;

        public Generic(CommandService service)
        {
            _service = service;
        }

        public InteractiveService Interactive { get; set; }

        [Command("help", RunMode = RunMode.Async)]
        [Summary("Replies with the command list of the bot.")]
        public async Task Help(string ModuleArg = null)
        {
            var dmChannel = await Context.User.CreateDMChannelAsync();

            var serverId = Context.Guild.Id;

            string prefix = BotConfig.DefaultCommandPrefix;

            var auth = new EmbedAuthorBuilder()
            {
                Name = "Command List"
            };

            var builder = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
            };

            foreach (var module in _service.Modules)
            {
                string description = "";
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess && !description.Contains(cmd.Name)) // Check for Duplicates and Permissions.
                        description += $"{prefix}{cmd.Name}\n";
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    if (ModuleArg != null && module.Name.ToLower().Equals(ModuleArg.ToLower()))
                    {
                        builder.AddField(x =>
                        {
                            x.Name = module.Name;
                            x.Value = description;
                            x.IsInline = true;
                        });
                    }
                    else if (ModuleArg == null)
                    {
                        builder.AddField(x =>
                        {
                            x.Name = module.Name;
                            x.Value = description;
                            x.IsInline = true;
                        });
                    }
                }
            }

            var slashCommands = await Context.Client.Rest.GetGlobalApplicationCommands();
            string slashCmdList = "";
            foreach (var slashCommand in slashCommands)
                if (!slashCmdList.Contains(slashCommand.Name))
                    slashCmdList += $"{slashCommand.Name}\n";

            if (builder.Fields.Count == 0)
            {
                if (ModuleArg.ToLower().Equals("slash"))
                {
                    builder.AddField(x =>
                    {
                        x.Name = "Slash Commands";
                        x.Value = String.IsNullOrWhiteSpace(slashCmdList) ? "No slash commands." : slashCmdList;
                        x.IsInline = false;
                    });
                }
                else
                {
                    await Context.Message.ReplyAsync($"No module by the name \"{ModuleArg}\" found.");
                    return;
                }
            }
            else if (builder.Fields.Count > 1)
            {
                builder.AddField(x =>
                {
                    x.Name = "Slash Commands";
                    x.Value = String.IsNullOrWhiteSpace(slashCmdList) ? "No slash commands." : slashCmdList;
                    x.IsInline = false;
                });
            }

            await dmChannel.SendMessageAsync("", false, builder.Build());

            await ReplyAsync($"{Context.User.Mention}, check your DMs. <a:verified:690374136526012506>");
        }

        [Command("info", RunMode = RunMode.Async)]
        [Summary("Replies with the info of the bot.")]
        public async Task InfoAsync()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var embed = new EmbedBuilder();
            embed.WithColor(new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B));

            embed.ThumbnailUrl = app.IconUrl;

            embed.Title = "Bot Information";
            embed.Description =
                "Destiny Utility is an [open-source](https://github.com/oatsfx/DestinyUtility) Discord bot using Discord.Net-Labs for various Destiny 2 Needs. " +
                "This bot is actively developed by [@OatsFX](https://twitter.com/OatsFX). It pulls most of its information from the Bungie API.";

            await ReplyAsync("", false, embed.Build());
        }

        [Command("invite", RunMode = RunMode.Async)]
        [Summary("Provides links that support this bot.")]
        public async Task Invite()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var embed = new EmbedBuilder();

            embed.WithColor(new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B));
            embed.ThumbnailUrl = app.IconUrl;
            embed.Title = "Invite Link";
            embed.Description =
                "__**Invite me to your server!**__" +
                "\n[Invite](https://discord.com/api/oauth2/authorize?client_id=882303133643047005&permissions=8&scope=applications.commands%20bot)";

            await ReplyAsync("", false, embed.Build());
        }

        [Command("ping", RunMode = RunMode.Async)]
        [Summary("Replies with latency in milliseconds.")]
        public async Task PingAsync()
        {
            int[] colors = new int[3];
            int latency = Context.Client.Latency;

            if (latency >= 0 && latency < 200)
            {
                colors[0] = 123;
                colors[1] = 232;
                colors[2] = 98;
            }
            else if (latency >= 200 && latency < 450)
            {
                colors[0] = 251;
                colors[1] = 254;
                colors[2] = 50;
            }
            else
            {
                colors[0] = 237;
                colors[1] = 69;
                colors[2] = 69;
            }

            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(colors[0], colors[1], colors[2]),
            };
            embed.Description =
                $"Pong! ({latency} ms)";

            await ReplyAsync("", false, embed.Build());
        }
    }
}
