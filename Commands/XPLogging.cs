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

            var normalCount = ActiveConfig.ActiveAFKUsers.Count;
            var priorityCount = ActiveConfig.PriorityActiveAFKUsers.Count;
            var maxCount = ActiveConfig.MaximumLoggingUsers;
            string p = normalCount != 0 ? $" (+{priorityCount})" : "";
            embed.Description = $"### {normalCount}/{maxCount}{p} people are logging their XP.";

            var isSupporter = BotConfig.IsSupporter(Context.User.Id) || BotConfig.IsStaff(Context.User.Id);

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
            if (!ActiveConfig.IsExistingActiveUser(Context.User.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"You are not using my logging feature. Look for an \"#xp-hub\" channel in a Discord I am in to get started.";

                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            var aau = ActiveConfig.GetActiveAFKUser(Context.User.Id);
            await RespondAsync(embed: XPLoggingHelper.GenerateSessionSummary(aau).Build(), ephemeral: hide);
        }
    }
}
