using DestinyUtility.Configs;
using DestinyUtility.Util;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DestinyUtility.Rotations
{
    public class GardenOfSalvationRotation
    {
        public static readonly int GardenOfSalvationEncounterCount = 4;
        public static readonly string FilePath = @"Trackers/gardenOfSalvation.json";

        [JsonProperty("GardenOfSalvationLinks")]
        public static List<GardenOfSalvationLink> GardenOfSalvationLinks { get; set; } = new List<GardenOfSalvationLink>();

        public class GardenOfSalvationLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("Encounter")]
            public GardenOfSalvationEncounter Encounter { get; set; } = GardenOfSalvationEncounter.Evade;
        }

        public static string GetEncounterString(GardenOfSalvationEncounter Encounter)
        {
            switch (Encounter)
            {
                case GardenOfSalvationEncounter.Evade: return "Evade the Consecrated Mind";
                case GardenOfSalvationEncounter.Summon: return "Summon the Consecrated Mind";
                case GardenOfSalvationEncounter.ConsecratedMind: return "Consecrated Mind";
                case GardenOfSalvationEncounter.SanctifiedMind: return "Sanctified Mind";
                default: return "Garden of Salvation";
            }
        }

        public static string GetChallengeString(GardenOfSalvationEncounter Encounter)
        {
            switch (Encounter)
            {
                case GardenOfSalvationEncounter.Evade: return "Staying Alive";
                case GardenOfSalvationEncounter.Summon: return "A Link to the Chain";
                case GardenOfSalvationEncounter.ConsecratedMind: return "To the Top";
                case GardenOfSalvationEncounter.SanctifiedMind: return "Zero to One Hundred";
                default: return "Garden of Salvation";
            }
        }

        public static string GetChallengeDescriptionString(GardenOfSalvationEncounter Encounter)
        {
            switch (Encounter)
            {
                case GardenOfSalvationEncounter.Evade: return "During the Evade encounter, players must not defeat any Cyclops, excluding all Cyclops after the last door.";
                case GardenOfSalvationEncounter.Summon: return "During the Summon encounter, all players must tether at the same time.";
                case GardenOfSalvationEncounter.ConsecratedMind: return "During the Consecrated Mind fight, players must only deposit ten motes at a time.";
                case GardenOfSalvationEncounter.SanctifiedMind: return "During the Sanctified Mind fight, players must fill each relay completely within ten seconds.";
                default: return "Garden of Salvation";
            }
        }

        public static EmbedBuilder GetRaidEmbed()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Raid Information",
                IconUrl = "https://www.bungie.net/img/destiny_content/pgcr/raid_garden_of_salvation.jpg",
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"The Black Garden"
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
                y.Value = $"DLC: Shadowkeep\n" +
                    $"Power: {DestinyEmote.Light}1100";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(GardenOfSalvationEncounter.Evade)}";
                y.Value = $"{DestinyEmote.RaidChallenge} {GetChallengeString(GardenOfSalvationEncounter.Evade)}\n" +
                    $"{GetChallengeDescriptionString(GardenOfSalvationEncounter.Evade)}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(GardenOfSalvationEncounter.Summon)}";
                y.Value = $"{DestinyEmote.RaidChallenge} {GetChallengeString(GardenOfSalvationEncounter.Summon)}\n" +
                    $"{GetChallengeDescriptionString(GardenOfSalvationEncounter.Summon)}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(GardenOfSalvationEncounter.ConsecratedMind)}";
                y.Value = $"{DestinyEmote.RaidChallenge} {GetChallengeString(GardenOfSalvationEncounter.ConsecratedMind)}\n" +
                    $"{GetChallengeDescriptionString(GardenOfSalvationEncounter.ConsecratedMind)}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(GardenOfSalvationEncounter.SanctifiedMind)}";
                y.Value = $"{DestinyEmote.RaidChallenge} {GetChallengeString(GardenOfSalvationEncounter.SanctifiedMind)}\n" +
                    $"{GetChallengeDescriptionString(GardenOfSalvationEncounter.SanctifiedMind)}";
                y.IsInline = false;
            });

            embed.Title = $"Garden of Salvation";
            embed.Description = $"Track the source of the Unknown Artifact's signal into the Black Garden.";

            embed.Url = "https://www.bungie.net/img/destiny_content/pgcr/raid_garden_of_salvation.jpg";
            embed.ThumbnailUrl = "https://www.bungie.net/common/destiny2_content/icons/6c13fd357e95348a3ab1892fc22ba3ac.png";

            return embed;
        }

        public static void AddUserTracking(ulong DiscordID, GardenOfSalvationEncounter Encounter)
        {
            GardenOfSalvationLinks.Add(new GardenOfSalvationLink() { DiscordID = DiscordID, Encounter = Encounter });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            GardenOfSalvationLinks.Remove(GetUserTracking(DiscordID, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static GardenOfSalvationLink GetUserTracking(ulong DiscordID, out GardenOfSalvationEncounter Encounter)
        {
            foreach (var Link in GardenOfSalvationLinks)
                if (Link.DiscordID == DiscordID)
                {
                    Encounter = Link.Encounter;
                    return Link;
                }
            Encounter = GardenOfSalvationEncounter.Evade;
            return null;
        }

        public static void CreateJSON()
        {
            GardenOfSalvationRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<GardenOfSalvationRotation>(json);
            }
            else
            {
                obj = new GardenOfSalvationRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new GardenOfSalvationRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(GardenOfSalvationEncounter Encounter)
        {
            GardenOfSalvationEncounter iterationEncounter = CurrentRotations.GoSChallengeEncounter;
            int WeeksUntil = 0;
            do
            {
                iterationEncounter = iterationEncounter == GardenOfSalvationEncounter.SanctifiedMind ? GardenOfSalvationEncounter.Evade : iterationEncounter + 1;
                WeeksUntil++;
            } while (iterationEncounter != Encounter);
            return CurrentRotations.WeeklyResetTimestamp.AddDays(WeeksUntil * 7); // Because there is no .AddWeeks().
        }
    }

    public enum GardenOfSalvationEncounter
    {
        Evade, // Staying Alive
        Summon, // A Link to the Chain
        ConsecratedMind, // To the Top
        SanctifiedMind, // Zero to One Hundred
    }
}
