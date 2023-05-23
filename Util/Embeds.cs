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
                Description = $"An error has occurred. If this error continues, [let us know](https://discord.gg/{BotConfig.SupportServer})!",
                Color = new Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                ThumbnailUrl = BotConfig.BotLogoUrl,
                Footer = new()
                {
                    IconUrl = BotConfig.BotAvatarUrl,
                    Text = $"{BotConfig.AppName} v{BotConfig.Version}",
                }
            };

            return embed;
        }
    }
}
