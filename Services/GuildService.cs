using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Services
{
    internal class GuildService
    {
    }

    public class Guild
    {
        [JsonProperty("GuildId")]
        public ulong GuildId { get; set; } = 0;

        [JsonProperty("Settings")]
        public GuildSettings Settings { get; set; } = new();
    }

    public class GuildSettings
    {
        [JsonProperty("IsSupporter", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool IsSupporter { get; set; } = false;

        [JsonProperty("WeeklyChannelId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(false)]
        public List<ulong> WeeklyChannelIds { get; set; } = new();

        [JsonProperty("DailyChannelIds", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(false)]
        public List<ulong> DailyChannelIds { get; set; } = new();

        [JsonProperty("EmblemChannelIds", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(false)]
        public List<EmblemAlert> EmblemChannelIds { get; set; } = new();
    }

    public class EmblemAlert
    {
        [JsonProperty("ChannelId")]
        public ulong ChannelId { get; set; }

        [JsonProperty("RoleId")]
        public ulong RoleId { get; set; }
    }
}
