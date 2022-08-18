using Levante.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Configs
{
    public class CountdownConfig : IConfig
    {
        public static string FilePath { get; } = @"Configs/countdownConfig.json";

        // <Name, Start Time>
        [JsonProperty("Countdowns")]
        public static Dictionary<string, DateTime> Countdowns { get; internal set; } = new Dictionary<string, DateTime>();

        public static void AddCountdown(string Name, DateTime StartTime)
        {
            if (!Countdowns.ContainsKey(Name))
                Countdowns.Add(Name, StartTime);

            File.WriteAllText(FilePath, JsonConvert.SerializeObject(new CountdownConfig(), Formatting.Indented));

            LogHelper.ConsoleLog($"[COUNTDOWNS] New countdown created: {Name}.");
        }

        public static void RemoveCountdown(string Name)
        {
            Countdowns.Remove(Name);
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(new CountdownConfig(), Formatting.Indented));

            LogHelper.ConsoleLog($"[COUNTDOWNS] Countdown removed: {Name}.");
        }
    }
}
