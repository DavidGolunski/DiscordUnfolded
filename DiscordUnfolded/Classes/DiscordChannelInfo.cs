using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {

    public enum ChannelTypes {
        UNKNOWN,
        TEXT,
        VOICE
    }


    public class DiscordChannelInfo {

        public ulong ChannelId { get; set; }
        public string ChannelName { get; set; }
        public ChannelTypes ChannelType { get; set;}
        public int Position { get; set; }   


        public DiscordChannelInfo() {
            ChannelId = 0;
            ChannelName = string.Empty;
            ChannelType = ChannelTypes.UNKNOWN;
            Position = -1;
        }


        public DiscordChannelInfo(ulong channelId, string channelName, ChannelTypes channelType, int position) {
            this.ChannelId = channelId;
            this.ChannelName = channelName;
            this.ChannelType = channelType;
            this.Position = position;
        }

        public override bool Equals(object obj) {
            if(obj == null || obj.GetType() != this.GetType())
                return false;
            DiscordChannelInfo other = obj as DiscordChannelInfo;

            return this.ChannelId == other.ChannelId
                && this.ChannelName == other.ChannelName
                && this.ChannelType == other.ChannelType
                && this.Position == other.Position;
        }

        public override int GetHashCode() {
            int hash = 17;

            hash = hash * 23 + (ChannelId.GetHashCode());
            hash = hash * 23 + (ChannelName.GetHashCode());
            hash = hash * 23 + (ChannelType.GetHashCode());
            hash = hash * 23 + (Position.GetHashCode());

            return hash;
        }

        public override string ToString() {
            return "DiscordChannel: " + ChannelId.ToString() + "," + ChannelName + "," + ChannelType + "," + Position.ToString();
        }
    }
}
