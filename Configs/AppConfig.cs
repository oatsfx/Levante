using Discord.WebSocket;
using Newtonsoft.Json;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Configs
{
    public class AppConfig : IConfig
    {
        public static readonly string FilePath = @"Configs/appConfig.json";

        [JsonProperty("App")]
        public static AppSettings App { get; set; } = new();

        [JsonProperty("Credentials")]
        public static AppCredentials Credentials { get; set; } = new();

        [JsonProperty("Discord")]
        public static DiscordSettings Discord { get; set; } = new();

        [JsonProperty("Logging")]
        public static LoggingSettings Logging { get; set; } = new();

        [JsonProperty("BotDevs")]
        public static List<ulong> BotDevDiscordIDs { get; set; } = new();

        [JsonProperty("BotStaff")]
        public static List<ulong> BotStaffDiscordIDs { get; set; } = new();

        [JsonProperty("BotSupporters")]
        public static List<ulong> BotSupportersDiscordIDs { get; set; } = new();

        [JsonProperty("GuildSupporters")]
        public static List<ulong> GuildSupportersDiscordIDs { get; set; } = new();

        [JsonProperty("ThirdPartyProjects")]
        public static List<ThirdParty> ThirdPartyProjects { get; set; } = new();

        [JsonProperty("SupportLinks")]
        public static List<ThirdParty> SupportLinks { get; set; } = new();

        [JsonProperty("UniversalCodes")]
        public static List<UniversalCode> UniversalCodes { get; set; } = new();

        [JsonProperty("Hashes")]
        public static HashesList Hashes { get; set; } = new();

        // <Hash, Name>
        [JsonProperty("SeasonalCurrencyHashes")]
        public static Dictionary<long, string> SeasonalCurrencyHashes { get; set; } = new();

        // <Hash, Emote>
        [JsonProperty("EngramHashes")]
        public static Dictionary<long, string> EngramHashes { get; set; } = new();

        public static bool IsDebug = false;

        public static AnsiConsoleTheme LevanteTheme { get; } = new(
            new Dictionary<ConsoleThemeStyle, string>
            {
                [ConsoleThemeStyle.Text] = "\x1b[38;5;0015m",
                [ConsoleThemeStyle.SecondaryText] = "\x1b[38;5;0007m",
                [ConsoleThemeStyle.TertiaryText] = "\x1b[38;5;0008m",
                [ConsoleThemeStyle.Invalid] = "\x1b[38;5;0011m",
                [ConsoleThemeStyle.Null] = "\x1b[38;5;0027m",
                [ConsoleThemeStyle.Name] = "\x1b[38;5;0007m",
                [ConsoleThemeStyle.String] = "\x1b[38;2;128;204;191m",
                [ConsoleThemeStyle.Number] = "\u001b[38;2;207;111;125m",
                [ConsoleThemeStyle.Boolean] = "\x1b[38;5;0027m",
                [ConsoleThemeStyle.Scalar] = "\x1b[38;5;0085m",
                [ConsoleThemeStyle.LevelVerbose] = "\x1b[38;5;0007m",
                [ConsoleThemeStyle.LevelDebug] = "\x1b[32;49m",
                [ConsoleThemeStyle.LevelInformation] = "\x1b[38;5;0015m",
                [ConsoleThemeStyle.LevelWarning] = "\x1b[38;5;0011m",
                [ConsoleThemeStyle.LevelError] = "\x1b[38;5;0015m\x1b[48;5;0196m",
                [ConsoleThemeStyle.LevelFatal] = "\x1b[38;5;0015m\x1b[48;5;0196m",
            });

        public class EmbedColorGroup
        {
            [JsonProperty("R")]
            public int R { get; set; }

            [JsonProperty("G")]
            public int G { get; set; }

            [JsonProperty("B")]
            public int B { get; set; }
        }

        public class UniversalCode
        {
            [JsonProperty("Name")]
            public string Name { get; set; }

            [JsonProperty("ImageUrl")]
            public string ImageUrl { get; set; }

            [JsonProperty("Code")]
            public string Code { get; set; }
        }

        public class HashesList
        {
            [JsonProperty("BaseRanks")]
            public string BaseRanks { get; set; } = "";

            [JsonProperty("ExtraRanks")]
            public string ExtraRanks { get; set; } = "";
        }

        public class ThirdParty
        {
            [JsonProperty("Name")]
            public string Name { get; set; }

            [JsonProperty("Description")]
            public string Description { get; set; }

            [JsonProperty("Links")]
            public List<ThirdPartyLink> Links { get; set; } = new();
        }

        public class ThirdPartyLink
        {
            [JsonProperty("Name")]
            public string Name { get; set; }

            [JsonProperty("Link")]
            public string Link { get; set; }
        }

        public class AppSettings
        {
            [JsonProperty("Name")]
            public string Name { get; set; } = "Levante";

            [JsonProperty("Website")]
            public string Website { get; set; } = "oatsfx.com";

            // Include the @ in this field as the string this is used in, does not include the @.         
            [JsonProperty("Twitter")]
            public string Twitter { get; set; } = "@OatsFX";

            [JsonProperty("Version")]
            public string Version { get; set; } = "1.0.0";

            [JsonProperty("OauthPort")]
            public int OauthPort { get; set; } = 8000;

            [JsonProperty("LogoUrl")]
            public string LogoUrl = "https://www.levante.dev/images/Levante-Logo.png";

            [JsonProperty("AvatarUrl")]
            public string AvatarUrl = "https://www.levante.dev/images/Levante-Avatar.png";
        }

        public class AppCredentials
        {
            [JsonProperty("BungieApiKey")]
            public string BungieApiKey { get; set; } = "[YOUR API KEY HERE]";

            [JsonProperty("BungieClientId")]
            public string BungieClientId { get; set; } = "[YOUR CLIENT ID HERE]";

            [JsonProperty("BungieClientSecret")]
            public string BungieClientSecret { get; set; } = "[YOUR CLIENT SECRET HERE]";

            [JsonProperty("EmblemReportApiKey")]
            public string EmblemReportApiKey { get; set; } = "[YOUR API KEY HERE]";
        }

        public class DiscordSettings
        {
            [JsonProperty("Token")]
            public string Token { get; set; } = "[YOUR TOKEN HERE]";

            [JsonProperty("Shards")]
            public int Shards { get; set; } = 1;

            // Exclude the https://discord.gg/ and only include the ending.
            [JsonProperty("SupportServerInvite")]
            public string SupportServerInvite { get; set; } = "Levante";

            [JsonProperty("PlayingStatuses")]
            public List<string> PlayingStatuses { get; set; } = new();

            [JsonProperty("WatchingStatuses")]
            public List<string> WatchingStatuses { get; set; } = new();

            [JsonProperty("CustomStatuses")]
            public List<string> CustomStatuses { get; set; } = new();

            [JsonProperty("DurationToWaitForNextMessage")]
            public int DurationToWaitForNextMessage { get; set; } = 20;

            [JsonProperty("DurationToWaitForPaginator")]
            public int DurationToWaitForPaginator { get; set; } = 75;

            [JsonProperty("DevServerId")]
            public ulong DevServerId { get; set; } = 0;

            [JsonProperty("SupportServerId")]
            public ulong SupportServerId { get; set; } = 0;

            [JsonProperty("LogChannelId")]
            public ulong LogChannelId { get; set; } = 0;

            [JsonIgnore]
            public SocketTextChannel LoggingChannel { get; set; } = null;

            [JsonProperty("CommunityCreationsLogChannelId")]
            public ulong CommunityCreationsLogChannelId { get; set; } = 0;

            [JsonIgnore]
            public SocketTextChannel CreationsLogChannel { get; set; } = null;

            [JsonProperty("EmbedColor")]
            public EmbedColorGroup EmbedColor { get; set; } = new();
        }

        public class LoggingSettings
        {
            [JsonProperty("MaximumLoggingUsers")]
            public int MaximumLoggingUsers = 20;

            [JsonProperty("RefreshesBeforeKick")]
            public int RefreshesBeforeKick = 2;

            [JsonProperty("TimeBetweenRefresh")]
            public int TimeBetweenRefresh = 2;

            [JsonProperty("RefreshScaling")]
            public double RefreshScaling = 0.025;
        }

        public static bool IsSupporter(ulong DiscordID) => BotSupportersDiscordIDs.Contains(DiscordID);

        public static bool IsSupporter(string BungieTag)
        {
            var user = DataConfig.DiscordIDLinks.FirstOrDefault(x => x.UniqueBungieName == BungieTag);
            return user == null ? false : BotSupportersDiscordIDs.Contains(user.DiscordID);
        }

        public static bool IsStaff(ulong DiscordID) => BotStaffDiscordIDs.Contains(DiscordID);

        public static bool IsStaff(string BungieTag)
        {
            var user = DataConfig.DiscordIDLinks.FirstOrDefault(x => x.UniqueBungieName == BungieTag);
            return user == null ? false : BotStaffDiscordIDs.Contains(user.DiscordID);
        }

        public static bool IsDeveloper(ulong DiscordID) => BotDevDiscordIDs.Contains(DiscordID);

        public static bool IsDeveloper(string BungieTag)
        {
            var user = DataConfig.DiscordIDLinks.FirstOrDefault(x => x.UniqueBungieName == BungieTag);
            return user == null ? false : BotDevDiscordIDs.Contains(user.DiscordID);
        }
    }
}
