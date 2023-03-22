using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Levante.Rotations.Interfaces;

namespace Levante.Rotations.Abstracts
{
    public abstract class Rotation<R, T, P> where T : IRotationTracker where P : IRotationPrediction
    {
        public string FilePath;
        public string RotationFilePath;

        public List<R> Rotations = new();
        public List<T> Trackers = new();

        public void GetTrackerJSON()
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                Trackers = JsonConvert.DeserializeObject<List<T>>(json);
            }
            else
            {
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(Trackers, Formatting.Indented));
                Log.Warning("No {FilePath} file detected; it has been created for you. No action needed.", FilePath);
            }
        }

        public void GetRotationJSON()
        {
            if (File.Exists(RotationFilePath))
            {
                string json = File.ReadAllText(RotationFilePath);
                Rotations = JsonConvert.DeserializeObject<List<R>>(json);
            }
            else
            {
                File.WriteAllText(RotationFilePath, JsonConvert.SerializeObject(Rotations, Formatting.Indented));
                Log.Warning("No {RotationFilePath} file detected; it has been created for you. No action needed.", RotationFilePath);
            }
        }

        public void AddUserTracking(T Tracker)
        {
            Trackers.Add(Tracker);
            UpdateJSON();
        }

        public void RemoveUserTracking(ulong DiscordID)
        {
            Trackers.Remove(GetUserTracking(DiscordID));
            UpdateJSON();
        }

        public T GetUserTracking(ulong DiscordID) => Trackers.First(x => x.DiscordID == DiscordID);

        public void UpdateJSON()
        {
            string output = JsonConvert.SerializeObject(Trackers, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public abstract P DatePrediction(int Rotation, int Skip);
    }
}
