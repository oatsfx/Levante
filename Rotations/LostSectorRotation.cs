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

            [JsonProperty("ArmorDrop")]
            public ExoticArmorType? ArmorDrop { get; set; } = ExoticArmorType.Helmet;
        }

        public static LostSector GetPredictedLostSector(int Days)
        {
            return (LostSector)((((int)CurrentRotations.LostSector) + Days) % LostSectorCount);
        }

        public static ExoticArmorType GetPredictedLostSectorArmorDrop(int Days)
        {
            return (ExoticArmorType)((((int)CurrentRotations.LostSectorArmorDrop) + Days) % 4);
        }

        public static string GetLostSectorString(LostSector ls)
        {
            switch (ls)
            {
                case LostSector.BayOfDrownedWishes: return "Bay of Drowned Wishes";
                case LostSector.ChamberOfStarlight: return "Chamber of Starlight";
                case LostSector.AphelionsRest: return "Aphelion's Rest";
                //case LostSector.TheEmptyTank: return "The Empty Tank";
                case LostSector.K1Logistics: return "K1 Logistics";
                //case LostSector.K1Communion: return "K1 Communion";
                case LostSector.K1CrewQuarters: return "K1 Crew Quarters";
                case LostSector.K1Revelation: return "K1 Revelation";
                //case LostSector.ConcealedVoid: return "Concealed Void";
                //case LostSector.BunkerE15: return "Bunker E15";
                //case LostSector.Perdition: return "Perdition";
                case LostSector.VelesLabyrinth: return "Veles Labyrinth";
                case LostSector.ExodusGarden2A: return "Exodus Garden 2A";
                case LostSector.Metamorphosis: return "Metamorphosis";
                case LostSector.Sepulcher: return "Sepulcher";
                case LostSector.Extraction: return "Extraction";
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
                //case LostSector.TheEmptyTank: return "Thieves' Landing, The Tangled Shore";
                case LostSector.K1Logistics: return "Archer's Line, The Moon";
                //case LostSector.K1Communion: return "Anchor of Light, The Moon";
                case LostSector.K1CrewQuarters: return "Hellmouth, The Moon";
                case LostSector.K1Revelation: return "Sorrow's Harbor, The Moon";
                //case LostSector.ConcealedVoid: return "Asterion Abyss, Europa";
                //case LostSector.BunkerE15: return "Eventide Ruins, Europa";
                //case LostSector.Perdition: return "Cadmus Ridge, Europa";
                case LostSector.VelesLabyrinth: return "Forgotten Shore, The Cosmodrome";
                case LostSector.ExodusGarden2A: return "The Divide, The Cosmodrome";
                case LostSector.Metamorphosis: return "Miasma, Savathûn's Throne World";
                case LostSector.Sepulcher: return "Florescent Canal, Savathûn's Throne World";
                case LostSector.Extraction: return "Quagmire, Savathûn's Throne World";
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
                //case LostSector.TheEmptyTank: return "Azilis, Dusk Marauder";
                case LostSector.K1Logistics: return "Nightmare of Kelniks Reborn";
                //case LostSector.K1Communion: return "Nightmare of Rizaahn, The Lost";
                case LostSector.K1CrewQuarters: return "Nightmare of Reyiks, Actuator";
                case LostSector.K1Revelation: return "Nightmare of Arguth, The Tormented";
                //case LostSector.ConcealedVoid: return "Teliks, House Salvation";
                //case LostSector.BunkerE15: return "Inquisitor Hydra";
                //case LostSector.Perdition: return "Alkestis, Sacrificial Mind";
                case LostSector.VelesLabyrinth: return "Ak-Baral, Rival of Navôta";
                case LostSector.ExodusGarden2A: return "Deksis-5, Taskmaster4";
                case LostSector.Metamorphosis: return "Dread Tatsrekka";
                case LostSector.Sepulcher: return "Bar-Zel, Tutelary of Savathun";
                case LostSector.Extraction: return "Hathrek, the Glasweard";
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
                //case LostSector.TheEmptyTank: return "https://www.bungie.net/img/destiny_content/pgcr/free_roam_tangled_shore.jpg";
                case LostSector.K1Logistics: return "https://www.bungie.net/img/destiny_content/pgcr/moon_k1_logistics.jpg";
                //case LostSector.K1Communion: return "https://www.bungie.net/img/destiny_content/pgcr/moon_k1_communion.jpg";
                case LostSector.K1CrewQuarters: return "https://www.bungie.net/img/destiny_content/pgcr/moon_k1_crew_quarters.jpg";
                case LostSector.K1Revelation: return "https://www.bungie.net/img/destiny_content/pgcr/moon_k1_revelation.jpg";
                //case LostSector.ConcealedVoid: return "https://www.bungie.net/img/destiny_content/pgcr/europa-lost-sector-frost.jpg";
                //case LostSector.BunkerE15: return "https://www.bungie.net/img/destiny_content/pgcr/europa-lost-sector-overhang.jpg";
                //case LostSector.Perdition: return "https://www.bungie.net/img/destiny_content/pgcr/europa-lost-sector-shear.jpg";
                case LostSector.VelesLabyrinth: return "https://www.bungie.net/img/destiny_content/pgcr/cosmodrome-lost-sector-dry-sea.jpg";
                case LostSector.ExodusGarden2A: return "https://bungie.net/img/destiny_content/pgcr/cosmodrome-lost-sector-graveyard.jpg";
                case LostSector.Metamorphosis: return "https://bungie.net/img/destiny_content/pgcr/bayou_ls.jpg";
                case LostSector.Sepulcher: return "https://bungie.net/img/destiny_content/pgcr/canal_ls.jpg";
                case LostSector.Extraction: return "https://bungie.net/img/destiny_content/pgcr/gateway_ls.jpg";
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
                    //case LostSector.TheEmptyTank: return $"2 {DestinyEmote.Barrier} (Barrier)\n2 {DestinyEmote.Overload} (Overload)";
                    case LostSector.K1Logistics: return $"3 {DestinyEmote.Barrier} (Barrier)\n2 {DestinyEmote.Overload} (Overload)";
                    //case LostSector.K1Communion: return $"3 {DestinyEmote.Barrier} (Barrier)\n2 {DestinyEmote.Overload} (Overload)";
                    case LostSector.K1CrewQuarters: return $"3 {DestinyEmote.Barrier} (Barrier)\n2 {DestinyEmote.Overload} (Overload)";
                    case LostSector.K1Revelation: return $"3 {DestinyEmote.Unstoppable} (Upstoppable)\n4 {DestinyEmote.Barrier} (Barrier)";
                    //case LostSector.ConcealedVoid: return $"1 {DestinyEmote.Barrier} (Barrier)\n3 {DestinyEmote.Overload} (Overload)";
                    //case LostSector.BunkerE15: return $"1 {DestinyEmote.Barrier} (Barrier)\n4 {DestinyEmote.Overload} (Overload)";
                    //case LostSector.Perdition: return $"2 {DestinyEmote.Barrier} (Barrier)\n2 {DestinyEmote.Overload} (Overload)";
                    case LostSector.VelesLabyrinth: return $"1 {DestinyEmote.Unstoppable} (Upstoppable)\n2 {DestinyEmote.Barrier} (Barrier)";
                    case LostSector.ExodusGarden2A: return $"2 {DestinyEmote.Barrier} (Barrier)\n2 {DestinyEmote.Overload} (Overload)";
                    case LostSector.Metamorphosis: return $"3 {DestinyEmote.Overload} (Overload)\n2 {DestinyEmote.Unstoppable} (Upstoppable)";
                    case LostSector.Sepulcher: return $"1 {DestinyEmote.Unstoppable} (Upstoppable)\n3 {DestinyEmote.Barrier} (Barrier)";
                    case LostSector.Extraction: return $"1 {DestinyEmote.Overload} (Overload)\n2 {DestinyEmote.Unstoppable} (Upstoppable)";
                    default: return "";
                }
            else if (lsd == LostSectorDifficulty.Master)
                switch (ls)
                {
                    case LostSector.BayOfDrownedWishes: return $"3 {DestinyEmote.Unstoppable} (Unstoppable)\n3 {DestinyEmote.Barrier} (Barrier)";
                    case LostSector.ChamberOfStarlight: return $"6 {DestinyEmote.Unstoppable} (Unstoppable)\n3 {DestinyEmote.Overload} (Overload)";
                    case LostSector.AphelionsRest: return $"3 {DestinyEmote.Unstoppable} (Unstoppable)\n4 {DestinyEmote.Overload} (Overload)";
                    //case LostSector.TheEmptyTank: return $"5 {DestinyEmote.Barrier} (Barrier)\n3 {DestinyEmote.Overload} (Overload)";
                    case LostSector.K1Logistics: return $"4 {DestinyEmote.Barrier} (Barrier)\n6 {DestinyEmote.Overload} (Overload)";
                    //case LostSector.K1Communion: return $"4 {DestinyEmote.Barrier} (Barrier)\n6 {DestinyEmote.Overload} (Overload)";
                    case LostSector.K1CrewQuarters: return $"4 {DestinyEmote.Barrier} (Barrier)\n6 {DestinyEmote.Overload} (Overload)";
                    case LostSector.K1Revelation: return $"3 {DestinyEmote.Unstoppable} (Upstoppable)\n7 {DestinyEmote.Barrier} (Barrier)";
                    //case LostSector.ConcealedVoid: return $"3 {DestinyEmote.Barrier} (Barrier)\n5 {DestinyEmote.Overload} (Overload)";
                    //case LostSector.BunkerE15: return $"2 {DestinyEmote.Barrier} (Barrier)\n4 {DestinyEmote.Overload} (Overload)";
                    //case LostSector.Perdition: return $"4 {DestinyEmote.Barrier} (Barrier)\n2 {DestinyEmote.Overload} (Overload)";
                    case LostSector.VelesLabyrinth: return $"4 {DestinyEmote.Unstoppable} (Upstoppable)\n4 {DestinyEmote.Barrier} (Barrier)";
                    case LostSector.ExodusGarden2A: return $"5 {DestinyEmote.Barrier} (Barrier)\n4 {DestinyEmote.Overload} (Overload)";
                    case LostSector.Metamorphosis: return $"3 {DestinyEmote.Overload} (Overload)\n4 {DestinyEmote.Unstoppable} (Upstoppable)";
                    case LostSector.Sepulcher: return $"2 {DestinyEmote.Unstoppable} (Upstoppable)\n5 {DestinyEmote.Barrier} (Barrier)";
                    case LostSector.Extraction: return $"4 {DestinyEmote.Overload} (Overload)\n2 {DestinyEmote.Unstoppable} (Upstoppable)";
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
                    //case LostSector.TheEmptyTank: return $"1 {DestinyEmote.Arc}";
                    case LostSector.K1Logistics: return $"8 {DestinyEmote.Solar}\n3 {DestinyEmote.Arc}";
                    //case LostSector.K1Communion: return $"1 {DestinyEmote.Solar}\n2 {DestinyEmote.Void}";
                    case LostSector.K1CrewQuarters: return $"10 {DestinyEmote.Solar}";
                    case LostSector.K1Revelation: return $"4 {DestinyEmote.Arc}";
                    //case LostSector.ConcealedVoid: return $"2 {DestinyEmote.Solar}\n3 {DestinyEmote.Void}\n1 {DestinyEmote.Arc}";
                    //case LostSector.BunkerE15: return $"2 {DestinyEmote.Void}";
                    //case LostSector.Perdition: return $"2 {DestinyEmote.Void}\n22 {DestinyEmote.Arc}";
                    case LostSector.VelesLabyrinth: return $"2 {DestinyEmote.Solar}\n4 {DestinyEmote.Arc}";
                    case LostSector.ExodusGarden2A: return $"4 {DestinyEmote.Void}";
                    case LostSector.Metamorphosis: return $"1 {DestinyEmote.Solar}\n2 {DestinyEmote.Arc}";
                    case LostSector.Sepulcher: return $"1 {DestinyEmote.Solar}\n2 {DestinyEmote.Arc}";
                    case LostSector.Extraction: return $"5 {DestinyEmote.Void}\n7 {DestinyEmote.Arc}";
                    default: return "";
                }
            else if (lsd == LostSectorDifficulty.Master)
                switch (ls)
                {
                    case LostSector.BayOfDrownedWishes: return $"1 {DestinyEmote.Void}";
                    case LostSector.ChamberOfStarlight: return $"2 {DestinyEmote.Solar}\n23 {DestinyEmote.Void}";
                    case LostSector.AphelionsRest: return $"9 {DestinyEmote.Void}";
                    //case LostSector.TheEmptyTank: return $"2 {DestinyEmote.Arc}";
                    case LostSector.K1Logistics: return $"8 {DestinyEmote.Solar}\n3 {DestinyEmote.Arc}";
                    //case LostSector.K1Communion: return $"1 {DestinyEmote.Solar}";
                    case LostSector.K1CrewQuarters: return $"10 {DestinyEmote.Solar}";
                    case LostSector.K1Revelation: return $"1 {DestinyEmote.Arc}";
                    //case LostSector.ConcealedVoid: return $"2 {DestinyEmote.Solar}\n3 {DestinyEmote.Void}";
                    //case LostSector.BunkerE15: return $"2 {DestinyEmote.Void}";
                    //case LostSector.Perdition: return $"2 {DestinyEmote.Void}\n22 {DestinyEmote.Arc}";
                    case LostSector.VelesLabyrinth: return $"2 {DestinyEmote.Solar}\n2 {DestinyEmote.Arc}";
                    case LostSector.ExodusGarden2A: return $"3 {DestinyEmote.Void}";
                    case LostSector.Metamorphosis: return $"1 {DestinyEmote.Solar}\n2 {DestinyEmote.Arc}";
                    case LostSector.Sepulcher: return $"1 {DestinyEmote.Arc}";
                    case LostSector.Extraction: return $"5 {DestinyEmote.Void}\n4 {DestinyEmote.Arc}";
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
                case LostSector.BayOfDrownedWishes: result += $"{DestinyEmote.MatchGame} Match Game\n{DestinyEmote.LimitedRevives} Limited Revives\n{DestinyEmote.EquipmentLocked} Equipment Locked\n{DestinyEmote.RaiderShield} Raider Shield"; break;
                case LostSector.ChamberOfStarlight: result += $"{DestinyEmote.MatchGame} Match Game\n{DestinyEmote.LimitedRevives} Limited Revives\n{DestinyEmote.EquipmentLocked} Equipment Locked\n{DestinyEmote.Epitaph} Epitaph"; break;
                case LostSector.AphelionsRest: result += $"{DestinyEmote.MatchGame} Match Game\n{DestinyEmote.LimitedRevives} Limited Revives\n{DestinyEmote.EquipmentLocked} Equipment Locked\n{DestinyEmote.Epitaph} Epitaph"; break;
                //case LostSector.TheEmptyTank: result += $"<:SolarBurn:900142136996020224> Solar Burn\n<:MatchGame:900142096013459516> Match Game\n<:LimitedRevives:900142086270115901> Limited Revives\n<:EquipmentLocked:900142051725819965> Equipment Locked\n<:ArachNO:900142007840833576> Arach-NO!"; break;
                case LostSector.K1Logistics: result += $"{DestinyEmote.MatchGame} Match Game\n{DestinyEmote.LimitedRevives} Limited Revives\n{DestinyEmote.EquipmentLocked} Equipment Locked\n{DestinyEmote.HotKnife} Hot Knife"; break;
                //case LostSector.K1Communion: result += $"<:SolarBurn:900142136996020224> Solar Burn\n<:MatchGame:900142096013459516> Match Game\n<:LimitedRevives:900142086270115901> Limited Revives\n<:EquipmentLocked:900142051725819965> Equipment Locked\n<:ArachNO:900142007840833576> Arach-NO!"; break;
                case LostSector.K1CrewQuarters: result += $"{DestinyEmote.MatchGame} Match Game\n{DestinyEmote.LimitedRevives} Limited Revives\n{DestinyEmote.EquipmentLocked} Equipment Locked\n{DestinyEmote.HotKnife} Hot Knife"; break;
                case LostSector.K1Revelation: result += $"{DestinyEmote.MatchGame} Match Game\n{DestinyEmote.LimitedRevives} Limited Revives\n{DestinyEmote.EquipmentLocked} Equipment Locked\n{DestinyEmote.FirePit} Fire Pit"; break;
                //case LostSector.ConcealedVoid: result += $"{DestinyEmote.SolarBurn} Solar Burn\n{DestinyEmote.MatchGame} Match Game\n{DestinyEmote.LimitedRevives} Limited Revives\n{DestinyEmote.EquipmentLocked} Equipment Locked\n{DestinyEmote.ArachNO} Arach-NO!"; break;
                //case LostSector.BunkerE15: result += $"{DestinyEmote.VoidBurn} Void Burn\n{DestinyEmote.MatchGame} Match Game\n{DestinyEmote.LimitedRevives} Limited Revives\n{DestinyEmote.EquipmentLocked} Equipment Locked\n{DestinyEmote.Shocker} Shocker"; break;
                //case LostSector.Perdition: result += $"{DestinyEmote.ArcBurn} Arc Burn\n{DestinyEmote.MatchGame} Match Game\n{DestinyEmote.LimitedRevives} Limited Revives\n{DestinyEmote.EquipmentLocked} Equipment Locked\n{DestinyEmote.Shocker} Shocker"; break;
                case LostSector.VelesLabyrinth: result += $"{DestinyEmote.MatchGame} Match Game\n{DestinyEmote.LimitedRevives} Limited Revives\n{DestinyEmote.EquipmentLocked} Equipment Locked\n{DestinyEmote.FirePit} Fire Pit"; break;
                case LostSector.ExodusGarden2A: result += $"{DestinyEmote.MatchGame} Match Game\n{DestinyEmote.LimitedRevives} Limited Revives\n{DestinyEmote.EquipmentLocked} Equipment Locked\n{DestinyEmote.ScorchedEarth} Scorched Earth"; break;
                case LostSector.Metamorphosis: result += $"{DestinyEmote.MatchGame} Match Game\n{DestinyEmote.LimitedRevives} Limited Revives\n{DestinyEmote.EquipmentLocked} Equipment Locked\n{DestinyEmote.ScorchedEarth} Scorched Earth"; break;
                case LostSector.Sepulcher: result += $"{DestinyEmote.MatchGame} Match Game\n{DestinyEmote.LimitedRevives} Limited Revives\n{DestinyEmote.EquipmentLocked} Equipment Locked\n{DestinyEmote.FirePit} Fire Pit"; break;
                case LostSector.Extraction: result += $"{DestinyEmote.MatchGame} Match Game\n{DestinyEmote.LimitedRevives} Limited Revives\n{DestinyEmote.EquipmentLocked} Equipment Locked\n{DestinyEmote.RaiderShield} Raider Shield"; break;
                default: result += ""; break;
            }

            if (lsd == LostSectorDifficulty.Master)
            {
                switch (ls)
                {
                    case LostSector.BayOfDrownedWishes: result += $"\n{DestinyEmote.Chaff} Chaff"; break;
                    case LostSector.ChamberOfStarlight: result += $"\n{DestinyEmote.Attrition} Attrition"; break;
                    case LostSector.AphelionsRest: result += $"\n{DestinyEmote.Attrition} Attrition"; break;
                    //case LostSector.TheEmptyTank: result += "\n<:Chaff:900142037020598293> Chaff"; break;
                    case LostSector.K1Logistics: result += $"\n{DestinyEmote.Chaff} Chaff"; break;
                    //case LostSector.K1Communion: result += "\n<:Famine:900142061695672331> Famine"; break;
                    case LostSector.K1CrewQuarters: result += $"\n{DestinyEmote.Attrition} Attrition"; break;
                    case LostSector.K1Revelation: result += $"\n{DestinyEmote.Chaff} Chaff"; break;
                    //case LostSector.ConcealedVoid: result += $"\n{DestinyEmote.Chaff} Chaff"; break;
                    //case LostSector.BunkerE15: result += $"\n{DestinyEmote.Attrition} Attrition"; break;
                    //case LostSector.Perdition: result += $"\n{DestinyEmote.Famine} Famine"; break;
                    case LostSector.VelesLabyrinth: result += $"\n{DestinyEmote.Chaff} Chaff"; break;
                    case LostSector.ExodusGarden2A: result += $"\n{DestinyEmote.Attrition} Attrition"; break;
                    case LostSector.Metamorphosis: result += $"\n{DestinyEmote.Attrition} Attrition"; break;
                    case LostSector.Sepulcher: result += $"\n{DestinyEmote.Chaff} Chaff"; break;
                    case LostSector.Extraction: result += $"\n{DestinyEmote.Chaff} Chaff"; break;
                    default: result += ""; break;
                }
            }

            return result;
        }

        public static string GetLostSectorBurn(LostSector ls)
        {
            switch (ls)
            {
                case LostSector.BayOfDrownedWishes: return $"{DestinyEmote.Arc}"; 
                case LostSector.ChamberOfStarlight: return $"{DestinyEmote.Solar}";
                case LostSector.AphelionsRest: return $"{DestinyEmote.Void}";
                //case LostSector.TheEmptyTank: result += $"{DestinyEmote.Solar}";
                case LostSector.K1Logistics: return $"{DestinyEmote.Void}";
                //case LostSector.K1Communion: result += $"{DestinyEmote.Solar}";
                case LostSector.K1CrewQuarters: return $"{DestinyEmote.Arc}";
                case LostSector.K1Revelation: return $"{DestinyEmote.Void}";
                //case LostSector.ConcealedVoid: result += $"{DestinyEmote.Solar}";
                //case LostSector.BunkerE15: result += $"{DestinyEmote.Void}";
                //case LostSector.Perdition: result += $"{DestinyEmote.Arc}";
                case LostSector.VelesLabyrinth: return $"{DestinyEmote.Arc}";
                case LostSector.ExodusGarden2A: return $"{DestinyEmote.Void}";
                case LostSector.Metamorphosis: return $"{DestinyEmote.Arc}";
                case LostSector.Sepulcher: return $"{DestinyEmote.Solar}";
                case LostSector.Extraction: return $"{DestinyEmote.Arc}";
                default: return "";
            }
        }

        public static EmbedBuilder GetLostSectorEmbed(LostSector LS, LostSectorDifficulty LSD)
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
                y.Value = $"Recommended Power: {DestinyEmote.Light}{GetLostSectorDifficultyLight(LSD)}\n" +
                    $"Burn: {GetLostSectorBurn(LS)}";
                y.IsInline = false;
            })
            .AddField(y =>
            {
                y.Name = "Champions";
                y.Value = GetLostSectorChampionsString(LS, LSD);
                y.IsInline = true;
            })
            .AddField(y =>
            {
                y.Name = "Modifiers";
                y.Value = GetLostSectorModifiersString(LS, LSD);
                y.IsInline = true;
            })
            .AddField(y =>
            {
                y.Name = "Shields";
                y.Value = GetLostSectorShieldsString(LS, LSD);
                y.IsInline = true;
            });

            embed.Title = $"{GetLostSectorString(LS)}";
            embed.Description = $"Boss: {GetLostSectorBossString(LS)}";

            embed.Url = GetLostSectorImageURL(LS);
            embed.ThumbnailUrl = "https://www.bungie.net/common/destiny2_content/icons/6a2761d2475623125d896d1a424a91f9.png";

            return embed;
        }

        public static string GetLostSectorDifficultyLight(LostSectorDifficulty lsd)
        {
            switch (lsd)
            {
                case LostSectorDifficulty.Legend: return "1550";
                case LostSectorDifficulty.Master: return "1580";
                default: return "";
            }
        }

        public static void AddUserTracking(ulong DiscordID, LostSector? LS, ExoticArmorType? EAT = null)
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
        public static LostSectorLink GetUserTracking(ulong DiscordID, out LostSector? LS, out ExoticArmorType? EAT)
        {
            foreach (var Link in LostSectorLinks)
                if (Link.DiscordID == DiscordID)
                {
                    LS = Link.LostSector;
                    EAT = Link.ArmorDrop;
                    return Link;
                }
            LS = null;
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
        public static DateTime DatePrediction(LostSector? LS, ExoticArmorType? ArmorType)
        {
            ExoticArmorType iterationEAT = CurrentRotations.LostSectorArmorDrop;
            LostSector iterationLS = CurrentRotations.LostSector;
            int DaysUntil =  0;

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
                    iterationLS = iterationLS == LostSector.Extraction ? LostSector.VelesLabyrinth : iterationLS + 1;
                    DaysUntil++;
                } while (iterationLS != LS);
            }
            else if (ArmorType != null && LS != null)
            {
                do
                {
                    iterationEAT = iterationEAT == ExoticArmorType.Chest ? ExoticArmorType.Helmet : iterationEAT + 1;
                    iterationLS = iterationLS == LostSector.Extraction ? LostSector.VelesLabyrinth : iterationLS + 1;
                    DaysUntil++;
                } while (iterationEAT != ArmorType || iterationLS != LS);
            }
            return CurrentRotations.DailyResetTimestamp.AddDays(DaysUntil);
        }
    }

    public enum LostSector
    {
        // Cosmodrome
        VelesLabyrinth,
        ExodusGarden2A,
        // Dreaming City
        AphelionsRest,
        BayOfDrownedWishes,
        ChamberOfStarlight,
        // Tangled Shore
        //TheEmptyTank, // Gone
        // Luna
        K1Revelation,
        K1CrewQuarters,
        K1Logistics,
        //K1Communion, // Gone
        // Throne World
        Metamorphosis,
        Sepulcher,
        Extraction,
        // Europa
        //ConcealedVoid,
        //BunkerE15,
        //Perdition,

        // Season 17 (Season of the Haunted)
        //K1CrewQuarters,
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
