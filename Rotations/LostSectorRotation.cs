using DestinyUtility.Configs;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DestinyUtility.Rotations
{
    public class LostSectorRotation
    {
        public static readonly int LostSectorCount = 11;

        public static void LostSectorChange()
        {
            CurrentLegendLostSector = CurrentLegendLostSector == LostSector.Perdition ? LostSector.BayOfDrownedWishes : CurrentLegendLostSector + 1;
            CurrentLegendArmorDrop = CurrentLegendArmorDrop == ExoticArmorType.Chest ? ExoticArmorType.Helmet : CurrentLegendArmorDrop + 1;
            UpdateLostSectorsJSON();
        }

        [JsonProperty("CurrentLegendLostSector")]
        public static LostSector CurrentLegendLostSector = LostSector.BayOfDrownedWishes;

        [JsonProperty("CurrentLegendArmorDrop")]
        public static ExoticArmorType CurrentLegendArmorDrop = ExoticArmorType.Arms;

        [JsonProperty("LostSectorLinks")]
        public static List<LostSectorLink> LostSectorLinks { get; set; } = new List<LostSectorLink>();

        [JsonProperty("AnnounceLostSectorUpdates")]
        public static List<ulong> AnnounceLostSectorUpdates { get; set; } = new List<ulong>();

        public class LostSectorLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("LostSector")]
            public LostSector LostSector { get; set; } = LostSector.AphelionsRest;

            [JsonProperty("Difficulty")]
            public LostSectorDifficulty Difficulty { get; set; } = LostSectorDifficulty.Legend;

            [JsonProperty("ArmorDrop")]
            public ExoticArmorType ArmorDrop { get; set; } = ExoticArmorType.Helmet;
        }

        public static LostSector GetLegendLostSector() => CurrentLegendLostSector;
        public static ExoticArmorType GetLegendLostSectorArmorDrop() => CurrentLegendArmorDrop;

        public static LostSector GetMasterLostSector() => CurrentLegendLostSector - 1 < 0 ? LostSector.Perdition : CurrentLegendLostSector - 1;
        public static ExoticArmorType GetMasterLostSectorArmorDrop() => CurrentLegendArmorDrop - 1 < 0 ? ExoticArmorType.Chest : CurrentLegendArmorDrop - 1;

        public static LostSector GetPredictedLegendLostSector(int Days)
        {
            return (LostSector)((((int)GetLegendLostSector()) + Days) % LostSectorCount);
        }

        public static ExoticArmorType GetPredictedLegendLostSectorArmorDrop(int Days)
        {
            return (ExoticArmorType)((((int)GetLegendLostSectorArmorDrop()) + Days) % 4);
        }

        public static LostSector GetPredictedMasterLostSector(int Days)
        {
            return (LostSector)((((int)GetMasterLostSector()) + Days) % LostSectorCount);
        }

        public static ExoticArmorType GetPredictedMasterLostSectorArmorDrop(int Days)
        {
            return (ExoticArmorType)((((int)GetMasterLostSectorArmorDrop()) + Days) % 4);
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

        public static string GetLostSectorChampionsString(LostSector ls, LostSectorDifficulty lsd)
        {
            if (lsd == LostSectorDifficulty.Legend)
                switch (ls)
                {
                    case LostSector.BayOfDrownedWishes: return "3 <:Unstoppable:900045499162320896> (Unstoppable)\n2 <:Barrier:900045481319731221> (Barrier)";
                    case LostSector.ChamberOfStarlight: return "3 <:Unstoppable:900045499162320896> (Unstoppable)\n1 <:Overload:900045490987610172> (Overload)";
                    case LostSector.AphelionsRest: return "2 <:Unstoppable:900045499162320896> (Unstoppable)\n2 <:Overload:900045490987610172> (Overload)";
                    case LostSector.TheEmptyTank: return "2 <:Barrier:900045481319731221> (Barrier)\n2 <:Overload:900045490987610172> (Overload)";
                    case LostSector.K1Logistics: return "3 <:Barrier:900045481319731221> (Barrier)\n2 <:Overload:900045490987610172> (Overload)";
                    case LostSector.K1Communion: return "3 <:Barrier:900045481319731221> (Barrier)\n2 <:Overload:900045490987610172> (Overload)";
                    case LostSector.K1CrewQuarters: return "3 <:Barrier:900045481319731221> (Barrier)\n2 <:Overload:900045490987610172> (Overload)";
                    case LostSector.K1Revelation: return "3 <:Unstoppable:900045499162320896> (Upstoppable)\n4 <:Barrier:900045481319731221> (Barrier)";
                    case LostSector.ConcealedVoid: return "1 <:Barrier:900045481319731221> (Barrier)\n3 <:Overload:900045490987610172> (Overload)";
                    case LostSector.BunkerE15: return "1 <:Barrier:900045481319731221> (Barrier)\n4 <:Overload:900045490987610172> (Overload)";
                    case LostSector.Perdition: return "2 <:Barrier:900045481319731221> (Barrier)\n2 <:Overload:900045490987610172> (Overload)";
                    default: return "";
                }
            else if (lsd == LostSectorDifficulty.Master)
                switch (ls)
                {
                    case LostSector.BayOfDrownedWishes: return "3 <:Unstoppable:900045499162320896> (Unstoppable)\n3 <:Barrier:900045481319731221> (Barrier)";
                    case LostSector.ChamberOfStarlight: return "6 <:Unstoppable:900045499162320896> (Unstoppable)\n3 <:Overload:900045490987610172> (Overload)";
                    case LostSector.AphelionsRest: return "3 <:Unstoppable:900045499162320896> (Unstoppable)\n4 <:Overload:900045490987610172> (Overload)";
                    case LostSector.TheEmptyTank: return "5 <:Barrier:900045481319731221> (Barrier)\n3 <:Overload:900045490987610172> (Overload)";
                    case LostSector.K1Logistics: return "4 <:Barrier:900045481319731221> (Barrier)\n6 <:Overload:900045490987610172> (Overload)";
                    case LostSector.K1Communion: return "4 <:Barrier:900045481319731221> (Barrier)\n6 <:Overload:900045490987610172> (Overload)";
                    case LostSector.K1CrewQuarters: return "4 <:Barrier:900045481319731221> (Barrier)\n6 <:Overload:900045490987610172> (Overload)";
                    case LostSector.K1Revelation: return "3 <:Unstoppable:900045499162320896> (Upstoppable)\n7 <:Barrier:900045481319731221> (Barrier)";
                    case LostSector.ConcealedVoid: return "3 <:Barrier:900045481319731221> (Barrier)\n5 <:Overload:900045490987610172> (Overload)";
                    case LostSector.BunkerE15: return "2 <:Barrier:900045481319731221> (Barrier)\n4 <:Overload:900045490987610172> (Overload)";
                    case LostSector.Perdition: return "4 <:Barrier:900045481319731221> (Barrier)\n2 <:Overload:900045490987610172> (Overload)";
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
                    case LostSector.BayOfDrownedWishes: return "1 <:Void:900144180288958474>";
                    case LostSector.ChamberOfStarlight: return "2 <:Solar:900144171267022888>\n17 <:Void:900144180288958474>";
                    case LostSector.AphelionsRest: return "9 <:Void:900144180288958474>";
                    case LostSector.TheEmptyTank: return "1 <:Arc:900144187696099359>";
                    case LostSector.K1Logistics: return "8 <:Solar:900144171267022888>\n3 <:Arc:900144187696099359>";
                    case LostSector.K1Communion: return "1 <:Solar:900144171267022888>\n2 <:Void:900144180288958474>";
                    case LostSector.K1CrewQuarters: return "10 <:Solar:900144171267022888>";
                    case LostSector.K1Revelation: return "4 <:Arc:900144187696099359>";
                    case LostSector.ConcealedVoid: return "2 <:Solar:900144171267022888>\n3 <:Void:900144180288958474>\n1 <:Arc:900144187696099359>";
                    case LostSector.BunkerE15: return "2 <:Void:900144180288958474>";
                    case LostSector.Perdition: return "2 <:Void:900144180288958474>\n22 <:Arc:900144187696099359>";
                    default: return "";
                }
            else if (lsd == LostSectorDifficulty.Master)
                switch (ls)
                {
                    case LostSector.BayOfDrownedWishes: return "1 <:Void:900144180288958474>";
                    case LostSector.ChamberOfStarlight: return "2 <:Solar:900144171267022888>\n23 <:Void:900144180288958474>";
                    case LostSector.AphelionsRest: return "9 <:Void:900144180288958474>";
                    case LostSector.TheEmptyTank: return "2 <:Arc:900144187696099359>";
                    case LostSector.K1Logistics: return "8 <:Solar:900144171267022888>\n3 <:Arc:900144187696099359>";
                    case LostSector.K1Communion: return "1 <:Solar:900144171267022888>";
                    case LostSector.K1CrewQuarters: return "10 <:Solar:900144171267022888>";
                    case LostSector.K1Revelation: return "1 <:Arc:900144187696099359>";
                    case LostSector.ConcealedVoid: return "2 <:Solar:900144171267022888>\n3 <:Void:900144180288958474>";
                    case LostSector.BunkerE15: return "2 <:Void:900144180288958474>";
                    case LostSector.Perdition: return "2 <:Void:900144180288958474>\n22 <:Arc:900144187696099359>";
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
                y.Value = $"Recommended Power: <:LightLevel:844029708077367297>{(LSD == LostSectorDifficulty.Legend ? GetLostSectorDifficultyLight(LostSectorDifficulty.Legend) : GetLostSectorDifficultyLight(LostSectorDifficulty.Master))}";
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

        public static LostSector? ParseLostSectorString(string LS)
        {
            if (LS.ToLower().Contains("bay") || LS.ToLower().Contains("drowned") || LS.ToLower().Contains("wishes"))
                return LostSector.BayOfDrownedWishes;
            else if (LS.ToLower().Contains("chamber") || LS.ToLower().Contains("starlight"))
                return LostSector.ChamberOfStarlight;
            else if (LS.ToLower().Contains("aphelion") || LS.ToLower().Contains("rest"))
                return LostSector.AphelionsRest;
            else if (LS.ToLower().Contains("empty") || LS.ToLower().Contains("tank"))
                return LostSector.TheEmptyTank;
            else if (LS.ToLower().Contains("k1"))
            {
                if (LS.ToLower().Contains("logistics"))
                    return LostSector.K1Logistics;
                else if (LS.ToLower().Contains("communion"))
                    return LostSector.K1Communion;
                else if (LS.ToLower().Contains("crew") || LS.ToLower().Contains("quarters"))
                    return LostSector.K1CrewQuarters;
                else if (LS.ToLower().Contains("revelation"))
                    return LostSector.K1Revelation;
                else
                    return null;
            }
            else if (LS.ToLower().Contains("concealed") || LS.ToLower().Contains("void"))
                return LostSector.ConcealedVoid;
            else if (LS.ToLower().Contains("bunker") || LS.ToLower().Contains("e15"))
                return LostSector.BunkerE15;
            else if (LS.ToLower().Contains("perdition"))
                return LostSector.Perdition;
            else
                return null;
        }

        public enum LostSectorDifficulty
        {
            Legend,
            Master
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

        public static ExoticArmorType? ParseArmorString(string Armor)
        {
            if (Armor.ToLower().Contains("head") || Armor.ToLower().Contains("helmet"))
                return ExoticArmorType.Helmet;
            else if (Armor.ToLower().Contains("arms") || Armor.ToLower().Contains("gauntlets"))
                return ExoticArmorType.Arms;
            else if (Armor.ToLower().Contains("legs") || Armor.ToLower().Contains("boots"))
                return ExoticArmorType.Legs;
            else if (Armor.ToLower().Contains("chest") || Armor.ToLower().Contains("robe"))
                return ExoticArmorType.Chest;
            else
                return null;
        }

        public static int DaysUntilNextOccurance(LostSector? LS = null, ExoticArmorType? ArmorType = null)
        {
            ExoticArmorType iterationEAT = CurrentLegendArmorDrop;
            LostSector iterationLS = CurrentLegendLostSector;
            if (LS == null && ArmorType != null)
            {
                int iterationCount = 0;
                while (iterationEAT != ArmorType)
                {
                    iterationEAT = iterationEAT == ExoticArmorType.Chest ? ExoticArmorType.Helmet : iterationEAT + 1;
                    iterationCount++;
                }
                return iterationCount;
            }
            else if (ArmorType == null && LS != null)
            {
                int iterationCount = 0;
                while (iterationLS != LS)
                {
                    iterationLS = iterationLS == LostSector.Perdition ? LostSector.BayOfDrownedWishes : iterationLS + 1;
                    iterationCount++;
                }
                return iterationCount;
            }
            else if (ArmorType != null && LS != null)
            {
                int iterationCount = 0;
                bool dayFound = false;
                while (!dayFound)
                {
                    iterationEAT = iterationEAT == ExoticArmorType.Chest ? ExoticArmorType.Helmet : iterationEAT + 1;
                    iterationLS = iterationLS == LostSector.Perdition ? LostSector.BayOfDrownedWishes : iterationLS + 1;
                    iterationCount++;
                    if (iterationEAT == ArmorType && iterationLS == LS)
                    {
                        dayFound = true;
                    }
                }
                return iterationCount;
            }
            return -1;
        }

        public enum ExoticArmorType
        {
            Helmet,
            Legs,
            Arms,
            Chest
        }

        #region JSONFileManagement

        public static void AddChannelToLostSectorUpdates(ulong ChannelID)
        {
            string json = File.ReadAllText(DestinyUtilityCord.LostSectorTrackingConfigPath);
            AnnounceLostSectorUpdates.Clear();
            LostSectorLinks.Clear();
            LostSectorRotation jsonObj = JsonConvert.DeserializeObject<LostSectorRotation>(json);

            AnnounceLostSectorUpdates.Add(ChannelID);
            LostSectorRotation ac = new LostSectorRotation();
            string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.LostSectorTrackingConfigPath, output);
        }

        public static void RemoveChannelFromLostSectorUpdates(ulong ChannelID)
        {
            string json = File.ReadAllText(DestinyUtilityCord.LostSectorTrackingConfigPath);
            AnnounceLostSectorUpdates.Clear();
            LostSectorLinks.Clear();
            LostSectorRotation ac = JsonConvert.DeserializeObject<LostSectorRotation>(json);
            for (int i = 0; i < AnnounceLostSectorUpdates.Count; i++)
                if (AnnounceLostSectorUpdates[i] == ChannelID)
                    AnnounceLostSectorUpdates.RemoveAt(i);
            string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.LostSectorTrackingConfigPath, output);
        }

        public static bool IsExistingChannelForLostSectorUpdates(ulong ChannelID)
        {
            for (int i = 0; i < AnnounceLostSectorUpdates.Count; i++)
                if (AnnounceLostSectorUpdates[i] == ChannelID)
                    return true;

            return false;
        }

        public static void UpdateLostSectorsList()
        {
            string json = File.ReadAllText(DestinyUtilityCord.LostSectorTrackingConfigPath);
            AnnounceLostSectorUpdates.Clear();
            LostSectorLinks.Clear();
            LostSectorRotation jsonObj = JsonConvert.DeserializeObject<LostSectorRotation>(json);
        }

        public static void UpdateLostSectorsJSON()
        {
            LostSectorRotation ac = new LostSectorRotation();
            string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.LostSectorTrackingConfigPath, output);
        }

        public static void ClearLostSectorsList()
        {
            AnnounceLostSectorUpdates.Clear();
            LostSectorLinks.Clear();
            string output = JsonConvert.SerializeObject(new LostSectorRotation(), Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.LostSectorTrackingConfigPath, output);
        }

        public static void AddLostSectorsTrackingToConfig(ulong DiscordID, LostSector LS, LostSectorDifficulty Difficulty, ExoticArmorType ArmorDrop)
        {
            LostSectorLink lsl = new LostSectorLink()
            {
                DiscordID = DiscordID,
                LostSector = LS,
                Difficulty = Difficulty,
                ArmorDrop = ArmorDrop
            };
            string json = File.ReadAllText(DestinyUtilityCord.LostSectorTrackingConfigPath);
            AnnounceLostSectorUpdates.Clear();
            LostSectorLinks.Clear();
            LostSectorRotation jsonObj = JsonConvert.DeserializeObject<LostSectorRotation>(json);

            LostSectorLinks.Add(lsl);
            LostSectorRotation ac = new LostSectorRotation();
            string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.LostSectorTrackingConfigPath, output);
        }

        public static void DeleteLostSectorsTrackingFromConfig(ulong DiscordID)
        {
            string json = File.ReadAllText(DestinyUtilityCord.LostSectorTrackingConfigPath);
            AnnounceLostSectorUpdates.Clear();
            LostSectorLinks.Clear();
            LostSectorRotation ac = JsonConvert.DeserializeObject<LostSectorRotation>(json);
            for (int i = 0; i < LostSectorLinks.Count; i++)
                if (LostSectorLinks[i].DiscordID == DiscordID)
                    LostSectorLinks.RemoveAt(i);
            string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            File.WriteAllText(DestinyUtilityCord.LostSectorTrackingConfigPath, output);
        }

        public static bool IsExistingLinkedLostSectorsTracking(ulong DiscordID)
        {
            string json = File.ReadAllText(DestinyUtilityCord.LostSectorTrackingConfigPath);
            AnnounceLostSectorUpdates.Clear();
            LostSectorLinks.Clear();
            LostSectorRotation jsonObj = JsonConvert.DeserializeObject<LostSectorRotation>(json);
            foreach (LostSectorLink dil in LostSectorLinks)
                if (dil.DiscordID == DiscordID)
                    return true;
            return false;
        }

        public static LostSectorLink GetLostSectorsTracking(ulong DiscordID)
        {
            string json = File.ReadAllText(DestinyUtilityCord.LostSectorTrackingConfigPath);
            AnnounceLostSectorUpdates.Clear();
            LostSectorLinks.Clear();
            LostSectorRotation jsonObj = JsonConvert.DeserializeObject<LostSectorRotation>(json);
            foreach (LostSectorLink dil in LostSectorLinks)
                if (dil.DiscordID == DiscordID)
                    return dil;
            return null;
        }

        #endregion
    }
}
