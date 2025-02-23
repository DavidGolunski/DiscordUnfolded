using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BarRaider.SdTools;
using DiscordUnfolded.DiscordStructure;

namespace DiscordUnfolded {
    public class ChannelGridInfo {

        public DiscordChannelInfo ChannelInfo { get; set; }
        public List<ulong> UsersInChannel { get; set; }
        public DiscordUserInfo UserInfo { get; set; }

        // a button can also just be a "PLUS" or "MINUS" button, to change the volume of a selected user
        public string SpecialCaseString { get; set; }   

        public ChannelGridInfo() {
            ChannelInfo = null;
            UsersInChannel = new List<ulong>();
            UserInfo = null;
            SpecialCaseString = null;
        }

        public ChannelGridInfo(DiscordChannelInfo channelInfo) {
            ChannelInfo = channelInfo;
            UsersInChannel = new List<ulong>();
            UserInfo = null;
        }

        public ChannelGridInfo(DiscordChannelInfo channelInfo, List<ulong> usersInChannel) {
            ChannelInfo = channelInfo;
            UsersInChannel = usersInChannel;
            UserInfo = null;
        }

        public ChannelGridInfo(DiscordUserInfo userInfo) {
            ChannelInfo = null;
            UsersInChannel = new List<ulong>();
            UserInfo = userInfo;
        }

        public ChannelGridInfo(string specialCase) {
            Debug.Assert("PLUS".Equals(specialCase) || "MINUS".Equals(specialCase));

            ChannelInfo = null;
            UsersInChannel = new List<ulong>();
            UserInfo = null;
            SpecialCaseString = specialCase;
        }

        /*
         * Overrides
         */
        public override bool Equals(object obj) {
            if(obj == null || this.GetType() != obj.GetType()) return false;

            ChannelGridInfo other = obj as ChannelGridInfo;
            
            // channel info
            if(this.ChannelInfo == null && other.ChannelInfo != null)
                return false;
            if(this.ChannelInfo != null && !this.ChannelInfo.Equals(other.ChannelInfo))
                return false;

            // user info
            if(this.UserInfo == null && other.UserInfo != null)
                return false;
            if(this.UserInfo != null && !this.UserInfo.Equals(other.UserInfo)) 
                return false;

            // special case string
            if(this.SpecialCaseString == null && other.SpecialCaseString != null) 
                return false;
            if(this.SpecialCaseString != null && !this.SpecialCaseString.Equals(other.SpecialCaseString))
                return false;

            // UsersInChannel (can never be null)
            if(this.UsersInChannel.Count != other.UsersInChannel.Count)
                return false;

            for(int i = 0; i < UsersInChannel.Count; i++) {
                if(this.UsersInChannel[i] != other.UsersInChannel[i])
                    return false;
            }

            return true;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }


        public override string ToString() {
            string message = string.Empty;
            if(ChannelInfo != null) 
                message += ChannelInfo.ToString();
            else 
                message += "null";
            message += "[";
            bool first = true;
            foreach(ulong userId in UsersInChannel) {
                if(first) {
                    message += userId.ToString();
                    first = false;
                    continue;
                }
                message += ", " +  userId.ToString();
            }
            message += "]";

            if(UserInfo != null)
                message += UserInfo.ToString();
            else
                message += "null";

            if(string.IsNullOrEmpty(SpecialCaseString))
                message += ", null";
            else
                message += ", " + SpecialCaseString;


            return message;
        }
    }
}
