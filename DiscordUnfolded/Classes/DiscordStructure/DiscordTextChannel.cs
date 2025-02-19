using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded.DiscordStructure {

    public class DiscordTextChannel : IDisposable {


        private DiscordGuild guild;
        public readonly ulong ChannelId;

        private string channelName;
        public string ChannelName {
            get => channelName;
            set {
                if(channelName == value)
                    return;
                channelName = value;
                // invoke the event on the guild level
                guild.OnTextChannelInfoChanged?.Invoke(this, GetInfo());
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
                // invoke the event on the guild level
                guild.OnTextChannelInfoChanged?.Invoke(this, GetInfo());
            }
        }

        public DiscordTextChannel(DiscordGuild guild, ulong channelId, string channelName, int position) {
            Debug.Assert(guild != null);
            this.guild = guild;
            this.ChannelId = channelId;
            this.channelName = channelName;
            this.ChannelType = ChannelTypes.TEXT;
            this.position = position;
        }

        public void Dispose() {
            guild = null;
        }

        public DiscordChannelInfo GetInfo() {
            return new DiscordChannelInfo(ChannelId, ChannelName, ChannelType, Position);
        }

        public DiscordGuild GetGuild() {
            return guild;
        }

        public override string ToString() {
            return GetInfo().ToString();
        }
    }

}
