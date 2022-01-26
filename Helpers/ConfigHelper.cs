using DestinyUtility.Configs;
using Newtonsoft.Json;
using System;
using System.IO;

namespace DestinyUtility.Helpers
{
    public class ConfigHelper
    {
        public static bool CheckAndLoadConfigFiles()
        {
            BotConfig bConfig;
            DataConfig dConfig;
            ActiveConfig aConfig;

            bool closeProgram = false;
            if (File.Exists(BotConfig.FilePath))
            {
                string json = File.ReadAllText(BotConfig.FilePath);
                bConfig = JsonConvert.DeserializeObject<BotConfig>(json);
            }
            else
            {
                bConfig = new BotConfig();
                File.WriteAllText(BotConfig.FilePath, JsonConvert.SerializeObject(bConfig, Formatting.Indented));
                Console.WriteLine($"No botConfig.json file detected. A new one has been created and the program has stopped. Go and change API tokens and other items.");
                closeProgram = true;
            }

            if (File.Exists(DataConfig.FilePath))
            {
                string json = File.ReadAllText(DataConfig.FilePath);
                dConfig = JsonConvert.DeserializeObject<DataConfig>(json);
            }
            else
            {
                dConfig = new DataConfig();
                File.WriteAllText(DataConfig.FilePath, JsonConvert.SerializeObject(dConfig, Formatting.Indented));
                Console.WriteLine($"No dataConfig.json file detected. A new one has been created and the program has stopped. No action is needed.");
                closeProgram = true;
            }

            if (File.Exists(ActiveConfig.FilePath))
            {
                string json = File.ReadAllText(ActiveConfig.FilePath);
                aConfig = JsonConvert.DeserializeObject<ActiveConfig>(json);

                try
                {
                    foreach (ActiveConfig.ActiveAFKUser aau in ActiveConfig.ActiveAFKUsers)
                    {
                        int updatedLevel = DataConfig.GetUserSeasonPassLevel(aau.DiscordID, out int updatedProgression);
                        aau.LastLevelProgress = updatedProgression;
                        aau.LastLoggedLevel = updatedLevel;
                    }
                }
                catch
                {
                    DataConfig.UpdateUsersList();
                    Console.WriteLine($"[{String.Format("{0:00}", DateTime.Now.Hour)}:{String.Format("{0:00}", DateTime.Now.Minute)}:{String.Format("{0:00}", DateTime.Now.Second)}] Bungie API is down, loading stored data and continuing.");
                }


                string output = JsonConvert.SerializeObject(aConfig, Formatting.Indented);
                File.WriteAllText(ActiveConfig.FilePath, output);
            }
            else
            {
                aConfig = new ActiveConfig();
                File.WriteAllText(ActiveConfig.FilePath, JsonConvert.SerializeObject(aConfig, Formatting.Indented));
                Console.WriteLine($"No activeConfig.json file detected. A new one has been created and the program has stopped. Go and change API tokens and other items.");
                closeProgram = true;
            }

            if (closeProgram == true) return false;
            return true;
        }
    }
}
