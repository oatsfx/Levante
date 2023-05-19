using Discord;
using Levante.Configs;
using Levante.Rotations.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Levante.Rotations.Abstracts;

namespace Levante.Rotations
{
    public class KingsFallRotation : SetRotation<KingsFall, KingsFallLink, KingsFallPrediction>
    {
        public KingsFallRotation()
        {
            FilePath = @"Trackers/kingsFall.json";
            RotationFilePath = @"Rotations/kingsFall.json";

            IsDaily = false;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override KingsFallPrediction DatePrediction(int Encounter, int Skip)
        {
            int iteration = CurrentRotations.Actives.KFChallenge;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Encounter)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new KingsFallPrediction { KingsFall = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(KingsFallLink Tracker) => Tracker.Encounter == CurrentRotations.Actives.KFChallenge || CurrentRotations.FeaturedRaid.Rotations[CurrentRotations.Actives.FeaturedRaid].Raid.Equals("King's Fall");

        public override string ToString() => "King's Fall Challenge";
    }

    /*public enum KingsFallEncounter
    {
        Basilica, // The Grass Is Always Greener
        Warpriest, // Devious Thievery
        Golgoroth, // Gaze Amaze
        Daughters, // Under Construction
        Oryx, // Hands Off
    }*/

    public class KingsFall
    {
        [JsonProperty("Encounter")]
        public readonly string Encounter;

        [JsonProperty("ChallengeName")]
        public readonly string ChallengeName;

        public override string ToString() => $"{Encounter} ({ChallengeName})";
    }

    public class KingsFallLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Encounter")]
        public int Encounter { get; set; } = 0;

        public override string ToString() => $"{CurrentRotations.KingsFall.Rotations[Encounter]}";
    }

    public class KingsFallPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public KingsFall KingsFall { get; set; }
    }
}
