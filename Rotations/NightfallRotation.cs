using Levante.Configs;
using Levante.Util;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Levante.Rotations
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
            public NightfallWeapon? WeaponDrop { get; set; } = NightfallWeapon.SiliconNeuroma;
        }

        public static string GetStrikeNameString(Nightfall Nightfall)
        {
            switch (Nightfall)
            {
                //case Nightfall.TheScarletKeep: return "The Scarlet Keep";
                case Nightfall.TheArmsDealer: return "The Arms Dealer";
                //case Nightfall.TheLightblade: return "The Lightblade";
                //case Nightfall.TheGlassway: return "The Glassway";
                //case Nightfall.FallenSABER: return "The Fallen S.A.B.E.R.";
                //case Nightfall.BirthplaceOfTheVile: return "Birthplace of the Vile";

                //case Nightfall.TheHollowedLair: return "The Hollowed Lair";
                //case Nightfall.LakeOfShadows: return "Lake of Shadows";
                //case Nightfall.ExodusCrash: return "Exodus Crash";
                case Nightfall.TheCorrupted: return "The Corrupted";
                //case Nightfall.TheDevilsLair: return "The Devils' Lair";
                case Nightfall.ProvingGrounds: return "Proving Grounds";

                case Nightfall.TheInsightTerminus: return "The Insight Terminus";
                case Nightfall.WardenOfNothing: return "Warden of Nothing";
                case Nightfall.TheInvertedSpire: return "The Inverted Spire";

                default: return "Nightfall: The Ordeal";
            }
        }

        public static string GetStrikeBossString(Nightfall Nightfall)
        {
            switch (Nightfall)
            {
                //case Nightfall.TheScarletKeep: return "Hashladûn, Daughter of Crota";
                case Nightfall.TheArmsDealer: return "Bracus Zahn";
                //case Nightfall.TheLightblade: return "Alak-Hul, The Lightblade";
                //case Nightfall.TheGlassway: return "Belmon, Transcendent Mind";
                //case Nightfall.FallenSABER: return "S.A.B.E.R.-2";
                //case Nightfall.BirthplaceOfTheVile: return "Heimiks, Warden of the Harvest";
                //case Nightfall.TheHollowedLair: return "Fikrul, the Fanatic";
                //case Nightfall.LakeOfShadows: return "Grask, the Consumed";
                //case Nightfall.ExodusCrash: return "Thaviks, the Depraved";
                //case Nightfall.TheCorrupted: return "Sedia, the Corrupted";
                //case Nightfall.TheDevilsLair: return "Sepiks Prime";
                //case Nightfall.ProvingGrounds: return "Ignovun, Chosen of Caiatl";
                default: return "Nightfall: The Ordeal Boss";
            }
        }

        public static string GetStrikeImageURL(Nightfall Nightfall)
        {
            switch (Nightfall)
            {
                //case Nightfall.TheScarletKeep: return "https://www.bungie.net/img/destiny_content/pgcr/strike_the_scarlet_keep.jpg";
                case Nightfall.TheArmsDealer: return "https://www.bungie.net/img/destiny_content/pgcr/strike_the_arms_dealer.jpg";
                //case Nightfall.TheLightblade: return "https://www.bungie.net/img/destiny_content/pgcr/strike_lightblade.jpg";
                //case Nightfall.TheGlassway: return "https://www.bungie.net/img/destiny_content/pgcr/europa-strike-blackbird.jpg";
                //case Nightfall.FallenSABER: return "https://www.bungie.net/img/destiny_content/pgcr/cosmodrome_fallen_saber.jpg";
                //case Nightfall.BirthplaceOfTheVile: return "https://www.bungie.net/img/destiny_content/pgcr/strike_birthplace.jpg";
                //case Nightfall.TheHollowedLair: return "https://www.bungie.net/img/destiny_content/pgcr/strike_taurus.jpg";
                //case Nightfall.LakeOfShadows: return "https://www.bungie.net/img/destiny_content/pgcr/strike_lake_of_shadows.jpg";
                //case Nightfall.ExodusCrash: return "https://www.bungie.net/img/destiny_content/pgcr/strike_exodus_crash.jpg";
                //case Nightfall.TheCorrupted: return "https://www.bungie.net/img/destiny_content/pgcr/strike_gemini.jpg";
                //case Nightfall.TheDevilsLair: return "https://www.bungie.net/img/destiny_content/pgcr/cosmodrome_devils_lair.jpg";
                //case Nightfall.ProvingGrounds: return "https://www.bungie.net/img/destiny_content/pgcr/nessus_proving_grounds.jpg";
                default: return "https://www.bungie.net/common/destiny2_content/icons/f2154b781b36b19760efcb23695c66fe.png";
            }
        }

        public static string GetStrikeObjectiveString(Nightfall Nightfall)
        {
            switch (Nightfall)
            {
                //case Nightfall.TheScarletKeep: return "Investigate the recently erected Scarlet Keep and discover its dark purpose.";
                case Nightfall.TheArmsDealer: return "Shut down the operations of an ironmonger providing weapons to the Red Legion.";
                //case Nightfall.TheLightblade: return "Recover an artifact from a monument to Oryx, located deep in the swamps of Savathûn's throne world.";
                //case Nightfall.TheGlassway: return "Prevent ancient and powerful Vex from escaping the Glassway on Europa.";
                //case Nightfall.FallenSABER: return "Enter Rasputin's bunker in the Cosmodrome and discover the source of the security breach.";
                //case Nightfall.BirthplaceOfTheVile: return "Aided by the Witness, the Scorn have gained the power to break into the Throne World through areas the Light cannot touch. Beat them back.";
                //case Nightfall.TheHollowedLair: return "The Fanatic has returned. Take him down and finish the job you started.";
                //case Nightfall.LakeOfShadows: return "Stem the tide of Taken flowing into the European Dead Zone from beneath the waves.";
                //case Nightfall.ExodusCrash: return "Purge the Fallen infestation of the Exodus Black.";
                //case Nightfall.TheCorrupted: return "Hunt down one of Queen Mara's most trusted advisors and free her from Taken possession.";
                //case Nightfall.TheDevilsLair: return "Enter into the Devils' Lair and weaken the Fallen presence within the Cosmodrome.";
                //case Nightfall.ProvingGrounds: return "Defeat Caiatl's Chosen aboard the Land Tank, Halphas Electus, on Nessus.";
                default: return "Clear the nightfall.";
            }
        }

        public static string GetStrikeLocationString(Nightfall Nightfall)
        {
            switch (Nightfall)
            {
                //case Nightfall.TheScarletKeep: return "Scarlet Keep, The Moon";
                case Nightfall.TheArmsDealer: return "European Dead Zone, Earth";
                //case Nightfall.TheLightblade: return "Court of Savathûn, Throne World";
                //case Nightfall.TheGlassway: return "Rathmore Chaos, Europa";
                //case Nightfall.FallenSABER: return "The Cosmodrome, Earth";
                //case Nightfall.BirthplaceOfTheVile: return "Court of Savathûn, Throne World";
                //case Nightfall.TheHollowedLair: return "The Tangled Shore, The Reef";
                //case Nightfall.LakeOfShadows: return "European Dead Zone, Earth";
                //case Nightfall.ExodusCrash: return "Arcadian Valley, Nessus";
                case Nightfall.TheCorrupted: return "The Dreaming City, The Reef";
                //case Nightfall.TheDevilsLair: return "The Cosmodrome, Earth";
                case Nightfall.ProvingGrounds: return "Halphas Electus, Nessus.";
                default: return "The Sol System, Space";
            }
        }

        public static string GetStrikeModifiers(Nightfall Nightfall, NightfallDifficulty Difficulty)
        {
            switch (Nightfall)
            {
                /*case Nightfall.TheScarletKeep:
                    {
                        switch (Difficulty)
                        {
                            case NightfallDifficulty.Adept: return $"{DestinyEmote.Empath} {DestinyEmote.AcuteArcBurn}";
                            case NightfallDifficulty.Hero: return $"{DestinyEmote.Empath} {DestinyEmote.Poleharm} {DestinyEmote.AcuteArcBurn}";
                            case NightfallDifficulty.Legend: return $"{DestinyEmote.Empath} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.Poleharm} {DestinyEmote.AcuteArcBurn}";
                            case NightfallDifficulty.Master: return $"{DestinyEmote.Empath} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.Togetherness} {DestinyEmote.Poleharm} {DestinyEmote.AcuteArcBurn}";
                            case NightfallDifficulty.Grandmaster: return $"{DestinyEmote.FirePit} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.MatchGame} {DestinyEmote.EquipmentLocked}" +
                                    $"{DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.AcuteArcBurn}";
                            default: return "None.";
                        }
                    }
                case Nightfall.TheArmsDealer: // Unknown, don't have in-game screenshots
                    {
                        switch (Difficulty)
                        {
                            case NightfallDifficulty.Adept: return $"";
                            case NightfallDifficulty.Hero: return $"";
                            case NightfallDifficulty.Legend: return $"";
                            case NightfallDifficulty.Master: return $"";
                            case NightfallDifficulty.Grandmaster: return $"";
                            default: return "None.";
                        }
                    }
                case Nightfall.TheLightblade:
                    {
                        switch (Difficulty)
                        {
                            case NightfallDifficulty.Adept: return $"{DestinyEmote.Martyr} {DestinyEmote.AcuteArcBurn}";
                            case NightfallDifficulty.Hero: return $"{DestinyEmote.Martyr} {DestinyEmote.SubtleFoes} {DestinyEmote.AcuteArcBurn}";
                            case NightfallDifficulty.Legend: return $"{DestinyEmote.Martyr} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.SubtleFoes} {DestinyEmote.AcuteArcBurn}";
                            case NightfallDifficulty.Master: return $"{DestinyEmote.Martyr} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.Chaff} {DestinyEmote.SubtleFoes} {DestinyEmote.AcuteArcBurn}";
                            case NightfallDifficulty.Grandmaster: return $"{DestinyEmote.Empath} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.MatchGame} {DestinyEmote.EquipmentLocked}" +
                                    $"{DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.AcuteArcBurn}";
                            default: return "None.";
                        }
                    }
                case Nightfall.TheGlassway:
                    {
                        switch (Difficulty)
                        {
                            case NightfallDifficulty.Adept: return $"{DestinyEmote.ArachNO} {DestinyEmote.AcuteVoidBurn}";
                            case NightfallDifficulty.Hero: return $"{DestinyEmote.ArachNO} {DestinyEmote.Poleharm} {DestinyEmote.AcuteVoidBurn}";
                            case NightfallDifficulty.Legend: return $"{DestinyEmote.ArachNO} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.Poleharm} {DestinyEmote.AcuteVoidBurn}";
                            case NightfallDifficulty.Master: return $"{DestinyEmote.ArachNO} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.Famine} {DestinyEmote.Poleharm} {DestinyEmote.AcuteVoidBurn}";
                            case NightfallDifficulty.Grandmaster: return $"{DestinyEmote.ArachNO} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.MatchGame} {DestinyEmote.EquipmentLocked}" +
                                    $"{DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.AcuteVoidBurn}";
                            default: return "None.";
                        }
                    }*/
                /*case Nightfall.TheHollowedLair:
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
                        case NightfallDifficulty.Adept: return $"{DestinyEmote.Empath}";
                        case NightfallDifficulty.Hero: return $"{DestinyEmote.Empath} {DestinyEmote.GrasksBile}";
                        case NightfallDifficulty.Legend: return $"{DestinyEmote.Empath} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.GrasksBile}";
                        case NightfallDifficulty.Master: return $"{DestinyEmote.Empath} {DestinyEmote.Famine} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.GrasksBile}";
                        case NightfallDifficulty.Grandmaster: return $"{DestinyEmote.Epitaph} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.MatchGame} {DestinyEmote.EquipmentLocked}" +
                                $"{DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.GrasksBile}";
                        default: return "None.";
                    }
                case Nightfall.ExodusCrash:
                    switch (Difficulty)
                    {
                        case NightfallDifficulty.Adept: return $"{DestinyEmote.ScorchedEarth}";
                        case NightfallDifficulty.Hero: return $"{DestinyEmote.ScorchedEarth} {DestinyEmote.ThaviksImplant}";
                        case NightfallDifficulty.Legend: return $"{DestinyEmote.ScorchedEarth} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.ThaviksImplant}";
                        case NightfallDifficulty.Master: return $"{DestinyEmote.ScorchedEarth} {DestinyEmote.Chaff} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.ThaviksImplant}";
                        case NightfallDifficulty.Grandmaster: return $"{DestinyEmote.ArachNO} {DestinyEmote.ScorchedEarth} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.MatchGame} " +
                                $"{DestinyEmote.EquipmentLocked} {DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.ThaviksImplant}";
                        default: return "None.";
                    }
                case Nightfall.TheCorrupted:
                    switch (Difficulty)
                    {
                        case NightfallDifficulty.Adept: return $"{DestinyEmote.Epitaph}";
                        case NightfallDifficulty.Hero: return $"{DestinyEmote.Epitaph} {DestinyEmote.SediasDurance}";
                        case NightfallDifficulty.Legend: return $"{DestinyEmote.Epitaph} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.SediasDurance}";
                        case NightfallDifficulty.Master: return $"{DestinyEmote.Epitaph} {DestinyEmote.Famine} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.SediasDurance}";
                        case NightfallDifficulty.Grandmaster: return $"{DestinyEmote.Epitaph} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.MatchGame} {DestinyEmote.EquipmentLocked} " +
                                $"{DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.SediasDurance}";
                        default: return "None.";
                    }
                case Nightfall.TheDevilsLair:
                    switch (Difficulty)
                    {
                        case NightfallDifficulty.Adept: return $"{DestinyEmote.HotKnife}";
                        case NightfallDifficulty.Hero: return $"{DestinyEmote.HotKnife} {DestinyEmote.SepiksGaze}";
                        case NightfallDifficulty.Legend: return $"{DestinyEmote.HotKnife} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.SepiksGaze}";
                        case NightfallDifficulty.Master: return $"{DestinyEmote.HotKnife} {DestinyEmote.Togetherness} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.SepiksGaze}";
                        case NightfallDifficulty.Grandmaster: return $"{DestinyEmote.ArachNO} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} " +
                                $"{DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.SepiksGaze}";
                        default: return "None.";
                    }
                case Nightfall.ProvingGrounds:
                    switch (Difficulty)
                    {
                        case NightfallDifficulty.Adept: return $"{DestinyEmote.Empath}";
                        case NightfallDifficulty.Hero: return $"{DestinyEmote.Empath} {DestinyEmote.IgnovunsChallenge}";
                        case NightfallDifficulty.Legend: return $"{DestinyEmote.Empath} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.IgnovunsChallenge}";
                        case NightfallDifficulty.Master: return $"{DestinyEmote.Empath} {DestinyEmote.Attrition} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} {DestinyEmote.IgnovunsChallenge}";
                        case NightfallDifficulty.Grandmaster: return $"{DestinyEmote.ScorchedEarth} {DestinyEmote.Chaff} {DestinyEmote.GrandmasterModifiers} {DestinyEmote.EquipmentLocked} {DestinyEmote.MatchGame} " +
                                $"{DestinyEmote.Extinguish} {DestinyEmote.LimitedRevives} {DestinyEmote.IgnovunsChallenge}";
                        default: return "None.";
                    }*/
                default: return "None.";
            }
        }

        public static string GetWeaponString(NightfallWeapon Weapon)
        {
            switch (Weapon)
            {
                //case NightfallWeapon.ThePalindrome: return "The Palindrome";
                //case NightfallWeapon.TheSWARM: return "THE S.W.A.R.M.";
                //case NightfallWeapon.TheComedian: return "The Comedian";
                //case NightfallWeapon.ShadowPrice: return "Shadow Price";
                //case NightfallWeapon.HungJurySR4: return "Hung Jury SR4";
                case NightfallWeapon.TheHothead: return "The Hothead";
                case NightfallWeapon.PlugOne1: return "PLUG ONE.1";
                //case NightfallWeapon.UzumeRR4: return "Uzume RR4";
                case NightfallWeapon.DutyBound: return "Duty Bound";
                case NightfallWeapon.SiliconNeuroma: return "Silicon Neuroma";
                case NightfallWeapon.DFA: return "D.F.A.";
                case NightfallWeapon.HorrorsLeast: return "Horror's Least";
                default: return "Nightfall Weapon";
            }
        }

        public static string GetWeaponEmote(NightfallWeapon Weapon)
        {
            switch (Weapon)
            {
                //case NightfallWeapon.ThePalindrome: return $"{DestinyEmote.HandCannon}";
                //case NightfallWeapon.TheSWARM: return $"{DestinyEmote.MachineGun}";
                //case NightfallWeapon.TheComedian: return $"{DestinyEmote.Shotgun}";
                //case NightfallWeapon.ShadowPrice: return $"{DestinyEmote.AutoRifle}";
                //case NightfallWeapon.HungJurySR4: return $"{DestinyEmote.ScoutRifle}";
                case NightfallWeapon.TheHothead: return $"{DestinyEmote.RocketLauncher}";
                case NightfallWeapon.PlugOne1: return $"{DestinyEmote.FusionRifle}";
                //case NightfallWeapon.UzumeRR4: return $"{DestinyEmote.SniperRifle}";
                case NightfallWeapon.DutyBound: return $"{DestinyEmote.AutoRifle}";
                case NightfallWeapon.SiliconNeuroma: return $"{DestinyEmote.SniperRifle}";
                case NightfallWeapon.DFA: return $"{DestinyEmote.HandCannon}";
                case NightfallWeapon.HorrorsLeast: return $"{DestinyEmote.PulseRifle}";
                default: return "Nightfall Weapon Emote";
            }
        }

        public static string GetDifficultyPower(NightfallDifficulty Difficulty)
        {
            switch (Difficulty)
            {
                case NightfallDifficulty.Adept: return $"1490";
                case NightfallDifficulty.Hero: return $"1530";
                case NightfallDifficulty.Legend: return $"1560";
                case NightfallDifficulty.Master: return $"1590";
                case NightfallDifficulty.Grandmaster: return $"1610";
                default: return "Nightfall Power Level";
            }
        }

        public static EmbedBuilder GetNightfallEmbed(Nightfall Nightfall, NightfallDifficulty Difficulty)
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
                y.Value = $"Recommended Power: {DestinyEmote.Light}{GetDifficultyPower(Difficulty)}\n" +
                    $"Champions: \n" +
                    $"Modifiers: {GetStrikeModifiers(Nightfall, Difficulty)}";
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
            NightfallWeapon iterationWeapon = CurrentRotations.NightfallWeaponDrop;
            Nightfall iterationStrike = CurrentRotations.Nightfall;
            int WeeksUntil = 0;
            if (NightfallStrike == null && WeaponDrop != null)
            {
                do
                {
                    iterationWeapon = iterationWeapon >= NightfallWeapon.PlugOne1 ? NightfallWeapon.DutyBound : iterationWeapon + 1;
                    WeeksUntil++;
                } while (iterationWeapon != WeaponDrop);
            }
            else if (WeaponDrop == null && NightfallStrike != null)
            {
                do
                {
                    iterationStrike = iterationStrike == Nightfall.TheArmsDealer ? Nightfall.ProvingGrounds : iterationStrike + 1;
                    WeeksUntil++;
                } while (iterationStrike != NightfallStrike);
            }
            else if (WeaponDrop != null && NightfallStrike != null)
            {
                do
                {
                    iterationWeapon = iterationWeapon >= NightfallWeapon.PlugOne1 ? NightfallWeapon.SiliconNeuroma : iterationWeapon + 1;
                    iterationStrike = iterationStrike == Nightfall.TheArmsDealer ? Nightfall.ProvingGrounds : iterationStrike + 1;
                    WeeksUntil++;
                } while (iterationStrike != NightfallStrike && iterationWeapon != WeaponDrop);
            }
            return CurrentRotations.WeeklyResetTimestamp.AddDays(WeeksUntil * 7); // Because there is no .AddWeeks().
        }

        public static Nightfall ActivityPrediction(DateTime Date, out NightfallWeapon WeaponDrop)
        {
            DateTime iterationDate = CurrentRotations.WeeklyResetTimestamp;
            NightfallWeapon iterationWeapon = CurrentRotations.NightfallWeaponDrop;
            Nightfall iterationStrike = CurrentRotations.Nightfall;

            do
            {
                iterationWeapon = iterationWeapon >= NightfallWeapon.PlugOne1 ? NightfallWeapon.SiliconNeuroma : iterationWeapon + 1;
                iterationStrike = iterationStrike == Nightfall.TheArmsDealer ? Nightfall.ProvingGrounds : iterationStrike + 1;
                iterationDate.AddDays(7);
            } while ((iterationDate - Date).Days >= 7);

            WeaponDrop = iterationWeapon;
            return iterationStrike;
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
        //TheHollowedLair,
        //LakeOfShadows,
        //ExodusCrash,
        //TheCorrupted,
        //TheDevilsLair,
        //ProvingGrounds,

        ProvingGrounds,
        TheInsightTerminus,
        WardenOfNothing,
        TheCorrupted,
        TheInvertedSpire,
        TheArmsDealer,
    }

    public enum NightfallWeapon
    {
        //ThePalindrome,
        //TheSWARM, // Gone
        //TheComedian,
        //ShadowPrice, // Gone
        //HungJurySR4, // Gone
        //TheHothead,
        //PlugOne1,
        //UzumeRR4, // Gone

        //DutyBound,
        //SiliconNeuroma,
        //TheComedian,
        //TheHothead,

        SiliconNeuroma,
        DFA,
        DutyBound,
        HorrorsLeast,
        TheHothead,
        PlugOne1,
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
