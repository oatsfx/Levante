using Levante.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Levante.Rotations
{
    public class WellspringRotation
    {
        public static readonly int WellspringCount = 4;
        public static readonly string FilePath = @"Trackers/wellspring.json";

        [JsonProperty("WellspringLinks")]
        public static List<WellspringLink> WellspringLinks { get; set; } = new List<WellspringLink>();

        public class WellspringLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("WellspringBoss")]
            public Wellspring WellspringBoss { get; set; } = Wellspring.Golmag;
        }

        public static string GetWellspringBossString(Wellspring Wellspring)
        {
            switch (Wellspring)
            {
                case Wellspring.Golmag: return "Golmag, Warden of the Spring";
                case Wellspring.Vezuul: return "Vezuul, Lightflayer";
                case Wellspring.Borgong: return "Bor'gong, Warden of the Spring";
                case Wellspring.Zeerik: return "Zeerik, Lightflayer";
                default: return "The Wellspring";
            }
        }

        public static string GetWellspringTypeString(Wellspring Wellspring)
        {
            switch (Wellspring)
            {
                case Wellspring.Golmag: return "Attack";
                case Wellspring.Vezuul: return "Defend";
                case Wellspring.Borgong: return "Attack";
                case Wellspring.Zeerik: return "Defend";
                default: return "The Wellspring Type";
            }
        }

        public static string GetWeaponNameString(Wellspring Wellspring)
        {
            switch (Wellspring)
            {
                case Wellspring.Golmag: return "Come to Pass";
                case Wellspring.Vezuul: return "Tarnation";
                case Wellspring.Borgong: return "Fel Taradiddle";
                case Wellspring.Zeerik: return "Father's Sins";
                default: return "The Wellspring Weapon";
            }
        }

        public static string GetWeaponTypeString(Wellspring Wellspring)
        {
            switch (Wellspring)
            {
                case Wellspring.Golmag: return "Auto Rifle";
                case Wellspring.Vezuul: return "Grenade Launcher";
                case Wellspring.Borgong: return "Bow";
                case Wellspring.Zeerik: return "Sniper";
                default: return "The Wellspring Weapon Type";
            }
        }

        public static string GetWeaponEmote(Wellspring Wellspring)
        {
            switch (Wellspring)
            {
                case Wellspring.Golmag: return $"{DestinyEmote.AutoRifle}";
                case Wellspring.Vezuul: return $"{DestinyEmote.HeavyGrenadeLauncher}";
                case Wellspring.Borgong: return $"{DestinyEmote.Bow}";
                case Wellspring.Zeerik: return $"{DestinyEmote.SniperRifle}";
                default: return "The Wellspring Weapon Emote";
            }
        }

        public static void AddUserTracking(ulong DiscordID, Wellspring Wellspring)
        {
            WellspringLinks.Add(new WellspringLink() { DiscordID = DiscordID, WellspringBoss = Wellspring });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            WellspringLinks.Remove(GetUserTracking(DiscordID, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static WellspringLink GetUserTracking(ulong DiscordID, out Wellspring Wellspring)
        {
            foreach (var Link in WellspringLinks)
                if (Link.DiscordID == DiscordID)
                {
                    Wellspring = Link.WellspringBoss;
                    return Link;
                }
            Wellspring = Wellspring.Golmag;
            return null;
        }

        public static void CreateJSON()
        {
            WellspringRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<WellspringRotation>(json);
            }
            else
            {
                obj = new WellspringRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new WellspringRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(Wellspring Wellspring)
        {
            Wellspring iterationWellspring = CurrentRotations.Wellspring;
            int DaysUntil = 0;
            do
            {
                iterationWellspring = iterationWellspring == Wellspring.Zeerik ? Wellspring.Golmag : iterationWellspring + 1;
                DaysUntil++;
            } while (iterationWellspring != Wellspring);
            return CurrentRotations.DailyResetTimestamp.AddDays(DaysUntil);
        }
    }

    public enum Wellspring
    {
        Golmag, // Attack, Come to Pass (Auto Rifle)
        Vezuul, // Defense, Tarnation (Heavy Grenade Launcher)
        Borgong, // Attack, Fel Taradiddle (Bow)
        Zeerik, // Defense, Father's Sins (Sniper Rifle)
    }
}
