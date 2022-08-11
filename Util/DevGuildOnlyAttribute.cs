using Levante.Configs;
using System;

namespace Levante.Util
{
    public class DevGuildOnlyAttribute : Attribute
    {
        public ulong GuildID { get; }

        public DevGuildOnlyAttribute()
        {
            GuildID = BotConfig.DevServerID;
        }
    }
}
