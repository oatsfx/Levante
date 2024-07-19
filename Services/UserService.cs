using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Services
{
    internal class UserService
    {
    }

    public class User
    {
        [JsonProperty("UserId")]
        public ulong UserId { get; set; } = 0;

        [JsonProperty("BungieCredentials")]
        public BungieUserCredentials BungieCredentials { get; set; } = new();

        [JsonProperty("BungieCredentials")]
        public UserSettings Settings { get; set; } = new();
    }

    public class BungieUserCredentials
    {
        [JsonProperty("MembershipId")]
        public string MembershipId { get; set; }

        [JsonProperty("MembershipType")]
        public string MembershipType { get; set; }

        [JsonProperty("BungieName")]
        public string BungieName { get; set; }

        [JsonProperty("AccessToken")]
        public string AccessToken { get; set; }

        [JsonProperty("RefreshToken")]
        public string RefreshToken { get; set; }

        [JsonProperty("AccessExpiration")]
        public DateTime AccessExpiration { get; set; } = DateTime.Now;

        [JsonProperty("RefreshExpiration")]
        public DateTime RefreshExpiration { get; set; } = DateTime.Now;
    }

    public class UserSettings
    {
        [JsonProperty("IsSupporter", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool IsSupporter { get; set; } = false;

        [JsonProperty("IsStaff", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool IsStaff { get; set; } = false;

        [JsonProperty("IsDeveloper", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool IsDeveloper { get; set; } = false;
    }
}
