using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {

    internal class ServerGridNavigationSettings {

        [JsonProperty(PropertyName = "direction")]
        public string Direction { get; set; } // can either be "up" or "down" refering to the 


        public ServerGridNavigationSettings() {
            Direction = "down";
        }
    }
}
