using Discord;
using Levante.Configs;

namespace Levante.Util
{
    public static class Embeds
    {
        public static EmbedBuilder GetErrorEmbed()
        {
            var embed = new EmbedBuilder()
            {
                Title = "Uh oh!",
                Description = $"An error has occurred. If this error continues, [let us know](https://discord.gg/{AppConfig.Discord.SupportServerInvite})!",
                Color = new Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                ThumbnailUrl = AppConfig.App.LogoUrl,
                Footer = new()
                {
                    IconUrl = AppConfig.App.AvatarUrl,
                    Text = $"{AppConfig.App.Name} v{AppConfig.App.Version}",
                }
            };

            return embed;
        }

        public static EmbedBuilder GetHelpEmbed()
        {
            var embed = new EmbedBuilder()
            {
                Title = "Bot Help",
                Description = "All commands and bot features can be found at [this page](https://www.levante.dev/features/).",
                Color = new Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                ThumbnailUrl = AppConfig.App.LogoUrl,
                Footer = new()
                {
                    IconUrl = AppConfig.App.AvatarUrl,
                    Text = $"{AppConfig.App.Name} v{AppConfig.App.Version}",
                },
            };

            return embed;
        }
    }
}
