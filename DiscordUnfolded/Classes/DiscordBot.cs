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

            Task.Run(() => ListenForMessages(cancellationTokenSource.Token), token);

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

        private async Task ListenForMessages(CancellationToken cancellationToken) {
            client = new DiscordSocketClient();

            var token = "MTMzNTAyMzQ3NjczMzUxMzgxOQ.GkuN8c.cfea9Nf7CYrAsSvxdb01PLEz1yrZoJzucnWXgk";

            await client.LoginAsync(Discord.TokenType.Bot, token);
            await client.StartAsync();

            client.Ready += OnClientReady;

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

            foreach(var currentGuild in client.Guilds) {
                string message = "GuildID: " + currentGuild.Id + " GuildName: " + currentGuild.Name + " OwnerID: " + currentGuild.OwnerId;

                foreach(var user in currentGuild.Users) {
                    
                    message += "\n\t UserID: " + user.Id + " UserName: " + user.DisplayName;
                }
                foreach(var channel in currentGuild.VoiceChannels) {

                    message += "\n\t VoiceChannel: " + channel.Id + " ChannelName: " + channel.Name;
                }
                Logger.Instance.LogMessage(TracingLevel.DEBUG, message);

                var daweedUser = currentGuild.GetUser(712795448125030432);
                if(daweedUser != null) {
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, "User Daweed was found " + daweedUser.Nickname);
                }
                else {
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, "User Daweed was NOT found");
                }
            }


            List<DiscordGuildInfo> guildInfos = new List<DiscordGuildInfo>();
            foreach(var currentGuild in client.Guilds) {

                DiscordGuildInfo guildInfo = new DiscordGuildInfo();
                guildInfo.GuildId = currentGuild.Id;
                guildInfo.GuildName = currentGuild.Name;
                guildInfo.IconUrl = currentGuild.IconUrl;
                
                guildInfos.Add(guildInfo);
                Logger.Instance.LogMessage(TracingLevel.DEBUG, currentGuild.ToString());
            }

            ServerBrowserManager.Instance.UpdateGuildList(guildInfos);
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
