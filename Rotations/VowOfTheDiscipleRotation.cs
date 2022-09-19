using Levante.Configs;
using Levante.Util;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Rotations
{
    public class VowOfTheDiscipleRotation
    {
        public static readonly int VowOfTheDiscipleEncounterCount = 5;
        public static readonly string FilePath = @"Trackers/vowOfTheDisciple.json";

        [JsonProperty("VowOfTheDiscipleLinks")]
        public static List<VowOfTheDiscipleLink> VowOfTheDiscipleLinks { get; set; } = new List<VowOfTheDiscipleLink>();

        public class VowOfTheDiscipleLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("Encounter")]
            public VowOfTheDiscipleEncounter Encounter { get; set; } = VowOfTheDiscipleEncounter.Acquisition;
        }

        public static string GetEncounterString(VowOfTheDiscipleEncounter Encounter)
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
        }

        public static string GetChallengeDescriptionString(VowOfTheDiscipleEncounter Encounter)
        {
            switch (Encounter)
            {
                case VowOfTheDiscipleEncounter.Acquisition: return "";
                case VowOfTheDiscipleEncounter.Caretaker: return "";
                case VowOfTheDiscipleEncounter.Exhibition: return "";
                case VowOfTheDiscipleEncounter.Rhulk: return "";
                default: return "The Vow of the Disciple";
            }
        }

        public static EmbedBuilder GetRaidEmbed()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Raid Information",
                IconUrl = "https://www.bungie.net/img/destiny_content/pgcr/raid_nemesis.jpg",
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Savathûn's Throne World"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };
            embed.AddField(y =>
            {
                y.Name = $"Requirements";
                y.Value = $"Power: {DestinyEmote.Light}1520";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(VowOfTheDiscipleEncounter.Acquisition)}";
                y.Value = $"{DestinyEmote.VoGRaidChallenge} {GetChallengeString(VowOfTheDiscipleEncounter.Acquisition)}\n" +
                    $"{GetChallengeDescriptionString(VowOfTheDiscipleEncounter.Acquisition)}";
                y.IsInline = false;
            });

            embed.Title = $"Vow of the Disciple";
            embed.Description = $"Clear out the Pyramid within Savathûn's Throne World.";

            embed.Url = "https://www.bungie.net/img/destiny_content/pgcr/raid_nemesis.jpg";
            embed.ThumbnailUrl = "https://www.bungie.net/common/destiny2_content/icons/1f66fa02b19f40e6ce5d8336c7ed5a00.png";

            return embed;
        }

        public static void AddUserTracking(ulong DiscordID, VowOfTheDiscipleEncounter Encounter)
        {
            VowOfTheDiscipleLinks.Add(new VowOfTheDiscipleLink() { DiscordID = DiscordID, Encounter = Encounter });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            VowOfTheDiscipleLinks.Remove(GetUserTracking(DiscordID, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static VowOfTheDiscipleLink GetUserTracking(ulong DiscordID, out VowOfTheDiscipleEncounter Encounter)
        {
            foreach (var Link in VowOfTheDiscipleLinks)
                if (Link.DiscordID == DiscordID)
                {
                    Encounter = Link.Encounter;
                    return Link;
                }
            Encounter = VowOfTheDiscipleEncounter.Acquisition;
            return null;
        }

        public static void CreateJSON()
        {
            VowOfTheDiscipleRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<VowOfTheDiscipleRotation>(json);
            }
            else
            {
                obj = new VowOfTheDiscipleRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new VowOfTheDiscipleRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(VowOfTheDiscipleEncounter Encounter)
        {
            VowOfTheDiscipleEncounter iterationEncounter = CurrentRotations.VowChallengeEncounter;
            int WeeksUntil = 0;
            do
            {
                iterationEncounter = iterationEncounter == VowOfTheDiscipleEncounter.Rhulk ? VowOfTheDiscipleEncounter.Acquisition : iterationEncounter + 1;
                WeeksUntil++;
            } while (iterationEncounter != Encounter);
            return CurrentRotations.WeeklyResetTimestamp.AddDays(WeeksUntil * 7); // Because there is no .AddWeeks().
        }
    }

    public enum VowOfTheDiscipleEncounter
    {
        Acquisition, // Swift Destruction
        Caretaker, // Base Information
        Exhibition, // Defenses Down
        Rhulk, // Looping Catalyst
    }
}
