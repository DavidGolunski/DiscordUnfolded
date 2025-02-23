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
        DEAFENED
    }

    public class DiscordUserInfo {
        public ulong UserId { get; set; }
        public string UserName { get; set; }
        public VoiceStates VoiceState { get; set; }
        public string IconUrl { get; set; }
        public bool IsSpeaking { get; set; }
        public int Volume { get; set; }


        public DiscordUserInfo() {
            UserId = 0;
            UserName = string.Empty;
            VoiceState = VoiceStates.DISCONNECTED;
            IconUrl = string.Empty;
            IsSpeaking = false;
            Volume = 100;
        }


        public DiscordUserInfo(ulong userId, string userName, VoiceStates voiceState, string iconUrl, bool isSpeaking, int volume) {
            Debug.Assert(userId != 0);
            this.UserId = userId;
            this.UserName = userName;
            this.VoiceState = voiceState;
            this.IconUrl = iconUrl;
            this.IsSpeaking = isSpeaking;
            this.Volume = volume;
        }




        public override bool Equals(object obj) {
            if(obj == null || obj.GetType() != this.GetType())
                return false;
            DiscordUserInfo other = obj as DiscordUserInfo;

            return this.UserId == other.UserId
                && this.UserName == other.UserName
                && this.VoiceState == other.VoiceState
                && this.IconUrl == other.IconUrl
                && this.IsSpeaking == other.IsSpeaking
                && this.Volume == other.Volume;
        }

        public override int GetHashCode() {
            int hash = 17;

            hash = hash * 23 + (UserId.GetHashCode());
            hash = hash * 23 + (UserName.GetHashCode());
            hash = hash * 23 + (VoiceState.GetHashCode());
            hash = hash * 23 + (IconUrl.GetHashCode());
            hash = hash * 23 + (IsSpeaking.GetHashCode());
            hash = hash * 23 + (Volume.GetHashCode());

            return hash;
        }

        public override string ToString() {
            return "User: " + UserId.ToString() + "," + UserName + "," + VoiceState.ToString() + "," + IconUrl + "," + IsSpeaking + "," + Volume;
        }
    }
}
