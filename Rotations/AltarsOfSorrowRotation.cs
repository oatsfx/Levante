using Levante.Rotations.Interfaces;
using Levante.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Levante.Rotations.Abstracts;
using Discord;

namespace Levante.Rotations
{
    public class AltarsOfSorrowRotation : SetRotation<AltarsOfSorrow, AltarsOfSorrowLink, AltarsOfSorrowPrediction>
    {
        public AltarsOfSorrowRotation()
        {
            FilePath = @"Trackers/altarsOfSorrow.json";
            RotationFilePath = @"Rotations/altarsOfSorrow.json";

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override AltarsOfSorrowPrediction DatePrediction(int Location, int Skip)
        {
            int iteration = CurrentRotations.Actives.AltarWeapon;
            int DaysUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                DaysUntil++;
                if (iteration == Location)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new AltarsOfSorrowPrediction { AltarsOfSorrow = Rotations[iteration], Date = CurrentRotations.Actives.DailyResetTimestamp.AddDays(DaysUntil) };
        }

        public override bool IsTrackerInRotation(AltarsOfSorrowLink Tracker) => Tracker.WeaponDrop == CurrentRotations.Actives.AltarWeapon;

        public override string ToString() => "Altars of Sorrow";
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

        public override string ToString() => $"{Weapon} ({WeaponType}), {Boss}";
    }

    public class AltarsOfSorrowLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; }

        [JsonProperty("WeaponDrop")]
        public int WeaponDrop { get; set; }

        public override string ToString() => $"{CurrentRotations.AltarsOfSorrow.Rotations[WeaponDrop]}";
    }

    public class AltarsOfSorrowPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public AltarsOfSorrow AltarsOfSorrow { get; set; }
    }
}
