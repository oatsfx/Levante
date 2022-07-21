using System.Linq;

namespace Levante.Util
{
    // This class contains all of the Discord Emote strings to use for UI elements in Destiny.
    public static class DestinyEmote
    {
        // Class Icons
        public static readonly string Hunter = "<:Hunter:844031780108763146>";
        public static readonly string Titan = "<:Titan:844031790950776842>";
        public static readonly string Warlock = "<:Warlock:844031791119073320>";

        // Activity Modifiers
        public static readonly string AcuteArcBurn = "<:AcuteArcBurn:947296733761765447>";
        public static readonly string AcuteSolarBurn = "<:AcuteSolarBurn:950997186013970453>";
        public static readonly string AcuteVoidBurn = "<:AcuteVoidBurn:954803928883675176>";
        public static readonly string ArachNO = "<:ArachNO:900142007840833576>";
        public static readonly string ArcBurn = "<:ArcBurn:900142018280456213>";
        public static readonly string Attrition = "<:Attrition:900142029487632395>";
        public static readonly string Chaff = "<:Chaff:900142037020598293>";
        public static readonly string Empath = "<:Empath:938569958806392902>";
        public static readonly string Epitaph = "<:Epitaph:900142044368994315>";
        public static readonly string EquipmentLocked = "<:EquipmentLocked:900142051725819965>";
        public static readonly string Extinguish = "<:Extinguish:935313319110254592>";
        public static readonly string Famine = "<:Famine:900142061695672331>";
        public static readonly string FanaticsZeal = "<:FanaticsZeal:935349482302885958>";
        public static readonly string FesteringRupture = "<:FesteringRupture:935355773960220752>";
        public static readonly string FirePit = "<:FirePit:900142069182521345>";
        public static readonly string GrandmasterModifiers = "<:GrandmasterModifiers:935312672856092703>";
        public static readonly string GrasksBile = "<:GrasksBile:944671093296361552>";
        public static readonly string HotKnife = "<:HotKnife:900142076673540096>";
        public static readonly string IgnovunsChallenge = "<:IgnovunsChallenge:938570421534601277>";
        public static readonly string LimitedRevives = "<:LimitedRevives:900142086270115901>";
        public static readonly string Martyr = "<:Martyr:951000502190112808>";
        public static readonly string MatchGame = "<:MatchGame:900142096013459516>";
        public static readonly string Pestilence = "<:Pestilence:987504632433631292>";
        public static readonly string Poleharm = "<:Poleharm:947300080308879371>";
        public static readonly string RaiderShield = "<:RaiderShield:900142103697428562>";
        public static readonly string SediasDurance = "<:SediasDurance:935306050683404288>";
        public static readonly string SepiksGaze = "<:SepiksGaze:937512845111857222>";
        public static readonly string ScorchedEarth = "<:ScorchedEarth:935381186136641546>";
        public static readonly string Shocker = "<:Shocker:900142114426486844>";
        public static readonly string SolarBurn = "<:SolarBurn:900142136996020224>";
        public static readonly string SubtleFoes = "<:SubtleFoes:951000792465285120>";
        public static readonly string ThaviksImplant = "<:ThaviksImplant:935381584159342642>";
        public static readonly string Togetherness = "<:Togetherness:937513440933740584>";
        public static readonly string VoidBurn = "<:VoidBurn:900142150338105404>";

        // Elements
        public static readonly string Arc = "<:Arc:900144187696099359>";
        public static readonly string Kinetic = "<:Kinetic:955691794455232543>";
        public static readonly string Solar = "<:Solar:900144171267022888>";
        public static readonly string Stasis = "<:Stasis:955698714738061362>";
        public static readonly string Void = "<:Void:900144180288958474>";

        // Weapon Types
        public static readonly string AutoRifle = "<:AR:933969813909418047>";
        public static readonly string Bow = "<:Bow:947292944677896202>";
        public static readonly string FusionRifle = "<:Fusion:933969813615829012>";
        public static readonly string GrenadeLauncher = "<:GL:933969813720682496>";
        public static readonly string HandCannon = "<:HC:933969813657776178>";
        public static readonly string HeavyGrenadeLauncher = "<:HeavyGL:933969813770997841>";
        public static readonly string LinearFusionRifle = "<:LFR:933969813674545162>";
        public static readonly string MachineGun = "<:LMG:933969813817151529>";
        public static readonly string PulseRifle = "<:Pulse:933969813745836042>";
        public static readonly string RocketLauncher = "<:Rocket:933969813733265488>";
        public static readonly string SubmachineGun = "<:SMG:933969813922009118>";
        public static readonly string ScoutRifle = "<:Scout:933969813678727258>";
        public static readonly string Shotgun = "<:Shotgun:933969813594837093>";
        public static readonly string Sidearm = "<:Sidearm:933969813678743572>";
        public static readonly string SniperRifle = "<:Sniper:933969813322203198>";
        public static readonly string Sword = "<:Sword:933969814379196456>";

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
        public static readonly string ThroneWorld = "<:ThroneWorld:947293281983799337>";

        // Misc
        public static readonly string AscendantChallengeBounty = "<:ACBounty:934478080737693787>";
        public static readonly string Gilded = "<:Gilded:994067890024235029>";
        public static readonly string GildedPurple = "<:GildedPurple:996604027867500574>";
        public static readonly string Light = "<:LightLevel:844029708077367297>";
        public static readonly string LostSector = "<:LostSector:955004180618174475>";
        public static readonly string Nightfall = "<:Nightfall:934476602006458409>";
        public static readonly string Pattern = "<:Pattern:995752936586092595>";
        public static readonly string RaidChallenge = "<:RaidChallenge:933971625118924820>";
        public static readonly string VoGRaidChallenge = "<:VoGRaidChallenge:933963748295720960>";
        public static readonly string WellspringActivity = "<:WellspringActivity:947293754174365726>";
        public static readonly string VowRaidChallenge = "<:VowRaidChallenge:951003008072831039>";

        // Materials/Currencies
        public static readonly string AscendantAlloy = "<:AscendantAlloy:986514954620403752>";
        public static readonly string AscendantShard = "<:AscendantShard:986514809635897394>";
        public static readonly string BrightDust = "<:BrightDust:986514672532471818>";
        public static readonly string DrownedAlloy = "<:DrownedAlloy:986514928401797150>";
        public static readonly string EnhancementCore = "<:EnhancementCore:986514755458064414>";
        public static readonly string EnhancementPrism = "<:EnhancementPrism:986514778451226744>";
        public static readonly string Glimmer = "<:Glimmer:986514625531088906>";
        public static readonly string LegendaryShards = "<:LegendaryShards:986514643449163776>";
        public static readonly string ResonantAlloy = "<:ResonantAlloy:986514895946268742>";
        public static readonly string ResonantElement = "<:ResonantElement:986676994731278417>";
        public static readonly string SpoilsOfConquest = "<:SpoilsOfConquest:986514871850008616>";
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
                return null;
        }
    }
}
