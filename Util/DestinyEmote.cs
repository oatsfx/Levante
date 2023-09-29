using BungieSharper.Entities.Destiny;
using Serilog;
using System.Linq;
using Levante.Helpers;

namespace Levante.Util
{
    // This class contains all of the Discord Emote strings to use for UI elements in Destiny.
    public static class DestinyEmote
    {
        // Class Icons
        public static readonly string Hunter = "<:Hunter:844031780108763146>";
        public static readonly string Titan = "<:Titan:844031790950776842>";
        public static readonly string Warlock = "<:Warlock:844031791119073320>";

        // Elements
        public static readonly string Arc = "<:Arc:900144187696099359>";
        public static readonly string Kinetic = "<:Kinetic:955691794455232543>";
        public static readonly string Solar = "<:Solar:900144171267022888>";
        public static readonly string Stasis = "<:Stasis:955698714738061362>";
        public static readonly string Strand = "<:Strand:1081668335831371819>";
        public static readonly string Void = "<:Void:900144180288958474>";

        // Weapon Types
        public static readonly string AutoRifle = "<:AR:933969813909418047>";
        public static readonly string Bow = "<:Bow:947292944677896202>";
        public static readonly string FusionRifle = "<:Fusion:933969813615829012>";
        public static readonly string Glaive = "<:Glaive:1062967521336098816>";
        public static readonly string GrenadeLauncher = "<:GL:933969813720682496>";
        public static readonly string HandCannon = "<:HC:933969813657776178>";
        public static readonly string HeavyGrenadeLauncher = "<:HeavyGL:933969813770997841>";
        public static readonly string LinearFusionRifle = "<:LFR:933969813674545162>";
        public static readonly string MachineGun = "<:LMG:933969813817151529>";
        public static readonly string Melee = "<:Melee:1062972320483913818>";
        public static readonly string PulseRifle = "<:Pulse:933969813745836042>";
        public static readonly string RocketLauncher = "<:Rocket:933969813733265488>";
        public static readonly string SubmachineGun = "<:SMG:933969813922009118>";
        public static readonly string ScoutRifle = "<:Scout:933969813678727258>";
        public static readonly string Shotgun = "<:Shotgun:933969813594837093>";
        public static readonly string Sidearm = "<:Sidearm:933969813678743572>";
        public static readonly string SniperRifle = "<:Sniper:933969813322203198>";
        public static readonly string Sword = "<:Sword:933969814379196456>";
        public static readonly string TraceRifle = "<:TraceRifle:1062970952213868614>";

        // Armor Types
        public static readonly string Helmet = "<:Helmet:926269144406577173>";
        public static readonly string Arms = "<:Arms:926269107823853698>";
        public static readonly string Chest = "<:Chest:926269118821306448>";
        public static readonly string Legs = "<:Legs:926269152744853524>";
        public static readonly string Class = "<:Class:926269133660753981>";

        // Champion Types
        public static readonly string Barrier = "<:Barrier:900045481319731221>";
        public static readonly string Overload = "<:Overload:900045490987610172>";
        public static readonly string Unstoppable = "<:Unstoppable:900045499162320896>";

        // Locations
        public static readonly string DreamingCity = "<:DreamingCity:934496727820554341>";
        public static readonly string Europa = "<:Europa:933972047980285972>";
        public static readonly string Luna = "<:Luna:934486787101958205>";
        public static readonly string TerminalOverload = "<:TerminalOverload:1082431658743046254>";
        public static readonly string ThroneWorld = "<:ThroneWorld:947293281983799337>";

        // Misc
        public static readonly string Ada1 = "<:Ada1:1009837298814288003>";
        public static readonly string AscendantChallengeBounty = "<:ACBounty:934478080737693787>";
        public static readonly string Bait = "<:Bait:1115561961980186694>";
        public static readonly string Classified = "<:Classified:1014305658390184006>";
        public static readonly string Commendations = "<:Commendations:1082135754320384001>";
        public static readonly string CERaidChallenge = "<:CrotasEndChallenge:1157358243132227615>";
        public static readonly string Dungeon = "<:Dungeon:1109018943525507102>";
        public static readonly string Emblem = "<:Emblem:1109022520411164755>";
        public static readonly string Enhanced = "<:Enhanced:1020834766397911080>";
        public static readonly string Gilded = "<:Gilded:994067890024235029>";
        public static readonly string GildedPurple = "<:GildedPurple:996604027867500574>";
        public static readonly string GuardianRank = "<:GuardianRank:1081811162808721469>";
        public static readonly string Headshot = "<:Headshot:1062970942369824828>";
        public static readonly string KFRaidChallenge = "<:KFRaidChallenge:1021602526652534864>";
        public static readonly string Light = "<:LightLevel:844029708077367297>";
        public static readonly string Lightfall = "<:Lightfall:1110088351974961182>";
        public static readonly string LostSector = "<:LostSector:955004180618174475>";
        public static readonly string Nightfall = "<:Nightfall:934476602006458409>";
        public static readonly string Pattern = "<:Pattern:995752936586092595>";
        public static readonly string RaidChallenge = "<:RaidChallenge:933971625118924820>";
        public static readonly string RoNRaidChallenge = "<:RoNRaidChallenge:1106771755047059506>";
        public static readonly string Shadowkeep = "<:Shadowkeep:1110088049511112744>";
        public static readonly string VoGRaidChallenge = "<:VoGRaidChallenge:933963748295720960>";
        public static readonly string WellspringActivity = "<:WellspringActivity:947293754174365726>";
        public static readonly string WitchQueen = "<:WitchQueen:1110088262527225936>";
        public static readonly string VowRaidChallenge = "<:VowRaidChallenge:951003008072831039>";

        // Materials/Currencies
        public static readonly string AscendantAlloy = "<:AscendantAlloy:986514954620403752>";
        public static readonly string AscendantShard = "<:AscendantShard:986514809635897394>";
        public static readonly string BrightDust = "<:BrightDust:986514672532471818>";
        public static readonly string EnhancementCore = "<:EnhancementCore:986514755458064414>";
        public static readonly string EnhancementPrism = "<:EnhancementPrism:986514778451226744>";
        public static readonly string Glimmer = "<:Glimmer:986514625531088906>";
        public static readonly string HarmonicAlloy = "<:HarmonicAlloy:986514928401797150>";
        public static readonly string HerealwaysPieces = "<:HerealwayPieces:1020468055853240320>";
        public static readonly string LegendaryShards = "<:LegendaryShards:986514643449163776>";
        public static readonly string ParaversalHauls = "<:ParaversalHauls:1020468359260811274>";
        public static readonly string PhantasmalFragments = "<:PhantasmalFragments:1020468360464576602>";
        public static readonly string RaidBanners = "<:RaidBanners:1020468791244763226>";
        public static readonly string ResonantAlloy = "<:ResonantAlloy:986514895946268742>";
        public static readonly string SpoilsOfConquest = "<:SpoilsOfConquest:986514871850008616>";
        public static readonly string StrandMeditations = "<:StrandMeditations:1082072750518173737>";
        public static readonly string StrangeCoins = "<:StrangeCoins:1020469985132425348>";
        public static readonly string TerminalOverloadKey = "<:TerminalOverloadKey:1082072793002287226>";
        public static readonly string TinctureOfQueensfoil = "<:TinctureOfQueensfoil:1020468790078738462>";
        public static readonly string TreasureKeys = "<:TreasureKeys:1020468358229016596>";
        public static readonly string UpgradeModule = "<:UpgradeModule:986514733609926656>";

        // Stats
        public static readonly string Mobility = "<:Mobility:994283641259708417>";
        public static readonly string Resilience = "<:Resilience:994283643965026334>";
        public static readonly string Recovery = "<:Recovery:994283642694140005>";
        public static readonly string Discipline = "<:Discipline:994283639019946114>";
        public static readonly string Intellect = "<:Intellect:994283640349536357>";
        public static readonly string Strength = "<:Strength:994283644938100746>";

        // Needs to be char for char.
        public static string MatchEmote(string Query)
        {
            Query = Query.Replace(" ", "").Replace("-", "").Replace("'", "");
            if (typeof(DestinyEmote).GetField(Query) != null)
                return (string)typeof(DestinyEmote).GetField(Query).GetValue(null);
            else
                return Classified;
        }

        // Parses Bungie's "Emotes" ("[Glaive]") into our app's Discord emotes.
        public static string ParseBungieText(string Text)
        {
            if (!Text.Contains('[')) return Text;
            int replacements = Text.Count(x => x == '[');
            for (int i = 0; i < replacements; i++)
            {
                int leftIndex = Text.IndexOf('[') + 1;
                int rightIndex = Text.IndexOf(']');

                string inBetween = Text[leftIndex..rightIndex];
                Text = Text.Replace($"[{inBetween}]", MatchEmote(inBetween));
            }
            return Text;
        }

        public static string ParseBungieVars(string Text)
        {
            if (!Text.Contains('{')) return Text;
            int replacements = Text.Count(x => x == '{');
            for (int i = 0; i < replacements; i++)
            {
                int leftIndex = Text.IndexOf('{') + 1;
                int rightIndex = Text.IndexOf('}');

                string inBetween = Text[leftIndex..rightIndex];
                int colonIndex = inBetween.IndexOf(':') + 1;
                string variableHash = Text[(leftIndex+colonIndex)..rightIndex];
                Text = Text.Replace($"{{{inBetween}}}", $"{ManifestHelper.StringVariables[variableHash]}");
            }
            return Text;
        }

        public static string MatchWeaponItemSubtypeToEmote(DestinyItemSubType subType, bool HeavyGLOverride = false)
        {
            if (HeavyGLOverride)
                return HeavyGrenadeLauncher;

            switch (subType)
            {
                case DestinyItemSubType.AutoRifle: return AutoRifle;
                case DestinyItemSubType.Bow: return Bow;
                case DestinyItemSubType.FusionRifle: return FusionRifle;
                case DestinyItemSubType.GrenadeLauncher: return GrenadeLauncher;
                case DestinyItemSubType.HandCannon: return HandCannon;
                case DestinyItemSubType.FusionRifleLine: return LinearFusionRifle;
                case DestinyItemSubType.Machinegun: return MachineGun;
                case DestinyItemSubType.PulseRifle: return PulseRifle;
                case DestinyItemSubType.RocketLauncher: return RocketLauncher;
                case DestinyItemSubType.SubmachineGun: return SubmachineGun;
                case DestinyItemSubType.ScoutRifle: return ScoutRifle;
                case DestinyItemSubType.Shotgun: return Shotgun;
                case DestinyItemSubType.Sidearm: return Sidearm;
                case DestinyItemSubType.SniperRifle: return SniperRifle;
                case DestinyItemSubType.Sword: return Sword;
                default: return Classified;
            }
        }
    }
}
