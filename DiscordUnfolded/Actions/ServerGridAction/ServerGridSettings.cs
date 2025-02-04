using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {

    internal class ServerGridSettings {

        [JsonProperty(PropertyName = "position")]
        public int Position { get; set; } // position of the button between 0 and 7


        public ServerGridSettings() {
            Position = 0;
        }
    }
}
