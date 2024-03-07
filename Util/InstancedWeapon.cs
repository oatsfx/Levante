using APIHelper;
using Levante.Configs;
using Levante.Helpers;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Util
{
    public class InstancedWeapon : Weapon
    {
        private string InstanceID;

        private List<string> Perks = new();

        private EmoteConfig Emotes = JsonConvert.DeserializeObject<EmoteConfig>(File.ReadAllText(EmoteConfig.FilePath));

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
        }

        public string PerksToString()
        {
            string result = "";
            foreach (var perk in Perks)
            {
                if (Emotes.HasEmote(perk.Replace(" ", "").Replace("-", "").Replace("'", "")))
                    result += $"{Emotes.GetEmote(perk.Replace(" ", "").Replace("-", "").Replace("'", ""))}";
                else
                    result += $"{DestinyEmote.Classified}";
            }
            return result;
        }
    }
}
