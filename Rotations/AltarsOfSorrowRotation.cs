using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DestinyUtility.Rotations
{
    public class AltarsOfSorrowRotation
    {
        public static readonly int AltarWeaponCount = 3;
        public static readonly string FilePath = @"Trackers/altarsOfSorrow.json";

        [JsonProperty("AltarsOfSorrowLinks")]
        public static List<AltarsOfSorrowLink> AltarsOfSorrowLinks { get; set; } = new List<AltarsOfSorrowLink>();

        public class AltarsOfSorrowLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("WeaponDrop")]
            public AltarsOfSorrow WeaponDrop { get; set; } = AltarsOfSorrow.Shotgun;
        }

        public static string GetAltarBossString(AltarsOfSorrow Weapon)
        {
            switch (Weapon)
            {
                case AltarsOfSorrow.Shotgun: return "Phogoth, the Untamed";
                case AltarsOfSorrow.Sniper: return "Taniks, the Scarred";
                case AltarsOfSorrow.Rocket: return "Zydron, Gate Lord";
                default: return "Altars of Sorrow";
            }
        }

        public static string GetWeaponNameString(AltarsOfSorrow Weapon)
        {
            switch (Weapon)
            {
                case AltarsOfSorrow.Shotgun: return "Blasphemer";
                case AltarsOfSorrow.Sniper: return "Apostate";
                case AltarsOfSorrow.Rocket: return "Heretic";
                default: return "Altars of Sorrow";
            }
        }

        public static string GetWeaponEmote(AltarsOfSorrow Weapon)
        {
            switch (Weapon)
            {
                case AltarsOfSorrow.Shotgun: return "<:Shotgun:933969813594837093>";
                case AltarsOfSorrow.Sniper: return "<:Sniper:933969813322203198>";
                case AltarsOfSorrow.Rocket: return "<:Rocket:933969813733265488>";
                default: return "Altars of Sorrow Weapon Emote";
            }
        }

        public static void AddUserTracking(ulong DiscordID, AltarsOfSorrow WeaponDrop)
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
        public static AltarsOfSorrowLink GetUserTracking(ulong DiscordID, out AltarsOfSorrow Weapon)
        {
            foreach (var Link in AltarsOfSorrowLinks)
                if (Link.DiscordID == DiscordID)
                {
                    Weapon = Link.WeaponDrop;
                    return Link;
                }
            Weapon = AltarsOfSorrow.Shotgun;
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

        public static void UpdateJSON()
        {
            var obj = new AltarsOfSorrowRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(AltarsOfSorrow Weapon)
        {
            AltarsOfSorrow iterationWeapon = CurrentRotations.AltarWeapon;
            int DaysUntil = 0;
            do
            {
                iterationWeapon = iterationWeapon == AltarsOfSorrow.Rocket ? AltarsOfSorrow.Shotgun : iterationWeapon + 1;
                DaysUntil++;
            } while (iterationWeapon != Weapon);
            return CurrentRotations.DailyResetTimestamp.AddDays(DaysUntil);
        }
    }

    public enum AltarsOfSorrow
    {
        Shotgun, // Blasphemer
        Sniper, // Apostate
        Rocket, // Heretic
    }
}
