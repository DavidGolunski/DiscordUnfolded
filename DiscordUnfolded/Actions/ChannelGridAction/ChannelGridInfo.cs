using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordUnfolded.DiscordStructure;

namespace DiscordUnfolded {
    public class ChannelGridInfo {

        public DiscordChannelInfo ChannelInfo { get; set; }
        public List<ulong> UsersInChannel { get; set; }
        public DiscordUserInfo UserInfo { get; set; }

        public ChannelGridInfo() {
            ChannelInfo = null;
            UsersInChannel = new List<ulong>();
            UserInfo = null;
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
            return message;
        }
    }
}
