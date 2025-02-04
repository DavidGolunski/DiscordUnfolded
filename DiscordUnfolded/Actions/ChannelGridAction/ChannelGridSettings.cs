using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {

    internal class ChannelGridSettings {

        [JsonProperty(PropertyName = "xPos")]
        public int XPos { get; set; } // x Position of the button between 0 and 7
        [JsonProperty(PropertyName = "yPos")]
        public int YPos { get; set; } // y Position of the button between 0 and 3


        public ChannelGridSettings() {
            XPos = 0;
            YPos = 0;
        }
    }
}
