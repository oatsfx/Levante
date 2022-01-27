using DestinyUtility.Configs;
using DestinyUtility.Util;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DestinyUtility.Rotations
{
    public class NightfallRotation
    {
        public static readonly int NightfallStrikeCount = 6;
        public static readonly int NightfallWeaponCount = 8;
        public static readonly string FilePath = @"Trackers/nightfall.json";

        [JsonProperty("NightfallLinks")]
        public static List<NightfallLink> NightfallLinks { get; set; } = new List<NightfallLink>();

        public class NightfallLink
        {
            [JsonProperty("DiscordID")]
            public ulong DiscordID { get; set; } = 0;

            [JsonProperty("NightfallStrike")]
            public Nightfall? Nightfall { get; set; } = 0;

            [JsonProperty("WeaponDrop")]
            public NightfallWeapon? WeaponDrop { get; set; } = NightfallWeapon.ThePalindrome;
        }

        public static string GetStrikeNameString(Nightfall Nightfall)
        {
            switch (Nightfall)
            {
                case Nightfall.TheHollowedLair: return "The Hollowed Lair";
                case Nightfall.LakeOfShadows: return "Lake of Shadows";
                case Nightfall.ExodusCrash: return "Exodus Crash";
                case Nightfall.TheCorrupted: return "The Corrupted";
                case Nightfall.TheDevilsLair: return "The Devils' Lair";
                case Nightfall.ProvingGrounds: return "Proving Grounds";
                default: return "Nightfall: The Ordeal";
            }
        }

        public static string GetStrikeBossString(Nightfall Nightfall)
        {
            switch (Nightfall)
            {
                case Nightfall.TheHollowedLair: return "Fikrul, the Fanatic";
                case Nightfall.LakeOfShadows: return "Grask, the Consumed";
                case Nightfall.ExodusCrash: return "Thaviks, the Depraved";
                case Nightfall.TheCorrupted: return "Sedia, the Corrupted";
                case Nightfall.TheDevilsLair: return "Sepiks Prime";
                case Nightfall.ProvingGrounds: return "Ignovun, Chosen of Caiatl";
                default: return "Nightfall: The Ordeal Boss";
            }
        }

        public static string GetStrikeImageURL(Nightfall Nightfall)
        {
            switch (Nightfall)
            {
                case Nightfall.TheHollowedLair: return "https://www.bungie.net/img/destiny_content/pgcr/strike_taurus.jpg";
                case Nightfall.LakeOfShadows: return "https://www.bungie.net/img/destiny_content/pgcr/strike_lake_of_shadows.jpg";
                case Nightfall.ExodusCrash: return "https://www.bungie.net/img/destiny_content/pgcr/strike_exodus_crash.jpg";
                case Nightfall.TheCorrupted: return "https://www.bungie.net/img/destiny_content/pgcr/strike_gemini.jpg";
                case Nightfall.TheDevilsLair: return "https://www.bungie.net/img/destiny_content/pgcr/cosmodrome_devils_lair.jpg";
                case Nightfall.ProvingGrounds: return "https://www.bungie.net/img/destiny_content/pgcr/nessus_proving_grounds.jpg";
                default: return "https://www.bungie.net/common/destiny2_content/icons/f2154b781b36b19760efcb23695c66fe.png";
            }
        }

        public static string GetStrikeObjectiveString(Nightfall Nightfall)
        {
            switch (Nightfall)
            {
                case Nightfall.TheHollowedLair: return "The Fanatic has returned. Take him down and finish the job you started.";
                case Nightfall.LakeOfShadows: return "Stem the tide of Taken flowing into the European Dead Zone from beneath the waves.";
                case Nightfall.ExodusCrash: return "Purge the Fallen infestation of the Exodus Black.";
                case Nightfall.TheCorrupted: return "Hunt down one of Queen Mara's most trusted advisors and free her from Taken possession.";
                case Nightfall.TheDevilsLair: return "Enter into the Devils' Lair and weaken the Fallen presence within the Cosmodrome.";
                case Nightfall.ProvingGrounds: return "Defeat Caiatl's Chosen aboard the Land Tank, Halphas Electus, on Nessus.";
                default: return "Clear the nightfall.";
            }
        }

        public static string GetStrikeLocationString(Nightfall Nightfall)
        {
            switch (Nightfall)
            {
                case Nightfall.TheHollowedLair: return "The Tangled Shore, The Reef";
                case Nightfall.LakeOfShadows: return "European Dead Zone, Earth";
                case Nightfall.ExodusCrash: return "Arcadian Valley, Nessus";
                case Nightfall.TheCorrupted: return "The Dreaming City, The Reef";
                case Nightfall.TheDevilsLair: return "The Cosmodrome, Earth";
                case Nightfall.ProvingGrounds: return "Halphas Electus, Nessus.";
                default: return "The Sol System, Space";
            }
        }

        public static string GetStrikeModifiers(Nightfall Nightfall, NightfallDifficulty Difficulty)
        {
            switch (Nightfall)
            {
                case Nightfall.TheHollowedLair:
                    switch (Difficulty)
                    {
                        case NightfallDifficulty.Adept: return $"{DestinyEmote.FesteringRupture}";
                        case NightfallDifficulty.Hero: return $"{DestinyEmote.FesteringRupture} {DestinyEmote.FanaticsZeal}";
                        case NightfallDifficulty.Legend: return $"{DestinyEmote.FesteringRupture} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.FanaticsZeal}";
                        case NightfallDifficulty.Master: return $"{DestinyEmote.FesteringRupture} {DestinyEmote.Chaff} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.FanaticsZeal}";
                        case NightfallDifficulty.Grandmaster: return $"{DestinyEmote.FesteringRupture} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.MatchGame} {DestinyEmote.EquipmentLocked}" +
                                $"{DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.FanaticsZeal}";
                        default: return "None.";
                    }
                case Nightfall.LakeOfShadows:
                    switch (Difficulty)
                    {
                        case NightfallDifficulty.Adept: return $"";
                        case NightfallDifficulty.Hero: return $"";
                        case NightfallDifficulty.Legend: return $"";
                        case NightfallDifficulty.Master: return $"";
                        case NightfallDifficulty.Grandmaster: return $"";
                        default: return "None.";
                    }
                case Nightfall.ExodusCrash:
                    switch (Difficulty)
                    {
                        case NightfallDifficulty.Adept: return $"{DestinyEmote.ScorchedEarth}";
                        case NightfallDifficulty.Hero: return $"{DestinyEmote.ScorchedEarth} {DestinyEmote.ThaviksImplant}";
                        case NightfallDifficulty.Legend: return $"{DestinyEmote.ScorchedEarth} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.ThaviksImplant}";
                        case NightfallDifficulty.Master: return $"{DestinyEmote.ScorchedEarth} {DestinyEmote.Chaff} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.ThaviksImplant}";
                        case NightfallDifficulty.Grandmaster: return $"{DestinyEmote.ArachNO} {DestinyEmote.ScorchedEarth} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.MatchGame} {DestinyEmote.EquipmentLocked}" +
                                $"{DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.ThaviksImplant}";
                        default: return "None.";
                    }
                case Nightfall.TheCorrupted:
                    switch (Difficulty)
                    {
                        case NightfallDifficulty.Adept: return $"{DestinyEmote.Epitaph}";
                        case NightfallDifficulty.Hero: return $"{DestinyEmote.Epitaph} {DestinyEmote.SediasDurance}";
                        case NightfallDifficulty.Legend: return $"{DestinyEmote.Epitaph} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.SediasDurance}";
                        case NightfallDifficulty.Master: return $"{DestinyEmote.Epitaph} {DestinyEmote.Famine} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.SediasDurance}";
                        case NightfallDifficulty.Grandmaster: return $"{DestinyEmote.Epitaph} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.MatchGame} {DestinyEmote.EquipmentLocked}" +
                                $"{DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.SediasDurance}";
                        default: return "None.";
                    }
                case Nightfall.TheDevilsLair:
                    switch (Difficulty)
                    {
                        case NightfallDifficulty.Adept: return $"";
                        case NightfallDifficulty.Hero: return $"";
                        case NightfallDifficulty.Legend: return $"";
                        case NightfallDifficulty.Master: return $"";
                        case NightfallDifficulty.Grandmaster: return $"";
                        default: return "None.";
                    }
                case Nightfall.ProvingGrounds:
                    switch (Difficulty)
                    {
                        case NightfallDifficulty.Adept: return $"";
                        case NightfallDifficulty.Hero: return $"";
                        case NightfallDifficulty.Legend: return $"";
                        case NightfallDifficulty.Master: return $"";
                        case NightfallDifficulty.Grandmaster: return $"";
                        default: return "None.";
                    }
                default: return "None.";
            }
        }

        public static string GetWeaponString(NightfallWeapon Weapon)
        {
            switch (Weapon)
            {
                case NightfallWeapon.ThePalindrome: return "The Palindrome";
                case NightfallWeapon.TheSWARM: return "THE S.W.A.R.M.";
                case NightfallWeapon.TheComedian: return "The Comedian";
                case NightfallWeapon.ShadowPrice: return "Shadow Price";
                case NightfallWeapon.HungJurySR4: return "Hung Jury SR4";
                case NightfallWeapon.TheHothead: return "The Hothead";
                case NightfallWeapon.PlugOne1: return "PLUG ONE.1";
                case NightfallWeapon.UzumeRR4: return "Uzume RR4";
                default: return "Nightfall Weapon";
            }
        }

        public static string GetWeaponEmote(NightfallWeapon Weapon)
        {
            switch (Weapon)
            {
                case NightfallWeapon.ThePalindrome: return $"{DestinyEmote.HandCannon}";
                case NightfallWeapon.TheSWARM: return $"{DestinyEmote.MachineGun}";
                case NightfallWeapon.TheComedian: return $"{DestinyEmote.Shotgun}";
                case NightfallWeapon.ShadowPrice: return $"{DestinyEmote.AutoRifle}";
                case NightfallWeapon.HungJurySR4: return $"{DestinyEmote.ScoutRifle}";
                case NightfallWeapon.TheHothead: return $"{DestinyEmote.RocketLauncher}";
                case NightfallWeapon.PlugOne1: return $"{DestinyEmote.FusionRifle}";
                case NightfallWeapon.UzumeRR4: return $"{DestinyEmote.SniperRifle}";
                default: return "Nightfall Weapon Emote";
            }
        }

        public static EmbedBuilder GetNightfallEmbed(Nightfall Nightfall)
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Nightfall Information",
                IconUrl = GetStrikeImageURL(Nightfall),
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"{GetStrikeLocationString(Nightfall)}"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };
            embed.AddField(y =>
            {
                y.Name = $"Adept";
                y.Value = $"Recommended Power: {DestinyEmote.Light}1250\n" +
                    $"Champions: None.\n" +
                    $"Modifiers: ";
                y.IsInline = false;
            });

            embed.Title = $"{GetStrikeNameString(Nightfall)}";
            embed.Description = $"{GetStrikeObjectiveString(Nightfall)}";

            embed.Url = "https://www.bungie.net/img/destiny_content/pgcr/vault_of_glass.jpg";
            embed.ThumbnailUrl = $"https://www.bungie.net/common/destiny2_content/icons/f2154b781b36b19760efcb23695c66fe.png";

            return embed;
        }

        public static void AddUserTracking(ulong DiscordID, Nightfall? Nightfall, NightfallWeapon? WeaponDrop)
        {
            NightfallLinks.Add(new NightfallLink() { DiscordID = DiscordID, Nightfall = Nightfall, WeaponDrop = WeaponDrop });
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            NightfallLinks.Remove(GetUserTracking(DiscordID, out _, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static NightfallLink GetUserTracking(ulong DiscordID, out Nightfall? Nightfall, out NightfallWeapon? WeaponDrop)
        {
            foreach (var Link in NightfallLinks)
                if (Link.DiscordID == DiscordID)
                {
                    Nightfall = Link.Nightfall;
                    WeaponDrop = Link.WeaponDrop;
                    return Link;
                }
            Nightfall = null;
            WeaponDrop = null;
            return null;
        }

        public static void CreateJSON()
        {
            NightfallRotation obj;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                obj = JsonConvert.DeserializeObject<NightfallRotation>(json);
            }
            else
            {
                obj = new NightfallRotation();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
                Console.WriteLine($"No {FilePath} file detected. No action needed.");
            }
        }

        public static void UpdateJSON()
        {
            var obj = new NightfallRotation();
            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(Nightfall? NightfallStrike, NightfallWeapon? WeaponDrop)
        {
            NightfallWeapon[] iterationWeapons = CurrentRotations.NightfallWeaponDrops;
            Nightfall iterationStrike = CurrentRotations.Nightfall;
            int WeeksUntil = 0;
            if (NightfallStrike == null && WeaponDrop != null)
            {
                do
                {
                    iterationWeapons[0] = iterationWeapons[0] >= NightfallWeapon.PlugOne1 ? NightfallWeapon.ThePalindrome : iterationWeapons[0] + 2;
                    iterationWeapons[1] = iterationWeapons[1] >= NightfallWeapon.PlugOne1 ? NightfallWeapon.TheSWARM : iterationWeapons[1] + 2;
                    WeeksUntil++;
                } while (iterationWeapons[0] != WeaponDrop && iterationWeapons[1] != WeaponDrop);
            }
            else if (WeaponDrop == null && NightfallStrike != null)
            {
                do
                {
                    iterationStrike = iterationStrike == Nightfall.ProvingGrounds ? Nightfall.TheHollowedLair : iterationStrike + 1;
                    WeeksUntil++;
                } while (iterationStrike != NightfallStrike);
            }
            else if (WeaponDrop != null && NightfallStrike != null)
            {
                do
                {
                    iterationWeapons[0] = iterationWeapons[0] >= NightfallWeapon.PlugOne1 ? NightfallWeapon.ThePalindrome : iterationWeapons[0] + 2;
                    iterationWeapons[1] = iterationWeapons[1] >= NightfallWeapon.PlugOne1 ? NightfallWeapon.TheSWARM : iterationWeapons[1] + 2;
                    iterationStrike = iterationStrike == Nightfall.ProvingGrounds ? Nightfall.TheHollowedLair : iterationStrike + 1;
                    WeeksUntil++;
                } while (iterationStrike != NightfallStrike && (iterationWeapons[0] != WeaponDrop && iterationWeapons[1] != WeaponDrop));
            }
            return CurrentRotations.WeeklyResetTimestamp.AddDays(WeeksUntil * 7); // Because there is no .AddWeeks().
        }
    }

    public enum Nightfall
    {
        TheHollowedLair,
        LakeOfShadows,
        ExodusCrash,
        TheCorrupted,
        TheDevilsLair,
        ProvingGrounds,
    }

    public enum NightfallWeapon
    {
        ThePalindrome,
        TheSWARM,
        TheComedian,
        ShadowPrice,
        HungJurySR4,
        TheHothead,
        PlugOne1,
        UzumeRR4,
    }

    public enum NightfallDifficulty
    {
        Adept,
        Hero,
        Legend,
        Master,
        Grandmaster,
    }
}
