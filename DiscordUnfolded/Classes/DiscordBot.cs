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

            await Task.Delay(-1, cancellationToken);
        }

        /*
         * Bot functions
         */

        // return a list of guild ids in which the bot and the userID are both present
        public List<ulong> GetGuildIDs(ulong userID) {

            return client.Guilds
                .Where(guild => guild.GetUser(userID) != null)
                .Select(guild => guild.Id)
                .ToList();
        }


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task OnClientReady() {
            // Ensure bot is connected to at least one guild
            if(client.Guilds.Count == 0) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Bot is not in any guilds.");
                return;
            }

            foreach(SocketGuild guild in client.Guilds) {
                DiscordGuild.AddGuild(new DiscordGuild(guild.Id, guild.Name, guild.IconUrl));

                foreach(SocketTextChannel textChannel in guild.TextChannels) {

                }

                foreach(SocketVoiceChannel voiceChannel in guild.VoiceChannels) {
                    
                }

            }
        }

        private async Task OnGuildAdded(SocketGuild guild) {
            DiscordGuild discordGuild = new DiscordGuild(guild.Id, guild.Name, guild.IconUrl);
            DiscordGuild.AddGuild(discordGuild);
        }

        private async Task OnGuildRemoved(SocketGuild guild) {
            DiscordGuild.RemoveGuild(guild.Id);
        }

        private async Task OnGuildUpdated(SocketGuild guild, SocketGuild guild2) {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, guild.Name + " " + guild2.Name);

            if(guild.Name != guild2.Name) {
                DiscordGuild.GetGuild(guild.Id).GuildName = guild2.Name;
            }
            if(guild.IconUrl != guild2.IconUrl) {
                DiscordGuild.GetGuild(guild.Id).IconUrl = guild2.IconUrl;
            }
            
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    }
}
