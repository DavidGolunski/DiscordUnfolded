using BarRaider.SdTools;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded.DiscordStructure {
    public class DiscordGuild : IDisposable {


        #region static guild management
        private static readonly Dictionary<ulong, DiscordGuild> DiscordGuilds = new Dictionary<ulong, DiscordGuild>();
        public static EventHandler<List<DiscordGuild>> OnGuildListChanged;

        public static List<DiscordGuild> Guilds { get => DiscordGuilds.Values.ToList(); }

        public static void AddGuild(DiscordGuild discordGuild) {
            if(discordGuild == null)
                return;
            if(DiscordGuilds.ContainsKey(discordGuild.GuildId))
                DiscordGuilds.Remove(discordGuild.GuildId);

            DiscordGuilds.Add(discordGuild.GuildId, discordGuild);
            OnGuildListChanged?.Invoke(null, Guilds);
        }

        public static void RemoveGuild(ulong GuildId) {
            if(!DiscordGuilds.ContainsKey(GuildId))
                return;
            DiscordGuild guild = DiscordGuilds[GuildId];


            DiscordGuilds.Remove(GuildId);
            OnGuildListChanged?.Invoke(null, Guilds);
        }

        public static void RemoveAllGuilds() {
            if(DiscordGuilds.Count == 0) return;
            DiscordGuilds.Clear();
            OnGuildListChanged?.Invoke(null, Guilds);
        }

        public static DiscordGuild GetGuild(ulong guildId) {
            return DiscordGuilds.ContainsKey(guildId) ? DiscordGuilds[guildId] : null;
        }

        #endregion


        public readonly ulong GuildId;

        private string guildName;
        public string GuildName {
            get => guildName;
            set {
                if (guildName == value) return;
                guildName = value;
                OnGuildInfoChanged?.Invoke(this, GetInfo());
            }
        }
        private string iconUrl;
        public string IconUrl {
            get => iconUrl;
            set {
                if (iconUrl == value) return;
                iconUrl = value;
                OnGuildInfoChanged?.Invoke(this, GetInfo());
            }
        }

        // called when the guild info has been changed
        public EventHandler<DiscordGuildInfo> OnGuildInfoChanged;

        // called when a text channel has been added or removed. Parameters: (bool (true if channel was added), ulong (id of text channel))
        public EventHandler<(bool, ulong)> OnTextChannelChanged;
        // called when existing text channel has been modified (e.g. name or position changed)
        public EventHandler<DiscordChannelInfo> OnTextChannelInfoChanged;

        // called when a voice channel has been added or removed. Parameters: (bool (true if channel was added), ulong (id of voice channel))
        public EventHandler<(bool, ulong)> OnVoiceChannelChanged;
        // called when existing voice channel has been modified (e.g. name or position changed)
        public EventHandler<DiscordChannelInfo> OnVoiceChannelInfoChanged;

        // called when a user has joined or left a voice channel (moving between voice channels counts as leaving and joining)
        public EventHandler<(bool, ulong)> OnUserChanged;
        // called when existing user has been modified (e.g. muted, unmuted)
        public EventHandler<DiscordUserInfo> OnUserInfoChanged;


        private readonly Dictionary<ulong, DiscordTextChannel> textChannels = new Dictionary<ulong, DiscordTextChannel>();
        private readonly Dictionary<ulong, DiscordVoiceChannel> voiceChannels = new Dictionary<ulong, DiscordVoiceChannel>();


        public DiscordGuild(ulong guildId, string guildName, string iconUrl) {
            this.GuildId = guildId;
            this.guildName = guildName;
            this.iconUrl = iconUrl;
        }

        public DiscordGuildInfo GetInfo() {
            return new DiscordGuildInfo(GuildId, GuildName, IconUrl);   
        }

        public void Dispose() {
            foreach(var textChannel in textChannels.Values) {
                textChannel.Dispose();
            }
            textChannels.Clear();

            foreach(var voiceChannel in voiceChannels.Values) {
                voiceChannel.Dispose();
            }

            voiceChannels.Clear();
        }


        /*
         * Text Channels
         */
        public void AddTextChannel(DiscordTextChannel textChannel) {
            if(textChannel == null || textChannels.ContainsKey(textChannel.ChannelId))
                return;

            textChannels[textChannel.ChannelId] = textChannel;
            OnTextChannelChanged?.Invoke(this, (true, textChannel.ChannelId));
        }

        public void RemoveTextChannel(ulong textChannelId) {
            if(!textChannels.ContainsKey(textChannelId))
                return;

            textChannels.Remove(textChannelId);
            OnTextChannelChanged?.Invoke(this, (false, textChannelId));
        }

        public DiscordTextChannel GetTextChannel(ulong textChannelId) {
            return textChannels.ContainsKey(textChannelId) ? textChannels[textChannelId] : null;
        }

        public List<ulong> GetOrderedTextChannelIDs() {
            return textChannels.OrderBy(pair => pair.Value.Position)
                   .Select(pair => pair.Key)
                   .ToList();
        }

        /*
         * Voice Channels
         */
        public void AddVoiceChannel(DiscordVoiceChannel voiceChannel) {
            if(voiceChannel == null || voiceChannels.ContainsKey(voiceChannel.ChannelId))
                return;

            voiceChannels[voiceChannel.ChannelId] = voiceChannel;
            OnVoiceChannelChanged?.Invoke(this, (true, voiceChannel.ChannelId));
        }

        public void RemoveVoiceChannel(ulong voiceChannel) {
            if(!voiceChannels.ContainsKey(voiceChannel))
                return;

            voiceChannels.Remove(voiceChannel);
            OnVoiceChannelChanged?.Invoke(this, (false, voiceChannel));
        }

        public DiscordVoiceChannel GetVoiceChannel(ulong voiceChannelId) {
            return voiceChannels.ContainsKey(voiceChannelId) ? voiceChannels[voiceChannelId] : null;
        }

        public List<ulong> GetOrderedVoiceChannelIDs() {
            return voiceChannels.OrderBy(pair => pair.Value.Position)
                   .Select(pair => pair.Key)
                   .ToList();
        }

        /*
         * Users in Voice Channels
         */
        public DiscordUser GetUser(ulong userID) {
            foreach(DiscordVoiceChannel voiceChannel in voiceChannels.Values) {
                DiscordUser discordUser = voiceChannel.GetUser(userID);
                if(discordUser != null) {
                    return discordUser;
                }
            }
            return null;
        }


        public override string ToString() {
            string result = GetInfo().ToString();
            result += "\nTextChannels: ";
            foreach(var item in textChannels.Values) {
                result += "\n\t\t" + item.ToString();
            }
            result += "\nVoiceChannels: ";
            foreach(var item in voiceChannels.Values) {
                result += "\n\t\t" + item.ToString();
            }
            return result;
        }

    }
}
