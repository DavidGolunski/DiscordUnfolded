using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


    }
}
