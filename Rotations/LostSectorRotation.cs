using Levante.Configs;
using Levante.Util;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using BungieSharper.Entities.Destiny.Definitions;
using BungieSharper.Entities.Destiny.Definitions.Records;
using BungieSharper.Entities.Destiny;
using Levante.Helpers;
using System.Linq;
using System.Net.Http;
using Levante.Rotations.Abstracts;
using Levante.Rotations.Interfaces;

namespace Levante.Rotations
{
    public class LostSectorRotation : Rotation<>
    {
        public static readonly string FilePath = @"Trackers/lostSector.json";
        public static readonly string RotationFilePath = @"Rotations/lostSector.json";

        public static string GetArmorEmote(ExoticArmorType EAT)
        {
            return EAT switch
            {
                ExoticArmorType.Helmet => $"{DestinyEmote.Helmet}",
                ExoticArmorType.Legs => $"{DestinyEmote.Legs}",
                ExoticArmorType.Arms => $"{DestinyEmote.Arms}",
                ExoticArmorType.Chest => $"{DestinyEmote.Chest}",
                _ => "Lost Sector Armor Emote",
            };
        }

        public static EmbedBuilder GetLostSectorEmbed(int LS, LostSectorDifficulty LSD)
        {
            var LostSector = LostSectors[LS];

            var auth = new EmbedAuthorBuilder()
            {
                Name = $"{(LSD == LostSectorDifficulty.Legend ? "Legend" : "Master")} Lost Sector",
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"{LostSector.Location}"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };
            embed.AddField(y =>
            {
                y.Name = LSD == LostSectorDifficulty.Legend ? "Legend" : "Master";
                y.Value = $"Recommended Power: {DestinyEmote.Light}{GetLostSectorDifficultyLight(LSD)}\n" +
                    $"Burn: {DestinyEmote.MatchEmote(LostSector.Burn)}{LostSector.Burn}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = "Champions";
                y.Value = LostSector.GetChampions(LSD);
                y.IsInline = true;
            })
            .AddField(y =>
            {
                y.Name = "Modifiers";
                y.Value = LostSector.GetModifiers(LSD);
                y.IsInline = true;
            })
            .AddField(y =>
            {
                y.Name = "Shields";
                y.Value = LostSector.GetShields(LSD);
                y.IsInline = true;
            });

            embed.Title = $"{LostSector.Name}";
            embed.ImageUrl = LostSector.PGCRImage;
            embed.ThumbnailUrl = "https://www.bungie.net/common/destiny2_content/icons/6a2761d2475623125d896d1a424a91f9.png";

            return embed;
        }

        public static string GetLostSectorDifficultyLight(LostSectorDifficulty lsd)
        {
            return lsd switch
            {
                LostSectorDifficulty.Legend => "1830",
                LostSectorDifficulty.Master => "1840",
                _ => "",
            };
        }

        public static LostSectorPrediction DatePrediction(int LS, ExoticArmorType? ArmorType, int skip)
        {
            ExoticArmorType iterationEAT = CurrentRotations.LostSectorArmorDrop;
            int iterationLS = CurrentRotations.LostSector;
            int correctIterations = -1;
            int DaysUntil = 0;

            if (LS == -1 && ArmorType != null)
            {
                do
                {
                    iterationEAT = iterationEAT == ExoticArmorType.Chest ? ExoticArmorType.Helmet : iterationEAT + 1;
                    iterationLS = iterationLS == LostSectors.Count - 1 ? 0 : iterationLS + 1;
                    DaysUntil++;
                    if (iterationEAT == ArmorType)
                        correctIterations++;

                } while (skip != correctIterations);
            }
            else if (ArmorType == null && LS != -1)
            {
                do
                {
                    iterationEAT = iterationEAT == ExoticArmorType.Chest ? ExoticArmorType.Helmet : iterationEAT + 1;
                    iterationLS = iterationLS == LostSectors.Count - 1 ? 0 : iterationLS + 1;
                    DaysUntil++;
                    if (iterationLS == LS)
                        correctIterations++;

                } while (skip != correctIterations);
            }
            else if (ArmorType != null && LS != -1)
            {
                do
                {
                    iterationEAT = iterationEAT == ExoticArmorType.Chest ? ExoticArmorType.Helmet : iterationEAT + 1;
                    iterationLS = iterationLS == LostSectors.Count - 1 ? 0 : iterationLS + 1;
                    DaysUntil++;
                    if (iterationEAT == ArmorType && iterationLS == LS)
                        correctIterations++;

                } while (skip != correctIterations);
            }
            return new LostSectorPrediction { LostSector = LostSectors[iterationLS], ArmorDrop = iterationEAT, Date = CurrentRotations.DailyResetTimestamp.AddDays(DaysUntil) };
        }
    }

    public class LostSector
    {
        public string Name;
        public string Location;
        public string PGCRImage;
        [JsonProperty("LegendActivityHash")]
        public long LegendActivityHash;
        [JsonProperty("MasterActivityHash")]
        public long MasterActivityHash;
        [JsonProperty("Burn")]
        public string Burn;
        // <Champion Type, Count>
        [JsonProperty("LegendChampions")]
        public Dictionary<string, int> LegendChampions;
        [JsonProperty("MasterChampions")]
        public Dictionary<string, int> MasterChampions;
        // <Shield Type, Count>
        [JsonProperty("LegendShields")]
        public Dictionary<string, int> LegendShields;
        [JsonProperty("MasterShields")]
        public Dictionary<string, int> MasterShields;

        public List<string> LegendModifiers = new();
        public List<string> MasterModifiers = new();

        public string GetModifiers(LostSectorDifficulty Difficulty)
        {
            string result = "";
            if (Difficulty == LostSectorDifficulty.Legend)
                foreach (var modifier in LegendModifiers)
                    result += $"{DestinyEmote.MatchEmote(modifier.Replace(" ", "").Replace("-", "").Replace("'", "").Replace("!", ""))}{modifier}\n";
            else
                foreach (var modifier in MasterModifiers)
                    result += $"{DestinyEmote.MatchEmote(modifier.Replace(" ", "").Replace("-", "").Replace("'", "").Replace("!", ""))}{modifier}\n";

            return result;
        }

        public string GetChampions(LostSectorDifficulty Difficulty)
        {
            string result = "";
            if (Difficulty == LostSectorDifficulty.Legend)
                foreach (var champion in LegendChampions)
                    result += $"{DestinyEmote.MatchEmote(champion.Key)}{champion.Value} ({champion.Key})\n";
            else
                foreach (var champion in MasterChampions)
                    result += $"{DestinyEmote.MatchEmote(champion.Key)}{champion.Value} ({champion.Key})\n";

            return result;
        }

        public string GetShields(LostSectorDifficulty Difficulty)
        {
            string result = "";
            if (Difficulty == LostSectorDifficulty.Legend)
                foreach (var shield in LegendShields)
                    result += $"{DestinyEmote.MatchEmote(shield.Key)}{shield.Value}\n";
            else
                foreach (var shield in MasterShields)
                    result += $"{DestinyEmote.MatchEmote(shield.Key)}{shield.Value}\n";

            return result;
        }

        public override string ToString() => $"{Name} ({Location})";
    }

    public enum LostSectorDifficulty
    {
        Legend,
        Master
    }

    /*public enum ExoticArmorType
    {
        Helmet,
        Legs,
        Arms,
        Chest
    }*/

    public class ExoticArmor
    {
        [JsonProperty("Type")]
        public readonly string Type;

        [JsonProperty("ArmorEmote")]
        public readonly string ArmorEmote;
    }

    public class LostSectorLink : IRotationTracker
    {
        [JsonProperty("DiscordID")]
        public ulong DiscordID { get; set; } = 0;

        [JsonProperty("Encounter")]
        public int LostSector { get; set; } = 0;

        [JsonProperty("ArmorDrop")]
        public int ArmorDrop { get; set; } = 0;
    }

    public class LostSectorPrediction : IRotationPrediction
    {
        public DateTime Date { get; set; }
        public LostSector LostSector { get; set; }
        public ExoticArmor ExoticArmor { get; set; }
    }
}
