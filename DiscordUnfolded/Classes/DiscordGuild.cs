﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {
    public class DiscordGuild {


        #region static guild management
        private static readonly Dictionary<ulong, DiscordGuild> DiscordGuilds = new Dictionary<ulong, DiscordGuild>();
        private static EventHandler<List<DiscordGuild>> DiscordGuildsChangedEvent;

        public static List<DiscordGuild> Guilds { get => DiscordGuilds.Values.ToList(); }

        public static void AddGuild(DiscordGuild discordGuild) {
            if(discordGuild == null || DiscordGuilds.ContainsKey(discordGuild.GuildId))
                return;
            DiscordGuilds.Add(discordGuild.GuildId, discordGuild);
            DiscordGuildsChangedEvent?.Invoke(null, Guilds);
        }

        public static void RemoveGuild(ulong GuildId) {
            if(!DiscordGuilds.ContainsKey(GuildId))
                return;
            DiscordGuilds.Remove(GuildId);
            DiscordGuildsChangedEvent?.Invoke(null, Guilds);
        }

        public static void RemoveAllGuilds() {
            if(DiscordGuilds.Count == 0) return;
            DiscordGuilds.Clear();
            DiscordGuildsChangedEvent?.Invoke(null, Guilds);
        }

        public static DiscordGuild GetGuild(ulong guildId) {
            return DiscordGuilds.ContainsKey(guildId) ? DiscordGuilds[guildId] : null;
        }

        public static void SubscribeToGuildsChanged(EventHandler<List<DiscordGuild>> handler) {
            DiscordGuildsChangedEvent += handler;
        }

        public static void UnsubscribeFromGuildsChanged(EventHandler<List<DiscordGuild>> handler) {
            DiscordGuildsChangedEvent -= handler;
        }
        #endregion


        public readonly ulong GuildId;

        private string guildName;
        public string GuildName {
            get => guildName;
            set {
                if (guildName == value) return;
                guildName = value;
                guildInfoChanged?.Invoke(this, GetInfo());
            }
        }
        private string iconUrl;
        public string IconUrl {
            get => iconUrl;
            set {
                if (iconUrl == value) return;
                iconUrl = value;
                guildInfoChanged?.Invoke(this, GetInfo());
            }
        }

        private EventHandler<DiscordGuildInfo> guildInfoChanged;



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

        public void SubscribeToGuildInfo(EventHandler<DiscordGuildInfo> handler) {
            guildInfoChanged += handler;
        }

        public void UnsubscribeFromGuildInfo(EventHandler<DiscordGuildInfo> handler) {
            guildInfoChanged -= handler;
        }


        public void AddTextChannel(DiscordTextChannel textChannel) {
            if(textChannel == null || textChannels.ContainsKey(textChannel.ChannelId))
                return;

            textChannels[textChannel.ChannelId] = textChannel;
        }

        public void RemoveTextChannel(ulong textChannelId) {
            if(!textChannels.ContainsKey(textChannelId))
                return;

            textChannels.Remove(textChannelId);
        }

        public DiscordTextChannel GetTextChannel(ulong textChannelId) {
            return textChannels.ContainsKey(textChannelId) ? textChannels[textChannelId] : null;
        }



        public void AddVoiceChannel(DiscordVoiceChannel voiceChannel) {
            if(voiceChannel == null || voiceChannels.ContainsKey(voiceChannel.ChannelId))
                return;

            voiceChannels[voiceChannel.ChannelId] = voiceChannel;
        }

        public void RemoveVoiceChannel(ulong voiceChannel) {
            if(!voiceChannels.ContainsKey(voiceChannel))
                return;

            voiceChannels.Remove(voiceChannel);
        }

        public DiscordVoiceChannel GetVoiceChannel(ulong voiceChannelId) {
            return voiceChannels.ContainsKey(voiceChannelId) ? voiceChannels[voiceChannelId] : null;
        }

    }
}
