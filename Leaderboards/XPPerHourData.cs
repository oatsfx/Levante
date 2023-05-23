using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Levante.Leaderboards
{
    public class XPPerHourData
    {
        public static readonly string FilePathS15 = @"Data/S15/xpPerHourData.json";
        public static readonly string FilePathS16 = @"Data/S16/xpPerHourData.json";
        public static readonly string FilePathS17 = @"Data/S17/xpPerHourData.json";
        public static readonly string FilePathS18 = @"Data/S18/xpPerHourData.json";
        public static readonly string FilePathS19 = @"Data/S19/xpPerHourData.json";
        public static readonly string FilePathS20 = @"Data/S20/xpPerHourData.json";
        public static readonly string FilePath = @"Data/S21/xpPerHourData.json";

        [JsonProperty("XPPerHourEntries")]
        public List<XPPerHourEntry> XPPerHourEntries { get; set; } = new();

        public partial class XPPerHourEntry : LeaderboardEntry
        {
            [JsonProperty("XPPerHour")]
            public int XPPerHour { get; set; } = -1;

            [JsonProperty("UniqueBungieName")]
            public string UniqueBungieName { get; set; } = "Guardian#0000";
        }

        public List<XPPerHourEntry> GetSortedLevelData()
        {
            QuickSort(0, XPPerHourEntries.Count - 1);
            return XPPerHourEntries;
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
            int Center = XPPerHourEntries[End].XPPerHour;

            int i = Start - 1;
            for (int j = Start; j < End; j++)
            {
                if (XPPerHourEntries[j].XPPerHour >= Center)
                {
                    i++;
                    var temp1 = XPPerHourEntries[i];
                    XPPerHourEntries[i] = XPPerHourEntries[j];
                    XPPerHourEntries[j] = temp1;
                }
            }

            var temp = XPPerHourEntries[i + 1];
            XPPerHourEntries[i + 1] = XPPerHourEntries[End];
            XPPerHourEntries[End] = temp;

            return i + 1;
        }

        #region JSONFileHandling

        public static void UpdateEntriesConfig()
        {
            XPPerHourData xph = new XPPerHourData();
            string output = JsonConvert.SerializeObject(xph, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void AddEntryToConfig(int XPPerHour, string BungieName)
        {
            XPPerHourEntry xphe = new XPPerHourEntry()
            {
                XPPerHour = XPPerHour,
                UniqueBungieName = BungieName

            };
            string json = File.ReadAllText(FilePath);
            XPPerHourData xph = JsonConvert.DeserializeObject<XPPerHourData>(json);

            xph.XPPerHourEntries.Add(xphe);
            string output = JsonConvert.SerializeObject(xph, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void AddEntryToConfig(XPPerHourEntry xphe)
        {
            string json = File.ReadAllText(FilePath);
            XPPerHourData xph = JsonConvert.DeserializeObject<XPPerHourData>(json);

            xph.XPPerHourEntries.Add(xphe);
            string output = JsonConvert.SerializeObject(xph, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void DeleteEntryFromConfig(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            XPPerHourData xph = JsonConvert.DeserializeObject<XPPerHourData>(json);
            for (int i = 0; i < xph.XPPerHourEntries.Count; i++)
                if (xph.XPPerHourEntries[i].UniqueBungieName.Equals(BungieName))
                    xph.XPPerHourEntries.RemoveAt(i);
            string output = JsonConvert.SerializeObject(xph, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static bool IsExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            XPPerHourData xph = JsonConvert.DeserializeObject<XPPerHourData>(json);
            foreach (XPPerHourEntry xphe in xph.XPPerHourEntries)
                if (xphe.UniqueBungieName.Equals(BungieName))
                    return true;
            return false;
        }

        public static XPPerHourEntry GetExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            XPPerHourData xph = JsonConvert.DeserializeObject<XPPerHourData>(json);
            foreach (XPPerHourEntry xphe in xph.XPPerHourEntries)
                if (xphe.UniqueBungieName.Equals(BungieName))
                    return xphe;
            return null;
        }

        #endregion
    }
}
