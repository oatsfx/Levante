using Levante.Helpers;
using Newtonsoft.Json;
using Serilog;
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
        // TODO: Automate the countdown removal process.

        // <Name, Start Time>
        [JsonProperty("Countdowns")]
        public static Dictionary<string, DateTime> Countdowns { get; internal set; } = new();

        public static void AddCountdown(string Name, DateTime StartTime)
        {
            if (!Countdowns.ContainsKey(Name))
                Countdowns.Add(Name, StartTime);

            File.WriteAllText(FilePath, JsonConvert.SerializeObject(new CountdownConfig(), Formatting.Indented));

            Log.Information("[{Type}] New countdown created: {Name}", "Countdowns", Name);
        }

        public static void RemoveCountdown(string Name)
        {
            Countdowns.Remove(Name);
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(new CountdownConfig(), Formatting.Indented));

            Log.Information("[{Type}] Countdown removed: {Name}", "Countdowns", Name);
        }

        public static void CheckCountdowns()
        {
            var countdowns = Countdowns;
            foreach (var countdown in Countdowns)
                if (DateTime.Now >= countdown.Value)
                    RemoveCountdown(countdown.Key);
        }
    }
}
