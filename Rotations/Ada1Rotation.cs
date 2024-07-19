using APIHelper;
using Levante.Configs;
using Levante.Helpers;
using Levante.Rotations.Abstracts;
using Levante.Rotations.Interfaces;
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

namespace Levante.Rotations
{
    public class Ada1Rotation : Rotation<Ada1Link>
    {
        public Ada1Rotation()
        {
            FilePath = @"Trackers/ada1.json";

            IsDaily = true;

            GetTrackerJSON();
        }

        public void GetAda1Inventory()
        {
            try
            {
                var devLinked = DataConfig.DiscordIDLinks.FirstOrDefault(x => x.DiscordID == AppConfig.BotDevDiscordIDs[0]);
                devLinked = DataConfig.RefreshCode(devLinked);
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", AppConfig.Credentials.BungieApiKey);
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

                    CurrentRotations.Actives.Ada1Items.Clear();
                    for (int i = 0; i < token.Count(); i++)
                    {
                        long hash = long.Parse($"{item.Response.sales.data[keys[i]].itemHash}");
                        if (ManifestHelper.Ada1Items.ContainsKey(hash))
                        {
                            CurrentRotations.Actives.Ada1Items.Add(new Ada1 { ItemName = ManifestHelper.Ada1Items[hash], Hash = hash });
                        }
                    }
                }
            }
            catch (Exception x)
            {
                Log.Warning("[{Type}] Ada-1 Inventory Unavailable.", "Rotations");
            }
        }

        public override bool IsTrackerInRotation(Ada1Link Tracker) => CurrentRotations.Actives.Ada1Items.Exists(x => x.Hash == Tracker.Hash);

        public override string ToString() => "Ada-1 Vendor Sales";
    }

    public class Ada1
    {
        [JsonProperty("ItemName")]
        public string ItemName;

        [JsonProperty("Hash")]
        public long Hash;

        public override string ToString() => $"{ManifestHelper.Ada1Items[Hash]}";
    }

    public class Ada1Link : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Hash")]
        public long Hash { get; set; } = 0;

        public override string ToString() => $"{ManifestHelper.Ada1Items[Hash]}";
    }
}
