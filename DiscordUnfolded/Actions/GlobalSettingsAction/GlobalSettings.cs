using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {

    public class GlobalSettings {

        [JsonProperty(PropertyName = "clientId")]
        public string ClientId { get; set; }

        [JsonProperty(PropertyName = "clientSecret")]
        public string ClientSecret {  get; set; }

        [JsonProperty(PropertyName = "maxChannelWidth")]
        public int MaxChannelWidth { get; set; } // should be between 3 and 8

        [JsonProperty(PropertyName = "defaultGuildIdString")]
        public string DefaultGuildIdString { get; set; }    

        public GlobalSettings() {
            ClientId = string.Empty;
            ClientSecret = string.Empty;
            MaxChannelWidth = 3;
            DefaultGuildIdString = string.Empty;
        }

        public override string ToString() {
            return "ClientID: " + ClientId + " ClientSecret: " + ClientSecret + " MaxChannelWidth: " + MaxChannelWidth + " DefaultGuildIdString: " + DefaultGuildIdString;
        }
    }
}
