using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Levante.Configs;
using Discord.Interactions;
using Levante.Helpers;
using Levante.Util;
using Levante.Services;

namespace Levante.Commands
{
    public class XPLogging : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly LoggingService Logging;

        public XPLogging(LoggingService xpLoggingService)
        {
            Logging = xpLoggingService;
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
                Text = $"Powered by {AppConfig.App.Name} v{AppConfig.App.Version}"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                Author = auth,
                Footer = foot,
            };

            var normalCount = Logging.GetXpLoggingUserCount();
            var priorityCount = Logging.GetPriorityXpLoggingUserCount();
            var maxCount = Logging.GetMaxLoggingUsers();
            string p = normalCount != 0 ? $" (+{priorityCount})" : "";
            embed.Description = $"### {normalCount}/{maxCount}{p} people are logging their XP.";

            var isSupporter = AppConfig.IsSupporter(Context.User.Id) || AppConfig.IsStaff(Context.User.Id);

            embed.AddField(x =>
            {
                x.Name = "What does this mean?";
                x.Value = $"- Number of non-supporters{(!isSupporter ? " (you)" : "")} logging: **{normalCount}**\n" +
                          $"- Max number of non-supporters allowed at once: **{maxCount}**\n" +
                          $"- Number of supporters{(isSupporter ? " (you)" : "")} logging: **{priorityCount}**\n";
                x.IsInline = true;
            });

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("current-session", "Pulls stats of a current XP session.")]
        public async Task SessionStats([Summary("hide", "Hide this post from users except yourself. Default: false")] bool hide = false)
        {
            if (!Logging.IsExistingActiveUser(Context.User.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You are not using my logging feature. Look for an \"#xp-hub\" channel in a Discord I am in to get started.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            var aau = Logging.GetLoggingUser(Context.User.Id);
            await RespondAsync(embed: Logging.GenerateSessionSummary(aau).Build(), ephemeral: hide);
        }
    }
}
