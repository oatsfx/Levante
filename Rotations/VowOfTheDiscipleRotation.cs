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
        public static readonly int VaultOfGlassEncounterCount = 5;
        public static readonly string FilePath = @"Trackers/vowOfTheDisciple.json";

        [JsonProperty("VowOfTheDiscipleLinks")]
        public static List<VowOfTheDiscipleLink> VowOfTheDiscipleLinks { get; set; } = new List<VowOfTheDiscipleLink>();

        public class VowOfTheDiscipleLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("Encounter")]
            public VowOfTheDiscipleEncounter Encounter { get; set; } = VowOfTheDiscipleEncounter.One;
        }

        public static string GetEncounterString(VowOfTheDiscipleEncounter Encounter)
        {
            switch (Encounter)
            {
                default: return "The Vow of the Disciple";
            }
        }

        public static string GetChallengeString(VowOfTheDiscipleEncounter Encounter)
        {
            switch (Encounter)
            {
                default: return "The Vow of the Disciple";
            }
        }

        public static string GetChallengeDescriptionString(VowOfTheDiscipleEncounter Encounter)
        {
            switch (Encounter)
            {
                default: return "The Vow of the Disciple";
            }
        }

        public static EmbedBuilder GetRaidEmbed()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Raid Information",
                IconUrl = "API IMAGE URL",
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"LOCATION"
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
                y.Value = $"Power: {DestinyEmote.Light}LIGHT REQUIREMENT";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(VowOfTheDiscipleEncounter.One)}";
                y.Value = $"CHALLENGE EMOTE {GetChallengeString(VowOfTheDiscipleEncounter.One)}\n" +
                    $"{GetChallengeDescriptionString(VowOfTheDiscipleEncounter.One)}";
                y.IsInline = false;
            });

            embed.Title = $"The Vow of the Disciple";
            embed.Description = $"RAID DESCRIPTION";

            embed.Url = "https://www.bungie.net/img/destiny_content/pgcr/vault_of_glass.jpg";
            embed.ThumbnailUrl = "https://www.bungie.net/common/destiny2_content/icons/6d091410227eef82138a162df73065b9.png";

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
            Encounter = VowOfTheDiscipleEncounter.One;
            return null;
        }

        public static void CreateJSON()
        {
            VaultOfGlassRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<VaultOfGlassRotation>(json);
            }
            else
            {
                obj = new VaultOfGlassRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new VaultOfGlassRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(VaultOfGlassEncounter Encounter)
        {
            VaultOfGlassEncounter iterationEncounter = CurrentRotations.VoGChallengeEncounter;
            int WeeksUntil = 0;
            do
            {
                iterationEncounter = iterationEncounter == VaultOfGlassEncounter.Atheon ? VaultOfGlassEncounter.Confluxes : iterationEncounter + 1;
                WeeksUntil++;
            } while (iterationEncounter != Encounter);
            return CurrentRotations.WeeklyResetTimestamp.AddDays(WeeksUntil * 7); // Because there is no .AddWeeks().
        }
    }

    public enum VowOfTheDiscipleEncounter
    {
        One,
        Two,
        Three,
        Four,
        Boss,
    }
}
