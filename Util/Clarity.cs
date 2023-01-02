using BungieSharper.Entities.Destiny.HistoricalStats;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Util
{
    public class Clarity
    {
        [JsonProperty("hash")]
        public long Hash { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("itemHash")]
        public long? ItemHash { get; set; }

        [JsonProperty("itemName")]
        public string ItemName { get; set; }

        [JsonProperty("lastUpload")]
        public long LastUpload { get; set; }

        [JsonProperty("stats")]
        public Stats? Stats { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("uploadedBy")]
        public string UploadedBy { get; set; }

        [JsonProperty("descriptions")]
        public Dictionary<string, string> Descriptions { get; set; }
    }

    public class Stats
    {
        [JsonProperty("handling")]
        public List<Stat> Handling { get; set; }

        [JsonProperty("stability")]
        public List<Stat> Stability { get; set; }

        [JsonProperty("reload")]
        public List<Stat> Reload { get; set; }

        [JsonProperty("range")]
        public List<Stat> Range { get; set; }

        [JsonProperty("aimAssist")]
        public List<Stat> AimAssist { get; set; }

        [JsonProperty("zoom")]
        public List<Stat> Zoom { get; set; }

        [JsonProperty("damage")]
        public List<Stat> Damage { get; set; }

        [JsonProperty("stow")]
        public List<Stat> Stow { get; set; }

        [JsonProperty("chargeDrawTime")]
        public List<Stat> ChargeDrawTime { get; set; }

        [JsonProperty("chargeDraw")]
        public List<Stat> ChargeDraw { get; set; }

        [JsonProperty("draw")]
        public List<Stat> Draw { get; set; }
    }

    public class Stat
    {
        [JsonProperty("active")]
        public IndividualStat? Active { get; set; }

        [JsonProperty("passive")]
        public IndividualStat? Passive { get; set; }
    }

    public class IndividualStat
    {
        [JsonProperty("stat")]
        public List<double>? Stat { get; set; }

        [JsonProperty("multiplier")]
        public List<double>? Multiplier { get; set; }
    }

    // Not used at the moment. JSON converter gets angry
    public enum ClarityType
    {
        SubclassGrenade,
        ArmorExotic,
        WeaponPerk,
        WeaponPerkEnhanced,
        WeaponPerkExotic,
        Movement,
        Class,
        WeaponFrame,
        WeaponFrameExotic,
        WeaponOriginTrait,
        WeaponCatalystExotic,
        WeaponMod,
        Super,
        ArmorModActivity,
        ArmorModGeneral,
        Melee,
        Fragment,
    }
}
