using DestinyUtility.Configs;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DestinyUtility.Leaderboards
{

    public interface LeaderboardEntry
    {
        public string UniqueBungieName { get; set; }
    }

    public enum Leaderboard
    {
        Level,
        LongestSession,
        XPPerHour,
        MostThrallwayTime,
    }
}
