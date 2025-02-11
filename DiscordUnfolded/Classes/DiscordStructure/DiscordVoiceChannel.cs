using BarRaider.SdTools;
using Discord;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded.DiscordStructure {

    public class DiscordVoiceChannel : IDisposable {

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
                guild.OnVoiceChannelInfoChanged?.Invoke(this, GetInfo());
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
                guild.OnVoiceChannelInfoChanged?.Invoke(this, GetInfo());
            }
        }

        private readonly Dictionary<ulong, DiscordUser> discordUsers = new Dictionary<ulong, DiscordUser>();


        public DiscordVoiceChannel(DiscordGuild guild, ulong channelId, string channelName, int position) {
            Debug.Assert(guild != null);
            this.guild = guild;
            this.ChannelId = channelId;
            this.channelName = channelName;
            this.ChannelType = ChannelTypes.VOICE;
            this.position = position;
        }

        public void Dispose() {
            foreach(DiscordUser discordUser in discordUsers.Values) {
                discordUser.Dispose();
            }
            guild = null;
        }

        public DiscordChannelInfo GetInfo() {
            return new DiscordChannelInfo(ChannelId, ChannelName, ChannelType, Position);
        }

        public DiscordGuild GetGuild() {
            return guild;
        }

        public void AddUser(DiscordUser user) {
            if(discordUsers.ContainsKey(user.UserId))
                return;

            discordUsers.Add(user.UserId, user);
            guild.OnUserChanged?.Invoke(this, (true, user.UserId));
        }

        public void RemoveUser(ulong userId) {
            if(!discordUsers.ContainsKey(userId))
                return;

            discordUsers.Remove(userId);
            guild.OnUserChanged?.Invoke(this, (false, userId));
        }

        public DiscordUser GetUser(ulong userId) {
            return discordUsers.ContainsKey(userId) ? discordUsers[userId] : null; 
        }

        public List<ulong> GetUserIDs() {
            return discordUsers.Keys.ToList();
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
