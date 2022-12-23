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

namespace Levante.Rotations
{
    public class LostSectorRotation
    {
        public static readonly string FilePath = @"Trackers/lostSector.json";
        public static readonly string RotationFilePath = @"Rotations/lostSector.json";

        [JsonProperty("LostSectorLinks")]
        public static List<LostSectorLink> LostSectorLinks { get; set; } = new List<LostSectorLink>();

        [JsonProperty("LostSectors")]
        public static List<LostSector> LostSectors { get; set; } = new List<LostSector>();

        public class LostSectorLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("LostSector")]
            public int LostSector { get; set; } = 0;

            [JsonProperty("ArmorDrop")]
            public ExoticArmorType? ArmorDrop { get; set; } = ExoticArmorType.Helmet;
        }

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
                LostSectorDifficulty.Legend => "1580",
                LostSectorDifficulty.Master => "1610",
                _ => "",
            };
        }

        public static void AddUserTracking(ulong DiscordID, int LS, ExoticArmorType? EAT = null)
        {
            LostSectorLinks.Add(new LostSectorLink() { DiscordID = DiscordID, LostSector = LS, ArmorDrop = EAT });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            LostSectorLinks.Remove(GetUserTracking(DiscordID, out _, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static LostSectorLink GetUserTracking(ulong DiscordID, out int LS, out ExoticArmorType? EAT)
        {
            foreach (var Link in LostSectorLinks)
                if (Link.DiscordID == DiscordID)
                {
                    LS = Link.LostSector;
                    EAT = Link.ArmorDrop;
                    return Link;
                }
            LS = -1;
            EAT = null;
            return null;
        }

        public static void CreateJSON()
        {
            LostSectorRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                LostSectorLinks = JsonConvert.DeserializeObject<List<LostSectorLink>>(json);
            }
            else
            {
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(LostSectorLinks, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }

            if (File.Exists(RotationFilePath))
            {
                string json = File.ReadAllText(RotationFilePath);
                LostSectors = JsonConvert.DeserializeObject<List<LostSector>>(json);
            }
            else
            {
                File.WriteAllText(RotationFilePath, JsonConvert.SerializeObject(LostSectors, Formatting.Indented));
                Console.WriteLine($"No {RotationFilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            string output = JsonConvert.SerializeObject(LostSectorLinks, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(int LS, ExoticArmorType? ArmorType)
        {
            ExoticArmorType iterationEAT = CurrentRotations.LostSectorArmorDrop;
            int iterationLS = CurrentRotations.LostSector;
            int DaysUntil =  0;

            if (LS == -1 && ArmorType != null)
            {
                do
                {
                    iterationEAT = iterationEAT == ExoticArmorType.Chest ? ExoticArmorType.Helmet : iterationEAT + 1;
                    DaysUntil++;
                } while (iterationEAT != ArmorType);
            }
            else if (ArmorType == null && LS != -1)
            {
                do
                {
                    iterationLS = iterationLS == LostSectors.Count - 1 ? 0 : iterationLS + 1;
                    DaysUntil++;
                } while (iterationLS != LS);
            }
            else if (ArmorType != null && LS != -1)
            {
                do
                {
                    iterationEAT = iterationEAT == ExoticArmorType.Chest ? ExoticArmorType.Helmet : iterationEAT + 1;
                    iterationLS = iterationLS == LostSectors.Count - 1 ? 0 : iterationLS + 1;
                    DaysUntil++;
                } while (iterationEAT != ArmorType || iterationLS != LS);
            }
            return CurrentRotations.DailyResetTimestamp.AddDays(DaysUntil);
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
    }

    //public enum LostSector
    //{
    //    // Cosmodrome
    //    //VelesLabyrinth,
    //    //ExodusGarden2A,
    //    // Dreaming City
    //    //AphelionsRest,
    //    //BayOfDrownedWishes,
    //    //ChamberOfStarlight,
    //    // Tangled Shore
    //    //TheEmptyTank, // Gone
    //    // Luna
    //    //K1Revelation,
    //    //K1CrewQuarters,
    //    //K1Logistics,
    //    //K1Communion, // Gone
    //    // Throne World
    //    //Metamorphosis,
    //    //Sepulcher,
    //    //Extraction,
    //    // Europa
    //    //ConcealedVoid,
    //    //BunkerE15,
    //    //Perdition,

    //    // Season 17 (Season of the Haunted)
    //    // Luna
    //    K1CrewQuarters,
    //    K1Logistics,
    //    K1Revelation,
    //    K1Communion,
    //    // Nessus
    //    TheConflux,
    //    // Throne World
    //    Metamorphosis,
    //    Sepulcher,
    //    Extraction,
    //    // EDZ
    //    ExcavationSiteXII,
    //    SkydockIV,
    //    TheQuarry,
    //}

    public enum LostSectorDifficulty
    {
        Legend,
        Master
    }

    public enum ExoticArmorType
    {
        Helmet,
        Legs,
        Arms,
        Chest
    }
}
