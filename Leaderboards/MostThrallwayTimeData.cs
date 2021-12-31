using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace DestinyUtility.Leaderboards
{
    public class MostThrallwayTimeData
    {
        public static readonly string FilePath = @"Data/mostThrallwayTimeData.json";

        [JsonProperty("MostThrallwayTimeEntries")]
        public static List<MostThrallwayTimeEntry> MostThrallwayTimeEntries { get; set; } = new List<MostThrallwayTimeEntry>();

        public partial class MostThrallwayTimeEntry : LeaderboardEntry
        {
            [JsonProperty("Time")]
            public TimeSpan Time { get; set; } = TimeSpan.MinValue;

            [JsonProperty("UniqueBungieName")]
            public string UniqueBungieName { get; set; } = "Guardian#0000";
        }

        public static List<MostThrallwayTimeEntry> GetSortedLevelData()
        {
            QuickSort(0, MostThrallwayTimeEntries.Count - 1);
            return MostThrallwayTimeEntries;
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
            TimeSpan Center = MostThrallwayTimeEntries[End].Time;

            int i = Start - 1;
            for (int j = Start; j < End; j++)
            {
                if (MostThrallwayTimeEntries[j].Time >= Center)
                {
                    i++;
                    var temp1 = MostThrallwayTimeEntries[i];
                    MostThrallwayTimeEntries[i] = MostThrallwayTimeEntries[j];
                    MostThrallwayTimeEntries[j] = temp1;
                }
            }

            var temp = MostThrallwayTimeEntries[i + 1];
            MostThrallwayTimeEntries[i + 1] = MostThrallwayTimeEntries[End];
            MostThrallwayTimeEntries[End] = temp;

            return i + 1;
        }

        #region JSONFileHandling

        public static void UpdateEntriesConfig()
        {
            MostThrallwayTimeData mttd = new MostThrallwayTimeData();
            string output = JsonConvert.SerializeObject(mttd, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void AddEntryToConfig(TimeSpan Time, string BungieName)
        {
            MostThrallwayTimeEntry mtt = new MostThrallwayTimeEntry()
            {
                Time = Time,
                UniqueBungieName = BungieName
            };
            string json = File.ReadAllText(FilePath);
            MostThrallwayTimeEntries.Clear();
            MostThrallwayTimeData jsonObj = JsonConvert.DeserializeObject<MostThrallwayTimeData>(json);

            MostThrallwayTimeEntries.Add(mtt);
            MostThrallwayTimeData mttd = new MostThrallwayTimeData();
            string output = JsonConvert.SerializeObject(mttd, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void AddEntryToConfig(MostThrallwayTimeEntry lde)
        {
            string json = File.ReadAllText(FilePath);
            MostThrallwayTimeEntries.Clear();
            MostThrallwayTimeData jsonObj = JsonConvert.DeserializeObject<MostThrallwayTimeData>(json);

            MostThrallwayTimeEntries.Add(lde);
            MostThrallwayTimeData mttd = new MostThrallwayTimeData();
            string output = JsonConvert.SerializeObject(mttd, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void DeleteEntryFromConfig(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            MostThrallwayTimeEntries.Clear();
            MostThrallwayTimeData mttd = JsonConvert.DeserializeObject<MostThrallwayTimeData>(json);
            for (int i = 0; i < MostThrallwayTimeEntries.Count; i++)
                if (MostThrallwayTimeEntries[i].UniqueBungieName.Equals(BungieName))
                    MostThrallwayTimeEntries.RemoveAt(i);
            string output = JsonConvert.SerializeObject(mttd, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static bool IsExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            MostThrallwayTimeEntries.Clear();
            MostThrallwayTimeData jsonObj = JsonConvert.DeserializeObject<MostThrallwayTimeData>(json);
            foreach (MostThrallwayTimeEntry mtt in MostThrallwayTimeEntries)
                if (mtt.UniqueBungieName.Equals(BungieName))
                    return true;
            return false;
        }

        public static MostThrallwayTimeEntry GetExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            MostThrallwayTimeEntries.Clear();
            MostThrallwayTimeData jsonObj = JsonConvert.DeserializeObject<MostThrallwayTimeData>(json);
            foreach (MostThrallwayTimeEntry mtt in MostThrallwayTimeEntries)
                if (mtt.UniqueBungieName.Equals(BungieName))
                    return mtt;
            return null;
        }

        #endregion
    }
}
