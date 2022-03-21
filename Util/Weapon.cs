using System;
using Levante.Configs;
using System.Net.Http;
using Discord;

namespace Levante.Util
{
    public class Weapon : InventoryItem
    {
        public Weapon(long hashCode)
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

        public override EmbedBuilder GetEmbed()
        {
            throw new NotImplementedException();
        }
    }
}
