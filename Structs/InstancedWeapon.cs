using Levante.Configs;
using Levante.Helpers;
using Levante.Services;
using Levante.Util;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Levante.Structs
{
    public class InstancedWeapon : Weapon
    {
        private string InstanceID;

        private List<string> Perks = new();

        public InstancedWeapon(long hashCode, string memId, string memType, string instanceId, User linkedUser = null) : base(hashCode)
        {
            InstanceID = instanceId;

            var token = linkedUser?.BungieCredentials.AccessToken;
            var content = Task.Run(() => BungieApi.Query($"/Platform/Destiny2/{memType}/Profile/{memId}/Item/{instanceId}/?components=305", token)).Result;

            dynamic item = JsonConvert.DeserializeObject(content);

            if (item.Response.sockets.data != null && item.Response.sockets.data.sockets.Count > 0)
            {
                for (int i = 0; i < item.Response.sockets.data.sockets.Count; i++)
                {
                    var socket = item.Response.sockets.data.sockets[i];
                    if (socket.plugHash == null) continue;
                    long plugHash = (long)socket.plugHash;
                    if (ManifestHelper.Perks.ContainsKey(plugHash))
                        Perks.Add(ManifestHelper.Perks[plugHash]);
                    else if (ManifestHelper.EnhancedPerks.ContainsKey(plugHash))
                        Perks.Add(ManifestHelper.EnhancedPerks[plugHash]);
                }
            }
        }

        public string PerksToString()
        {
            string result = "";
            foreach (var perk in Perks)
            {
                var formattedPerk = perk.Replace(" ", "").Replace("-", "").Replace("'", "");
                if (Task.Run(() => EmoteConfig.HasEmote(formattedPerk)).Result)
                    result += $"{Task.Run(() => EmoteConfig.GetEmote(formattedPerk)).Result}";
                else
                    result += $"{EmoteConfig.GetEmote("Classified")}";
            }
            return result;
        }
    }
}
