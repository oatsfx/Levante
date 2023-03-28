using Levante.Rotations.Interfaces;
using Levante.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Levante.Rotations.Abstracts;

namespace Levante.Rotations
{
    public class AltarsOfSorrowRotation : Rotation<AltarsOfSorrow, AltarsOfSorrowLink, AltarsOfSorrowPrediction>
    {
        public AltarsOfSorrowRotation()
        {
            FilePath = @"Trackers/altarsOfSorrow.json";
            RotationFilePath = @"Rotations/altarsOfSorrow.json";

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override AltarsOfSorrowPrediction DatePrediction(int Weapon, int Skip)
        {
            int iteration = CurrentRotations.Actives.AltarWeapon;
            int DaysUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                DaysUntil++;
                if (iteration == Weapon)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new AltarsOfSorrowPrediction { AltarsOfSorrow = Rotations[iteration], Date = CurrentRotations.Actives.DailyResetTimestamp.AddDays(DaysUntil) };
        }

        public override bool IsTrackerInRotation(AltarsOfSorrowLink Tracker) => Tracker.WeaponDrop == CurrentRotations.Actives.AltarWeapon;
    }

    public class AltarsOfSorrow
    {
        [JsonProperty("Boss")]
        public readonly string Boss;
        [JsonProperty("Weapon")]
        public readonly string Weapon;
        [JsonProperty("WeaponType")]
        public readonly string WeaponType;
        [JsonProperty("WeaponEmote")]
        public readonly string WeaponEmote;

        public override string ToString() => $"{Weapon} ({WeaponType})";
    }

    public class AltarsOfSorrowLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("WeaponDrop")]
        public int WeaponDrop { get; set; } = 0;
    }

    public class AltarsOfSorrowPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public AltarsOfSorrow AltarsOfSorrow { get; set; }
    }
}
