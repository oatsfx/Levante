using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace DestinyUtility.Leaderboards
{
    public class LongestSessionData
    {
        public static readonly string FilePath = @"Data/longestSessionData.json";

        [JsonProperty("LongestSessionEntries")]
        public static List<LongestSessionEntry> LongestSessionEntries { get; set; } = new List<LongestSessionEntry>();

        public partial class LongestSessionEntry : LeaderboardEntry
        {
            [JsonProperty("Time")]
            public TimeSpan Time { get; set; } = TimeSpan.MinValue;

            [JsonProperty("UniqueBungieName")]
            public string UniqueBungieName { get; set; } = "Guardian#0000";
        }

        public static List<LongestSessionEntry> GetSortedLevelData()
        {
            QuickSort(0, LongestSessionEntries.Count - 1);
            return LongestSessionEntries;
        }

        private static void QuickSort(int Start, int End)
        {
            if (Start < End)
            {
                int partIndex = Partition(Start, End);

                QuickSort(Start, partIndex - 1);
                QuickSort(partIndex + 1, End);
            }
        }

        private static int Partition(int Start, int End)
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

        public static void UpdateEntriesConfig()
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
            LongestSessionEntries.Clear();
            LongestSessionData jsonObj = JsonConvert.DeserializeObject<LongestSessionData>(json);

            LongestSessionEntries.Add(mtt);
            LongestSessionData lsd = new LongestSessionData();
            string output = JsonConvert.SerializeObject(lsd, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void AddEntryToConfig(LongestSessionEntry lde)
        {
            string json = File.ReadAllText(FilePath);
            LongestSessionEntries.Clear();
            LongestSessionData jsonObj = JsonConvert.DeserializeObject<LongestSessionData>(json);

            LongestSessionEntries.Add(lde);
            LongestSessionData lsd = new LongestSessionData();
            string output = JsonConvert.SerializeObject(lsd, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void DeleteEntryFromConfig(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            LongestSessionEntries.Clear();
            LongestSessionData lsd = JsonConvert.DeserializeObject<LongestSessionData>(json);
            for (int i = 0; i < LongestSessionEntries.Count; i++)
                if (LongestSessionEntries[i].UniqueBungieName.Equals(BungieName))
                    LongestSessionEntries.RemoveAt(i);
            string output = JsonConvert.SerializeObject(lsd, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static bool IsExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            LongestSessionEntries.Clear();
            LongestSessionData jsonObj = JsonConvert.DeserializeObject<LongestSessionData>(json);
            foreach (LongestSessionEntry mtt in LongestSessionEntries)
                if (mtt.UniqueBungieName.Equals(BungieName))
                    return true;
            return false;
        }

        public static LongestSessionEntry GetExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            LongestSessionEntries.Clear();
            LongestSessionData jsonObj = JsonConvert.DeserializeObject<LongestSessionData>(json);
            foreach (LongestSessionEntry mtt in LongestSessionEntries)
                if (mtt.UniqueBungieName.Equals(BungieName))
                    return mtt;
            return null;
        }

        #endregion
    }
}
