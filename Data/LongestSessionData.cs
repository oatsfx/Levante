using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace DestinyUtility.Data
{
    public partial class LongestSessionData
    {
        [JsonProperty("XPPerHourEntries")]
        public static List<LongestSessionEntry> LongestSessionEntries { get; set; } = new List<LongestSessionEntry>();

        public partial class LongestSessionEntry : LeaderboardEntry
        {
            [JsonProperty("Time")]
            public TimeSpan Time { get; set; } = TimeSpan.MinValue;

            [JsonProperty("UniqueBungieName")]
            public string UniqueBungieName { get; set; } = "Guardian#0000";
        }
    }
}
