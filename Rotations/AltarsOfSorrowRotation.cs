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
        }

        public override AltarsOfSorrowPrediction DatePrediction(int Weapon, int Skip)
        {
            int iterationWeapon = CurrentRotations.AltarWeapon;
            int DaysUntil = 0;
            do
            {
                iterationWeapon = iterationWeapon == Rotations.Count - 1 ? 0 : iterationWeapon + 1;
                DaysUntil++;
            } while (iterationWeapon != Weapon);
            return new AltarsOfSorrowPrediction { AltarsOfSorrow = Rotations[iterationWeapon], Date = CurrentRotations.DailyResetTimestamp.AddDays(DaysUntil) };
        }
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
