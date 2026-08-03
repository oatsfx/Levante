using Levante.Configs;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;
using System.IO;

namespace Levante.Helpers
{
    public class ConfigHelper
    {
        public static bool CheckAndLoadConfigFiles()
        {
            if (!Directory.Exists("Configs"))
                Directory.CreateDirectory("Configs");

            AppConfig aConfig;
            CountdownConfig cConfig;

            bool closeProgram = false;
            if (File.Exists(AppConfig.FilePath))
            {
                string json = File.ReadAllText(AppConfig.FilePath);
                aConfig = JsonConvert.DeserializeObject<AppConfig>(json);
            }
            else
            {
                aConfig = new AppConfig();
                File.WriteAllText(AppConfig.FilePath, JsonConvert.SerializeObject(aConfig, Formatting.Indented));
                Log.Warning("No {FilePath} file detected. A new one has been created and the program has stopped. Go and change API tokens and other items.", AppConfig.FilePath);
                closeProgram = true;
            }

            if (Debugger.IsAttached)
                AppConfig.IsDebug = true;

            if (File.Exists(CountdownConfig.FilePath))
            {
                string json = File.ReadAllText(CountdownConfig.FilePath);
                cConfig = JsonConvert.DeserializeObject<CountdownConfig>(json);
            }
            else
            {
                cConfig = new CountdownConfig();
                File.WriteAllText(CountdownConfig.FilePath, JsonConvert.SerializeObject(cConfig, Formatting.Indented));
                Log.Warning("No {FilePath} file detected. A new one has been created and the program has stopped. No action is needed.", CountdownConfig.FilePath);
                closeProgram = true;
            }

            return !closeProgram;
        }
    }
}
