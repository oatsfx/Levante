using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using Dapper;
using Levante.Configs;
using Levante.Manifest;
using Newtonsoft.Json;

namespace Levante.Util
{
    internal class ManifestHelper
    {
        private static IEnumerable<DestinyInventoryItemDefinition> destinyInventoryItemDefinitionList;
        private static IEnumerable<DestinyPlugSetDefinition> destinyPlugSetDefinitionList;

        public static void PrepManifest()
        {
            Console.WriteLine("--- START PrepManifest");
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

            var response = client.GetAsync("https://www.bungie.net/Platform/Destiny2/Manifest/").Result;
            var content = response.Content.ReadAsStringAsync().Result;
            dynamic item = JsonConvert.DeserializeObject(content);

            if (item == null)
            {
                Console.WriteLine("Failed to load manifest.");
                return;
            }

            // TODO: check version to see if it needs updating
            Console.WriteLine($"Found manifest v.{item.Response.version}");
            
            string path = item.Response.mobileWorldContentPaths.en;
            var filePath = "Data/" + path.Split("/").LastOrDefault();

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Downloading manifest...");
                new WebClient().DownloadFile("https://bungie.net" + path, filePath);

                if (!Directory.Exists("Data/Manifest"))
                    Directory.CreateDirectory("Data/Manifest");

                Console.WriteLine("Unpacking manifest...");
                ZipFile.ExtractToDirectory(filePath, "Data/Manifest");

                Console.WriteLine($"Manifest extracted to {filePath}");
            }

            using (var cnn = ManifestConnection.ManifestRepository.ManifestDBConnection())
            {
                cnn.Open();
                Console.WriteLine("Populating DestinyInventoryItemDefinition from manifest...");
                destinyInventoryItemDefinitionList = cnn.Query<DestinyInventoryItemDefinition>("SELECT * FROM DestinyInventoryItemDefinition;");
                Console.WriteLine($"Loaded {destinyInventoryItemDefinitionList.Count()} definitions from DestinyInventoryItemDefinition manifest.");

                Console.WriteLine("Populating DestinyPlugSetDefinition from manifest...");
                destinyPlugSetDefinitionList = cnn.Query<DestinyPlugSetDefinition>("SELECT * FROM DestinyPlugSetDefinition;");
                Console.WriteLine($"Loaded {destinyPlugSetDefinitionList.Count()} definitions from DestinyPlugSetDefinition manifest.");
            }

            
            Console.WriteLine("--- END PrepManifest\n");
        }
    }
}