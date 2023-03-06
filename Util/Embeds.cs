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
                Color = new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
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
