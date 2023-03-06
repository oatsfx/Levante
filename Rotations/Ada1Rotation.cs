using Levante.Configs;
using Levante.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Levante.Rotations.LastWishRotation;

namespace Levante.Rotations
{
    public class Ada1Rotation
    {
        public static readonly string FilePath = @"Trackers/ada1.json";

        [JsonProperty("Ada1Links")]
        public static List<Ada1ModLink> Ada1Links { get; set; } = new List<Ada1ModLink>();

        public class Ada1ModLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("Hash")]
            public long Hash { get; set; } = 0;
        }

        public static void AddUserTracking(ulong DiscordID, long ModHash)
        {
            Ada1Links.Add(new Ada1ModLink() { DiscordID = DiscordID, Hash = ModHash });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            Ada1Links.Remove(GetUserTracking(DiscordID, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static Ada1ModLink GetUserTracking(ulong DiscordID, out long Hash)
        {
            foreach (var Link in Ada1Links)
                if (Link.DiscordID == DiscordID)
                {
                    Hash = Link.Hash;
                    return Link;
                }
            Hash = 0;
            return null;
        }

        public static void GetAda1Inventory()
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

                    response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + devLinked.BungieMembershipType + "/Profile/" + devLinked.BungieMembershipID + "/Character/" + charId + "/Vendors/350061650/?components=400,402").Result;
                    content = response.Content.ReadAsStringAsync().Result;
                    item = JsonConvert.DeserializeObject(content);

                    var token = JToken.Parse($"{item.Response.sales.data}");
                    var jObject = token.Value<JObject>();
                    List<string> keys = jObject.Properties().Select(p => p.Name).ToList();

                    CurrentRotations.Ada1Items.Clear();
                    for (int i = 0; i < token.Count(); i++)
                    {
                        long hash = long.Parse($"{item.Response.sales.data[keys[i]].itemHash}");
                        if (ManifestHelper.Ada1Items.ContainsKey(hash))
                        {
                            CurrentRotations.Ada1Items.Add(hash, ManifestHelper.Ada1Items[hash]);
                        }
                    }
                }
            }
            catch (Exception x)
            {
                Log.Warning("[{Type}] Ada-1 Inventory Unavailable.", "Rotations");
            }
        }

        public static void CreateJSON()
        {
            Ada1Rotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<Ada1Rotation>(json);
            }
            else
            {
                obj = new Ada1Rotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new Ada1Rotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }
    }
}
