using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DestinyUtility.Leaderboards
{
    public class PowerLevelData
    {
        public static readonly string FilePath = @"Data/powerLevelData.json";

        [JsonProperty("PowerLevelDataEntries")]
        public static List<PowerLevelDataEntry> PowerLevelDataEntries { get; set; } = new List<PowerLevelDataEntry>();

        public partial class PowerLevelDataEntry : LeaderboardEntry
        {
            [JsonProperty("PowerLevel")]
            public int PowerLevel { get; set; } = -1;

            [JsonProperty("UniqueBungieName")]
            public string UniqueBungieName { get; set; } = "Guardian#0000";
        }

        public static List<PowerLevelDataEntry> GetSortedLevelData()
        {
            QuickSort(0, PowerLevelDataEntries.Count - 1);
            return PowerLevelDataEntries;
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
            int Center = PowerLevelDataEntries[End].PowerLevel;

            int i = Start - 1;
            for (int j = Start; j < End; j++)
            {
                if (PowerLevelDataEntries[j].PowerLevel >= Center)
                {
                    i++;
                    var temp1 = PowerLevelDataEntries[i];
                    PowerLevelDataEntries[i] = PowerLevelDataEntries[j];
                    PowerLevelDataEntries[j] = temp1;
                }
            }

            var temp = PowerLevelDataEntries[i + 1];
            PowerLevelDataEntries[i + 1] = PowerLevelDataEntries[End];
            PowerLevelDataEntries[End] = temp;

            return i + 1;
        }

        #region JSONFileHandling

        public static void UpdateEntriesConfig()
        {
            PowerLevelData pld = new PowerLevelData();
            string output = JsonConvert.SerializeObject(pld, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void AddEntryToConfig(int PowerLevel, string BungieName)
        {
            PowerLevelDataEntry xphe = new PowerLevelDataEntry()
            {
                PowerLevel = PowerLevel,
                UniqueBungieName = BungieName
            };
            string json = File.ReadAllText(FilePath);
            PowerLevelDataEntries.Clear();
            PowerLevelData jsonObj = JsonConvert.DeserializeObject<PowerLevelData>(json);

            PowerLevelDataEntries.Add(xphe);
            PowerLevelData pld = new PowerLevelData();
            string output = JsonConvert.SerializeObject(pld, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void AddEntryToConfig(PowerLevelDataEntry plde)
        {
            string json = File.ReadAllText(FilePath);
            PowerLevelDataEntries.Clear();
            PowerLevelData jsonObj = JsonConvert.DeserializeObject<PowerLevelData>(json);

            PowerLevelDataEntries.Add(plde);
            PowerLevelData pld = new PowerLevelData();
            string output = JsonConvert.SerializeObject(pld, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void DeleteEntryFromConfig(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            PowerLevelDataEntries.Clear();
            PowerLevelData xph = JsonConvert.DeserializeObject<PowerLevelData>(json);
            for (int i = 0; i < PowerLevelDataEntries.Count; i++)
                if (PowerLevelDataEntries[i].UniqueBungieName.Equals(BungieName))
                    PowerLevelDataEntries.RemoveAt(i);
            string output = JsonConvert.SerializeObject(xph, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static bool IsExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            PowerLevelDataEntries.Clear();
            PowerLevelData jsonObj = JsonConvert.DeserializeObject<PowerLevelData>(json);
            foreach (PowerLevelDataEntry plde in PowerLevelDataEntries)
                if (plde.UniqueBungieName.Equals(BungieName))
                    return true;
            return false;
        }

        public static PowerLevelDataEntry GetExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            PowerLevelDataEntries.Clear();
            PowerLevelData jsonObj = JsonConvert.DeserializeObject<PowerLevelData>(json);
            foreach (PowerLevelDataEntry plde in PowerLevelDataEntries)
                if (plde.UniqueBungieName.Equals(BungieName))
                    return plde;
            return null;
        }

        #endregion
    }
}
