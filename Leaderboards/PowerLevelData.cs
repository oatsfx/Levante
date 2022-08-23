using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Levante.Leaderboards
{
    public class PowerLevelData
    {
        public static readonly string FilePathS15 = @"Data/S15/powerLevelData.json";
        public static readonly string FilePathS16 = @"Data/S16/powerLevelData.json";
        public static readonly string FilePathS17= @"Data/S17/powerLevelData.json";
        public static readonly string FilePath = @"Data/S18/powerLevelData.json";

        [JsonProperty("PowerLevelDataEntries")]
        public List<PowerLevelDataEntry> PowerLevelDataEntries { get; set; } = new List<PowerLevelDataEntry>();

        public partial class PowerLevelDataEntry : LeaderboardEntry
        {
            [JsonProperty("PowerLevel")]
            public int PowerLevel { get; set; } = -1;

            [JsonProperty("UniqueBungieName")]
            public string UniqueBungieName { get; set; } = "Guardian#0000";
        }

        public List<PowerLevelDataEntry> GetSortedLevelData()
        {
            QuickSort(0, PowerLevelDataEntries.Count - 1);
            return PowerLevelDataEntries;
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

        public void UpdateEntriesConfig()
        {
            string output = JsonConvert.SerializeObject(this, Formatting.Indented);
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
            PowerLevelData pld = JsonConvert.DeserializeObject<PowerLevelData>(json);

            pld.PowerLevelDataEntries.Add(xphe);
            string output = JsonConvert.SerializeObject(pld, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void AddEntryToConfig(PowerLevelDataEntry plde)
        {
            string json = File.ReadAllText(FilePath);
            PowerLevelData pld = JsonConvert.DeserializeObject<PowerLevelData>(json);

            pld.PowerLevelDataEntries.Add(plde);
            string output = JsonConvert.SerializeObject(pld, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static void DeleteEntryFromConfig(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            PowerLevelData pld = JsonConvert.DeserializeObject<PowerLevelData>(json);
            for (int i = 0; i < pld.PowerLevelDataEntries.Count; i++)
                if (pld.PowerLevelDataEntries[i].UniqueBungieName.Equals(BungieName))
                    pld.PowerLevelDataEntries.RemoveAt(i);
            string output = JsonConvert.SerializeObject(pld, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static bool IsExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            PowerLevelData pld = JsonConvert.DeserializeObject<PowerLevelData>(json);
            foreach (PowerLevelDataEntry plde in pld.PowerLevelDataEntries)
                if (plde.UniqueBungieName.Equals(BungieName))
                    return true;
            return false;
        }

        public static PowerLevelDataEntry GetExistingLinkedEntry(string BungieName)
        {
            string json = File.ReadAllText(FilePath);
            PowerLevelData pld = JsonConvert.DeserializeObject<PowerLevelData>(json);
            foreach (PowerLevelDataEntry plde in pld.PowerLevelDataEntries)
                if (plde.UniqueBungieName.Equals(BungieName))
                    return plde;
            return null;
        }

        #endregion
    }
}
