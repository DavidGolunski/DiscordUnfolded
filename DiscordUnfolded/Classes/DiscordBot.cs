using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Discord.WebSocket;
using System.Linq;
using System.Collections.Generic;
using System.Dynamic;
using DiscordUnfolded.DiscordStructure;
using Discord;

namespace DiscordUnfolded {
    internal class DiscordBot {

        private static DiscordBot instance;
        public static DiscordBot Instance {
            get => instance ??= new DiscordBot();
            private set => instance = value;
        }

        // shows if the autoclicker is currently running
        public bool IsRunning { get => cancellationTokenSource != null; }
        private CancellationTokenSource cancellationTokenSource;
        private DiscordSocketClient client;


        private DiscordBot() {
            cancellationTokenSource = null;
        }

        /*
         * Task Functions
         */
        public void Start() {
            if(IsRunning) {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            Task.Run(() => StartBotClient(cancellationTokenSource.Token), token);

            Logger.Instance.LogMessage(TracingLevel.INFO, "DiscordBot Started");

        }

        public void Stop() {
            if(!IsRunning) {
                return;
            }

            if(client != null) {
                client.StopAsync().GetAwaiter().GetResult();
                client = null;
            }

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;

            Logger.Instance.LogMessage(TracingLevel.INFO, "DiscordBot Stopped");
        }

        private async Task StartBotClient(CancellationToken cancellationToken) {
            DiscordGuild.RemoveAllGuilds();
            client = new DiscordSocketClient();

            var token = "MTMzNTAyMzQ3NjczMzUxMzgxOQ.GkuN8c.cfea9Nf7CYrAsSvxdb01PLEz1yrZoJzucnWXgk";

            await client.LoginAsync(Discord.TokenType.Bot, token);
            await client.StartAsync();

            client.Ready += OnClientReady;

            client.GuildAvailable += OnGuildAdded;
            client.GuildUnavailable += OnGuildRemoved;
            client.GuildUpdated += OnGuildUpdated;

            client.ChannelCreated += OnChannelAdded;
            client.ChannelDestroyed += OnChannelRemoved;
            client.ChannelUpdated += OnChannelUpdated;

            client.UserVoiceStateUpdated += OnVoiceStateUpdated;

            await Task.Delay(-1, cancellationToken);
        }

        /*
         * Bot functions
         */

        private VoiceStates GetDiscordVoiceState(SocketVoiceState socketVoiceState) {
            if(socketVoiceState.IsDeafened || socketVoiceState.IsSelfDeafened) {
                return VoiceStates.DEAFENED;
            }
            else if(socketVoiceState.IsMuted || socketVoiceState.IsSelfMuted) {
                 return VoiceStates.MUTED;
            }
            return VoiceStates.UNMUTED;
        }


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        // Creates and Populates "DiscordGuild" objects for each guild this bot is in
        private async Task OnClientReady() {
            // Ensure bot is connected to at least one guild
            if(client.Guilds.Count == 0) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Bot is not in any guilds.");
                return;
            }
            /* The "OnGuildAdded" functions is called for each guild when the bot starts up anyways
            foreach(SocketGuild guild in client.Guilds) {
                await OnGuildAdded(guild);
            }*/
        }
        /*
         * Guilds
         */
        private async Task OnGuildAdded(SocketGuild guild) {
            DiscordGuild discordGuild = new DiscordGuild(guild.Id, guild.Name, guild.IconUrl);

            foreach(SocketTextChannel textChannel in guild.TextChannels) {
                // i don't know why, but "guild.TextChannels" also returns voice channels. This filters them out
                if(textChannel.ChannelType.ToString() != "Text") {
                    continue;
                }

                DiscordTextChannel discordTextChannel = new DiscordTextChannel(discordGuild, textChannel.Id, textChannel.Name, textChannel.Position);
                discordGuild.AddTextChannel(discordTextChannel);
            }
            foreach(SocketVoiceChannel voiceChannel in guild.VoiceChannels) {
                // i don't know why, but "guild.VoiceChannels" also returns text channels. This filters them out
                if(voiceChannel.ChannelType.ToString() != "Voice") {
                    continue;
                }

                DiscordVoiceChannel discordVoiceChannel = new DiscordVoiceChannel(discordGuild, voiceChannel.Id, voiceChannel.Name, voiceChannel.Position);

                // add users
                foreach(SocketGuildUser user in voiceChannel.Users) {
                    VoiceStates voiceState = VoiceStates.DISCONNECTED;
                    SocketVoiceState? socketVoiceState = user.VoiceState;

                    if(socketVoiceState != null && socketVoiceState.HasValue) {
                        voiceState = GetDiscordVoiceState(socketVoiceState.Value);
                    }

                    if(voiceState == VoiceStates.DISCONNECTED)
                        continue;

                    if(socketVoiceState.Value.VoiceChannel.Id != voiceChannel.Id)
                        continue;

                    DiscordUser discordUser = new DiscordUser(discordVoiceChannel, user.Id, user.DisplayName, voiceState, user.GetDisplayAvatarUrl());
                    discordVoiceChannel.AddUser(discordUser);
                }

                discordGuild.AddVoiceChannel(discordVoiceChannel);

            }
            DiscordGuild.RemoveGuild(discordGuild.GuildId); // remove guild if it already exists

            DiscordGuild.AddGuild(discordGuild);
            Logger.Instance.LogMessage(TracingLevel.INFO, "Added Guild " + discordGuild.GuildName);
        }

        private async Task OnGuildRemoved(SocketGuild guild) {
            DiscordGuild.RemoveGuild(guild.Id);
        }

        private async Task OnGuildUpdated(SocketGuild guild, SocketGuild newGuild) {
            if(guild.Name != newGuild.Name) {
                DiscordGuild discordGuild = DiscordGuild.GetGuild(guild.Id);
                if(discordGuild == null) {
                    return;
                }
                discordGuild.GuildName = newGuild.Name;
            }

            if(guild.IconUrl != newGuild.IconUrl) {
                DiscordGuild discordGuild = DiscordGuild.GetGuild(guild.Id);
                if(discordGuild == null) {
                    return;
                }
                discordGuild.IconUrl = newGuild.IconUrl;
            }
        }

        /*
         * Channels
         */
        private async Task OnChannelAdded(SocketChannel channel) {
            if(channel.ChannelType.ToString() == "Text") {
                SocketTextChannel textChannel = channel as SocketTextChannel;

                DiscordGuild guild = DiscordGuild.GetGuild(textChannel.Guild.Id);
                if(guild == null) return;

                guild.AddTextChannel(new DiscordTextChannel(guild, textChannel.Id, textChannel.Name, textChannel.Position));
            }
            else if(channel.ChannelType.ToString() == "Voice") {
                SocketVoiceChannel voiceChannel = channel as SocketVoiceChannel;

                DiscordGuild guild = DiscordGuild.GetGuild(voiceChannel.Guild.Id);
                if(guild == null)
                    return;

                guild.AddVoiceChannel(new DiscordVoiceChannel(guild, voiceChannel.Id, voiceChannel.Name, voiceChannel.Position));
            }
        }

        private async Task OnChannelRemoved(SocketChannel channel) {
            if(channel.ChannelType.ToString() == "Text") {
                SocketTextChannel textChannel = channel as SocketTextChannel;

                DiscordGuild guild = DiscordGuild.GetGuild(textChannel.Guild.Id);
                if(guild == null)
                    return;

                guild.RemoveTextChannel(textChannel.Id);
            }
            else if(channel.ChannelType.ToString() == "Voice") {
                SocketVoiceChannel voiceChannel = channel as SocketVoiceChannel;

                DiscordGuild guild = DiscordGuild.GetGuild(voiceChannel.Guild.Id);
                if(guild == null)
                    return;

                guild.RemoveTextChannel(voiceChannel.Id);
            }
        }

        private async Task OnChannelUpdated(SocketChannel channel, SocketChannel newChannel) {
            if(newChannel.ChannelType.ToString() == "Text") { // "Category" is also an option ToDo
                SocketTextChannel textChannel = channel as SocketTextChannel;
                SocketTextChannel newTextChannel = newChannel as SocketTextChannel;

                DiscordGuild guild = DiscordGuild.GetGuild(textChannel.Guild.Id);
                if(guild == null) 
                    return;

                DiscordTextChannel discordTextChannel = guild.GetTextChannel(textChannel.Id);
                if(discordTextChannel == null) return;

                if(textChannel.Name != newTextChannel.Name) {
                    discordTextChannel.ChannelName = newTextChannel.Name;
                }
                if(textChannel.Position != newTextChannel.Position) {
                    discordTextChannel.Position = newTextChannel.Position;
                }

            }
            else if(newChannel.ChannelType.ToString() == "Voice") {
                SocketVoiceChannel voiceChannel = channel as SocketVoiceChannel;
                SocketVoiceChannel newVoiceChannel = newChannel as SocketVoiceChannel;

                DiscordGuild guild = DiscordGuild.GetGuild(voiceChannel.Guild.Id);
                if(guild == null)
                    return;

                DiscordVoiceChannel discordVoiceChannel = guild.GetVoiceChannel(voiceChannel.Id);
                if(discordVoiceChannel == null) return;

                if(voiceChannel.Name != newVoiceChannel.Name) {
                    discordVoiceChannel.ChannelName = newVoiceChannel.Name;
                }
                if(voiceChannel.Position != newVoiceChannel.Position) {
                    discordVoiceChannel.Position = newVoiceChannel.Position;
                }

            }
        }

        /*
         * Users
         */

        private async Task OnVoiceStateUpdated(SocketUser socketUser, SocketVoiceState socketVoiceState, SocketVoiceState newSocketVoiceState) {
            if(socketVoiceState.VoiceChannel == null && newSocketVoiceState.VoiceChannel == null) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "User " + socketUser.GlobalName + " switched voice states, but without any voice channels");
                return;
            }

            

            SocketGuildUser socketGuildUser = socketUser as SocketGuildUser;
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "DiscordBot: " + socketGuildUser.Username + " " + socketGuildUser.Guild.Name);
            DiscordGuild discordGuild = DiscordGuild.GetGuild(socketGuildUser.Guild.Id);
            if(discordGuild == null) return;

            // handle disconnects
            if(socketVoiceState.VoiceChannel != null && newSocketVoiceState.VoiceChannel == null) {
                DiscordVoiceChannel oldChannel = discordGuild.GetVoiceChannel(socketVoiceState.VoiceChannel.Id);
                oldChannel?.RemoveUser(socketGuildUser.Id);
                return;
            }

            VoiceStates newVoiceState = GetDiscordVoiceState(newSocketVoiceState);
            DiscordVoiceChannel newDiscordVoiceChannel = discordGuild.GetVoiceChannel(newSocketVoiceState.VoiceChannel.Id);

            Logger.Instance.LogMessage(TracingLevel.DEBUG, "DiscordBot: " + newVoiceState + " " + newDiscordVoiceChannel);

            // handle voice channel joins
            if(socketVoiceState.VoiceChannel == null && newSocketVoiceState.VoiceChannel != null) {
                DiscordUser newDiscordUser = new DiscordUser(newDiscordVoiceChannel, socketGuildUser.Id, socketGuildUser.DisplayName, newVoiceState, socketGuildUser.GetDisplayAvatarUrl());
                newDiscordVoiceChannel?.AddUser(newDiscordUser);
                return;
            }

            // handle moves between voice channels within a server
            if(socketVoiceState.VoiceChannel.Id != newSocketVoiceState.VoiceChannel.Id) {
                // remove user from old voice channel
                discordGuild.GetVoiceChannel(socketVoiceState.VoiceChannel.Id).RemoveUser(socketGuildUser.Id);

                // add user to new voice channel
                DiscordUser newDiscordUser = new DiscordUser(newDiscordVoiceChannel, socketGuildUser.Id, socketGuildUser.DisplayName, newVoiceState, socketGuildUser.GetDisplayAvatarUrl());
                newDiscordVoiceChannel.AddUser(newDiscordUser);
                return;
            }

            // handle any other changes to the user itself
            DiscordUser discordUser = newDiscordVoiceChannel.GetUser(socketGuildUser.Id);
            discordUser.UserName = socketGuildUser.DisplayName;
            discordUser.IconUrl = socketGuildUser.GetDisplayAvatarUrl();
            discordUser.VoiceState = newVoiceState;

        }


#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    }
}
