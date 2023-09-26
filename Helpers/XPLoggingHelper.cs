using Discord;
using Levante.Configs;
using System;

namespace Levante.Helpers
{
    public class XPLoggingHelper
    {
        public static ComponentBuilder GenerateChannelButtons(ulong DiscordID)
        {
            Emoji deleteEmote = new Emoji("⛔");
            Emoji restartEmote = new Emoji("😴");

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Delete Log Channel", customId: $"deleteChannel", ButtonStyle.Secondary, deleteEmote, row: 0)
                .WithButton("Restart Logging", customId: $"restartLogging:{DiscordID}", ButtonStyle.Secondary, restartEmote, row: 0);

            return buttonBuilder;
        }
    }
}
