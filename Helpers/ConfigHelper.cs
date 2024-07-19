using Levante.Configs;
using Levante.Leaderboards;
using Newtonsoft.Json;
using Serilog;
using System;
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
            DataConfig dConfig;
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

            if (File.Exists(DataConfig.FilePath))
            {
                string json = File.ReadAllText(DataConfig.FilePath);
                dConfig = JsonConvert.DeserializeObject<DataConfig>(json);
            }
            else
            {
                dConfig = new DataConfig();
                File.WriteAllText(DataConfig.FilePath, JsonConvert.SerializeObject(dConfig, Formatting.Indented));
                Log.Warning("No {FilePath} file detected. A new one has been created and the program has stopped. No action is needed.", DataConfig.FilePath);
                closeProgram = true;
            }

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

            if (!File.Exists(EmoteConfig.FilePath))
            {
                File.WriteAllText(EmoteConfig.FilePath, JsonConvert.SerializeObject(new EmoteConfig(), Formatting.Indented));
                Log.Warning("No {FilePath} file detected. A new one has been created.", EmoteConfig.FilePath);
            }

            return !closeProgram;
        }
    }
}
