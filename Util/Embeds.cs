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
                Description = "An error has occurred.",
                Color = new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                ThumbnailUrl = BotConfig.BotLogoUrl,
                Footer = new()
                {
                    IconUrl = BotConfig.BotAvatarUrl,
                    Text = $"Levante v{BotConfig.Version}",
                }
            };

            return embed;
        }
    }
}
