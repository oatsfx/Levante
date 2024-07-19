using Levante.Configs;
using System;

namespace Levante.Util.Attributes
{
    public class DevGuildOnlyAttribute : Attribute
    {
        public ulong GuildID { get; }

        public DevGuildOnlyAttribute()
        {
            GuildID = AppConfig.Discord.DevServerId;
        }
    }
}
