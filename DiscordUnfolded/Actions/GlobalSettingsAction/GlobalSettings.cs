using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {

    public class GlobalSettings {

        [JsonProperty(PropertyName = "userIdString")]
        public string UserIdString { get; set; }

        public ulong UserID {
           get {
                try {
                    ulong userId = Convert.ToUInt64(UserIdString);
                    return userId;
                }
                catch {
                    return 0;
                }
           }
        }

        [JsonProperty(PropertyName = "maxChannelWidth")]
        public int MaxChannelWidth { get; set; } // should be between 3 and 8

        public GlobalSettings() {
            UserIdString = "000000000000000000";
            MaxChannelWidth = 3;
        }
    }
}
