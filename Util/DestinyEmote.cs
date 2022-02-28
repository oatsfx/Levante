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
        public static readonly string MatchGame = "<:MatchGame:900142096013459516>";
        public static readonly string Poleharm = "<:Poleharm:947300080308879371>";
        public static readonly string RaiderShield = "<:RaiderShield:900142103697428562>";
        public static readonly string SediasDurance = "<:SediasDurance:935306050683404288>";
        public static readonly string SepiksGaze = "<:SepiksGaze:937512845111857222>";
        public static readonly string ScorchedEarth = "<:ScorchedEarth:935381186136641546>";
        public static readonly string Shocker = "<:Shocker:900142114426486844>";
        public static readonly string SolarBurn = "<:SolarBurn:900142136996020224>";
        public static readonly string ThaviksImplant = "<:ThaviksImplant:935381584159342642>";
        public static readonly string Togetherness = "<:Togetherness:937513440933740584>";
        public static readonly string VoidBurn = "<:VoidBurn:900142150338105404>";

        // Elements
        public static readonly string Arc = "<:Arc:900144187696099359>";
        public static readonly string Solar = "<:Solar:900144171267022888>";
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

        // Weapon Perks

        // Barrels
        public static readonly string ArrowheadBrake = "<:ArrowheadBrake:947664083006656533>";
        public static readonly string ChamberedCompensator = "<:ChamberedCompensator:947664086542479380>";
        public static readonly string CorkscrewRifling = "<:CorkscrewRifling:947664087041593355>";
        public static readonly string ExtendedBarrel = "<:ExtendedBarrel:947664084260778004>";
        public static readonly string FullBore = "<:FullBore:947664085175140362>";
        public static readonly string HammerForgedRifling = "<:HammerForgedRifling:947665786766524446>";
        public static readonly string PolygonalRifling = "<:PolygonalRifling:947664087255506974>";

        public static readonly string EnduringBlade = "<:EnduringBlade:947727236306452520>";
        public static readonly string HonedEdge = "<:HonedEdge:947727236637806592>";
        public static readonly string HungryEdge = "<:HungryEdge:947727232695144478>";
        public static readonly string JaggedEdge = "<:JaggedEdge:947727233940869120>";
        public static readonly string TemperedEdge = "<:TemperedEdge:947727234788118588>";

        // Mags/Rounds
        public static readonly string AccurizedRounds = "<:AccurizedRounds:947664087788175380>";
        public static readonly string AlloyMagazine = "<:AlloyMagazine:947664089742733313>";
        public static readonly string AppendedMag = "<:AppendedMag:947664087821733939>";
        public static readonly string ExtendedMag = "<:ExtendedMag:947664088874504285>";
        public static readonly string FlaredMagwell = "<:FlaredMagwell:947664090124386344>";
        public static readonly string FlutedBarrel = "<:FlutedBarrel:947664085070262303>";
        public static readonly string SteadyRounds = "<:SteadyRounds:947664089436524554>";
        public static readonly string TacticalMag = "<:TacticalMag:947664088257937409>";

        // Guards
        public static readonly string BalancedGuard = "<:BalancedGuard:947727237023674398>";
        public static readonly string BurstGuard = "<:BurstGuard:947727236792999997>";
        public static readonly string EnduringGuard = "<:EnduringGuard:947727237556351016>";
        public static readonly string HeavyGuard = "<:HeavyGuard:947727238042910740>";
        public static readonly string SwordmastersGuard = "<:SwordmastersGuard:947729449078648872>";

        // Other
        public static readonly string AdrenalineJunkie = "<:AdrenalineJunkie:947664095967076412>";
        public static readonly string AutoLoadingHolster = "<:AutoLoadingHolster:947665708681138256>";
        public static readonly string DuelistsTrance = "<:DuelistsTrance:947729672957988864>";
        public static readonly string EagerEdge = "<:EagerEdge:947727239317950524>";
        public static readonly string ElementalCapacitor = "<:ElementalCapacitor:947664096336162826>";
        public static readonly string EnergyTransfer = "<:EnergyTransfer:947727239431221259>";
        public static readonly string FocusedFury = "<:FocusedFury:947664095048507423>";
        public static readonly string Frenzy = "<:Frenzy:947664095547637781>";
        public static readonly string KillingWind = "<:KillingWind:947664091755986945>";
        public static readonly string PerpetualMotion = "<:PerpetualMotion:947664090816446515>";
        public static readonly string PulseMonitor = "<:PulseMonitor:947664093010067466>";
        public static readonly string Rangefinder = "<:Rangefinder:947664096273256449>";
        public static readonly string RelentlessStrikes = "<:RelentlessStrikes:947727240064553000>";
        public static readonly string Smallbore = "<:Smallbore:947664086454390804>";
        public static readonly string SteadyHands = "<:SteadyHands:947664092615802921>";
        public static readonly string Subsistence = "<:Subsistence:947664091806322688>";
        public static readonly string Surrounded = "<:Surrounded:947727241926815764>";
        public static readonly string Thresh = "<:Thresh:947664094394212383>";
        public static readonly string VorpalWeapon = "<:VorpalWeapon:947727242052665345>";
        public static readonly string WhirlwindBlade = "<:WhirlwindBlade:947727241444491325>";

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
        public static readonly string Light = "<:LightLevel:844029708077367297>";
        public static readonly string Nightfall = "<:Nightfall:934476602006458409>";
        public static readonly string RaidBounty = "<:LWRaidBounty:933963726535655425>";
        public static readonly string RaidChallenge = "<:RaidChallenge:933971625118924820>";
        public static readonly string VoGRaidChallenge = "<:VoGRaidChallenge:933963748295720960>";
        public static readonly string WellspringActivity = "<:WellspringActivity:947293754174365726>";

        // Needs to be char for char. Mainly used for Weapon Perks
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
