using Discord;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded.DiscordStructure {

    public enum VoiceStates {
        DISCONNECTED,
        UNMUTED,
        MUTED,
        DEAFENED,
        SPEAKING
    }

    public class DiscordUserInfo {
        public ulong UserId { get; set; }
        public string UserName { get; set; }
        public VoiceStates VoiceState { get; set; }
        public string IconUrl { get; set; }


        public DiscordUserInfo() {
            UserId = 0;
            UserName = string.Empty;
            VoiceState = VoiceStates.DISCONNECTED;
            IconUrl = string.Empty;
        }


        public DiscordUserInfo(ulong userId, string userName, VoiceStates voiceState, string iconUrl) {
            Debug.Assert(userId != 0);
            this.UserId = userId;
            this.UserName = userName;
            this.VoiceState = voiceState;
            this.IconUrl = iconUrl;
        }




        public override bool Equals(object obj) {
            if(obj == null || obj.GetType() != this.GetType())
                return false;
            DiscordUserInfo other = obj as DiscordUserInfo;

            return this.UserId == other.UserId
                && this.UserName == other.UserName
                && this.VoiceState == other.VoiceState
                && this.IconUrl == other.IconUrl;
        }

        public override int GetHashCode() {
            int hash = 17;

            hash = hash * 23 + (UserId.GetHashCode());
            hash = hash * 23 + (UserName.GetHashCode());
            hash = hash * 23 + (VoiceState.GetHashCode());
            hash = hash * 23 + (IconUrl.GetHashCode());

            return hash;
        }

        public override string ToString() {
            return "User: " + UserId.ToString() + "," + UserName + "," + VoiceState.ToString() + "," + IconUrl;
        }
    }
}
