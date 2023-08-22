using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Leaderboards
{
    public class MostXPLoggingTimeData
    {
        public static readonly string FilePathS15 = @"Data/S15/mostThrallwayTimeData.json";
        public static readonly string FilePathS16 = @"Data/S16/mostXPLoggingTimeData.json";
        public static readonly string FilePathS17 = @"Data/S17/mostXPLoggingTimeData.json";
        public static readonly string FilePathS18 = @"Data/S18/mostXPLoggingTimeData.json";
        public static readonly string FilePathS19 = @"Data/S19/mostXPLoggingTimeData.json";
        public static readonly string FilePathS20 = @"Data/S20/mostXPLoggingTimeData.json";
        public static readonly string FilePathS21 = @"Data/S21/mostXPLoggingTimeData.json";
        public static readonly string FilePath = @"Data/S22/mostXPLoggingTimeData.json";

        [JsonProperty("MostXPLogTimeEntries")]
        public List<MostXPLogTimeEntry> MostXPLogTimeEntries { get; set; } = new();

        [JsonProperty("MostThrallwayTimeEntries")]
        public List<MostXPLogTimeEntry> MostThrallwayTimeEntries { set { MostXPLogTimeEntries = value; } }

        public partial class MostXPLogTimeEntry : LeaderboardEntry
        {
            [JsonProperty("Time")]
            public TimeSpan Time { get; set; } = TimeSpan.MinValue;

            [JsonProperty("UniqueBungieName")]
            public string UniqueBungieName { get; set; } = "Guardian#0000";
        }

        public List<MostXPLogTimeEntry> GetSortedLevelData()
        {
            QuickSort(0, MostXPLogTimeEntries.Count - 1);
            return MostXPLogTimeEntries;
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
            TimeSpan Center = MostXPLogTimeEntries[End].Time;

            int i = Start - 1;
            for (int j = Start; j < End; j++)
            {
                if (MostXPLogTimeEntries[j].Time >= Center)
                {
                    i++;
                    var temp1 = MostXPLogTimeEntries[i];
                    MostXPLogTimeEntries[i] = MostXPLogTimeEntries[j];
                    MostXPLogTimeEntries[j] = temp1;
                }
            }

            var temp = MostXPLogTimeEntries[i + 1];
            MostXPLogTimeEntries[i + 1] = MostXPLogTimeEntries[End];
            MostXPLogTimeEntries[End] = temp;

            return i + 1;
        }

        #region JSONFileHandling

        public static void UpdateEntriesConfig()
        {
            MostXPLoggingTimeData mttd = new MostXPLoggingTimeData();
            string output = JsonConvert.SerializeObject(mttd, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void AddEntryToConfig(TimeSpan Time, string BungieName)
        {
            MostXPLogTimeEntry mtt = new MostXPLogTimeEntry()
            {
                Time = Time,
                UniqueBungieName = BungieName
            };
            string json = File.ReadAllText(FilePath);
            MostXPLoggingTimeData mttd = JsonConvert.DeserializeObject<MostXPLoggingTimeData>(json);

            mttd.MostXPLogTimeEntries.Add(mtt);
            string output = JsonConvert.SerializeObject(mttd, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void AddEntryToConfig(MostXPLogTimeEntry lde)
        {
            string json = File.ReadAllText(FilePath);
            MostXPLoggingTimeData mttd = JsonConvert.DeserializeObject<MostXPLoggingTimeData>(json);

            mttd.MostXPLogTimeEntries.Add(lde);
            string output = JsonConvert.SerializeObject(mttd, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void DeleteEntryFromConfig(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            MostXPLoggingTimeData mttd = JsonConvert.DeserializeObject<MostXPLoggingTimeData>(json);
            for (int i = 0; i < mttd.MostXPLogTimeEntries.Count; i++)
                if (mttd.MostXPLogTimeEntries[i].UniqueBungieName.Equals(BungieName))
                    mttd.MostXPLogTimeEntries.RemoveAt(i);
            string output = JsonConvert.SerializeObject(mttd, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static bool IsExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            MostXPLoggingTimeData mttd = JsonConvert.DeserializeObject<MostXPLoggingTimeData>(json);
            foreach (MostXPLogTimeEntry mtt in mttd.MostXPLogTimeEntries)
                if (mtt.UniqueBungieName.Equals(BungieName))
                    return true;
            return false;
        }

        public static MostXPLogTimeEntry GetExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            MostXPLoggingTimeData mttd = JsonConvert.DeserializeObject<MostXPLoggingTimeData>(json);
            foreach (MostXPLogTimeEntry mtt in mttd.MostXPLogTimeEntries)
                if (mtt.UniqueBungieName.Equals(BungieName))
                    return mtt;
            return null;
        }

        #endregion
    }
}
