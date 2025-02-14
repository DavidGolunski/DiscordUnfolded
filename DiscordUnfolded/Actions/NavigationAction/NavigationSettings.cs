using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace DiscordUnfolded {
    public class NavigationSettings {


        [JsonProperty(PropertyName = "direction")]
        public string Direction { get; set; } // can either be "up" or "down" refering to the 

        [JsonProperty(PropertyName = "navigationType")]
        public string NavigationType { get; set; } // can either be "server" or "channel"


        public NavigationSettings() {
            Direction = "down";
            NavigationType = "channel";
        }
    }
}
