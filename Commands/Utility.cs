using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Levante.Util;
using System;
using System.Text;
using Levante.Util.Attributes;
using Levante.Services;
using Levante.Configs;

namespace Levante.Commands
{
    public class Utility : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly UserService Users;

        [SlashCommand("link", "Link your Bungie account to your Discord account through Levante.")]
        public async Task Link()
        {
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {AppConfig.App.Name} v{AppConfig.App.Version}"
            };
            var auth = new EmbedAuthorBuilder()
            {
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                Name = "Account Linking"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                Footer = foot,
                Author = auth
            };
            var plainTextBytes = Encoding.UTF8.GetBytes($"{Context.User.Id}");
            string state = Convert.ToBase64String(plainTextBytes);

            embed.Title = $"Click here to start the linking process.";
            embed.Url = $"https://www.bungie.net/en/OAuth/Authorize?client_id={AppConfig.Credentials.BungieClientId}&response_type=code&state={state}";
            embed.Description = $"- Linking allows you to start XP Tracking, quick '/guardian' commands, and more.\n" +
                $"- After linking is complete, you'll receive another DM from me to confirm.\n" +
                $"- Experienced a name change? Relinking will update your name with our data.";

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Link with Levante", style: ButtonStyle.Link, url: $"https://www.bungie.net/en/OAuth/Authorize?client_id={AppConfig.Credentials.BungieClientId}&response_type=code&state={state}", emote: EmoteConfig.GetEmote("LevanteLogo"), row: 0);

            await RespondAsync(embed: embed.Build(), components: buttonBuilder.Build(), ephemeral: true);
        }

        [RequireBungieOauth]
        [SlashCommand("unlink", "Unlink your Bungie tag from your Discord account through Levante.")]
        public async Task Unlink()
        {
            var linkedUser = Users.GetUser(Context.User.Id);
            Users.RemoveUser(Context.User.Id);

            await RespondAsync($"Your Bungie account: {linkedUser.BungieCredentials.BungieName} has been unlinked. Use the command \"/link\" if you want to re-link!", ephemeral: true);
        }
    }
}
