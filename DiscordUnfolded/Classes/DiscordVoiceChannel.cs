using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {

    public class DiscordVoiceChannel {

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
        // called when a user has been added or removed. Parameters: (bool (true if channel was added), ulong (id of user))
        private EventHandler<(bool, ulong)> userChanged;



        private readonly Dictionary<ulong, DiscordUser> discordUsers = new Dictionary<ulong, DiscordUser>();


        public DiscordVoiceChannel(ulong channelId, string channelName, int position) {
            this.ChannelId = channelId;
            this.ChannelName = channelName;
            this.ChannelType = ChannelTypes.VOICE;
            this.Position = position;
        }

        public DiscordChannelInfo GetInfo() {
            return new DiscordChannelInfo(ChannelId, ChannelName, ChannelType, Position);
        }

        public void AddUser(DiscordUser user) {
            if(discordUsers.ContainsKey(user.UserId))
                return;

            discordUsers.Add(user.UserId, user);
        }

        public void RemoveUser(ulong userId) {
            if(!discordUsers.ContainsKey(userId))
                return;

            discordUsers.Remove(userId);
        }

        public DiscordUser GetUser(ulong userId) {
            return discordUsers.ContainsKey(userId) ? discordUsers[userId] : null; 
        }

        public List<ulong> GetUserIDs() {
            return discordUsers.Keys.ToList();
        }

        public void SubscribeToChannelInfo(EventHandler<DiscordChannelInfo> handler) {
            channelInfoChanged += handler;
        }

        public void UnsubscribeFromChannelInfo(EventHandler<DiscordChannelInfo> handler) {
            channelInfoChanged -= handler;
        }

        public void SubscribeToUserChanged(EventHandler<(bool, ulong)> handler) {
            userChanged += handler;
        }

        public void UnsubscribeFromUserChanged(EventHandler<(bool, ulong)> handler) {
            userChanged -= handler;
        }

        public override string ToString() {
            string result =  GetInfo().ToString();
            result += "\n\t Users: ";
            foreach(var item in discordUsers.Values) {
                result += "\n\t\t" + item.ToString();
            }
            return result;
        }
    }

}
