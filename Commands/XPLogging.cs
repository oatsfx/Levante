using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Levante.Configs;
using Discord.Interactions;
using Levante.Helpers;
using Levante.Util;

namespace Levante.Commands
{
    public class XPLogging : InteractionModuleBase<ShardedInteractionContext>
    {
        [SlashCommand("active-logging", "Gets the amount of users logging their XP.")]
        public async Task ActiveAFK()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Active XP Logging Users",
                IconUrl = app.IconUrl
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {BotConfig.AppName} v{String.Format("{0:0.00#}", BotConfig.Version)}"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };

            if (ActiveConfig.ActiveAFKUsers.Count >= 1)
            {
                string p = ActiveConfig.PriorityActiveAFKUsers.Count != 0 ? $" (+{ActiveConfig.PriorityActiveAFKUsers.Count})" : "";
                embed.Description = $"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers}{p} people are logging their XP.";
            }
            else
            {
                embed.Description = "No users are using my XP logging feature.";
            }

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("current-session", "Pulls stats of a current XP session.")]
        public async Task SessionStats()
        {
            if (!ActiveConfig.IsExistingActiveUser(Context.User.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You are not using my logging feature. Look for an \"#xp-hub\" channel in a Discord I am in to get started.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            var aau = ActiveConfig.GetActiveAFKUser(Context.User.Id);
            await RespondAsync(embed: XPLoggingHelper.GenerateSessionSummary(aau).Build());
        }
    }
}
