using APIHelper;
using Levante.Configs;
using Levante.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Util
{
    public class InstancedWeapon : Weapon
    {
        private string InstanceID;

        private List<string> Perks = new();

        public InstancedWeapon(long hashCode, string memId, string memType, string instanceId, DataConfig.DiscordIDLink linkedUser = null) : base(hashCode)
        {
            InstanceID = instanceId;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                if (linkedUser != null)
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {linkedUser.AccessToken}");

                var response = client.GetAsync("https://www.bungie.net/platform/Destiny2/" + memType + "/Profile/" + memId + "/Item/" + instanceId + "/?components=305").Result;
                var content = response.Content.ReadAsStringAsync().Result;

                dynamic item = JsonConvert.DeserializeObject(content);

                for (int i = 0; i < item.Response.sockets.data.sockets.Count; i++)
                {
                    var socket = item.Response.sockets.data.sockets[i];
                    if (ManifestHelper.Perks.ContainsKey(socket.plugHash))
                        Perks.Add(ManifestHelper.Perks[socket.plugHash]);
                }
            }
        }

        public string PerksToString()
        {
            // TODO: Test
            // TODO: Use emotes for cleanliness.
            string result = "";
            foreach (var perk in Perks)
                result += $"{perk} ";
            return result;
        }
    }
}
