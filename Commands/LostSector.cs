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
    public class LostSector : ModuleBase<SocketCommandContext>
    {
        [Command("lostSectorUpdates", RunMode = RunMode.Async)]
        [Summary("Set up the channel the command is used in for Lost Sector tracking updates.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetUpLSUpdates()
        {
            if (LostSectorRotation.IsExistingChannelForLostSectorUpdates(Context.Channel.Id))
            {
                await ReplyAsync("Channel has Lost Sector Tracking enabled already!");
                return;
            }

            LostSectorRotation.AddChannelToLostSectorUpdates(Context.Channel.Id);
            await ReplyAsync("Lost Sector Tracking enabled! Wait for daily reset!");
        }

        [Command("removeLostSectorUpdates", RunMode = RunMode.Async)]
        [Summary("Set up the channel the command is used in for Lost Sector tracking updates.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task RemoveLSUpdates()
        {
            if (!LostSectorRotation.IsExistingChannelForLostSectorUpdates(Context.Channel.Id))
            {
                await ReplyAsync("Channel does not have Lost Sector tracking enabled!");
                return;
            }

            LostSectorRotation.RemoveChannelFromLostSectorUpdates(Context.Channel.Id);
            await ReplyAsync("Lost Sector Tracking disabled!");
        }
    }
}
