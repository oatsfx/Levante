using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Leaderboards
{
    public class LongestSessionData
    {
        public static readonly string FilePathS15 = @"Data/S15/longestSessionData.json";
        public static readonly string FilePathS16 = @"Data/S16/longestSessionData.json";
        public static readonly string FilePathS17 = @"Data/S17/longestSessionData.json";
        public static readonly string FilePathS18 = @"Data/S18/longestSessionData.json";
        public static readonly string FilePathS19 = @"Data/S19/longestSessionData.json";
        public static readonly string FilePathS20 = @"Data/S20/longestSessionData.json";
        public static readonly string FilePath = @"Data/S21/longestSessionData.json";

        [JsonProperty("LongestSessionEntries")]
        public List<LongestSessionEntry> LongestSessionEntries { get; set; } = new();

        public partial class LongestSessionEntry : LeaderboardEntry
        {
            [JsonProperty("Time")]
            public TimeSpan Time { get; set; } = TimeSpan.MinValue;

            [JsonProperty("UniqueBungieName")]
            public string UniqueBungieName { get; set; } = "Guardian#0000";
        }

        public List<LongestSessionEntry> GetSortedLevelData()
        {
            QuickSort(0, LongestSessionEntries.Count - 1);
            return LongestSessionEntries;
        }

        private void QuickSort(int Start, int End)
        {
            if (Start < End)
            {
                int partIndex = Partition(Start, End);

                QuickSort(Start, partIndex - 1);
                QuickSort(partIndex + 1, End);
            }
        }

        private int Partition(int Start, int End)
        {
            TimeSpan Center = LongestSessionEntries[End].Time;

            int i = Start - 1;
            for (int j = Start; j < End; j++)
            {
                if (LongestSessionEntries[j].Time >= Center)
                {
                    i++;
                    var temp1 = LongestSessionEntries[i];
                    LongestSessionEntries[i] = LongestSessionEntries[j];
                    LongestSessionEntries[j] = temp1;
                }
            }

            var temp = LongestSessionEntries[i + 1];
            LongestSessionEntries[i + 1] = LongestSessionEntries[End];
            LongestSessionEntries[End] = temp;

            return i + 1;
        }

        #region JSONFileHandling

        public void UpdateEntriesConfig()
        {
            LongestSessionData mttd = new LongestSessionData();
            string output = JsonConvert.SerializeObject(mttd, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void AddEntryToConfig(TimeSpan Time, string BungieName)
        {
            LongestSessionEntry mtt = new LongestSessionEntry()
            {
                Time = Time,
                UniqueBungieName = BungieName
            };
            string json = File.ReadAllText(FilePath);
            LongestSessionData lsd = JsonConvert.DeserializeObject<LongestSessionData>(json);

            lsd.LongestSessionEntries.Add(mtt);
            string output = JsonConvert.SerializeObject(lsd, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void AddEntryToConfig(LongestSessionEntry lde)
        {
            string json = File.ReadAllText(FilePath);
            LongestSessionData lsd = JsonConvert.DeserializeObject<LongestSessionData>(json);

            lsd.LongestSessionEntries.Add(lde);
            string output = JsonConvert.SerializeObject(lsd, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void DeleteEntryFromConfig(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            LongestSessionData lsd = JsonConvert.DeserializeObject<LongestSessionData>(json);
            for (int i = 0; i < lsd.LongestSessionEntries.Count; i++)
                if (lsd.LongestSessionEntries[i].UniqueBungieName.Equals(BungieName))
                    lsd.LongestSessionEntries.RemoveAt(i);
            string output = JsonConvert.SerializeObject(lsd, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static bool IsExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            LongestSessionData lsd = JsonConvert.DeserializeObject<LongestSessionData>(json);
            foreach (LongestSessionEntry lse in lsd.LongestSessionEntries)
                if (lse.UniqueBungieName.Equals(BungieName))
                    return true;
            return false;
        }

        public static LongestSessionEntry GetExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            LongestSessionData lsd = JsonConvert.DeserializeObject<LongestSessionData>(json);
            foreach (LongestSessionEntry lse in lsd.LongestSessionEntries)
                if (lse.UniqueBungieName.Equals(BungieName))
                    return lse;
            return null;
        }

        #endregion
    }
}
