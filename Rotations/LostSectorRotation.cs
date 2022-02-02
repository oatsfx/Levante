using Levante.Configs;
using Levante.Util;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Rotations
{
    public class LostSectorRotation
    {
        public static readonly int LostSectorCount = 11;
        public static readonly string FilePath = @"Trackers/lostSector.json";

        [JsonProperty("LostSectorLinks")]
        public static List<LostSectorLink> LostSectorLinks { get; set; } = new List<LostSectorLink>();

        public class LostSectorLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("LostSector")]
            public LostSector? LostSector { get; set; } = 0;

            [JsonProperty("Difficulty")]
            public LostSectorDifficulty? Difficulty { get; set; } = LostSectorDifficulty.Legend;

            [JsonProperty("ArmorDrop")]
            public ExoticArmorType? ArmorDrop { get; set; } = ExoticArmorType.Helmet;
        }

        public static LostSector GetPredictedLegendLostSector(int Days)
        {
            return (LostSector)((((int)CurrentRotations.LegendLostSector) + Days) % LostSectorCount);
        }

        public static ExoticArmorType GetPredictedLegendLostSectorArmorDrop(int Days)
        {
            return (ExoticArmorType)((((int)CurrentRotations.LegendLostSectorArmorDrop) + Days) % 4);
        }

        public static LostSector GetPredictedMasterLostSector(int Days)
        {
            return (LostSector)((((int)CurrentRotations.MasterLostSector) + Days) % LostSectorCount);
        }

        public static ExoticArmorType GetPredictedMasterLostSectorArmorDrop(int Days)
        {
            return (ExoticArmorType)((((int)CurrentRotations.MasterLostSectorArmorDrop) + Days) % 4);
        }

        public static string GetLostSectorString(LostSector ls)
        {
            switch (ls)
            {
                case LostSector.BayOfDrownedWishes: return "Bay of Drowned Wishes";
                case LostSector.ChamberOfStarlight: return "Chamber of Starlight";
                case LostSector.AphelionsRest: return "Aphelion's Rest";
                case LostSector.TheEmptyTank: return "The Empty Tank";
                case LostSector.K1Logistics: return "K1 Logistics";
                case LostSector.K1Communion: return "K1 Communion";
                case LostSector.K1CrewQuarters: return "K1 Crew Quarters";
                case LostSector.K1Revelation: return "K1 Revelation";
                case LostSector.ConcealedVoid: return "Concealed Void";
                case LostSector.BunkerE15: return "Bunker E15";
                case LostSector.Perdition: return "Perdition";
                default: return "";
            }
        }

        public static string GetLostSectorLocationString(LostSector ls)
        {
            switch (ls)
            {
                case LostSector.BayOfDrownedWishes: return "Divalian Mists, The Dreaming City";
                case LostSector.ChamberOfStarlight: return "Rheasilvia, The Dreaming City";
                case LostSector.AphelionsRest: return "The Strand, The Dreaming City";
                case LostSector.TheEmptyTank: return "Thieves' Landing, The Tangled Shore";
                case LostSector.K1Logistics: return "Archer's Line, The Moon";
                case LostSector.K1Communion: return "Anchor of Light, The Moon";
                case LostSector.K1CrewQuarters: return "Hellmouth, The Moon";
                case LostSector.K1Revelation: return "Sorrow's Harbor, The Moon";
                case LostSector.ConcealedVoid: return "Asterion Abyss, Europa";
                case LostSector.BunkerE15: return "Eventide Ruins, Europa";
                case LostSector.Perdition: return "Cadmus Ridge, Europa";
                default: return "";
            }
        }

        public static string GetLostSectorBossString(LostSector ls)
        {
            switch (ls)
            {
                case LostSector.BayOfDrownedWishes: return "Yirskii, Subversive Captain";
                case LostSector.ChamberOfStarlight: return "Inkasi, Disciple of Quiria";
                case LostSector.AphelionsRest: return "Ur Haraak, Disciple of Quiria";
                case LostSector.TheEmptyTank: return "Azilis, Dusk Marauder";
                case LostSector.K1Logistics: return "Nightmare of Kelniks Reborn";
                case LostSector.K1Communion: return "Nightmare of Rizaahn, The Lost";
                case LostSector.K1CrewQuarters: return "Nightmare of Reyiks, Actuator";
                case LostSector.K1Revelation: return "Nightmare of Arguth, The Tormented";
                case LostSector.ConcealedVoid: return "Teliks, House Salvation";
                case LostSector.BunkerE15: return "Inquisitor Hydra";
                case LostSector.Perdition: return "Alkestis, Sacrificial Mind";
                default: return "";
            }
        }

        public static string GetLostSectorImageURL(LostSector ls)
        {
            switch (ls)
            {
                case LostSector.BayOfDrownedWishes: return "https://www.bungie.net/img/destiny_content/pgcr/dreaming_city_bay_of_drowned_wishes.jpg";
                case LostSector.ChamberOfStarlight: return "https://www.bungie.net/img/destiny_content/pgcr/dreaming_city_chamber_of_starlight.jpg";
                case LostSector.AphelionsRest: return "https://www.bungie.net/img/destiny_content/pgcr/dreaming_city_aphelions_rest.jpg";
                case LostSector.TheEmptyTank: return "https://www.bungie.net/img/destiny_content/pgcr/free_roam_tangled_shore.jpg";
                case LostSector.K1Logistics: return "https://www.bungie.net/img/destiny_content/pgcr/moon_k1_logistics.jpg";
                case LostSector.K1Communion: return "https://www.bungie.net/img/destiny_content/pgcr/moon_k1_communion.jpg";
                case LostSector.K1CrewQuarters: return "https://www.bungie.net/img/destiny_content/pgcr/moon_k1_crew_quarters.jpg";
                case LostSector.K1Revelation: return "https://www.bungie.net/img/destiny_content/pgcr/moon_k1_revelation.jpg";
                case LostSector.ConcealedVoid: return "https://www.bungie.net/img/destiny_content/pgcr/europa-lost-sector-frost.jpg";
                case LostSector.BunkerE15: return "https://www.bungie.net/img/destiny_content/pgcr/europa-lost-sector-overhang.jpg";
                case LostSector.Perdition: return "https://www.bungie.net/img/destiny_content/pgcr/europa-lost-sector-shear.jpg";
                default: return "";
            }
        }

        public static string GetArmorEmote(ExoticArmorType EAT)
        {
            switch (EAT)
            {
                case ExoticArmorType.Helmet: return $"{DestinyEmote.Helmet}";
                case ExoticArmorType.Legs: return $"{DestinyEmote.Legs}";
                case ExoticArmorType.Arms: return $"{DestinyEmote.Arms}";
                case ExoticArmorType.Chest: return $"{DestinyEmote.Chest}";
                default: return "Lost Sector Armor Emote";
            }
        }

        public static string GetLostSectorChampionsString(LostSector ls, LostSectorDifficulty lsd)
        {
            if (lsd == LostSectorDifficulty.Legend)
                switch (ls)
                {
                    case LostSector.BayOfDrownedWishes: return $"3 {DestinyEmote.Unstoppable} (Unstoppable)\n2 {DestinyEmote.Barrier} (Barrier)";
                    case LostSector.ChamberOfStarlight: return $"3 {DestinyEmote.Unstoppable} (Unstoppable)\n1 {DestinyEmote.Overload} (Overload)";
                    case LostSector.AphelionsRest: return $"2 {DestinyEmote.Unstoppable} (Unstoppable)\n2 {DestinyEmote.Overload} (Overload)";
                    case LostSector.TheEmptyTank: return $"2 {DestinyEmote.Barrier} (Barrier)\n2 {DestinyEmote.Overload} (Overload)";
                    case LostSector.K1Logistics: return $"3 {DestinyEmote.Barrier} (Barrier)\n2 {DestinyEmote.Overload} (Overload)";
                    case LostSector.K1Communion: return $"3 {DestinyEmote.Barrier} (Barrier)\n2 {DestinyEmote.Overload} (Overload)";
                    case LostSector.K1CrewQuarters: return $"3 {DestinyEmote.Barrier} (Barrier)\n2 {DestinyEmote.Overload} (Overload)";
                    case LostSector.K1Revelation: return $"3 {DestinyEmote.Unstoppable} (Upstoppable)\n4 {DestinyEmote.Barrier} (Barrier)";
                    case LostSector.ConcealedVoid: return $"1 {DestinyEmote.Barrier} (Barrier)\n3 {DestinyEmote.Overload} (Overload)";
                    case LostSector.BunkerE15: return $"1 {DestinyEmote.Barrier} (Barrier)\n4 {DestinyEmote.Overload} (Overload)";
                    case LostSector.Perdition: return $"2 {DestinyEmote.Barrier} (Barrier)\n2 {DestinyEmote.Overload} (Overload)";
                    default: return "";
                }
            else if (lsd == LostSectorDifficulty.Master)
                switch (ls)
                {
                    case LostSector.BayOfDrownedWishes: return $"3 {DestinyEmote.Unstoppable} (Unstoppable)\n3 {DestinyEmote.Barrier} (Barrier)";
                    case LostSector.ChamberOfStarlight: return $"6 {DestinyEmote.Unstoppable} (Unstoppable)\n3 {DestinyEmote.Overload} (Overload)";
                    case LostSector.AphelionsRest: return $"3 {DestinyEmote.Unstoppable} (Unstoppable)\n4 {DestinyEmote.Overload} (Overload)";
                    case LostSector.TheEmptyTank: return $"5 {DestinyEmote.Barrier} (Barrier)\n3 {DestinyEmote.Overload} (Overload)";
                    case LostSector.K1Logistics: return $"4 {DestinyEmote.Barrier} (Barrier)\n6 {DestinyEmote.Overload} (Overload)";
                    case LostSector.K1Communion: return $"4 {DestinyEmote.Barrier} (Barrier)\n6 {DestinyEmote.Overload} (Overload)";
                    case LostSector.K1CrewQuarters: return $"4 {DestinyEmote.Barrier} (Barrier)\n6 {DestinyEmote.Overload} (Overload)";
                    case LostSector.K1Revelation: return $"3 {DestinyEmote.Unstoppable} (Upstoppable)\n7 {DestinyEmote.Barrier} (Barrier)";
                    case LostSector.ConcealedVoid: return $"3 {DestinyEmote.Barrier} (Barrier)\n5 {DestinyEmote.Overload} (Overload)";
                    case LostSector.BunkerE15: return $"2 {DestinyEmote.Barrier} (Barrier)\n4 {DestinyEmote.Overload} (Overload)";
                    case LostSector.Perdition: return $"4 {DestinyEmote.Barrier} (Barrier)\n2 {DestinyEmote.Overload} (Overload)";
                    default: return "";
                }
            else
                return null;
        }

        public static string GetLostSectorShieldsString(LostSector ls, LostSectorDifficulty lsd)
        {
            if (lsd == LostSectorDifficulty.Legend)
                switch (ls)
                {
                    case LostSector.BayOfDrownedWishes: return $"1 {DestinyEmote.Void}";
                    case LostSector.ChamberOfStarlight: return $"2 {DestinyEmote.Solar}\n17 {DestinyEmote.Void}";
                    case LostSector.AphelionsRest: return $"9 {DestinyEmote.Void}";
                    case LostSector.TheEmptyTank: return $"1 {DestinyEmote.Arc}";
                    case LostSector.K1Logistics: return $"8 {DestinyEmote.Solar}\n3 {DestinyEmote.Arc}";
                    case LostSector.K1Communion: return $"1 {DestinyEmote.Solar}\n2 {DestinyEmote.Void}";
                    case LostSector.K1CrewQuarters: return $"10 {DestinyEmote.Solar}";
                    case LostSector.K1Revelation: return $"4 {DestinyEmote.Arc}";
                    case LostSector.ConcealedVoid: return $"2 {DestinyEmote.Solar}\n3 {DestinyEmote.Void}\n1 {DestinyEmote.Arc}";
                    case LostSector.BunkerE15: return $"2 {DestinyEmote.Void}";
                    case LostSector.Perdition: return $"2 {DestinyEmote.Void}\n22 {DestinyEmote.Arc}";
                    default: return "";
                }
            else if (lsd == LostSectorDifficulty.Master)
                switch (ls)
                {
                    case LostSector.BayOfDrownedWishes: return $"1 {DestinyEmote.Void}";
                    case LostSector.ChamberOfStarlight: return $"2 {DestinyEmote.Solar}\n23 {DestinyEmote.Void}";
                    case LostSector.AphelionsRest: return $"9 {DestinyEmote.Void}";
                    case LostSector.TheEmptyTank: return $"2 {DestinyEmote.Arc}";
                    case LostSector.K1Logistics: return $"8 {DestinyEmote.Solar}\n3 {DestinyEmote.Arc}";
                    case LostSector.K1Communion: return $"1 {DestinyEmote.Solar}";
                    case LostSector.K1CrewQuarters: return $"10 {DestinyEmote.Solar}";
                    case LostSector.K1Revelation: return $"1 {DestinyEmote.Arc}";
                    case LostSector.ConcealedVoid: return $"2 {DestinyEmote.Solar}\n3 {DestinyEmote.Void}";
                    case LostSector.BunkerE15: return $"2 {DestinyEmote.Void}";
                    case LostSector.Perdition: return $"2 {DestinyEmote.Void}\n22 {DestinyEmote.Arc}";
                    default: return "";
                }
            else
                return null;
        }

        public static string GetLostSectorModifiersString(LostSector ls, LostSectorDifficulty lsd)
        {
            string result = "";
            switch (ls)
            {
                case LostSector.BayOfDrownedWishes: result += "<:ArcBurn:900142018280456213> Arc Burn\n<:MatchGame:900142096013459516> Match Game\n<:LimitedRevives:900142086270115901> Limited Revives\n<:EquipmentLocked:900142051725819965> Equipment Locked\n<:RaiderShield:900142103697428562> Raider Shield"; break;
                case LostSector.ChamberOfStarlight: result += "<:SolarBurn:900142136996020224> Solar Burn\n<:MatchGame:900142096013459516> Match Game\n<:LimitedRevives:900142086270115901> Limited Revives\n<:EquipmentLocked:900142051725819965> Equipment Locked\n<:Epitaph:900142044368994315> Epitaph"; break;
                case LostSector.AphelionsRest: result += "<:VoidBurn:900142150338105404> Void Burn\n<:MatchGame:900142096013459516> Match Game\n<:LimitedRevives:900142086270115901> Limited Revives\n<:EquipmentLocked:900142051725819965> Equipment Locked\n<:Epitaph:900142044368994315> Epitaph"; break;
                case LostSector.TheEmptyTank: result += "<:SolarBurn:900142136996020224> Solar Burn\n<:MatchGame:900142096013459516> Match Game\n<:LimitedRevives:900142086270115901> Limited Revives\n<:EquipmentLocked:900142051725819965> Equipment Locked\n<:ArachNO:900142007840833576> Arach-NO!"; break;
                case LostSector.K1Logistics: result += "<:VoidBurn:900142150338105404> Void Burn\n<:MatchGame:900142096013459516> Match Game\n<:LimitedRevives:900142086270115901> Limited Revives\n<:EquipmentLocked:900142051725819965> Equipment Locked\n<:HotKnife:900142076673540096> Hot Knife"; break;
                case LostSector.K1Communion: result += "<:SolarBurn:900142136996020224> Solar Burn\n<:MatchGame:900142096013459516> Match Game\n<:LimitedRevives:900142086270115901> Limited Revives\n<:EquipmentLocked:900142051725819965> Equipment Locked\n<:ArachNO:900142007840833576> Arach-NO!"; break;
                case LostSector.K1CrewQuarters: result += "<:ArcBurn:900142018280456213> Arc Burn\n<:MatchGame:900142096013459516> Match Game\n<:LimitedRevives:900142086270115901> Limited Revives\n<:EquipmentLocked:900142051725819965> Equipment Locked\n<:HotKnife:900142076673540096> Hot Knife"; break;
                case LostSector.K1Revelation: result += "<:VoidBurn:900142150338105404> Void Burn\n<:MatchGame:900142096013459516> Match Game\n<:LimitedRevives:900142086270115901> Limited Revives\n<:EquipmentLocked:900142051725819965> Equipment Locked\n<:FirePit:900142069182521345> Fire Pit"; break;
                case LostSector.ConcealedVoid: result += "<:SolarBurn:900142136996020224> Solar Burn\n<:MatchGame:900142096013459516> Match Game\n<:LimitedRevives:900142086270115901> Limited Revives\n<:EquipmentLocked:900142051725819965> Equipment Locked\n<:ArachNO:900142007840833576> Arach-NO!"; break;
                case LostSector.BunkerE15: result += "<:VoidBurn:900142150338105404> Void Burn\n<:MatchGame:900142096013459516> Match Game\n<:LimitedRevives:900142086270115901> Limited Revives\n<:EquipmentLocked:900142051725819965> Equipment Locked\n<:Shocker:900142114426486844> Shocker"; break;
                case LostSector.Perdition: result += "<:ArcBurn:900142018280456213> Arc Burn\n<:MatchGame:900142096013459516> Match Game\n<:LimitedRevives:900142086270115901> Limited Revives\n<:EquipmentLocked:900142051725819965> Equipment Locked\n<:Shocker:900142114426486844> Shocker"; break;
                default: return "";
            }

            if (lsd == LostSectorDifficulty.Master)
            {
                switch (ls)
                {
                    case LostSector.BayOfDrownedWishes: result += "\n<:Chaff:900142037020598293> Chaff"; break;
                    case LostSector.ChamberOfStarlight: result += "\n<:Attrition:900142029487632395> Attrition"; break;
                    case LostSector.AphelionsRest: result += "\n<:Attrition:900142029487632395> Attrition"; break;
                    case LostSector.TheEmptyTank: result += "\n<:Chaff:900142037020598293> Chaff"; break;
                    case LostSector.K1Logistics: result += "\n<:Chaff:900142037020598293> Chaff"; break;
                    case LostSector.K1Communion: result += "\n<:Famine:900142061695672331> Famine"; break;
                    case LostSector.K1CrewQuarters: result += "\n<:Attrition:900142029487632395> Attrition"; break;
                    case LostSector.K1Revelation: result += "\n<:Chaff:900142037020598293> Chaff"; break;
                    case LostSector.ConcealedVoid: result += "\n<:Chaff:900142037020598293> Chaff"; break;
                    case LostSector.BunkerE15: result += "\n<:Attrition:900142029487632395> Attrition"; break;
                    case LostSector.Perdition: result += "\n<:Famine:900142061695672331> Famine"; break;
                    default: return "";
                }
            }

            return result;
        }

        public static EmbedBuilder GetLostSectorEmbed(LostSector LS, LostSectorDifficulty LSD, ExoticArmorType? EAT = null)
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"{(LSD == LostSectorDifficulty.Legend ? "Legend" : "Master")} Lost Sector",
                IconUrl = GetLostSectorImageURL(LS),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"{GetLostSectorLocationString(LS)}"
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
                y.Value = $"Recommended Power: {DestinyEmote.Light}{(LSD == LostSectorDifficulty.Legend ? GetLostSectorDifficultyLight(LostSectorDifficulty.Legend) : GetLostSectorDifficultyLight(LostSectorDifficulty.Master))}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = "Champions";
                y.Value = LSD == LostSectorDifficulty.Legend ? GetLostSectorChampionsString(LS, LostSectorDifficulty.Legend) : GetLostSectorChampionsString(LS, LostSectorDifficulty.Master);
                y.IsInline = true;
            })
            .AddField(y =>
            {
                y.Name = "Modifiers";
                y.Value = LSD == LostSectorDifficulty.Legend ? GetLostSectorModifiersString(LS, LostSectorDifficulty.Legend) : GetLostSectorModifiersString(LS, LostSectorDifficulty.Master);
                y.IsInline = true;
            })
            .AddField(y =>
            {
                y.Name = "Shields";
                y.Value = LSD == LostSectorDifficulty.Legend ? GetLostSectorShieldsString(LS, LostSectorDifficulty.Legend) : GetLostSectorShieldsString(LS, LostSectorDifficulty.Master);
                y.IsInline = true;
            });

            embed.Title = $"{GetLostSectorString(LS)}";
            embed.Description = $"Boss: {GetLostSectorBossString(LS)}";

            if (EAT != null)
                embed.Description += $"Armor Drop: {EAT}";

            embed.Url = GetLostSectorImageURL(LS);
            embed.ThumbnailUrl = "https://www.bungie.net/common/destiny2_content/icons/6a2761d2475623125d896d1a424a91f9.png";

            return embed;
        }

        public static string GetLostSectorDifficultyLight(LostSectorDifficulty lsd)
        {
            switch (lsd)
            {
                case LostSectorDifficulty.Legend: return "1320";
                case LostSectorDifficulty.Master: return "1350";
                default: return "";
            }
        }

        public static void AddUserTracking(ulong DiscordID, LostSector? LS, LostSectorDifficulty? LSD, ExoticArmorType? EAT = null)
        {
            LostSectorLinks.Add(new LostSectorLink() { DiscordID = DiscordID, LostSector = LS, Difficulty = LSD, ArmorDrop = EAT });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            LostSectorLinks.Remove(GetUserTracking(DiscordID, out _, out _, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static LostSectorLink GetUserTracking(ulong DiscordID, out LostSector? LS, out LostSectorDifficulty? LSD, out ExoticArmorType? EAT)
        {
            foreach (var Link in LostSectorLinks)
                if (Link.DiscordID == DiscordID)
                {
                    LS = Link.LostSector;
                    LSD = Link.Difficulty;
                    EAT = Link.ArmorDrop;
                    return Link;
                }
            LS = null;
            LSD = null;
            EAT = null;
            return null;
        }

        public static void CreateJSON()
        {
            LostSectorRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<LostSectorRotation>(json);
            }
            else
            {
                obj = new LostSectorRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new LostSectorRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        // This predicts for Legend Difficulty, add a day for Master Difficulty.
        public static DateTime DatePrediction(LostSector? LS, LostSectorDifficulty? LSD, ExoticArmorType? ArmorType)
        {
            ExoticArmorType iterationEAT = CurrentRotations.LegendLostSectorArmorDrop;
            LostSector iterationLS = CurrentRotations.LegendLostSector;
            int DaysUntil = LSD == LostSectorDifficulty.Master ? 1 : 0;

            // Special case where if the user happens to get tomorrow's Master Lost Sector.
            if (LSD == LostSectorDifficulty.Master && LS == iterationLS && ArmorType == iterationEAT)
                return CurrentRotations.DailyResetTimestamp.AddDays(DaysUntil);

            if (LS == null && ArmorType != null)
            {
                do
                {
                    iterationEAT = iterationEAT == ExoticArmorType.Chest ? ExoticArmorType.Helmet : iterationEAT + 1;
                    DaysUntil++;
                } while (iterationEAT != ArmorType);
            }
            else if (ArmorType == null && LS != null)
            {
                do
                {
                    iterationLS = iterationLS == LostSector.Perdition ? LostSector.BayOfDrownedWishes : iterationLS + 1;
                    DaysUntil++;
                } while (iterationLS != LS);
            }
            else if (ArmorType != null && LS != null)
            {
                do
                {
                    iterationEAT = iterationEAT == ExoticArmorType.Chest ? ExoticArmorType.Helmet : iterationEAT + 1;
                    iterationLS = iterationLS == LostSector.Perdition ? LostSector.BayOfDrownedWishes : iterationLS + 1;
                    DaysUntil++;
                } while (iterationEAT != ArmorType && iterationLS != LS);
            }
            return CurrentRotations.DailyResetTimestamp.AddDays(DaysUntil);
        }

        public static LostSector ActivityPrediction(DateTime Date, LostSectorDifficulty Difficulty, out ExoticArmorType ArmorDrop)
        {
            DateTime iterationDate = CurrentRotations.WeeklyResetTimestamp;
            ExoticArmorType iterationEAT;
            LostSector iterationLS;
            if (Difficulty == LostSectorDifficulty.Legend)
            {
                iterationEAT = CurrentRotations.LegendLostSectorArmorDrop;
                iterationLS = CurrentRotations.LegendLostSector;
            }
            else
            {
                iterationEAT = CurrentRotations.MasterLostSectorArmorDrop;
                iterationLS = CurrentRotations.MasterLostSector;
            }

            do
            {
                iterationEAT = iterationEAT == ExoticArmorType.Chest ? ExoticArmorType.Helmet : iterationEAT + 1;
                iterationLS = iterationLS == LostSector.Perdition ? LostSector.BayOfDrownedWishes : iterationLS + 1;
                iterationDate.AddDays(1);
            } while ((iterationDate - Date).Days >= 1);

            ArmorDrop = iterationEAT;
            return iterationLS;
        }
    }

    public enum LostSector
    {
        // Dreaming City
        BayOfDrownedWishes,
        ChamberOfStarlight,
        AphelionsRest,
        // Tangled Shore
        TheEmptyTank,
        // Luna
        K1Logistics,
        K1Communion,
        K1CrewQuarters,
        K1Revelation,
        // Europa
        ConcealedVoid,
        BunkerE15,
        Perdition,
    }

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
