using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Levante.Configs;
using Discord.Interactions;
using Levante.Helpers;
using Levante.Services;
using Levante.Util;
using Serilog;

namespace Levante.Commands
{
    public class XPLogging : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly XPLoggingService XpLoggingService;

        public XPLogging(XPLoggingService xpLoggingService)
        {
            XpLoggingService = xpLoggingService;
        }

        [SlashCommand("active-logging", "Gets the amount of users logging their XP.")]
        public async Task ActiveAFK()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder()
            {
                Name = "Active XP Logging Users",
                IconUrl = app.IconUrl
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {BotConfig.AppName} v{BotConfig.Version}"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = auth,
                Footer = foot,
            };

            var xpLoggingList = XpLoggingService.GetXpLoggingUsers();
            var priorityXpLoggingList = XpLoggingService.GetPriorityXpLoggingUsers();

            if (xpLoggingList.Count >= 1)
            {
                string p = priorityXpLoggingList.Count != 0 ? $" (+{priorityXpLoggingList.Count})" : "";
                embed.Description = $"{xpLoggingList.Count}/{XpLoggingService.GetMaxLoggingUsers()}{p} people are logging their XP.";
            }
            else
            {
                embed.Description = "No users are using my XP logging feature.";
            }

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("current-session", "Pulls stats of a current XP session.")]
        public async Task SessionStats([Summary("hide", "Hide this post from users except yourself. Default: false")] bool hide = false)
        {
            if (!XpLoggingService.IsExistingActiveUser(Context.User.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You are not using my logging feature. Look for an \"#xp-hub\" channel in a Discord I am in to get started.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            var aau = XpLoggingService.GetXpLoggingUser(Context.User.Id);
            await RespondAsync(embed: XpLoggingService.GenerateSessionSummary(aau).Build(), ephemeral: hide);
        }
    }
}
