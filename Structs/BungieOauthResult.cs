using Levante.Configs;
using Levante.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Structs
{
    public class BungieOauthResult
    {
        private string DiscordUsername;
        private BungieOauthErrorReason Reason;

        public BungieOauthResult(string discordName = "", BungieOauthErrorReason reason = BungieOauthErrorReason.None)
        {
            DiscordUsername = string.IsNullOrEmpty(discordName) ? AppConfig.App.Name : discordName;
            Reason = reason;
        }

        public string GetDiscordUsername() => DiscordUsername;

        public void SetDiscordUsername(string name) => DiscordUsername = name;

        public BungieOauthErrorReason GetReason() => Reason;

        public void SetReason(BungieOauthErrorReason reason) => Reason = reason;
    }

    public enum BungieOauthErrorReason
    {
        None,
        MissingParameters,
        OldCode,
        NoProfileDataFound,
        DiscordUserNotFound,
        NoDiscordMessageSent,
        Unknown,
    }
}
