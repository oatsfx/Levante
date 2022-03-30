using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Levante.Configs;
using Discord.Interactions;
using Levante.Helpers;

namespace Levante.Commands
{
    public class XPLogging : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("active-logging", "Gets a list of the users that are using my XP logging feature.")]
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
                Text = $"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers} people are logging their XP."
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };

            if (ActiveConfig.ActiveAFKUsers.Count >= 1)
            {
                embed.Description = $"__XP Logging List:__\n";
                foreach (var aau in ActiveConfig.ActiveAFKUsers)
                {
                    embed.Description +=
                        $"{aau.UniqueBungieName}: Level {aau.LastLoggedLevel}\n";
                }
            }
            else
            {
                embed.Description = "No users are using my XP logging feature.";
            }

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("current-session", "Pulls stats of a current XP session.")]
        public async Task SessionStats([Summary("user", "User you want the current XP Session stats for. Leave empty for your own.")] IUser User = null)
        {
            if (User == null)
                User = Context.User as SocketGuildUser;

            if (!ActiveConfig.IsExistingActiveUser(User.Id))
            {
                if (Context.User.Id == User.Id)
                {
                    await RespondAsync("You are not using my logging feature.", ephemeral: true);
                    return;
                }
                else
                {
                    await RespondAsync($"{User.Username} is not using my logging feature.", ephemeral: true);
                    return;
                }
            }

            var aau = ActiveConfig.GetActiveAFKUser(User.Id);
            await RespondAsync(embed: XPLoggingHelper.GenerateSessionSummary(aau, Context.Client.CurrentUser.GetAvatarUrl()).Build());
        }
    }
}
