using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Levante.Util;
using Levante.Services;

namespace Levante.Configs
{
    public class EmoteConfig
    {
        private static IEnumerable<Emote> Emotes = [];

        public static async Task<Emote> AddEmote(string name, Image image)
        {
            if (HasEmote(name)) return GetEmote(name);
            var emote = await LevanteCordInstance.Client.Shards.First().CreateApplicationEmoteAsync(name, image);
            image.Dispose();
            await RefreshEmotes();
            return emote;
        }

        public static IEnumerable<Emote> GetEmotes() => Emotes;

        public static Emote GetEmote(string name)
        {
            if (Emotes.Any(x => x.Name.ToLower().Equals(name.ToLower())))
                return Emotes.First(x => x.Name.ToLower().Equals(name.ToLower()));

            throw new KeyNotFoundException($"Emote {name} not found.");
        }

        public static bool HasEmote(string name)
        {
            var emotes = GetEmotes();

            return emotes.Any(x => x.Name.ToLower().Equals(name.ToLower()));
        }

        public static async Task RefreshEmotes()
        {
            Emotes = await LevanteCordInstance.Client.Shards.First().GetApplicationEmotesAsync();
        }
    }
}
