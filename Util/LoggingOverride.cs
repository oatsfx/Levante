using Discord;
using Fergun.Interactive;
using Levante.Configs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Util
{
    public class LoggingOverride
    {
        [JsonProperty("Hash")]
        public long Hash { get; set; } = 0;

        [JsonProperty("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("ShortName")]
        public string ShortName { get; set; } = string.Empty;

        /// <summary>
        /// Emote to show on the Discord select menu.
        /// </summary>
        [JsonProperty("DiscordEmote")]
        public string DiscordEmote { get; set; } = string.Empty;

        /// <summary>
        /// This property being true prevents users from starting to log with this override so 
        /// that it can be eventually removed.
        /// </summary>
        [JsonProperty("Disabled")]
        public bool Disabled { get; set; } = false;

        [JsonProperty("IsAllowedActivityChange")]
        public bool IsAllowedActivityChange { get; set; } = false;

        [JsonProperty("TrackXp")]
        public bool TrackXp { get; set; } = false;

        /// <summary>
        /// Determine if we're checking for an increase in value (true), or a decrease (false).
        /// </summary>
        [JsonProperty("CheckForIncrease")]
        public bool CheckForIncrease { get; set; } = true;

        [JsonProperty("OverrideType")]
        public LoggingOverrideType OverrideType { get; set; } = LoggingOverrideType.ProfileProgression;

        public EmbedBuilder GetEmbed()
        {
            var emote = Emote.Parse(DiscordEmote);
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Logging Override",
                IconUrl = emote.Url,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {AppConfig.App.Name} v{AppConfig.App.Version}"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                Author = auth,
                Footer = foot
            };
            embed.ThumbnailUrl = emote.Url;
            embed.Description = $"## {Name} ({ShortName})" +
                $"Override Type? {OverrideType}" +
                $"Disabled? {(Disabled ? "Yes." : "No.")}" +
                $"Can the Activity Change? {(IsAllowedActivityChange ? "Yes." : "No.")}" +
                $"Tracking XP? {(TrackXp ? "Yes." : "No.")}" +
                $"Increase or Decrease? {(CheckForIncrease ? "Increase." : "Decrease.")}";
            return embed;
        }
    }

    public enum LoggingOverrideType
    {
        /// <summary>
        /// Not supported. There is no foreseeable reason to create overrides for this.
        /// </summary>
        [EnumMember(Value = "Profile Progression")]
        ProfileProgression,
        [EnumMember(Value = "Character Progression")]
        CharacterProgression,
        [EnumMember(Value = "Inventory Item")]
        InventoryItem,
        [EnumMember(Value = "Consumable")]
        Consumable,
        [EnumMember(Value = "Profile String Variable")]
        ProfileStringVariable,
        [EnumMember(Value = "Character String Variable")]
        CharacterStringVariable,
    }
}
