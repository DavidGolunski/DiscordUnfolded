using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {

    internal class ChannelGridNavigationSettings {

        [JsonProperty(PropertyName = "direction")]
        public string Direction { get; set; } // can either be "up", "down", "left" or "right" refering to the 


        public ChannelGridNavigationSettings() {
            Direction = "down";
        }
    }
}
