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
        PowerLevel,
    }
}
