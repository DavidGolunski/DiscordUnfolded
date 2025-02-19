using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BarRaider.SdTools;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using DiscordUnfolded.DiscordStructure;
using DiscordUnfolded.DiscordCommunication;

namespace DiscordUnfolded {
    public class DiscordRPC {

        private static DiscordRPC instance;
        public static DiscordRPC Instance {
            get => instance ??= new DiscordRPC();
            private set => instance = value;
        }

        private const string clientId = "1337102485017595966"; // Replace with your actual Client ID
        private const string clientSecret = "6FWjjmhTxcaNMBwzVCZq_bKuS6KDy-qp";


        public ulong CurrentUserID { get; private set; } = 0;

        public bool IsRunning { get => cancellationTokenSource != null; }
        private CancellationTokenSource cancellationTokenSource;
        


        private string authorizationCode = null;
        private string accessToken = null; // Retrieved after authenticate
        private const string redirectUri = "https://127.0.0.1:7393/callback";

        private readonly IPCMessenger messenger;

        private DiscordRPC() {
            cancellationTokenSource = null;
            messenger = new IPCMessenger(true);
        }

        public void Start() {
            if(IsRunning) {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            // start the IPC Messenger
            Task.Run(() => messenger.Connect(token), token);

            Connect(token);

           Logger.Instance.LogMessage(TracingLevel.INFO, "DiscordRPC Started");
        }

        public void Stop() {
            if(!IsRunning) {
                return;
            }
            

            cancellationTokenSource.Cancel();

            // Disconnect the IPC Messenger
            messenger.Disconnect();

            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;

            Disconnect();

            Logger.Instance.LogMessage(TracingLevel.INFO, "DiscordRPC Stopped");
        }

        private void Connect(CancellationToken token) {
            while(!token.IsCancellationRequested && !messenger.Connected) {
                Task.Delay(100, token);
            }
            if(token.IsCancellationRequested) {
                Stop();
                return;
            }


            IPCMessage dispatchMessage = messenger.SendDispatchRequest(clientId);

            if(dispatchMessage.Error != null || token.IsCancellationRequested) {
                Stop();
                return;
            }

            CurrentUserID = UInt64.Parse(dispatchMessage.Data["user"]["id"].ToString());
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "CurrentUserID: " + CurrentUserID);

            // try getting the previous token from the saved file




            // If we get here, then authenticating with a previously stored token was unsuccessfull and we need to reauthenticate again
            IPCMessage authorizeMessage = messenger.SendAuthorizeRequest(clientId);

            if(authorizeMessage.Error != null || token.IsCancellationRequested) {
                Stop();
                return;
            }


            // Send authorization Code to OAuth2 to get Bearer Access Token
            string authorizationCode = authorizeMessage.Data["code"]?.ToString();
            string accessToken;
            try {
                accessToken = DiscordOAuth2.ExchangeCode(authorizationCode, clientId, clientSecret, redirectUri, token);
            }
            catch(Exception ex) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Received Error while retrieving access token: " + ex.StackTrace);
                Stop();
                return;
            }
            if(accessToken == null) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Access Token could not be retrieved");
                Stop();
                return;
            }

            // saves the access token to the file
            SaveTokenToFile(accessToken);


            // Send Authentication Request after OAuth2 has provided the bearer access token
            IPCMessage authenticationMessage = messenger.SendAuthenticateRequest(accessToken);
            if(authenticationMessage.Error != null || token.IsCancellationRequested) {
                Stop();
                return;
            }
            

            //GetAvailableGuilds();
        }

        private void Disconnect() {
            CurrentUserID = 0;
            messenger.Disconnect();
        }

        // selects a voice or text channel.To disconnect from a channel set the channelID to 0
        public void SelectChannel(ChannelTypes channelType, ulong channelID) {
            if(!IsRunning || !messenger.Connected || (channelType != ChannelTypes.VOICE && channelType != ChannelTypes.TEXT))
                return;

            IPCMessage message = null;
            if(channelType == ChannelTypes.VOICE) {

                // if the user is already inside of a Voice Channel, disconnect from the channel first
                IPCMessage currentChannel = messenger.SendGetSelectedVoiceChannelRequest();
                string currentChannelID = currentChannel.Data?["id"]?.ToString();
                if(currentChannelID != null) {
                    message = messenger.SendSelectVoiceChannelRequest(0);
                }
                if(channelID.ToString() != currentChannelID) {
                    message = messenger.SendSelectVoiceChannelRequest(channelID);
                }
            }
            else {
                message = messenger.SendSelectTextChannelRequest(channelID);
            }

            if(message != null && message.Error != null) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, message.ToString());
            }
        }


        /*
        public List<DiscordGuild> GetAvailableGuilds() {
            if(!IsRunning || pipe == null || !pipe.IsConnected) {
                return null;
            }

            List<DiscordGuild> guilds = new List<DiscordGuild>();

            // populate the list with all guilds that have been found
            var guildRequest = new {
                nonce = Guid.NewGuid().ToString(),
                cmd = "GET_GUILDS",
                args = new { }
            };

            SendMessageAsync(1, guildRequest).GetAwaiter().GetResult();

            JObject guildMessage = ListenForMessages(cancellationTokenSource.Token);
            if(guildMessage == null) {
                return null;
            }
            var guildData = guildMessage["data"]?["guilds"] as JArray;
            foreach(var guildJToken in guildData) {
                string id = guildJToken["id"]?.ToString();
                string name = guildJToken["name"]?.ToString();
                string iconURL = guildJToken["icon_url"]?.ToString();

                DiscordGuild discordGuild = new DiscordGuild(UInt64.Parse(id), name, iconURL);
                guilds.Add(discordGuild);
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "DiscordRPC: " + discordGuild.ToString());
            }

            return null;

        }
        */

        private void SaveTokenToFile(string access_token) {
            access_token ??= String.Empty;

            var data = new { token = access_token }; 
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText("StoredToken.json", json);

        }

        private string GetTokenFromFile() {
            if(!File.Exists("StoredToken.json"))
                return String.Empty;

            string json = File.ReadAllText("StoredToken.json");
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            return data.token;
        }
    }

}
