using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {

    public class DiscordTextChannel {

        public readonly ulong ChannelId;

        private string channelName;
        public string ChannelName {
            get => channelName;
            set {
                if(channelName == value)
                    return;
                channelName = value;
                channelInfoChanged?.Invoke(this, GetInfo());
            }
        }
        public readonly ChannelTypes ChannelType;
        private int position;
        public int Position {
            get => position;
            set {
                if(position == value)
                    return;
                position = value;
                channelInfoChanged?.Invoke(this, GetInfo());
            }
        }

        private EventHandler<DiscordChannelInfo> channelInfoChanged;

        public DiscordTextChannel(ulong channelId, string channelName, int position) {
            this.ChannelId = channelId;
            this.ChannelName = channelName;
            this.ChannelType = ChannelTypes.TEXT;
            this.Position = position;
        }

        public DiscordChannelInfo GetInfo() {
            return new DiscordChannelInfo(ChannelId, ChannelName, ChannelType, Position);
        }


        public void SubscribeToChannelInfo(EventHandler<DiscordChannelInfo> handler) {
            channelInfoChanged += handler;
        }

        public void UnsubscribeFromChannelInfo(EventHandler<DiscordChannelInfo> handler) {
            channelInfoChanged -= handler;
        }

        public override string ToString() {
            return GetInfo().ToString();
        }
    }

}
