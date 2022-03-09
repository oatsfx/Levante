using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Discord;
using Levante.Configs;
using Levante.Util;
using Newtonsoft.Json;

// ReSharper disable UnusedMember.Global

namespace Levante.Rotations
{
    public class NightfallRotation
    {
        public static readonly int NightfallStrikeCount = 6;
        public static readonly int NightfallWeaponCount = 8;
        public static readonly string FilePath = @"Trackers/nightfall.json";

        [JsonProperty("NightfallLinks")]
        public static List<NightfallLink> NightfallLinks { get; set; } = new List<NightfallLink>();

        public static string GetStrikeNameString(Nightfall Nightfall)
        {
            return Nightfall switch
            {
                Nightfall.TheHollowedLair => "The Hollowed Lair",
                Nightfall.LakeOfShadows => "Lake of Shadows",
                Nightfall.ExodusCrash => "Exodus Crash",
                Nightfall.TheCorrupted => "The Corrupted",
                Nightfall.TheDevilsLair => "The Devils' Lair",
                Nightfall.ProvingGrounds => "Proving Grounds",
                _ => "Nightfall: The Ordeal"
            };
        }

        public static string GetStrikeBossString(Nightfall Nightfall)
        {
            return Nightfall switch
            {
                Nightfall.TheHollowedLair => "Fikrul, the Fanatic",
                Nightfall.LakeOfShadows => "Grask, the Consumed",
                Nightfall.ExodusCrash => "Thaviks, the Depraved",
                Nightfall.TheCorrupted => "Sedia, the Corrupted",
                Nightfall.TheDevilsLair => "Sepiks Prime",
                Nightfall.ProvingGrounds => "Ignovun, Chosen of Caiatl",
                _ => "Nightfall: The Ordeal Boss"
            };
        }

        public static string GetStrikeImageURL(Nightfall Nightfall)
        {
            return Nightfall switch
            {
                Nightfall.TheHollowedLair => "https://www.bungie.net/img/destiny_content/pgcr/strike_taurus.jpg",
                Nightfall.LakeOfShadows => "https://www.bungie.net/img/destiny_content/pgcr/strike_lake_of_shadows.jpg",
                Nightfall.ExodusCrash => "https://www.bungie.net/img/destiny_content/pgcr/strike_exodus_crash.jpg",
                Nightfall.TheCorrupted => "https://www.bungie.net/img/destiny_content/pgcr/strike_gemini.jpg",
                Nightfall.TheDevilsLair => "https://www.bungie.net/img/destiny_content/pgcr/cosmodrome_devils_lair.jpg",
                Nightfall.ProvingGrounds =>
                    "https://www.bungie.net/img/destiny_content/pgcr/nessus_proving_grounds.jpg",
                _ => "https://www.bungie.net/common/destiny2_content/icons/f2154b781b36b19760efcb23695c66fe.png"
            };
        }

        public static string GetStrikeObjectiveString(Nightfall Nightfall)
        {
            return Nightfall switch
            {
                Nightfall.TheHollowedLair => "The Fanatic has returned. Take him down and finish the job you started.",
                Nightfall.LakeOfShadows =>
                    "Stem the tide of Taken flowing into the European Dead Zone from beneath the waves.",
                Nightfall.ExodusCrash => "Purge the Fallen infestation of the Exodus Black.",
                Nightfall.TheCorrupted =>
                    "Hunt down one of Queen Mara's most trusted advisors and free her from Taken possession.",
                Nightfall.TheDevilsLair =>
                    "Enter into the Devils' Lair and weaken the Fallen presence within the Cosmodrome.",
                Nightfall.ProvingGrounds => "Defeat Caiatl's Chosen aboard the Land Tank, Halphas Electus, on Nessus.",
                _ => "Clear the nightfall."
            };
        }

        public static string GetStrikeLocationString(Nightfall Nightfall)
        {
            return Nightfall switch
            {
                Nightfall.TheHollowedLair => "The Tangled Shore, The Reef",
                Nightfall.LakeOfShadows => "European Dead Zone, Earth",
                Nightfall.ExodusCrash => "Arcadian Valley, Nessus",
                Nightfall.TheCorrupted => "The Dreaming City, The Reef",
                Nightfall.TheDevilsLair => "The Cosmodrome, Earth",
                Nightfall.ProvingGrounds => "Halphas Electus, Nessus.",
                _ => "The Sol System, Space"
            };
        }

        public static string GetStrikeModifiers(Nightfall Nightfall, NightfallDifficulty Difficulty)
        {
            return Nightfall switch
            {
                Nightfall.TheHollowedLair => Difficulty switch
                {
                    NightfallDifficulty.Adept => $"{DestinyEmote.FesteringRupture}",
                    NightfallDifficulty.Hero => $"{DestinyEmote.FesteringRupture} {DestinyEmote.FanaticsZeal}",
                    NightfallDifficulty.Legend =>
                        $"{DestinyEmote.FesteringRupture} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.FanaticsZeal}",
                    NightfallDifficulty.Master =>
                        $"{DestinyEmote.FesteringRupture} {DestinyEmote.Chaff} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.FanaticsZeal}",
                    NightfallDifficulty.Grandmaster =>
                        $"{DestinyEmote.FesteringRupture} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.MatchGame} {DestinyEmote.EquipmentLocked}" +
                        $"{DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.FanaticsZeal}",
                    _ => "None."
                },
                Nightfall.LakeOfShadows => Difficulty switch
                {
                    NightfallDifficulty.Adept => $"{DestinyEmote.Empath}",
                    NightfallDifficulty.Hero => $"{DestinyEmote.Empath} {DestinyEmote.GrasksBile}",
                    NightfallDifficulty.Legend =>
                        $"{DestinyEmote.Empath} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.GrasksBile}",
                    NightfallDifficulty.Master =>
                        $"{DestinyEmote.Empath} {DestinyEmote.Famine} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.GrasksBile}",
                    NightfallDifficulty.Grandmaster =>
                        $"{DestinyEmote.Epitaph} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.MatchGame} {DestinyEmote.EquipmentLocked}" +
                        $"{DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.GrasksBile}",
                    _ => "None."
                },
                Nightfall.ExodusCrash => Difficulty switch
                {
                    NightfallDifficulty.Adept => $"{DestinyEmote.ScorchedEarth}",
                    NightfallDifficulty.Hero => $"{DestinyEmote.ScorchedEarth} {DestinyEmote.ThaviksImplant}",
                    NightfallDifficulty.Legend =>
                        $"{DestinyEmote.ScorchedEarth} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.ThaviksImplant}",
                    NightfallDifficulty.Master =>
                        $"{DestinyEmote.ScorchedEarth} {DestinyEmote.Chaff} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.ThaviksImplant}",
                    NightfallDifficulty.Grandmaster =>
                        $"{DestinyEmote.ArachNO} {DestinyEmote.ScorchedEarth} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.MatchGame} " +
                        $"{DestinyEmote.EquipmentLocked} {DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.ThaviksImplant}",
                    _ => "None."
                },
                Nightfall.TheCorrupted => Difficulty switch
                {
                    NightfallDifficulty.Adept => $"{DestinyEmote.Epitaph}",
                    NightfallDifficulty.Hero => $"{DestinyEmote.Epitaph} {DestinyEmote.SediasDurance}",
                    NightfallDifficulty.Legend =>
                        $"{DestinyEmote.Epitaph} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.SediasDurance}",
                    NightfallDifficulty.Master =>
                        $"{DestinyEmote.Epitaph} {DestinyEmote.Famine} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.SediasDurance}",
                    NightfallDifficulty.Grandmaster =>
                        $"{DestinyEmote.Epitaph} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.MatchGame} {DestinyEmote.EquipmentLocked} " +
                        $"{DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.SediasDurance}",
                    _ => "None."
                },
                Nightfall.TheDevilsLair => Difficulty switch
                {
                    NightfallDifficulty.Adept => $"{DestinyEmote.HotKnife}",
                    NightfallDifficulty.Hero => $"{DestinyEmote.HotKnife} {DestinyEmote.SepiksGaze}",
                    NightfallDifficulty.Legend =>
                        $"{DestinyEmote.HotKnife} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.SepiksGaze}",
                    NightfallDifficulty.Master =>
                        $"{DestinyEmote.HotKnife} {DestinyEmote.Togetherness} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.SepiksGaze}",
                    NightfallDifficulty.Grandmaster =>
                        $"{DestinyEmote.ArachNO} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} " +
                        $"{DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.SepiksGaze}",
                    _ => "None."
                },
                Nightfall.ProvingGrounds => Difficulty switch
                {
                    NightfallDifficulty.Adept => $"{DestinyEmote.Empath}",
                    NightfallDifficulty.Hero => $"{DestinyEmote.Empath} {DestinyEmote.IgnovunsChallenge}",
                    NightfallDifficulty.Legend =>
                        $"{DestinyEmote.Empath} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.IgnovunsChallenge}",
                    NightfallDifficulty.Master =>
                        $"{DestinyEmote.Empath} {DestinyEmote.Attrition} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.IgnovunsChallenge}",
                    NightfallDifficulty.Grandmaster =>
                        $"{DestinyEmote.ScorchedEarth} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} " +
                        $"{DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.IgnovunsChallenge}",
                    _ => "None."
                },
                _ => "None."
            };
        }

        public static string GetWeaponString(NightfallWeapon Weapon)
        {
            return Weapon switch
            {
                NightfallWeapon.ThePalindrome => "The Palindrome",
                NightfallWeapon.TheSWARM => "THE S.W.A.R.M.",
                NightfallWeapon.TheComedian => "The Comedian",
                NightfallWeapon.ShadowPrice => "Shadow Price",
                NightfallWeapon.HungJurySR4 => "Hung Jury SR4",
                NightfallWeapon.TheHothead => "The Hothead",
                NightfallWeapon.PlugOne1 => "PLUG ONE.1",
                NightfallWeapon.UzumeRR4 => "Uzume RR4",
                _ => "Nightfall Weapon"
            };
        }

        public static string GetWeaponEmote(NightfallWeapon Weapon)
        {
            return Weapon switch
            {
                NightfallWeapon.ThePalindrome => $"{DestinyEmote.HandCannon}",
                NightfallWeapon.TheSWARM => $"{DestinyEmote.MachineGun}",
                NightfallWeapon.TheComedian => $"{DestinyEmote.Shotgun}",
                NightfallWeapon.ShadowPrice => $"{DestinyEmote.AutoRifle}",
                NightfallWeapon.HungJurySR4 => $"{DestinyEmote.ScoutRifle}",
                NightfallWeapon.TheHothead => $"{DestinyEmote.RocketLauncher}",
                NightfallWeapon.PlugOne1 => $"{DestinyEmote.FusionRifle}",
                NightfallWeapon.UzumeRR4 => $"{DestinyEmote.SniperRifle}",
                _ => "Nightfall Weapon Emote"
            };
        }

        public static EmbedBuilder GetNightfallEmbed(Nightfall Nightfall)
        {
            var auth = new EmbedAuthorBuilder
            {
                Name = "Nightfall Information",
                IconUrl = GetStrikeImageURL(Nightfall)
            };
            var foot = new EmbedFooterBuilder
            {
                Text = $"{GetStrikeLocationString(Nightfall)}"
            };
            var embed = new EmbedBuilder
            {
                Color =
                    new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot
            };
            embed.AddField(y =>
            {
                y.Name = "Adept";
                y.Value = $"Recommended Power: {DestinyEmote.Light}1250\n" +
                          "Champions: None.\n" +
                          "Modifiers: ";
                y.IsInline = false;
            });

            embed.Title = $"{GetStrikeNameString(Nightfall)}";
            embed.Description = $"{GetStrikeObjectiveString(Nightfall)}";

            embed.Url = "https://www.bungie.net/img/destiny_content/pgcr/vault_of_glass.jpg";
            embed.ThumbnailUrl =
                "https://www.bungie.net/common/destiny2_content/icons/f2154b781b36b19760efcb23695c66fe.png";

            return embed;
        }

        public static void AddUserTracking(ulong DiscordID, Nightfall? Nightfall, NightfallWeapon? WeaponDrop)
        {
            NightfallLinks.Add(
                new NightfallLink {DiscordID = DiscordID, Nightfall = Nightfall, WeaponDrop = WeaponDrop});
            UpdateJSON();
        }

        public static void RemoveUserTracking(ulong DiscordID)
        {
            NightfallLinks.Remove(GetUserTracking(DiscordID, out _, out _));
            UpdateJSON();
        }

        // Returns null if no tracking is found.
        public static NightfallLink GetUserTracking(ulong DiscordID, out Nightfall? Nightfall,
            out NightfallWeapon? WeaponDrop)
        {
            foreach (var Link in NightfallLinks.Where(Link => Link.DiscordID == DiscordID))
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
                var json = File.ReadAllText(FilePath);
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
            var output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public static DateTime DatePrediction(Nightfall? NightfallStrike, NightfallWeapon? WeaponDrop)
        {
            var iterationWeapons = CurrentRotations.NightfallWeaponDrops;
            var iterationStrike = CurrentRotations.Nightfall;
            var WeeksUntil = 0;
            if (NightfallStrike == null && WeaponDrop != null)
                do
                {
                    iterationWeapons[0] = iterationWeapons[0] >= NightfallWeapon.PlugOne1
                        ? NightfallWeapon.ThePalindrome
                        : iterationWeapons[0] + 2;
                    iterationWeapons[1] = iterationWeapons[1] >= NightfallWeapon.PlugOne1
                        ? NightfallWeapon.TheSWARM
                        : iterationWeapons[1] + 2;
                    WeeksUntil++;
                } while (iterationWeapons[0] != WeaponDrop && iterationWeapons[1] != WeaponDrop);
            else if (WeaponDrop == null && NightfallStrike != null)
                do
                {
                    iterationStrike = iterationStrike == Nightfall.ProvingGrounds
                        ? Nightfall.TheHollowedLair
                        : iterationStrike + 1;
                    WeeksUntil++;
                } while (iterationStrike != NightfallStrike);
            else if (WeaponDrop != null)
                do
                {
                    iterationWeapons[0] = iterationWeapons[0] >= NightfallWeapon.PlugOne1
                        ? NightfallWeapon.ThePalindrome
                        : iterationWeapons[0] + 2;
                    iterationWeapons[1] = iterationWeapons[1] >= NightfallWeapon.PlugOne1
                        ? NightfallWeapon.TheSWARM
                        : iterationWeapons[1] + 2;
                    iterationStrike = iterationStrike == Nightfall.ProvingGrounds
                        ? Nightfall.TheHollowedLair
                        : iterationStrike + 1;
                    WeeksUntil++;
                } while (iterationStrike != NightfallStrike && iterationWeapons[0] != WeaponDrop &&
                         iterationWeapons[1] != WeaponDrop);

            return CurrentRotations.WeeklyResetTimestamp.AddDays(WeeksUntil * 7); // Because there is no .AddWeeks().
        }

        public static Nightfall ActivityPrediction(DateTime Date, out NightfallWeapon[] WeaponDrops)
        {
            var iterationDate = CurrentRotations.WeeklyResetTimestamp;
            var iterationWeapons = CurrentRotations.NightfallWeaponDrops;
            var iterationStrike = CurrentRotations.Nightfall;

            do
            {
                iterationWeapons[0] = iterationWeapons[0] >= NightfallWeapon.PlugOne1
                    ? NightfallWeapon.ThePalindrome
                    : iterationWeapons[0] + 2;
                iterationWeapons[1] = iterationWeapons[1] >= NightfallWeapon.PlugOne1
                    ? NightfallWeapon.TheSWARM
                    : iterationWeapons[1] + 2;
                iterationStrike = iterationStrike == Nightfall.ProvingGrounds
                    ? Nightfall.TheHollowedLair
                    : iterationStrike + 1;
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                iterationDate.AddDays(7);
            } while ((iterationDate - Date).Days >= 7);

            WeaponDrops = iterationWeapons;
            return iterationStrike;
        }

        public class NightfallLink
        {
            [JsonProperty("DiscordID")] public ulong DiscordID { get; set; }

            [JsonProperty("NightfallStrike")] public Nightfall? Nightfall { get; set; } = 0;

            [JsonProperty("WeaponDrop")]
            public NightfallWeapon? WeaponDrop { get; set; } = NightfallWeapon.ThePalindrome;
        }
    }

    public enum Nightfall
    {
        //TheScarletKeep,
        //TheArmsDealer,
        //TheLightblade,
        //TheGlassway,
        //FallenSABER,
        //BirthplaceOfTheVile,
        TheHollowedLair,
        LakeOfShadows,
        ExodusCrash,
        TheCorrupted,
        TheDevilsLair,
        ProvingGrounds
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

        //DutyBound,
        //SiliconNeuroma
        UzumeRR4
    }

    public enum NightfallDifficulty
    {
        Adept,
        Hero,
        Legend,
        Master,
        Grandmaster
    }
}