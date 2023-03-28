using Levante.Configs;
using Levante.Util;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Levante.Helpers;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net.Http;
using Serilog;

namespace Levante.Rotations
{
    public class NightfallRotation
    {
        public static readonly string FilePath = @"Trackers/nightfall.json";
        public static readonly string RotationFilePath = @"Rotations/nfWeapons.json";

        [JsonProperty("NightfallLinks")]
        public static List<NightfallLink> NightfallLinks { get; set; } = new List<NightfallLink>();

        public static List<string> Nightfalls { get; set; } = new();
        public static List<NightfallWeapon> NightfallWeapons { get; set; } = new();

        public class NightfallLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("NightfallStrike")]
            public int? Nightfall { get; set; } = 0;

            [JsonProperty("WeaponDrop")]
            public int? WeaponDrop { get; set; } = 0;
        }

        public static void GetCurrentNightfall()
        {
            try
            {
                var devLinked = DataConfig.DiscordIDLinks.FirstOrDefault(x => x.DiscordID == BotConfig.BotDevDiscordIDs[0]);
                devLinked = DataConfig.RefreshCode(devLinked);
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {devLinked.AccessToken}");

                    var response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/" + devLinked.BungieMembershipType + "/Profile/" + devLinked.BungieMembershipID + "?components=100,200").Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    dynamic item = JsonConvert.DeserializeObject(content);

                    string charId = $"{item.Response.profile.data.characterIds[0]}";

                    response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + devLinked.BungieMembershipType + "/Profile/" + devLinked.BungieMembershipID + "/Character/" + charId + "/?components=204").Result;
                    content = response.Content.ReadAsStringAsync().Result;
                    item = JsonConvert.DeserializeObject(content);

                    var availActivities = item.Response.activities.data.availableActivities;

                    for (int i = 0; i < availActivities.Count; i++)
                    {
                        if (ManifestHelper.Nightfalls.ContainsKey((long)availActivities[i].activityHash))
                        {
                            CurrentRotations.Nightfall = Nightfalls.IndexOf(ManifestHelper.Nightfalls[(long)availActivities[i].activityHash]);
                            Log.Debug("Nightfall is {Nightfall}.", ManifestHelper.Nightfalls[(long)availActivities[i].activityHash]);
                        }
                    }
                }
            }
            catch (Exception x)
            {
                Log.Warning("[{Type}] Nightfall Activity Unavailable.", "Rotations");
            }
        }

        public static void AddUserTracking(ulong DiscordID, int? Nightfall, int? WeaponDrop)
        {
            NightfallLinks.Add(new NightfallLink() { DiscordID = DiscordID, Nightfall = Nightfall, WeaponDrop = WeaponDrop });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            NightfallLinks.Remove(GetUserTracking(DiscordID, out _, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static NightfallLink GetUserTracking(ulong DiscordID, out int? Nightfall, out int? WeaponDrop)
        {
            foreach (var Link in NightfallLinks)
                if (Link.DiscordID == DiscordID)
                {
                    Nightfall = Link.Nightfall;
                    WeaponDrop = Link.WeaponDrop;
                    return Link;
                }
            Nightfall = null;
            WeaponDrop = null;
            return null;
        }

        public static void CreateJSON()
        {
            NightfallRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<NightfallRotation>(json);
            }
            else
            {
                obj = new NightfallRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }

            if (File.Exists(RotationFilePath))
            {
                string json = File.ReadAllText(RotationFilePath);
                NightfallWeapons = JsonConvert.DeserializeObject<List<NightfallWeapon>>(json);
            }
            else
            {
                File.WriteAllText(RotationFilePath, JsonConvert.SerializeObject(NightfallWeapons, Formatting.Indented));
                Console.WriteLine($"No {RotationFilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new NightfallRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(int? NightfallStrike, int? WeaponDrop)
        {
            int iterationWeapon = CurrentRotations.NightfallWeaponDrop;
            int iterationStrike = CurrentRotations.Nightfall;
            int WeeksUntil = 0;
            // This logic only works if the position in the enums match up with strike drops.
            if (Nightfalls.Count == NightfallWeapons.Count && NightfallStrike != null && WeaponDrop != null)
                if ((int)NightfallStrike != (int)WeaponDrop)
                    return new DateTime();

            if (NightfallStrike == null && WeaponDrop != null)
            {
                do
                {
                    iterationWeapon = iterationWeapon == NightfallWeapons.Count - 1 ? 0 : iterationWeapon + 1;
                    WeeksUntil++;
                } while (iterationWeapon != WeaponDrop);
            }
            else if (WeaponDrop == null && NightfallStrike != null)
            {
                do
                {
                    iterationStrike = iterationStrike == Nightfalls.Count - 1 ? 0 : iterationStrike + 1;
                    WeeksUntil++;
                } while (iterationStrike != NightfallStrike);
            }
            else if (WeaponDrop != null && NightfallStrike != null)
            {
                do
                {
                    iterationWeapon = iterationWeapon == NightfallWeapons.Count - 1 ? 0 : iterationWeapon + 1;
                    iterationStrike = iterationStrike == Nightfalls.Count - 1 ? 0 : iterationStrike + 1;
                    WeeksUntil++;
                } while (iterationStrike != NightfallStrike && iterationWeapon != WeaponDrop);
            }
            return CurrentRotations.WeeklyResetTimestamp.AddDays(WeeksUntil * 7); // Because there is no .AddWeeks().
        }
    }

    public class NightfallWeapon
    {
        [JsonProperty("Hash")]
        public long Hash;
        [JsonProperty("AdeptHash")]
        public long AdeptHash;
        public string Name;
        public string Emote;
    }
}
