using Levante.Configs;
using Levante.Util;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Rotations
{
    public class DeepStoneCryptRotation
    {
        public static readonly int DeepStoneCryptEncounterCount = 4;
        public static readonly string FilePath = @"Trackers/deepStoneCrypt.json";

        [JsonProperty("DeepStoneCryptLinks")]
        public static List<DeepStoneCryptLink> DeepStoneCryptLinks { get; set; } = new List<DeepStoneCryptLink>();

        public class DeepStoneCryptLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("Encounter")]
            public DeepStoneCryptEncounter Encounter { get; set; } = DeepStoneCryptEncounter.Security;
        }

        public static string GetEncounterString(DeepStoneCryptEncounter Encounter)
        {
            switch (Encounter)
            {
                case DeepStoneCryptEncounter.Security: return "Crypt Security";
                case DeepStoneCryptEncounter.Atraks1: return "Atraks-1";
                case DeepStoneCryptEncounter.Descent: return "The Descent";
                case DeepStoneCryptEncounter.Taniks: return "Taniks";
                default: return "Deep Stone Crypt";
            }
        }

        public static string GetChallengeString(DeepStoneCryptEncounter Encounter)
        {
            switch (Encounter)
            {
                case DeepStoneCryptEncounter.Security: return "Red Rover";
                case DeepStoneCryptEncounter.Atraks1: return "Copies of Copies";
                case DeepStoneCryptEncounter.Descent: return "Of All Trades";
                case DeepStoneCryptEncounter.Taniks: return "The Core Four";
                default: return "Deep Stone Crypt";
            }
        }

        public static string GetChallengeDescriptionString(DeepStoneCryptEncounter Encounter)
        {
            switch (Encounter)
            {
                case DeepStoneCryptEncounter.Security: return "During the Crypt Security encounter, while having the Operator Augment, each player must shoot two panels. This requires three phases.";
                case DeepStoneCryptEncounter.Atraks1: return "During the Atraks-1 fight, players must not open any airlocks on the top level.";
                case DeepStoneCryptEncounter.Descent: return "During the Descent encounter, players must use each Augment once.";
                case DeepStoneCryptEncounter.Taniks: return "During the Taniks fight, players must deposit four Nuclear Cores in the same phase.";
                default: return "Deep Stone Crypt";
            }
        }

        public static EmbedBuilder GetRaidEmbed()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Raid Information",
                IconUrl = "https://www.bungie.net/img/destiny_content/pgcr/europa-raid-deep-stone-crypt.jpg",
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Deep Stone Crypt, Europa"
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
                y.Value = $"DLC: Beyond Light\n" +
                    $"Power: {DestinyEmote.Light}1220";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(DeepStoneCryptEncounter.Security)}";
                y.Value = $"{DestinyEmote.RaidChallenge} {GetChallengeString(DeepStoneCryptEncounter.Security)}\n" +
                    $"{GetChallengeDescriptionString(DeepStoneCryptEncounter.Security)}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(DeepStoneCryptEncounter.Atraks1)}";
                y.Value = $"{DestinyEmote.RaidChallenge} {GetChallengeString(DeepStoneCryptEncounter.Atraks1)}\n" +
                    $"{GetChallengeDescriptionString(DeepStoneCryptEncounter.Atraks1)}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(DeepStoneCryptEncounter.Descent)}";
                y.Value = $"{DestinyEmote.RaidChallenge} {GetChallengeString(DeepStoneCryptEncounter.Descent)}\n" +
                    $"{GetChallengeDescriptionString(DeepStoneCryptEncounter.Descent)}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(DeepStoneCryptEncounter.Taniks)}";
                y.Value = $"{DestinyEmote.RaidChallenge} {GetChallengeString(DeepStoneCryptEncounter.Taniks)}\n" +
                    $"{GetChallengeDescriptionString(DeepStoneCryptEncounter.Taniks)}";
                y.IsInline = false;
            });

            embed.Title = $"Deep Stone Crypt";
            embed.Description = $"Purge the House of Salvation from the Deep Stone Crypt.";

            embed.Url = "https://www.bungie.net/img/destiny_content/pgcr/europa-raid-deep-stone-crypt.jpg";
            embed.ThumbnailUrl = "https://www.bungie.net/common/destiny2_content/icons/f71c1a6ab05d2c287352c8ee0aae644e.png";

            return embed;
        }

        public static void AddUserTracking(ulong DiscordID, DeepStoneCryptEncounter Encounter)
        {
            DeepStoneCryptLinks.Add(new DeepStoneCryptLink() { DiscordID = DiscordID, Encounter = Encounter });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            DeepStoneCryptLinks.RemoveAll(x => x.DiscordID == DiscordID);
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static DeepStoneCryptLink GetUserTracking(ulong DiscordID, out DeepStoneCryptEncounter Encounter)
        {
            foreach (var Link in DeepStoneCryptLinks)
                if (Link.DiscordID == DiscordID)
                {
                    Encounter = Link.Encounter;
                    return Link;
                }
            Encounter = DeepStoneCryptEncounter.Security;
            return null;
        }

        public static void CreateJSON()
        {
            DeepStoneCryptRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<DeepStoneCryptRotation>(json);
            }
            else
            {
                obj = new DeepStoneCryptRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new DeepStoneCryptRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(DeepStoneCryptEncounter Encounter)
        {
            DeepStoneCryptEncounter iterationEncounter = CurrentRotations.DSCChallengeEncounter;
            int WeeksUntil = 0;
            do
            {
                iterationEncounter = iterationEncounter == DeepStoneCryptEncounter.Taniks ? DeepStoneCryptEncounter.Security : iterationEncounter + 1;
                WeeksUntil++;
            } while (iterationEncounter != Encounter);
            return CurrentRotations.WeeklyResetTimestamp.AddDays(WeeksUntil * 7); // Because there is no .AddWeeks().
        }
    }

    public enum DeepStoneCryptEncounter
    {
        Security, // Red Rover
        Atraks1, // Copies of Copies
        Descent, // Of All Trades
        Taniks, // The Core Four
    }
}
