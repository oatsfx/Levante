using Levante.Configs;
using Levante.Util;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Levante.Rotations.Interfaces;
using Levante.Rotations.Abstracts;

namespace Levante.Rotations
{
    public class VowOfTheDiscipleRotation : SetRotation<RaidEncounter, VowOfTheDiscipleLink, VowOfTheDisciplePrediction>
    {
        public VowOfTheDiscipleRotation()
        {
            FilePath = @"Trackers/vowOfTheDisciple.json";
            RotationFilePath = @"Rotations/vowOfTheDisciple.json";

            IsDaily = false;

            GetRotationJSON();
            GetTrackerJSON();
        }

        public override VowOfTheDisciplePrediction DatePrediction(int Encounter, int Skip)
        {
            int iteration = CurrentRotations.Actives.DSCChallenge;
            int WeeksUntil = 0;
            int correctIterations = -1;
            do
            {
                iteration = iteration == Rotations.Count - 1 ? 0 : iteration + 1;
                WeeksUntil++;
                if (iteration == Encounter)
                    correctIterations++;
            } while (Skip != correctIterations);
            return new VowOfTheDisciplePrediction { VowOfTheDisciple = Rotations[iteration], Date = CurrentRotations.Actives.WeeklyResetTimestamp.AddDays(WeeksUntil * 7) };
        }

        public override bool IsTrackerInRotation(VowOfTheDiscipleLink Tracker) => Tracker.Encounter == CurrentRotations.Actives.VowChallenge || CurrentRotations.FeaturedRaid.Rotations[CurrentRotations.Actives.FeaturedRaid].Raid.Equals("Vow of the Disciple");

        public override string ToString() => "Vow of the Disciple Challenge";
    }

    public class VowOfTheDiscipleLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Encounter")]
        public int Encounter { get; set; } = 0;

        public override string ToString() => $"{CurrentRotations.VowOfTheDisciple.Rotations[Encounter]}";
    }

    public class VowOfTheDisciplePrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public RaidEncounter VowOfTheDisciple { get; set; }
    }
}
