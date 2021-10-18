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
        // component: buttonBuilder.Build()
        public async static Task Log(ITextChannel Channel, string Message)
        {
            if (Channel == null) return;
            await Channel.SendMessageAsync($"{GetTimePrefix()} {Message}");
        }

        public async static Task Log(ITextChannel Channel, string Message, ComponentBuilder CB)
        {
            if (Channel == null) return;
            await Channel.SendMessageAsync($"{GetTimePrefix()} {Message}", component: CB.Build());
        }

        public async static Task Log(ITextChannel Channel, string Message, EmbedBuilder Embed)
        {
            if (Channel == null) return;
            await Channel.SendMessageAsync($"{GetTimePrefix()} {Message}", embed: Embed.Build());
        }

        public async static Task Log(ITextChannel Channel, string Message, EmbedBuilder Embed, ComponentBuilder CB)
        {
            if (Channel == null) return;
            await Channel.SendMessageAsync($"{GetTimePrefix()} {Message}", embed: Embed.Build(), component: CB.Build());
        }

        public async static Task Log(IDMChannel Channel, string Message)
        {
            if (Channel == null) return;
            await Channel.SendMessageAsync($"{GetTimePrefix()} {Message}");
        }

        private static string GetTimePrefix()
        {
            return $"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}]:";
        }
    }
}
