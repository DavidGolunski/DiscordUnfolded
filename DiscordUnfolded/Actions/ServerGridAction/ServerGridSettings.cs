using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {

    internal class ServerGridSettings {

        [JsonProperty(PropertyName = "position")]
        public int Position { get; set; } // can either be "up" or "down" refering to the 


        public ServerGridSettings() {
            Position = 0;
        }
    }
}
