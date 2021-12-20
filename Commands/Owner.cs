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
using DestinyUtility.Rotations;

namespace DestinyUtility.Commands
{
    public class Owner : ModuleBase<SocketCommandContext>
    {
        [Command("force", RunMode = RunMode.Async)]
        [Summary("force")]
        [RequireOwner]
        public async Task Force()
        {
            Emoji helpEmote = new Emoji("❔");

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Force Reset", customId: $"force", ButtonStyle.Secondary, helpEmote, row: 0);

            await ReplyAsync($"This shouldn't really be used...", components: buttonBuilder.Build());
        }

        [Command("maxUsers", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task ChangeMaxUsers(int NewMaxUserCount)
        {
            if (NewMaxUserCount > 50)
            {
                await ReplyAsync($"That's too high.");
                return;
            }
            else if (NewMaxUserCount < 1)
            {
                await ReplyAsync($"That's too low.");
                return;
            }

            ActiveConfig.MaximumThrallwayUsers = NewMaxUserCount;
            ActiveConfig.UpdateActiveAFKUsersConfig();

            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "" : "s";
            await Context.Client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumThrallwayUsers} Thrallway Farmer{s}", ActivityType.Watching));
            await ReplyAsync($"Changed maximum Thrallway users to {NewMaxUserCount}.");
        }
    }
}
