using Discord;
using Discord.WebSocket;
using Levante.Commands;
using Levante.Configs;
using Levante.Structs;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Levante.Services
{
    public class GuildService
    {
        public readonly string FilePath = @"Data/guildData.json";

        private Dictionary<ulong, Guild> guilds = new();

        public GuildService()
        {
            if (!File.Exists(FilePath))
            {
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(guilds, Formatting.Indented));
                Log.Information("[{Type}] No {FilePath} file detected. A new one has been created, no action needed.", "Guilds", FilePath);
            }

            string json = File.ReadAllText(FilePath);
            guilds = JsonConvert.DeserializeObject<Dictionary<ulong, Guild>>(json);
            Log.Information("[{Type}] Loaded {Count} Guilds from data.", "Guilds", guilds.Count);
        }

        public Guild AddGuild(ulong guildId)
        {
            // Don't create a new guild if one already exists.
            // Send the existing guild anyway.
            if (IsExistingGuild(guildId))
                return GetGuild(guildId);

            var newGuild = new Guild(guildId);
            guilds.Add(guildId, newGuild);
            WriteGuildsToFile();
            return newGuild;
        }

        public bool IsExistingGuild(ulong guildId) => guilds.ContainsKey(guildId);

        public Guild GetGuild(ulong guildId)
        {
            // Create a new guild if the current one doesn't exist.
            if (!IsExistingGuild(guildId))
                return AddGuild(guildId);

            return guilds[guildId];
        }

        public void AddEmblemChannel(ulong guildId, ulong channelId, ulong roleId)
        {
            var guild = GetGuild(guildId);

            guild.AddEmblemChannel(channelId, roleId);
            WriteGuildsToFile();
        }

        public void AddEmblemChannel(ulong guildId, ulong channelId)
        {
            var guild = GetGuild(guildId);

            guild.AddEmblemChannel(channelId);
            WriteGuildsToFile();
        }

        public void RemoveEmblemChannel(ulong guildId, ulong channelId)
        {
            var guild = GetGuild(guildId);

            guild.RemoveEmblemChannel(channelId);
            WriteGuildsToFile();
        }

        public IEnumerable<RolePingAlert> GetEmblemChannels() => guilds.SelectMany(x => x.Value.GetEmblemChannels());

        public int GetEmblemChannelCount() => guilds.Sum(x => x.Value.GetEmblemChannels().Count);

        public bool IsExistingEmblemChannel(ulong channelId) => guilds.Any(x => x.Value.IsExistingEmblemChannel(channelId));

        public void AddNewsChannel(ulong guildId, ulong channelId, ulong roleId)
        {
            var guild = GetGuild(guildId);

            guild.AddNewsChannel(channelId, roleId);
            WriteGuildsToFile();
        }

        public void AddNewsChannel(ulong guildId, ulong channelId)
        {
            var guild = GetGuild(guildId);

            guild.AddNewsChannel(channelId);
            WriteGuildsToFile();
        }

        public void RemoveNewsChannel(ulong guildId, ulong channelId)
        {
            var guild = GetGuild(guildId);

            guild.RemoveNewsChannel(channelId);
            WriteGuildsToFile();
        }

        public IEnumerable<RolePingAlert> GetNewsChannels() => guilds.SelectMany(x => x.Value.GetNewsChannels());

        public int GetNewsChannelCount() => guilds.Sum(x => x.Value.GetNewsChannels().Count);

        public bool IsExistingNewsChannel(ulong channelId) => guilds.Any(x => x.Value.IsExistingNewsChannel(channelId));

        private void WriteGuildsToFile()
        {
            string output = JsonConvert.SerializeObject(guilds, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        // Posting to linked channels methods.

        public async Task PostToEmblemChannels(EmblemOffer offer)
        {
            var embed = offer.BuildEmbed();
            var button = offer.BuildExternalButton();
            foreach (var Link in GetEmblemChannels().ToList())
            {
                var channel = LevanteCordInstance.Client.GetChannel(Link.ChannelId) as ITextChannel;
                var guildChannel = LevanteCordInstance.Client.GetChannel(Link.ChannelId) as IGuildChannel;
                try
                {
                    if (channel == null || guildChannel == null)
                    {
                        Log.Information("[{Type}] Could not find channel {Id}. Removing this element.", "Data", Link.ChannelId);
                        RemoveEmblemChannel(guildChannel.Guild.Id, Link.ChannelId);
                        continue;
                    }

                    if (Link.RoleId != 0)
                    {
                        var role = LevanteCordInstance.Client.GetGuild(guildChannel.Guild.Id).GetRole(Link.RoleId);
                        var msg = await channel.SendMessageAsync($"{role.Mention}", embed: embed.Build(), components: button.Build());
                        if (channel is SocketNewsChannel && channel.Guild.Id == AppConfig.Discord.SupportServerId)
                            await msg.CrosspostAsync();
                    }
                    else
                    {
                        // Crosspost/Publish for the support server news channel.
                        var msg = await channel.SendMessageAsync("", embed: embed.Build(), components: button.Build());
                        if (channel is SocketNewsChannel && channel.Guild.Id == AppConfig.Discord.SupportServerId)
                            await msg.CrosspostAsync();
                    }
                }
                catch (Exception x)
                {
                    //LogHelper.ConsoleLog($"[OFFERS] Could not send to channel {guildChannel.Id} in guild {guildChannel.Guild.Id}.");
                    Log.Warning("[{Type}] Could not send to channel {ChannelId} in guild {GuildId}. Reason: {Reason}", "Offers", guildChannel.Id, guildChannel.Guild.Id, x);
                }
            }
        }

        public async Task PostToNewsChannels(EmbedBuilder embed)
        {
            foreach (var Link in GetNewsChannels().ToList())
            {
                var channel = LevanteCordInstance.Client.GetChannel(Link.ChannelId) as ITextChannel;
                var guildChannel = LevanteCordInstance.Client.GetChannel(Link.ChannelId) as IGuildChannel;
                try
                {
                    if (channel == null || guildChannel == null)
                    {
                        Log.Information("[{Type}] Could not find channel {Id}. Removing this element.", "Data", Link.ChannelId);
                        RemoveEmblemChannel(guildChannel.Guild.Id, Link.ChannelId);
                        continue;
                    }

                    if (Link.RoleId != 0)
                    {
                        var role = LevanteCordInstance.Client.GetGuild(guildChannel.Guild.Id).GetRole(Link.RoleId);
                        var msg = await channel.SendMessageAsync($"{role.Mention}", embed: embed.Build());
                        if (channel is SocketNewsChannel && channel.Guild.Id == AppConfig.Discord.SupportServerId)
                            await msg.CrosspostAsync();
                    }
                    else
                    {
                        // Crosspost/Publish for the support server news channel.
                        var msg = await channel.SendMessageAsync("", embed: embed.Build());
                        if (channel is SocketNewsChannel && channel.Guild.Id == AppConfig.Discord.SupportServerId)
                            await msg.CrosspostAsync();
                    }
                }
                catch (Exception x)
                {
                    //LogHelper.ConsoleLog($"[OFFERS] Could not send to channel {guildChannel.Id} in guild {guildChannel.Guild.Id}.");
                    Log.Warning("[{Type}] Could not send to channel {ChannelId} in guild {GuildId}. Reason: {Reason}", "Offers", guildChannel.Id, guildChannel.Guild.Id, x);
                }
            }
        }
    }

    public class Guild
    {
        [JsonProperty(nameof(GuildId))]
        public ulong GuildId { get; set; } = 0;

        [JsonProperty(nameof(Settings), DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(typeof(GuildSettings))]
        public GuildSettings Settings { get; set; } = new();

        public Guild()
        {

        }

        public Guild(ulong guildId, GuildSettings settings = null)
        {
            GuildId = guildId;
            Settings = settings ?? new();
        }
        public void AddEmblemChannel(ulong channelId, ulong? roleId = null)
        {
            // Add a configurable limit? Increase limit for Guild Supporters? Is there a reason to have more than one?
            var channelCount = Settings?.EmblemChannels?.Count ?? 0;
            if (channelCount >= 1)
                return;

            Settings.EmblemChannels.Add(new RolePingAlert
            {
                ChannelId = channelId,
                RoleId = roleId ?? 0
            });
        }

        public void AddNewsChannel(ulong channelId, ulong? roleId = 0)
        {
            // Add a configurable limit? Increase limit for Guild Supporters? Is there a reason to have more than one?
            var channelCount = Settings?.NewsChannels?.Count ?? 0;
            if (channelCount >= 1)
                return;

            Settings.NewsChannels.Add(new RolePingAlert
            {
                ChannelId = channelId,
                RoleId = roleId ?? 0
            });
        }

        public bool HasEmblemChannel() => Settings.EmblemChannels.Any();

        public List<RolePingAlert> GetEmblemChannels() => Settings?.EmblemChannels ?? new List<RolePingAlert>();

        public bool IsExistingEmblemChannel(ulong channelId) => Settings.EmblemChannels.Any(x => x.ChannelId == channelId);

        public void RemoveEmblemChannel(ulong channelId) => Settings.EmblemChannels.RemoveAll(x => x.ChannelId == channelId);

        public bool HasNewsChannel() => Settings.NewsChannels.Any();

        public List<RolePingAlert> GetNewsChannels() => Settings?.NewsChannels ?? new List<RolePingAlert>();

        public bool IsExistingNewsChannel(ulong channelId) => Settings?.NewsChannels?.Any(x => x.ChannelId == channelId) ?? false;

        public void RemoveNewsChannel(ulong channelId) => Settings.NewsChannels.RemoveAll(x => x.ChannelId == channelId);

        public bool ShouldSerializeSettings() => Settings.ShouldSerialize();
    }

    public class GuildSettings
    {
        [JsonProperty(nameof(IsSupporter), DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool IsSupporter { get; set; } = false;

        [JsonProperty(nameof(EmblemChannels), DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(typeof(List<RolePingAlert>))]
        public List<RolePingAlert> EmblemChannels { get; set; } = new();

        [JsonProperty(nameof(NewsChannels), DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(typeof(List<RolePingAlert>))]
        public List<RolePingAlert> NewsChannels { get; set; } = new();
        public bool ShouldSerializeEmblemChannels() => EmblemChannels?.Count > 0;
        public bool ShouldSerializeNewsChannels() => NewsChannels?.Count > 0;

        public bool ShouldSerialize()
        {
            return IsSupporter ||
                   ShouldSerializeEmblemChannels() ||
                   ShouldSerializeNewsChannels();
        }
    }

    public class RolePingAlert
    {
        [JsonProperty(nameof(ChannelId))]
        public ulong ChannelId { get; set; } = 0;

        [JsonProperty(nameof(RoleId), DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(0)]
        public ulong RoleId { get; set; } = 0;
    }
}
