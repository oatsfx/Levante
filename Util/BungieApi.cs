using Levante.Configs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Util
{
    public class BungieApi
    {
        public const string API_BASE_URL = "https://bungie.net";
        private static string BungieAPIKey = AppConfig.Credentials.BungieApiKey;

        public static async Task<string> Query(string url, string accessToken = "")
        {
            Log.Debug(API_BASE_URL + url);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-API-Key", BungieAPIKey);
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"Mozilla/5.0 (compatible; {AppConfig.App.Name}/{AppConfig.App.Version})");
            if (!String.IsNullOrEmpty(accessToken))
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await client.GetAsync(API_BASE_URL + url).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public static async Task<string> Post(string url, Dictionary<string, string> headers)
        {
            Log.Debug(API_BASE_URL + url);

            using var client = new HttpClient();

            var postContent = new FormUrlEncodedContent(headers);

            var response = await client.PostAsync(API_BASE_URL + url, postContent).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public static async Task<byte[]> GetImage(string url)
        {
            Log.Debug(API_BASE_URL + url);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"Mozilla/5.0 (compatible; {AppConfig.App.Name}/{AppConfig.App.Version})");

            return await client.GetByteArrayAsync(API_BASE_URL + url).ConfigureAwait(false);
        }
    }
}
