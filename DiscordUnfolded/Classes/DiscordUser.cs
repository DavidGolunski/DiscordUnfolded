using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {

    public class DiscordUser {

        public ulong UserId { get; set; }
        public string UserName { get; set; }
        public VoiceStates VoiceState { get; set; }
        public string IconUrl { get; set; }



        public DiscordUser(ulong userId, string userName, VoiceStates voiceState, string iconUrl) {
            this.UserId = userId;
            this.UserName = userName;
            this.VoiceState = voiceState;
            this.IconUrl = iconUrl;
        }

        public DiscordUserInfo GetInfo() {
            return new DiscordUserInfo(UserId, UserName, VoiceState, IconUrl);
        }

        public override string ToString() {
            return GetInfo().ToString();
        }
    }

}
