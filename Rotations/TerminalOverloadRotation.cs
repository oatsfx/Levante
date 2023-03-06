using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Levante.Rotations.AltarsOfSorrowRotation;
using static Levante.Rotations.TerminalOverloadRotation;

namespace Levante.Rotations
{
    public class TerminalOverloadRotation
    {
        public static readonly string FilePath = @"Trackers/terminalOverload.json";
        public static readonly string RotationsFilePath = @"Rotations/terminalOverload.json";

        public static List<TerminalOverload> TerminalOverloads = new();

        [JsonProperty("TerminalOverloadLinks")]
        public static List<TerminalOverloadLink> TerminalOverloadLinks { get; set; } = new();

        public class TerminalOverloadLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("Location")]
            public int Location { get; set; } = 0;
        }

        public static void AddUserTracking(ulong DiscordID, int Location)
        {
            TerminalOverloadLinks.Add(new TerminalOverloadLink() { DiscordID = DiscordID, Location = Location });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            TerminalOverloadLinks.Remove(GetUserTracking(DiscordID, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static TerminalOverloadLink GetUserTracking(ulong DiscordID, out int Location)
        {
            foreach (var Link in TerminalOverloadLinks)
                if (Link.DiscordID == DiscordID)
                {
                    Location = Link.Location;
                    return Link;
                }
            Location = -1;
            return null;
        }

        public static void CreateJSON()
        {
            TerminalOverloadRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<TerminalOverloadRotation>(json);
            }
            else
            {
                obj = new TerminalOverloadRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void GetRotationJSON()
        {
            if (File.Exists(RotationsFilePath))
            {
                string json = File.ReadAllText(RotationsFilePath);
                TerminalOverloads = JsonConvert.DeserializeObject<List<TerminalOverload>>(json);
            }
            else
            {
                File.WriteAllText(RotationsFilePath, JsonConvert.SerializeObject(TerminalOverloads, Formatting.Indented));
                Console.WriteLine($"No {RotationsFilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new TerminalOverloadRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(int Location)
        {
            int iterationLoc = CurrentRotations.TerminalOverload;
            int DaysUntil = 0;
            do
            {
                iterationLoc = iterationLoc == TerminalOverloads.Count - 1 ? 0 : iterationLoc + 1;
                DaysUntil++;
            } while (iterationLoc != Location);
            return CurrentRotations.DailyResetTimestamp.AddDays(DaysUntil);
        }
    }

    public class TerminalOverload
    {
        [JsonProperty("Location")]
        public string Location;
        [JsonProperty("Weapon")]
        public string Weapon;
        [JsonProperty("WeaponType")]
        public string WeaponType;
        [JsonProperty("WeaponEmote")]
        public string WeaponEmote;
    }
}
