using Levante.Configs;
using Levante.Util;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Levante.Helpers;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net.Http;
using Serilog;
using Levante.Rotations.Abstracts;
using Levante.Rotations.Interfaces;

namespace Levante.Rotations
{
    public class NightfallRotation : SetRotation<Nightfall, NightfallLink, NightfallPrediction>
    {
        private string WeaponRotationFilePath;

        public List<NightfallWeapon> WeaponRotations = new();

        public NightfallRotation()
        {
            FilePath = @"Trackers/nightfall.json";
            RotationFilePath = @"Rotations/nightfall.json";
            WeaponRotationFilePath = @"Rotations/nfWeapons.json";

            IsDaily = false;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public new void GetRotationJSON()
        {
            if (File.Exists(RotationFilePath))
            {
                string json = File.ReadAllText(RotationFilePath);
                Rotations = JsonConvert.DeserializeObject<List<Nightfall>>(json);
            }
            else
            {
                File.WriteAllText(RotationFilePath, JsonConvert.SerializeObject(Rotations, Formatting.Indented));
                Log.Warning("No {RotationFilePath} file detected; it has been created for you. No action is needed.", RotationFilePath);
            }

            if (File.Exists(WeaponRotationFilePath))
            {
                string json = File.ReadAllText(WeaponRotationFilePath);
                WeaponRotations = JsonConvert.DeserializeObject<List<NightfallWeapon>>(json);
            }
            else
            {
                File.WriteAllText(WeaponRotationFilePath, JsonConvert.SerializeObject(WeaponRotations, Formatting.Indented));
                Log.Warning("No {ArmorRotationFilePath} file detected; it has been created for you. No action is needed.", WeaponRotationFilePath);
            }
        }

        public NightfallPrediction DatePrediction(int NightfallStrike, int WeaponDrop, int Skip)
        {
            int iterationWeapon = CurrentRotations.Actives.NightfallWeaponDrop;
            int iterationStrike = CurrentRotations.Actives.Nightfall;
            int correctIterations = -1;
            int WeeksUntil = 0;

            bool isImpossible = NightfallStrike > -1 && WeaponDrop > -1 && NightfallStrike != WeaponDrop && Rotations.Count == WeaponRotations.Count;

            // This logic only works if the position in the rotations match up with strike drops (aka they should).
            if (isImpossible)
                return null;

            if (WeaponDrop != -1 && NightfallStrike == -1)
            {
                do
                {
                    iterationWeapon = iterationWeapon == WeaponRotations.Count - 1 ? 0 : iterationWeapon + 1;
                    iterationStrike = iterationStrike == Rotations.Count - 1 ? 0 : iterationStrike + 1;
                    WeeksUntil++;
                    if (iterationWeapon == WeaponDrop)
                        correctIterations++;
                } while (Skip != correctIterations);
            }
            else if (WeaponDrop == -1 && NightfallStrike != -1)
            {
                do
                {
                    iterationWeapon = iterationWeapon == WeaponRotations.Count - 1 ? 0 : iterationWeapon + 1;
                    iterationStrike = iterationStrike == Rotations.Count - 1 ? 0 : iterationStrike + 1;
                    WeeksUntil++;
                    if (iterationStrike == NightfallStrike)
                        correctIterations++;
                } while (Skip != correctIterations);
            }
            else if (WeaponDrop != -1 && NightfallStrike != -1)
            {
                do
                {
                    iterationWeapon = iterationWeapon == WeaponRotations.Count - 1 ? 0 : iterationWeapon + 1;
                    iterationStrike = iterationStrike == Rotations.Count - 1 ? 0 : iterationStrike + 1;
                    WeeksUntil++;
                    if (iterationStrike == NightfallStrike && iterationWeapon == WeaponDrop)
                        correctIterations++;
                } while (Skip != correctIterations);
            }

            return new NightfallPrediction { Nightfall = Rotations[iterationStrike], NightfallWeapon = WeaponRotations[iterationWeapon], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) }; // Because there is no .AddWeeks().
        }

        public override NightfallPrediction DatePrediction(int Rotation, int Skip)
        {
            throw new NotImplementedException();
        }

        public override bool IsTrackerInRotation(NightfallLink Tracker)
        {
            if (Tracker.Nightfall == -1)
                return Tracker.WeaponDrop == CurrentRotations.Actives.NightfallWeaponDrop;
            else if (Tracker.WeaponDrop == -1)
                return Tracker.Nightfall == CurrentRotations.Actives.Nightfall;
            else
                return Tracker.Nightfall == CurrentRotations.Actives.Nightfall && Tracker.WeaponDrop == CurrentRotations.Actives.NightfallWeaponDrop;
        }
    }

    public class Nightfall
    {
        [JsonProperty("Name")]
        public string Name;

        public override string ToString() => Name;
    }

    public class NightfallWeapon
    {
        [JsonProperty("Hash")]
        public long Hash;
        [JsonProperty("AdeptHash")]
        public long AdeptHash;
        public string Name;
        public string Emote;

        public override string ToString() => $"{Name}";
    }

    public class NightfallLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Nightfall")]
        public int Nightfall { get; set; } = 0;

        [JsonProperty("WeaponDrop")]
        public int WeaponDrop { get; set; } = 0;

        public override string ToString()
        {
            string result = "Nightfall";
            if (Nightfall >= 0)
                result = $"{CurrentRotations.Nightfall.Rotations[Nightfall]}";

            if (WeaponDrop >= 0)
                result += $" dropping {CurrentRotations.Nightfall.WeaponRotations[WeaponDrop]}";

            return result;
        }
    }

    public class NightfallPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public Nightfall Nightfall { get; set; }
        public NightfallWeapon NightfallWeapon { get; set; }
    }
}
