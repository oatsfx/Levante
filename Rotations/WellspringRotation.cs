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
    public class WellspringRotation : Rotation<Wellspring, WellspringLink, WellspringPrediction>
    {
        public WellspringRotation()
        {
            FilePath = @"Trackers/wellspring.json";
            RotationFilePath = @"Rotations/wellspring.json";

            GetRotationJSON();
            GetTrackerJSON();
        }

        /*public static string GetWellspringBossString(Wellspring Wellspring)
        {
            switch (Wellspring)
            {
                case Wellspring.Golmag: return "Golmag, Warden of the Spring";
                case Wellspring.Vezuul: return "Vezuul, Lightflayer";
                case Wellspring.Borgong: return "Bor'gong, Warden of the Spring";
                case Wellspring.Zeerik: return "Zeerik, Lightflayer";
                default: return "The Wellspring";
            }
        }

        public static string GetWellspringTypeString(Wellspring Wellspring)
        {
            switch (Wellspring)
            {
                case Wellspring.Golmag: return "Attack";
                case Wellspring.Vezuul: return "Defend";
                case Wellspring.Borgong: return "Attack";
                case Wellspring.Zeerik: return "Defend";
                default: return "The Wellspring Type";
            }
        }

        public static string GetWeaponNameString(Wellspring Wellspring)
        {
            switch (Wellspring)
            {
                case Wellspring.Golmag: return "Come to Pass";
                case Wellspring.Vezuul: return "Tarnation";
                case Wellspring.Borgong: return "Fel Taradiddle";
                case Wellspring.Zeerik: return "Father's Sins";
                default: return "The Wellspring Weapon";
            }
        }

        public static string GetWeaponTypeString(Wellspring Wellspring)
        {
            switch (Wellspring)
            {
                case Wellspring.Golmag: return "Auto Rifle";
                case Wellspring.Vezuul: return "Grenade Launcher";
                case Wellspring.Borgong: return "Bow";
                case Wellspring.Zeerik: return "Sniper";
                default: return "The Wellspring Weapon Type";
            }
        }

        public static string GetWeaponEmote(Wellspring Wellspring)
        {
            switch (Wellspring)
            {
                case Wellspring.Golmag: return $"{DestinyEmote.AutoRifle}";
                case Wellspring.Vezuul: return $"{DestinyEmote.HeavyGrenadeLauncher}";
                case Wellspring.Borgong: return $"{DestinyEmote.Bow}";
                case Wellspring.Zeerik: return $"{DestinyEmote.SniperRifle}";
                default: return "The Wellspring Weapon Emote";
            }
        }*/

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
    }

    public class WellspringLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Wellspring")]
        public int Wellspring { get; set; } = 0;
    }

    public class WellspringPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public Wellspring Wellspring { get; set; }
    }
}
