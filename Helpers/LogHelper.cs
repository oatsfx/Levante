using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DestinyUtility.Helpers
{
    public static class LogHelper
    {
        public async static Task Log(IMessageChannel Channel, string Message)
        {
            if (Channel == null) return;
            await Channel.SendMessageAsync($"> {GetTimePrefix()} {Message}");
        }

        public async static Task Log(IMessageChannel Channel, string Message, ComponentBuilder CB)
        {
            if (Channel == null) return;
            await Channel.SendMessageAsync($"> {GetTimePrefix()} {Message}", components: CB.Build());
        }

        public async static Task Log(IMessageChannel Channel, string Message, EmbedBuilder Embed)
        {
            if (Channel == null) return;
            await Channel.SendMessageAsync($"> {GetTimePrefix()} {Message}", embed: Embed.Build());
        }

        public async static Task Log(IMessageChannel Channel, string Message, EmbedBuilder Embed, ComponentBuilder CB)
        {
            if (Channel == null) return;
            await Channel.SendMessageAsync($"> {GetTimePrefix()} {Message}", embed: Embed.Build(), components: CB.Build());
        }

        private static string GetTimePrefix()
        {
            return $"[{TimestampTag.FromDateTime(DateTime.Now, TimestampTagStyles.LongTime)}]:";
            //return $"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}]:";
        }
    }
}
