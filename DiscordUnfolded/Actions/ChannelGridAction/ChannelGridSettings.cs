using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {

    public class XPosItem {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        public XPosItem(string text, string value) {
            Text = text;
            Value = value;
        }
    }

    public class ChannelGridSettings {

        [JsonProperty("xPosDropdown")]
        public List<XPosItem> XPosDropdown { get; set; }


        [JsonProperty(PropertyName = "xPos")]
        public int XPos { get; set; } // x Position of the button between 0 and 7
        [JsonProperty(PropertyName = "yPos")]
        public int YPos { get; set; } // y Position of the button between 0 and 3
        
        
        public ChannelGridSettings() {
            XPosDropdown = new List<XPosItem>();
            for(int i = 0; i < 8; i++) {
                XPosDropdown.Add(new XPosItem("Position " + (i + 1).ToString(), i.ToString()));
            }
            XPos = 0;
            YPos = 0;
        }

        public void UpdateXPosDropdown(int newLength) {
            if(newLength == XPosDropdown.Count || newLength < 3 || newLength > 8)
                return;

            XPosDropdown.Clear();
            for(int i = 0; i < newLength; i++) {
                XPosDropdown.Add(new XPosItem("Position " + (i + 1).ToString(), i.ToString()));
            }

            if(XPos >= newLength) {
                XPos = 0;
            }
        }

    }
}
