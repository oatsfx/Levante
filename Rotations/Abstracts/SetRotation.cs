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
    public abstract class SetRotation<TRotation, TTracker, TPrediction> : Rotation<TTracker> where TTracker : IRotationTracker where TPrediction : IRotationPrediction
    {
        protected string RotationFilePath;

        public List<TRotation> Rotations = new();

        public void GetRotationJSON()
        {
            if (!Directory.Exists("Rotations"))
                Directory.CreateDirectory("Rotations");

            if (File.Exists(RotationFilePath))
            {
                string json = File.ReadAllText(RotationFilePath);
                Rotations = JsonConvert.DeserializeObject<List<TRotation>>(json);
            }
            else
            {
                File.WriteAllText(RotationFilePath, JsonConvert.SerializeObject(Rotations, Formatting.Indented));
                Log.Warning("No {RotationFilePath} file detected; it has been created for you. No action is needed.", RotationFilePath);
            }
        }

        public abstract TPrediction DatePrediction(int Rotation, int Skip);
    }
}
