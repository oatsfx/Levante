using Levante.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Rotations
{
    public class AltarsOfSorrowRotation
    {
        public static readonly string FilePath = @"Trackers/altarsOfSorrow.json";
        public static readonly string RotationsFilePath = @"Rotations/altarsOfSorrow.json";

        public static List<AltarsOfSorrow> AltarsOfSorrows = new();

        [JsonProperty("AltarsOfSorrowLinks")]
        public static List<AltarsOfSorrowLink> AltarsOfSorrowLinks { get; set; } = new List<AltarsOfSorrowLink>();

        public class AltarsOfSorrowLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("WeaponDrop")]
            public int WeaponDrop { get; set; } = 0;
        }

        public static void AddUserTracking(ulong DiscordID, int WeaponDrop)
        {
            AltarsOfSorrowLinks.Add(new AltarsOfSorrowLink() { DiscordID = DiscordID, WeaponDrop = WeaponDrop });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            AltarsOfSorrowLinks.Remove(GetUserTracking(DiscordID, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static AltarsOfSorrowLink GetUserTracking(ulong DiscordID, out int Weapon)
        {
            foreach (var Link in AltarsOfSorrowLinks)
                if (Link.DiscordID == DiscordID)
                {
                    Weapon = Link.WeaponDrop;
                    return Link;
                }
            Weapon = -1;
            return null;
        }

        public static void CreateJSON()
        {
            AltarsOfSorrowRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<AltarsOfSorrowRotation>(json);
            }
            else
            {
                obj = new AltarsOfSorrowRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void GetRotationJSON()
        {
            if (File.Exists(RotationsFilePath))
            {
                string json = File.ReadAllText(RotationsFilePath);
                AltarsOfSorrows = JsonConvert.DeserializeObject<List<AltarsOfSorrow>>(json);
            }
            else
            {
                File.WriteAllText(RotationsFilePath, JsonConvert.SerializeObject(AltarsOfSorrows, Formatting.Indented));
                Console.WriteLine($"No {RotationsFilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new AltarsOfSorrowRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(int Weapon)
        {
            int iterationWeapon = CurrentRotations.AltarWeapon;
            int DaysUntil = 0;
            do
            {
                iterationWeapon = iterationWeapon == AltarsOfSorrows.Count - 1 ? 0 : iterationWeapon + 1;
                DaysUntil++;
            } while (iterationWeapon != Weapon);
            return CurrentRotations.DailyResetTimestamp.AddDays(DaysUntil);
        }
    }

    public class AltarsOfSorrow
    {
        [JsonProperty("Boss")]
        public string Boss;
        [JsonProperty("Weapon")]
        public string Weapon;
        [JsonProperty("WeaponType")]
        public string WeaponType;
        [JsonProperty("WeaponEmote")]
        public string WeaponEmote;
    }

    /*public enum AltarsOfSorrow
    {
        Shotgun, // Blasphemer
        Sniper, // Apostate
        Rocket, // Heretic
    }*/
}
