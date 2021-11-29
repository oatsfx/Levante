using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace DestinyUtility.Data
{
    public partial class LevelData
    {
        [JsonProperty("LevelDataEntries")]
        public static List<LevelDataEntry> LevelDataEntries { get; set; } = new List<LevelDataEntry>();

        public partial class LevelDataEntry : LeaderboardEntry
        {
            [JsonProperty("LastLoggedLevel")]
            public int LastLoggedLevel { get; set; } = -1;

            [JsonProperty("UniqueBungieName")]
            public string UniqueBungieName { get; set; } = "Guardian#0000";
        }

        public static List<LevelDataEntry> GetSortedLevelData()
        {
            QuickSort(0, LevelDataEntries.Count - 1);
            return LevelDataEntries;
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
            int Center = LevelDataEntries[End].LastLoggedLevel;

            int i = Start - 1;
            for (int j = Start; j < End; j++)
            {
                if (LevelDataEntries[j].LastLoggedLevel >= Center)
                {
                    i++;
                    var temp1 = LevelDataEntries[i];
                    LevelDataEntries[i] = LevelDataEntries[j];
                    LevelDataEntries[j] = temp1;
                }
            }

            var temp = LevelDataEntries[i + 1];
            LevelDataEntries[i + 1] = LevelDataEntries[End];
            LevelDataEntries[End] = temp;

            return i + 1;
        }

        #region JSONFileHandling

        public static void UpdateEntriesConfig()
        {
            LevelData ld = new LevelData();
            string output = JsonConvert.SerializeObject(ld, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.LevelDataPath, output);
        }

        public static void AddEntryToConfig(int Level, string BungieName)
        {
            LevelDataEntry lde = new LevelDataEntry()
            {
                LastLoggedLevel = Level,
                UniqueBungieName = BungieName
            };
            string json = File.ReadAllText(DestinyUtilityCord.LevelDataPath);
            LevelDataEntries.Clear();
            LevelData jsonObj = JsonConvert.DeserializeObject<LevelData>(json);

            LevelDataEntries.Add(lde);
            LevelData ld = new LevelData();
            string output = JsonConvert.SerializeObject(ld, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.LevelDataPath, output);
        }

        public static void AddEntryToConfig(LevelDataEntry lde)
        {
            string json = File.ReadAllText(DestinyUtilityCord.LevelDataPath);
            LevelDataEntries.Clear();
            LevelData jsonObj = JsonConvert.DeserializeObject<LevelData>(json);

            LevelDataEntries.Add(lde);
            LevelData ld = new LevelData();
            string output = JsonConvert.SerializeObject(ld, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.LevelDataPath, output);
        }

        public static void DeleteEntryFromConfig(string BungieName)
        {
            string json = File.ReadAllText(DestinyUtilityCord.LevelDataPath);
            LevelDataEntries.Clear();
            LevelData ld = JsonConvert.DeserializeObject<LevelData>(json);
            for (int i = 0; i < LevelDataEntries.Count; i++)
                if (LevelDataEntries[i].UniqueBungieName.Equals(BungieName))
                    LevelDataEntries.RemoveAt(i);
            string output = JsonConvert.SerializeObject(ld, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.LevelDataPath, output);
        }

        public static bool IsExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(DestinyUtilityCord.LevelDataPath);
            LevelDataEntries.Clear();
            LevelData jsonObj = JsonConvert.DeserializeObject<LevelData>(json);
            foreach (LevelDataEntry ld in LevelDataEntries)
                if (ld.UniqueBungieName.Equals(BungieName))
                    return true;
            return false;
        }

        #endregion
    }
}
