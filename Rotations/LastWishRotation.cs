using Levante.Configs;
using Levante.Util;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Rotations
{
    public class LastWishRotation
    {
        public static readonly int LastWishEncounterCount = 5;
        public static readonly string FilePath = @"Trackers/lastWish.json";

        [JsonProperty("LastWishLinks")]
        public static List<LastWishLink> LastWishLinks { get; set; } = new List<LastWishLink>();

        public class LastWishLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("Encounter")]
            public LastWishEncounter Encounter { get; set; } = LastWishEncounter.Kalli;
        }

        public static string GetEncounterString(LastWishEncounter Encounter)
        {
            switch (Encounter)
            {
                case LastWishEncounter.Kalli: return "Kalli";
                case LastWishEncounter.ShuroChi: return "Shuro Chi";
                case LastWishEncounter.Morgeth: return "Morgeth";
                case LastWishEncounter.Vault: return "Vault";
                case LastWishEncounter.Riven: return "Riven";
                default: return "Last Wish";
            }
        }

        public static string GetChallengeString(LastWishEncounter Encounter)
        {
            switch (Encounter)
            {
                case LastWishEncounter.Kalli: return "Summoning Ritual";
                case LastWishEncounter.ShuroChi: return "Which Witch";
                case LastWishEncounter.Morgeth: return "Forever Fight";
                case LastWishEncounter.Vault: return "Keep Out";
                case LastWishEncounter.Riven: return "Strength of Memory";
                default: return "Last Wish";
            }
        }

        public static string GetChallengeDescriptionString(LastWishEncounter Encounter)
        {
            switch (Encounter)
            {
                case LastWishEncounter.Kalli: return "During the Kalli Fight, players must capture all of nine plates and kill the Taken Ogres that spawn.";
                case LastWishEncounter.ShuroChi: return "During the Shuro Chi fight, players must avoid Shuro Chi's ranged attack.";
                case LastWishEncounter.Morgeth: return "During the Morgeth fight, players must not defeat any ogres, besides Morgeth.";
                case LastWishEncounter.Vault: return "During the Vault encounter, players must prevent all \"Might of Riven\" enemies from entering the center room.";
                case LastWishEncounter.Riven: return "During the Riven fight, players must not shoot the same eye more than once.";
                default: return "Last Wish";
            }
        }

        public static EmbedBuilder GetRaidEmbed()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Raid Information",
                IconUrl = "https://www.bungie.net/img/destiny_content/pgcr/raid_beanstalk.jpg",
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"The Dreaming City, The Reef"
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
                y.Value = $"DLC: Forsaken\n" +
                    $"Power: {DestinyEmote.Light}1100";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(LastWishEncounter.Kalli)}";
                y.Value = $"{DestinyEmote.RaidChallenge} {GetChallengeString(LastWishEncounter.Kalli)}\n" +
                    $"{GetChallengeDescriptionString(LastWishEncounter.Kalli)}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(LastWishEncounter.ShuroChi)}";
                y.Value = $"{DestinyEmote.RaidChallenge} {GetChallengeString(LastWishEncounter.ShuroChi)}\n" +
                    $"{GetChallengeDescriptionString(LastWishEncounter.ShuroChi)}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(LastWishEncounter.Morgeth)}";
                y.Value = $"{DestinyEmote.RaidChallenge} {GetChallengeString(LastWishEncounter.Morgeth)}\n" +
                    $"{GetChallengeDescriptionString(LastWishEncounter.Morgeth)}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(LastWishEncounter.Vault)}";
                y.Value = $"{DestinyEmote.RaidChallenge} {GetChallengeString(LastWishEncounter.Vault)}\n" +
                    $"{GetChallengeDescriptionString(LastWishEncounter.Vault)}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(LastWishEncounter.Riven)}";
                y.Value = $"{DestinyEmote.RaidChallenge} {GetChallengeString(LastWishEncounter.Riven)}\n" +
                    $"{GetChallengeDescriptionString(LastWishEncounter.Riven)}";
                y.IsInline = false;
            });

            embed.Title = $"Last Wish";
            embed.Description = $"Put an end to the Taken curse within the Dreaming City through killing Riven of a Thousand Voices.";

            embed.Url = "https://www.bungie.net/img/destiny_content/pgcr/raid_beanstalk.jpg";
            embed.ThumbnailUrl = "https://www.bungie.net/common/destiny2_content/icons/fc5791eb2406bf5e6b361f3d16596693.png";

            return embed;
        }

        public static void AddUserTracking(ulong DiscordID, LastWishEncounter Encounter)
        {
            LastWishLinks.Add(new LastWishLink() { DiscordID = DiscordID, Encounter = Encounter });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            LastWishLinks.Remove(GetUserTracking(DiscordID, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static LastWishLink GetUserTracking(ulong DiscordID, out LastWishEncounter Encounter)
        {
            foreach (var Link in LastWishLinks)
                if (Link.DiscordID == DiscordID)
                {
                    Encounter = Link.Encounter;
                    return Link;
                }
            Encounter = LastWishEncounter.Kalli;
            return null;
        }

        public static void CreateJSON()
        {
            LastWishRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<LastWishRotation>(json);
            }
            else
            {
                obj = new LastWishRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new LastWishRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(LastWishEncounter Encounter)
        {
            LastWishEncounter iterationEncounter = CurrentRotations.LWChallengeEncounter;
            int WeeksUntil = 0;
            do
            {
                iterationEncounter = iterationEncounter == LastWishEncounter.Riven ? LastWishEncounter.Kalli : iterationEncounter + 1;
                WeeksUntil++;
            } while (iterationEncounter != Encounter);
            return CurrentRotations.WeeklyResetTimestamp.AddDays(WeeksUntil * 7); // Because there is no .AddWeeks().
        }
    }

    public enum LastWishEncounter
    {
        Kalli, // Summoning Ritual
        ShuroChi, // Which Witch
        Morgeth, // Forever Fight
        Vault, // Keep Out
        Riven, // Strength of Memory
    }
}
