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
    public class VowOfTheDiscipleRotation : Rotation<VowOfTheDisciple, VowOfTheDiscipleLink, VowOfTheDisciplePrediction>
    {
        public VowOfTheDiscipleRotation()
        {
            FilePath = @"Trackers/vowOfTheDisciple.json";
            RotationFilePath = @"Rotations/vowOfTheDisciple.json";

            GetRotationJSON();
            GetTrackerJSON();
        }

        /*public static string GetEncounterString(VowOfTheDiscipleEncounter Encounter)
        {
            switch (Encounter)
            {
                case VowOfTheDiscipleEncounter.Acquisition: return "Acquisition";
                case VowOfTheDiscipleEncounter.Caretaker: return "The Caretaker";
                case VowOfTheDiscipleEncounter.Exhibition: return "Exhibition";
                case VowOfTheDiscipleEncounter.Rhulk: return "Rhulk";
                default: return "The Vow of the Disciple";
            }
        }

        public static string GetChallengeString(VowOfTheDiscipleEncounter Encounter)
        {
            switch (Encounter)
            {
                case VowOfTheDiscipleEncounter.Acquisition: return "Swift Destruction";
                case VowOfTheDiscipleEncounter.Caretaker: return "Base Information";
                case VowOfTheDiscipleEncounter.Exhibition: return "Defenses Down";
                case VowOfTheDiscipleEncounter.Rhulk: return "Looping Catalyst";
                default: return "The Vow of the Disciple";
            }
        }*/

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

        public override bool IsTrackerInRotation(VowOfTheDiscipleLink Tracker) => Tracker.Encounter == CurrentRotations.Actives.VowChallenge;
    }

    /*public enum VowOfTheDiscipleEncounter
    {
        Acquisition, // Swift Destruction
        Caretaker, // Base Information
        Exhibition, // Defenses Down
        Rhulk, // Looping Catalyst
    }*/

    public class VowOfTheDisciple
    {
        [JsonProperty("Encounter")]
        public readonly string Encounter;

        [JsonProperty("ChallengeName")]
        public readonly string ChallengeName;
    }

    public class VowOfTheDiscipleLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Encounter")]
        public int Encounter { get; set; } = 0;
    }

    public class VowOfTheDisciplePrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public VowOfTheDisciple VowOfTheDisciple { get; set; }
    }
}
