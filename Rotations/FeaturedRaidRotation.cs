using Levante.Configs;
using Levante.Util;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Rotations
{
    public class FeaturedRaidRotation
    {
        // TO-DO
        public static readonly int DeepStoneCryptEncounterCount = 4;
        public static readonly string FilePath = @"Trackers/featuredRaid.json";

        [JsonProperty("FeaturedRaidLinks")]
        public static List<FeaturedRaidLink> FeaturedRaidLinks { get; set; } = new List<FeaturedRaidLink>();

        public class FeaturedRaidLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("Encounter")]
            public Raid FeaturedRaid { get; set; } = Raid.LastWish;
        }

        public static string GetRaidString(Raid Raid)
        {
            switch (Raid)
            {
                case Raid.LastWish: return "Last Wish";
                case Raid.GardenOfSalvation: return "Garden of Salvation";
                case Raid.DeepStoneCrypt: return "Deep Stone Crypt";
                case Raid.VaultOfGlass: return "Vault of Glass";
                default: return "Raid String";
            }
        }

        public static void AddUserTracking(ulong DiscordID, Raid Raid)
        {
            FeaturedRaidLinks.Add(new FeaturedRaidLink() { DiscordID = DiscordID, FeaturedRaid = Raid });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            FeaturedRaidLinks.RemoveAll(x => x.DiscordID == DiscordID);
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static FeaturedRaidLink GetUserTracking(ulong DiscordID, out Raid Raid)
        {
            foreach (var Link in FeaturedRaidLinks)
                if (Link.DiscordID == DiscordID)
                {
                    Raid = Link.FeaturedRaid;
                    return Link;
                }
            Raid = Raid.LastWish;
            return null;
        }

        public static void CreateJSON()
        {
            DeepStoneCryptRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<DeepStoneCryptRotation>(json);
            }
            else
            {
                obj = new DeepStoneCryptRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new DeepStoneCryptRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(Raid Raid)
        {
            Raid iterationRaid = CurrentRotations.FeaturedRaid;
            int WeeksUntil = 0;
            do
            {
                iterationRaid = iterationRaid == Raid.VaultOfGlass ? Raid.LastWish : iterationRaid + 1;
                WeeksUntil++;
            } while (iterationRaid != Raid);
            return CurrentRotations.WeeklyResetTimestamp.AddDays(WeeksUntil * 7); // Because there is no .AddWeeks().
        }
    }

    public enum Raid
    {
        LastWish,
        GardenOfSalvation,
        DeepStoneCrypt,
        VaultOfGlass,
    }
}
