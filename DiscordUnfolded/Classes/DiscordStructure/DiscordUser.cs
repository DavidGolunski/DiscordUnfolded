using BarRaider.SdTools;
using Discord;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded.DiscordStructure {

    public class DiscordUser : IDisposable {

        private DiscordVoiceChannel voiceChannel;
        public readonly ulong UserId;

        private string userName;
        public string UserName {
            get => userName;
            set {
                if(userName == value) 
                    return;
                userName = value;
                // invoke guild event from this level
                voiceChannel.GetGuild().OnUserInfoChanged?.Invoke(this, GetInfo());
            }
        }

        private VoiceStates voiceState;
        public VoiceStates VoiceState {
            get => voiceState;
            set {
                if(voiceState == value)
                    return;
                voiceState = value;
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "DiscordUser: VoiceStateChange called. Previous: " + voiceState + " Now: " + value);
                // invoke guild event from this level
                voiceChannel.GetGuild().OnUserInfoChanged.Invoke(this, GetInfo()); // WARNING: this is an unsafe call if objects have not unsubscribed! For testing only
            }
        }

        private string iconUrl;
        public string IconUrl {
            get => iconUrl;
            set {
                if(iconUrl == value)
                    return;
                iconUrl = value;
                // invoke guild event from this level
                voiceChannel.GetGuild().OnUserInfoChanged?.Invoke(this, GetInfo());
            }
        }

        private bool isSpeaking;
        public bool IsSpeaking {
            get => isSpeaking;
            set {
                if(isSpeaking == value)
                    return;

                isSpeaking = value;

                // invoke guild event from this level
                voiceChannel.GetGuild().OnUserInfoChanged?.Invoke(this, GetInfo());
            }
        }


        public DiscordUser(DiscordVoiceChannel voiceChannel, ulong userId, string userName, VoiceStates voiceState, string iconUrl) {
            Debug.Assert(voiceChannel != null);
            Debug.Assert(userId != 0);
            this.voiceChannel = voiceChannel;
            this.UserId = userId;
            this.userName = userName;
            this.voiceState = voiceState;
            this.iconUrl = iconUrl;
            this.isSpeaking = false;
        }

        public void Dispose() {
            voiceChannel = null;
        }

        public DiscordUserInfo GetInfo() {
            return new DiscordUserInfo(UserId, UserName, VoiceState, IconUrl, IsSpeaking);
        }

        public DiscordVoiceChannel GetVoiceChannel() {
            return voiceChannel;
        }

        public override string ToString() {
            return GetInfo().ToString();
        }

        
    }

}
