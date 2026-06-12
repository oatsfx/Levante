using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Levante.Configs;
using Levante.Helpers;
using Levante.Leaderboards;
using Levante.Rotations;
using Levante.Util;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Levante.Util.Attributes;
using System.Xml.Linq;
using Levante.Rotations.Interfaces;
using System.Diagnostics.Metrics;
using Microsoft.VisualBasic;
using System.Diagnostics;

namespace Levante.Commands
{
    public class Utility : InteractionModuleBase<ShardedInteractionContext>
    {
        [SlashCommand("link", "Link your Bungie account to your Discord account through Levante.")]
        public async Task Link()
        {
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {BotConfig.AppName} v{BotConfig.Version}"
            };
            var auth = new EmbedAuthorBuilder()
            {
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                Name = "Account Linking"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Footer = foot,
                Author = auth
            };
            var plainTextBytes = Encoding.UTF8.GetBytes($"{Context.User.Id}");
            string state = Convert.ToBase64String(plainTextBytes);

            embed.Title = $"Click here to start the linking process.";
            embed.Url = $"https://www.bungie.net/en/OAuth/Authorize?client_id={BotConfig.BungieClientID}&response_type=code&state={state}";
            embed.Description = $"- Linking allows you to start XP Tracking, quick '/guardian' commands, and more.\n" +
                $"- After linking is complete, you'll receive another DM from me to confirm.\n" +
                $"- Experienced a name change? Relinking will update your name with our data.";

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Link with Levante", style: ButtonStyle.Link, url: $"https://www.bungie.net/en/OAuth/Authorize?client_id={BotConfig.BungieClientID}&response_type=code&state={state}", emote: Emote.Parse(Emotes.Logo), row: 0);

            await RespondAsync(embed: embed.Build(), components: buttonBuilder.Build(), ephemeral: true);
        }

        [RequireBungieOauth]
        [SlashCommand("unlink", "Unlink your Bungie tag from your Discord account through Levante.")]
        public async Task Unlink([Summary("delete-leaderboards", "Delete your leaderboard stats when you unlink. This is true by default.")] bool RemoveLeaderboard = true)
        {
            var linkedUser = DataConfig.GetLinkedUser(Context.User.Id);
            DataConfig.DeleteUserFromConfig(Context.User.Id);

            // Remove leaderboard data to respect user data.
            if (RemoveLeaderboard)
            {
                LevelData.DeleteEntryFromConfig(linkedUser.UniqueBungieName);
                LongestSessionData.DeleteEntryFromConfig(linkedUser.UniqueBungieName);
                MostXPLoggingTimeData.DeleteEntryFromConfig(linkedUser.UniqueBungieName);
                PowerLevelData.DeleteEntryFromConfig(linkedUser.UniqueBungieName);
                XPPerHourData.DeleteEntryFromConfig(linkedUser.UniqueBungieName);
            }

            await RespondAsync($"Your Bungie account: {linkedUser.UniqueBungieName} has been unlinked. Use the command \"/link\" if you want to re-link!", ephemeral: true);
        }
    }
}
