using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Levante.Rotations.Abstracts;
using Levante.Rotations.Interfaces;

namespace Levante.Rotations
{
    public class TerminalOverloadRotation : SetRotation<TerminalOverload, TerminalOverloadLink, TerminalOverloadPrediction>
    {
        public TerminalOverloadRotation()
        {
            FilePath = @"Trackers/terminalOverload.json";
            RotationFilePath = @"Rotations/terminalOverload.json";

            IsDaily = true;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override TerminalOverloadPrediction DatePrediction(int Location, int Skip)
        {
            int iteration = CurrentRotations.Actives.TerminalOverload;
            int DaysUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                DaysUntil++;
                if (iteration == Location)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new TerminalOverloadPrediction { TerminalOverload = Rotations[Location], Date = CurrentRotations.Actives.DailyResetTimestamp.AddDays(DaysUntil) };
        }

        public override bool IsTrackerInRotation(TerminalOverloadLink Tracker) => Tracker.Location == CurrentRotations.Actives.TerminalOverload;

        public override string ToString() => "Terminal Overload";
    }

    public class TerminalOverload
    {
        [JsonProperty("Location")]
        public readonly string Location;
        [JsonProperty("Weapon")]
        public readonly string Weapon;
        [JsonProperty("WeaponType")]
        public readonly string WeaponType;
        [JsonProperty("WeaponEmote")]
        public readonly string WeaponEmote;

        public override string ToString() => $"{Weapon} ({WeaponType}), {Location}";
    }

    public class TerminalOverloadLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Location")]
        public int Location { get; set; } = 0;

        public override string ToString() => $"{CurrentRotations.TerminalOverload.Rotations[Location]}";
    }

    public class TerminalOverloadPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public TerminalOverload TerminalOverload { get; set; }
    }
}
