using BungieSharper.Entities.Destiny;
using Fergun.Interactive;
using Levante.Configs;
using Levante.Services;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Levante.Structs
{
    // emblem.report

    public class EmblemReport
    {
        public readonly List<EmblemReportData> Data;

        public EmblemReport(long collectibleHash) : this(new List<long> { collectibleHash }, 1)
        {

        }

        public EmblemReport(IEnumerable<long> collectibleHashes, int limit = 5)
        {
            var collectiblesBody = new
            {
                collectibles = collectibleHashes
            };
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"Mozilla/5.0 (compatible; {AppConfig.App.Name}/{AppConfig.App.Version})");
            client.DefaultRequestHeaders.Add("X-API-KEY", AppConfig.Credentials.EmblemReportApiKey);
            var postContent = new StringContent(JsonConvert.SerializeObject(collectiblesBody), Encoding.UTF8, "application/json");

            var response = client.PostAsync($"https://emblem.report/api/getRarestEmblems?limit={limit}", postContent).Result;

            var content = response.Content.ReadAsStringAsync().Result;
            var responseList = JsonConvert.DeserializeObject<EmblemReportResponse>(content);
            Data = responseList.Data;
        }
    }

    public class EmblemReportResponse
    {
        [JsonProperty("data")]
        public List<EmblemReportData> Data { get; set; }
    }

    public class EmblemReportData
    {
        [JsonProperty("collectible_hash")]
        public long CollectibleHash { get; set; }

        [JsonProperty("acquisition")]
        public long Acquisition { get; set; }

        [JsonProperty("percentage")]
        public double Percentage { get; set; }
    }

    public class EmblemReportEmblemDetail
    {
        public readonly List<EmblemReportData> Data;

        public EmblemReportEmblemDetail(long invHash)
        {
            var page = 1;
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"Mozilla/5.0 (compatible; {AppConfig.App.Name}/{AppConfig.App.Version})");
            client.DefaultRequestHeaders.Add("X-API-KEY", AppConfig.Credentials.EmblemReportApiKey);
            var collectiblesBody = new
            {
                hash = $"{invHash}",
                page,
            };
            var postContent = new StringContent(JsonConvert.SerializeObject(collectiblesBody), Encoding.UTF8, "application/json");

            var response = client.PostAsync($"https://emblem.report/api/getEmblemDetail", postContent).Result;

            var content = response.Content.ReadAsStringAsync().Result;
            var responseList = JsonConvert.DeserializeObject<EmblemReportDetailResponse>(content);

            Log.Debug($"{responseList.Players}");
        }
    }

    public class EmblemReportDetailResponse
    {
        [JsonProperty("players")]
        public List<EmblemReportDetailPlayerData> Players { get; set; }
    }

    public class EmblemReportDetailPlayerData
    {
        [JsonProperty("bungie_name")]
        public string BungieName { get; set; }

        [JsonProperty("membershipId")]
        public string MembershipId { get; set; }

        [JsonProperty("membershipType")]
        public int MembershipType { get; set; }
    }
}
