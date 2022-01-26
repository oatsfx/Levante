using DestinyUtility.Configs;
using Newtonsoft.Json;
using System.Net.Http;

namespace DestinyUtility.Util
{
    public class Emblem : InventoryItem
    {
        public Emblem(long hashCode)
        {
            HashCode = hashCode;
            APIUrl = $"https://www.bungie.net/platform/Destiny2/Manifest/DestinyInventoryItemDefinition/" + HashCode;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync(APIUrl).Result;
                Content = response.Content.ReadAsStringAsync().Result;
            }
        }

        public int[] GetRGBAsIntArray()
        {
            int[] result = new int[3];
            dynamic item = JsonConvert.DeserializeObject(Content);
            result[0] = item.Response.backgroundColor.red;
            result[1] = item.Response.backgroundColor.green;
            result[2] = item.Response.backgroundColor.blue;
            return result;
        }

        public string GetSourceString()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

                var response = client.GetAsync($"https://www.bungie.net/platform/Destiny2/Manifest/DestinyCollectibleDefinition/" + GetCollectableHash()).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);
                return item.Response.sourceString;
            }
        }

        public string GetBackgroundUrl()
        {
            dynamic item = JsonConvert.DeserializeObject(Content);
            return "https://www.bungie.net" + item.Response.secondaryIcon;
        }
    }
}
