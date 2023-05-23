using Levante.Rotations.Interfaces;
using Levante.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Levante.Rotations.Abstracts;

namespace Levante.Rotations
{
    public class WellspringRotation : SetRotation<Wellspring, WellspringLink, WellspringPrediction>
    {
        public WellspringRotation()
        {
            FilePath = @"Trackers/wellspring.json";
            RotationFilePath = @"Rotations/wellspring.json";

            IsDaily = true;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override WellspringPrediction DatePrediction(int Weapon, int Skip)
        {
            int iteration = CurrentRotations.Actives.Wellspring;
            int DaysUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                DaysUntil++;
                if (iteration == Weapon)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new WellspringPrediction { Wellspring = Rotations[iteration], Date = CurrentRotations.Actives.DailyResetTimestamp.AddDays(DaysUntil) };
        }

        public override bool IsTrackerInRotation(WellspringLink Tracker) => Tracker.Wellspring == CurrentRotations.Actives.Wellspring;

        public override string ToString() => "The Wellspring";
    }

    /*public enum Wellspring
    {
        Golmag, // Attack, Come to Pass (Auto Rifle)
        Vezuul, // Defense, Tarnation (Heavy Grenade Launcher)
        Borgong, // Attack, Fel Taradiddle (Bow)
        Zeerik, // Defense, Father's Sins (Sniper Rifle)
    }*/

    public class Wellspring
    {
        [JsonProperty("Boss")]
        public readonly string Boss;
        [JsonProperty("Weapon")]
        public readonly string Weapon;
        [JsonProperty("WeaponType")]
        public readonly string WeaponType;
        [JsonProperty("WeaponEmote")]
        public readonly string WeaponEmote;
        [JsonProperty("Type")]
        public readonly string Type;

        public override string ToString() => $"{Type}: {Weapon} ({WeaponType}), {Boss}";
    }

    public class WellspringLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Wellspring")]
        public int Wellspring { get; set; } = 0;

        public override string ToString() => $"{CurrentRotations.Wellspring.Rotations[Wellspring]}";
    }

    public class WellspringPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public Wellspring Wellspring { get; set; }
    }
}
