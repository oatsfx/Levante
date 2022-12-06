using Discord;
using Levante.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Util
{
    public static class Embeds
    {
        public static readonly EmbedBuilder ErrorEmbed = new()
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
    }
}
