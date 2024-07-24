using Discord;
using Levante.Configs;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
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

        public void AddDailyChannel(ulong guildId, ulong channelId)
        {
            var guild = GetGuild(guildId);

            guild.AddDailyChannel(channelId);
            WriteGuildsToFile();
        }

        public void AddWeeklyChannel(ulong guildId, ulong channelId)
        {
            var guild = GetGuild(guildId);

            guild.AddWeeklyChannel(channelId);
            WriteGuildsToFile();
        }

        public void AddEmblemChannel(ulong guildId, ulong channelId, ulong roleId)
        {
            var guild = GetGuild(guildId);

            guild.AddEmblemChannel(channelId, roleId);
            WriteGuildsToFile();
        }

        private void WriteGuildsToFile()
        {
            string output = JsonConvert.SerializeObject(guilds, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }
    }

    public class Guild
    {
        [JsonProperty("GuildId")]
        public ulong GuildId { get; set; } = 0;

        [JsonProperty("Settings", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(null)]
        public GuildSettings Settings { get; set; }

        public Guild()
        {

        }

        public Guild(ulong guildId, GuildSettings settings = null)
        {
            GuildId = guildId;
            Settings = settings;
        }

        public void AddDailyChannel(ulong channelId)
        {
            // Add a configurable limit? Increase limit for Guild Supporters? Is there a reason to have more than one?
            if (Settings.DailyChannelIds.Count >= 1)
                return;

            Settings.DailyChannelIds.Add(channelId);
        }

        public void AddWeeklyChannel(ulong channelId)
        {
            // Add a configurable limit? Increase limit for Guild Supporters? Is there a reason to have more than one?
            if (Settings.WeeklyChannelIds.Count >= 1)
                return;

            Settings.WeeklyChannelIds.Add(channelId);
        }

        public void AddEmblemChannel(ulong channelId, ulong roleId)
        {
            // Add a configurable limit? Increase limit for Guild Supporters? Is there a reason to have more than one?
            if (Settings.EmblemChannelIds.Count >= 1)
                return;

            Settings.EmblemChannelIds.Add(new EmblemAlert() { ChannelId = channelId, RoleId = roleId });
        }
    }

    public class GuildSettings
    {
        [JsonProperty("IsSupporter", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool IsSupporter { get; set; } = false;

        [JsonProperty("WeeklyChannelId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(typeof(List<ulong>))]
        public List<ulong> WeeklyChannelIds { get; set; } = new();

        [JsonProperty("DailyChannelIds", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(typeof(List<ulong>))]
        public List<ulong> DailyChannelIds { get; set; } = new();

        [JsonProperty("EmblemChannelIds", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(typeof(List<EmblemAlert>))]
        public List<EmblemAlert> EmblemChannelIds { get; set; } = new();
    }

    public class EmblemAlert
    {
        [JsonProperty("ChannelId")]
        public ulong ChannelId { get; set; } = 0;

        [JsonProperty("RoleId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(0)]
        public ulong RoleId { get; set; } = 0;
    }
}
