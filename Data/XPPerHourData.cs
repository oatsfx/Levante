using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace DestinyUtility.Data
{
    public partial class XPPerHourData
    {
        [JsonProperty("XPPerHourEntries")]
        public static List<XPPerHourEntry> XPPerHourEntries { get; set; } = new List<XPPerHourEntry>();

        public partial class XPPerHourEntry : LeaderboardEntry
        {
            [JsonProperty("XPPerHour")]
            public int XPPerHour { get; set; } = -1;

            [JsonProperty("UniqueBungieName")]
            public string UniqueBungieName { get; set; } = "Guardian#0000";
        }

        public static List<XPPerHourEntry> GetSortedLevelData()
        {
            QuickSort(0, XPPerHourEntries.Count - 1);
            return XPPerHourEntries;
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
            File.WriteAllText(DestinyUtilityCord.XPPerHourDataPath, output);
        }

        public static void AddEntryToConfig(int XPPerHour, string BungieName)
        {
            XPPerHourEntry xphe = new XPPerHourEntry()
            {
                XPPerHour = XPPerHour,
                UniqueBungieName = BungieName
            };
            string json = File.ReadAllText(DestinyUtilityCord.XPPerHourDataPath);
            XPPerHourEntries.Clear();
            XPPerHourData jsonObj = JsonConvert.DeserializeObject<XPPerHourData>(json);

            XPPerHourEntries.Add(xphe);
            XPPerHourData xph = new XPPerHourData();
            string output = JsonConvert.SerializeObject(xph, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.XPPerHourDataPath, output);
        }

        public static void AddEntryToConfig(XPPerHourEntry lde)
        {
            string json = File.ReadAllText(DestinyUtilityCord.XPPerHourDataPath);
            XPPerHourEntries.Clear();
            XPPerHourData jsonObj = JsonConvert.DeserializeObject<XPPerHourData>(json);

            XPPerHourEntries.Add(lde);
            XPPerHourData xph = new XPPerHourData();
            string output = JsonConvert.SerializeObject(xph, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.XPPerHourDataPath, output);
        }

        public static void DeleteEntryFromConfig(string BungieName)
        {
            string json = File.ReadAllText(DestinyUtilityCord.XPPerHourDataPath);
            XPPerHourEntries.Clear();
            XPPerHourData xph = JsonConvert.DeserializeObject<XPPerHourData>(json);
            for (int i = 0; i < XPPerHourEntries.Count; i++)
                if (XPPerHourEntries[i].UniqueBungieName.Equals(BungieName))
                    XPPerHourEntries.RemoveAt(i);
            string output = JsonConvert.SerializeObject(xph, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.XPPerHourDataPath, output);
        }

        public static bool IsExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(DestinyUtilityCord.XPPerHourDataPath);
            XPPerHourEntries.Clear();
            XPPerHourData jsonObj = JsonConvert.DeserializeObject<XPPerHourData>(json);
            foreach (XPPerHourEntry xphe in XPPerHourEntries)
                if (xphe.UniqueBungieName.Equals(BungieName))
                    return true;
            return false;
        }

        public static XPPerHourEntry GetExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(DestinyUtilityCord.XPPerHourDataPath);
            XPPerHourEntries.Clear();
            XPPerHourData jsonObj = JsonConvert.DeserializeObject<XPPerHourData>(json);
            foreach (XPPerHourEntry xphe in XPPerHourEntries)
                if (xphe.UniqueBungieName.Equals(BungieName))
                    return xphe;
            return null;
        }

        #endregion
    }
}
