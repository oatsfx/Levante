using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BungieSharper.Entities.Destiny;
using Levante.Configs;
using Newtonsoft.Json;
using Serilog;

namespace Levante.Util
{
    // emblem.report

    public class EmblemReport
    {
        public readonly EmblemReportData Data;

        // Supports only having emblem.report data of one emblem.
        public EmblemReport(long collectibleHash)
        {
            var collectiblesBody = new
            {
                collectibles = new List<long> { collectibleHash }
            };
            using var client = new HttpClient();
            var postContent = new StringContent(JsonConvert.SerializeObject(collectiblesBody), Encoding.UTF8, "application/json");

            var response = client.PostAsync("https://emblem.report/api/getRarestEmblems", postContent).Result;

            var content = response.Content.ReadAsStringAsync().Result;
            var responseList = JsonConvert.DeserializeObject<EmblemReportResponse>(content);
            Data = responseList.Data.FirstOrDefault(x => x.CollectibleHash == collectibleHash);
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
}
