using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using Discord;
using System.Globalization;
using System.Collections.Concurrent;
using System.Linq;
using Discord.WebSocket;
using DestinyUtility.Configs;

namespace DestinyUtility.Commands
{
    public class AccountLinking : ModuleBase<SocketCommandContext>
    {
        [Command("link", RunMode = RunMode.Async)]
        [Alias("register")]
        [Summary("Links a Bungie Membership ID to the User's Discord account.")]
        public async Task LinkAsync([Remainder] string BungieTag = null)
        {
            if (BungieTag == null)
            {
                await ReplyAsync($"Missing argument: Bungie Tag. Use the command like this: \"{BotConfig.DefaultCommandPrefix}link [YOUR BUNGIE TAG]\"");
            }
            else
            {
                if (DataConfig.IsExistingLinkedUser(Context.User.Id))
                {
                    await ReplyAsync("You have an account linked already.");
                    return;
                }

                string memId = DataConfig.GetValidDestinyMembership(BungieTag, out string memType);

                if (memId == null && memType == null)
                {
                    await ReplyAsync("Something went wrong. Is your Bungie Tag correct?");
                    return;
                }

                if (!DataConfig.IsPublicAccount(BungieTag))
                {
                    await ReplyAsync("Your account privacy is not set to public. I cannot access your information otherwise.");
                    return;
                }

                DataConfig.AddUserToConfig(Context.User.Id, memId, memType, BungieTag);
                await ReplyAsync($"Linked {Context.User.Mention} to {BungieTag}.");
            }
        }

        [Command("unlink", RunMode = RunMode.Async)]
        [Summary("Unlinks user's Bungie ID from their Discord ID.")]
        [RequireBotPermission(ChannelPermission.AddReactions)]
        public async Task UnlinkAsync()
        {
            if (!DataConfig.IsExistingLinkedUser(Context.User.Id))
            {
                await ReplyAsync("No Bungie Account ID Linked.");
                return;
            }

            DataConfig.DeleteUserFromConfig(Context.User.Id);
            await ReplyAsync("Unlinked your Bungie ID.");
        }
    }
}
