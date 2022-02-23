using Levante.Configs;
using Levante.Util;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Rotations
{
    public class VaultOfGlassRotation
    {
        public static readonly int VaultOfGlassEncounterCount = 5;
        public static readonly string FilePath = @"Trackers/vaultOfGlass.json";

        [JsonProperty("VaultOfGlassLinks")]
        public static List<VaultOfGlassLink> VaultOfGlassLinks { get; set; } = new List<VaultOfGlassLink>();

        public class VaultOfGlassLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("Encounter")]
            public VaultOfGlassEncounter Encounter { get; set; } = VaultOfGlassEncounter.Confluxes;
        }

        public static string GetEncounterString(VaultOfGlassEncounter Encounter)
        {
            switch (Encounter)
            {
                case VaultOfGlassEncounter.Confluxes: return "Confluxes";
                case VaultOfGlassEncounter.Oracles: return "Oracles";
                case VaultOfGlassEncounter.Templar: return "Templar";
                case VaultOfGlassEncounter.Gatekeepers: return "Gatekeepers";
                case VaultOfGlassEncounter.Atheon: return "Atheon";
                default: return "Vault of Glass";
            }
        }

        public static string GetChallengeString(VaultOfGlassEncounter Encounter)
        {
            switch (Encounter)
            {
                case VaultOfGlassEncounter.Confluxes: return "Wait for It...";
                case VaultOfGlassEncounter.Oracles: return "The Only Oracle for You";
                case VaultOfGlassEncounter.Templar: return "Out of Its Way";
                case VaultOfGlassEncounter.Gatekeepers: return "Strangers in Time";
                case VaultOfGlassEncounter.Atheon: return "Ensemble's Refrain";
                default: return "Vault of Glass";
            }
        }

        public static string GetChallengeRewardString(VaultOfGlassEncounter Encounter)
        {
            switch (Encounter)
            {
                case VaultOfGlassEncounter.Confluxes: return "Vision of Confluence (Timelost)";
                case VaultOfGlassEncounter.Oracles: return "Praedyth's Revenge (Timelost)";
                case VaultOfGlassEncounter.Templar: return "Fatebringer (Timelost)";
                case VaultOfGlassEncounter.Gatekeepers: return "Hezen Vengeance (Timelost)";
                case VaultOfGlassEncounter.Atheon: return "Corrective Measure (Timelost)";
                default: return "Vault of Glass Weapon";
            }
        }

        public static string GetChallengeDescriptionString(VaultOfGlassEncounter Encounter)
        {
            switch (Encounter)
            {
                case VaultOfGlassEncounter.Confluxes: return "During the Confluxes encounter, players must defeat the Wyverns while they are sacrificing to a conflux.";
                case VaultOfGlassEncounter.Oracles: return "During the Oracles encounter, players must not shoot the same oracle more than once.";
                case VaultOfGlassEncounter.Templar: return "During the Templar fight, players must prevent the Templar from teleporting.";
                case VaultOfGlassEncounter.Gatekeepers: return "During the Gatekeepers encounter, players must defeat the Praetorian and Wyvern at the same time.";
                case VaultOfGlassEncounter.Atheon: return "During the Atheon fight, players that are teleported must shoot exactly one oracle per wave.";
                default: return "Vault of Glass";
            }
        }

        public static EmbedBuilder GetRaidEmbed()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Raid Information: The Vow of the Disciple",
                IconUrl = "https://www.bungie.net/img/destiny_content/pgcr/vault_of_glass.jpg",
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Ishtar Sink, Venus"
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
                y.Value = $"Power: {DestinyEmote.Light}1300";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(VaultOfGlassEncounter.Confluxes)}";
                y.Value = $"{DestinyEmote.VoGRaidChallenge} {GetChallengeString(VaultOfGlassEncounter.Confluxes)}\n" +
                    $"{GetChallengeDescriptionString(VaultOfGlassEncounter.Confluxes)}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(VaultOfGlassEncounter.Oracles)}";
                y.Value = $"{DestinyEmote.VoGRaidChallenge} {GetChallengeString(VaultOfGlassEncounter.Oracles)}\n" +
                    $"{GetChallengeDescriptionString(VaultOfGlassEncounter.Oracles)}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(VaultOfGlassEncounter.Templar)}";
                y.Value = $"{DestinyEmote.VoGRaidChallenge} {GetChallengeString(VaultOfGlassEncounter.Templar)}\n" +
                    $"{GetChallengeDescriptionString(VaultOfGlassEncounter.Templar)}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(VaultOfGlassEncounter.Gatekeepers)}";
                y.Value = $"{DestinyEmote.VoGRaidChallenge} {GetChallengeString(VaultOfGlassEncounter.Gatekeepers)}\n" +
                    $"{GetChallengeDescriptionString(VaultOfGlassEncounter.Gatekeepers)}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = $"{GetEncounterString(VaultOfGlassEncounter.Atheon)}";
                y.Value = $"{DestinyEmote.VoGRaidChallenge} {GetChallengeString(VaultOfGlassEncounter.Atheon)}\n" +
                    $"{GetChallengeDescriptionString(VaultOfGlassEncounter.Atheon)}";
                y.IsInline = false;
            });

            embed.Title = $"Vault of Glass";
            embed.Description = $"Stop the Vex from spreading through all of time through destroying the source of their operation.";

            embed.Url = "https://www.bungie.net/img/destiny_content/pgcr/vault_of_glass.jpg";
            embed.ThumbnailUrl = "https://www.bungie.net/common/destiny2_content/icons/6d091410227eef82138a162df73065b9.png";

            return embed;
        }

        public static void AddUserTracking(ulong DiscordID, VaultOfGlassEncounter Encounter)
        {
            VaultOfGlassLinks.Add(new VaultOfGlassLink() { DiscordID = DiscordID, Encounter = Encounter });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            VaultOfGlassLinks.Remove(GetUserTracking(DiscordID, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static VaultOfGlassLink GetUserTracking(ulong DiscordID, out VaultOfGlassEncounter Encounter)
        {
            foreach (var Link in VaultOfGlassLinks)
                if (Link.DiscordID == DiscordID)
                {
                    Encounter = Link.Encounter;
                    return Link;
                }
            Encounter = VaultOfGlassEncounter.Confluxes;
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

    public enum VaultOfGlassEncounter
    {
        Confluxes, // Vision of Confluence, Wait for It...
        Oracles, // Praedyth's Revenge, The Only Oracle for You
        Templar, // Fatebringer, Out of Its Way
        Gatekeepers, // Hezen Vengeance, Strangers in Time
        Atheon, // Corrective Measure, Ensemble's Refrain
    }
}
