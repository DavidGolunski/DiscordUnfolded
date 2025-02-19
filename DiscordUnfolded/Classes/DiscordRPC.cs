using System;
using System.Threading;
using System.Threading.Tasks;
using BarRaider.SdTools;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DiscordUnfolded.DiscordStructure;
using DiscordUnfolded.DiscordCommunication;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Discord;

namespace DiscordUnfolded {
    public class DiscordRPC {

        private static DiscordRPC instance;
        public static DiscordRPC Instance {
            get => instance ??= new DiscordRPC();
            private set => instance = value;
        }

        private static readonly string UserTokenDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "davidgolunski", "discordUnfolded");
        private const string clientId = "1337102485017595966"; // Replace with your actual Client ID
        private const string clientSecret = "6FWjjmhTxcaNMBwzVCZq_bKuS6KDy-qp";
        private const string redirectUri = "https://127.0.0.1:7393/callback";


        public bool IsRunning { get => cancellationTokenSource != null; }
        private CancellationTokenSource cancellationTokenSource;
        private readonly IPCMessenger messenger;


        // A place to store all subscriptions that have been made without any additional parameters
        // Param1: EventType
        private readonly List<EventType> subscribedGeneral = new List<EventType>();

        // A place to store all subscriptions that have been made with a channel ID
        // Param1: EventType, Param2: channelID
        private readonly List<(EventType, ulong)> subscribedChannels = new List<(EventType, ulong)>();


        public ulong CurrentUserID { get; private set; } = 0;

        // a list of all guilds that the user is a member of
        public readonly List<DiscordGuildInfo> AvailableGuilds = new List<DiscordGuildInfo>();
        public event Action<List<DiscordGuildInfo>> OnAvailableGuildsChanged;


        public DiscordGuild SelectedGuild { get; private set; } = null;


        private DiscordRPC() {
            cancellationTokenSource = null;
            messenger = new IPCMessenger(true);
            messenger.OnEventReceived += OnEventReceived;
        }

        public void Start() {
            if(IsRunning) return;
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            // start the IPC Messenger
            Task.Run(() => messenger.Connect(token), token);

            Connect(token);

            if(token.IsCancellationRequested)
                Stop();

            UpdateAvailableGuilds();

            // subscribe to general events
            messenger.SendGeneralSubscribeEvent(EventType.GUILD_CREATE);
            subscribedGeneral.Add(EventType.GUILD_CREATE);
            messenger.SendGeneralSubscribeEvent(EventType.GUILD_STATUS);
            subscribedGeneral.Add(EventType.GUILD_STATUS);
            messenger.SendGeneralSubscribeEvent(EventType.CHANNEL_CREATE);
            subscribedGeneral.Add(EventType.CHANNEL_CREATE);


            GetDiscordGuild(1336080882758320311); // Bot Test Server


            Logger.Instance.LogMessage(TracingLevel.INFO, "DiscordRPC Started");
            if(token.IsCancellationRequested)
                Stop();
        }

        public void Stop() {
            if(!IsRunning) return;
            
            Disconnect();

            Logger.Instance.LogMessage(TracingLevel.INFO, "DiscordRPC Stopped");
        }

        /*
         * Connecting, Disconnecting and Events
         */
        private void Connect(CancellationToken token) {
            while(!token.IsCancellationRequested && !messenger.Connected) {
                Task.Delay(100, token);
            }

            if(token.IsCancellationRequested) return;

            IPCMessage dispatchMessage = messenger.SendDispatchRequest(clientId);

            if(dispatchMessage.Error != null || token.IsCancellationRequested) return;

            CurrentUserID = UInt64.Parse(dispatchMessage.Data["user"]["id"].ToString());
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "CurrentUserID: " + CurrentUserID);

            // try getting the previous token from the saved file
            string previousAccessToken = GetTokenFromFile();
            if(!string.IsNullOrEmpty(previousAccessToken)) {
                string newAccessToken = null;
                try {
                    newAccessToken = DiscordOAuth2.RefreshToken(previousAccessToken, clientId, clientSecret, token);
                }
                catch(Exception ex) {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Error while refreshing token: " + ex.StackTrace);
                }

                if(!string.IsNullOrEmpty(newAccessToken)) {
                    SaveTokenToFile(newAccessToken);
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, "Saved new token to file!");
                    return;
                }
            }


            // If we get here, then authenticating with a previously stored token was unsuccessfull and we need to reauthenticate again
            IPCMessage authorizeMessage = messenger.SendAuthorizeRequest(clientId);

            if(authorizeMessage.Error != null || token.IsCancellationRequested) return;


            // Send authorization Code to OAuth2 to get Bearer Access Token
            string authorizationCode = authorizeMessage.Data["code"]?.ToString();
            string accessToken;
            try {
                accessToken = DiscordOAuth2.ExchangeCode(authorizationCode, clientId, clientSecret, redirectUri, token);
            }
            catch(Exception ex) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Received Error while retrieving access token: " + ex.StackTrace);
                return;
            }
            if(accessToken == null) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Access Token could not be retrieved");
                return;
            }

            // saves the access token to the file
            SaveTokenToFile(accessToken);


            // Send Authentication Request after OAuth2 has provided the bearer access token
            IPCMessage authenticationMessage = messenger.SendAuthenticateRequest(accessToken);
            if(authenticationMessage.Error != null || token.IsCancellationRequested) return;
        }

        private void Disconnect() {
            CurrentUserID = 0;

            // unsubscribe from all events
            foreach(EventType eventType in subscribedGeneral) {
                messenger.SendGeneralUnsubscribeRequest(eventType);
            }
            subscribedGeneral.Clear();

            foreach((EventType evt, ulong channelId) in subscribedChannels) {
                messenger.SendChannelUnsubscribeRequest(evt, channelId);
            }
            subscribedChannels.Clear();


            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
            messenger.Disconnect();
        }

        private void OnEventReceived(EventType eventType, JObject eventData) {

            switch(eventType) {
                case EventType.GUILD_CREATE:
                case EventType.GUILD_STATUS:
                    UpdateAvailableGuilds();
                    break;
                case EventType.CHANNEL_CREATE:
                    AddChannelToGuild(this.SelectedGuild, eventData);
                    break;
                case EventType.VOICE_STATE_CREATE:
                    if(this.SelectedGuild == null)
                        break;
                    DiscordGuild guild = GetDiscordGuild(this.SelectedGuild.GuildId);
                    ulong newUserId = UInt64.Parse(eventData["user"]["id"].ToString());
                    DiscordUser newUser = guild.GetUser(newUserId);

                    DiscordVoiceChannel voiceChannel = this.SelectedGuild.GetVoiceChannel(newUser.GetVoiceChannel().ChannelId);
                    voiceChannel.AddUser(new DiscordUser(voiceChannel, newUser.UserId, newUser.UserName, newUser.VoiceState, newUser.IconUrl));

                    guild.Dispose();
                    break;
                case EventType.VOICE_STATE_DELETE:
                    if(this.SelectedGuild == null)
                        break;
                    ulong userDeleteId = UInt64.Parse(eventData["user"]["id"].ToString());
                    DiscordUser removedUser = this.SelectedGuild.GetUser(userDeleteId);
                    removedUser.GetVoiceChannel().RemoveUser(userDeleteId);
                    break;

                case EventType.VOICE_STATE_UPDATE:
                    if(this.SelectedGuild == null)
                        break;
                    ulong userUpdateId = UInt64.Parse(eventData["user"]["id"].ToString());
                    VoiceStates newVoiceState = GetVoiceState(eventData["voice_state"]);
                    this.SelectedGuild.GetUser(userUpdateId).VoiceState = newVoiceState;
                    break;
                case EventType.SPEAKING_START:
                    if(this.SelectedGuild == null)
                        break;
                    ulong userSpeakingStartId = UInt64.Parse(eventData["user_id"].ToString());
                    this.SelectedGuild.GetUser(userSpeakingStartId).VoiceState = VoiceStates.SPEAKING;
                    break;
                case EventType.SPEAKING_STOP:
                    if(this.SelectedGuild == null)
                        break;
                    ulong userSpeakingStopId = UInt64.Parse(eventData["user_id"].ToString());
                    this.SelectedGuild.GetUser(userSpeakingStopId).VoiceState = VoiceStates.UNMUTED;
                    break;

                default:
                    break;
            }

        }


        /*
         * Functions
         */
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

                // join the channel only, if you would not rejoin the same channel again
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

        
        private void UpdateAvailableGuilds() {
            AvailableGuilds.Clear();
            if(!IsRunning || !messenger.Connected) return;


            List<DiscordGuildInfo> guilds = new List<DiscordGuildInfo>();

            IPCMessage message = messenger.SendGetGuildsRequest();

            if(message.Error != null) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, message.Error.ToString());
                return;
            }

            var guildData = message.Data["guilds"] as JArray;
            foreach(var guildJToken in guildData) {
                string id = guildJToken["id"]?.ToString();
                string name = guildJToken["name"]?.ToString();
                string iconURL = guildJToken["icon_url"]?.ToString();

                DiscordGuildInfo discordGuildInfo = new DiscordGuildInfo(UInt64.Parse(id), name, iconURL);

                guilds.Add(discordGuildInfo);
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "DiscordRPC found Guild: " + discordGuildInfo.ToString());
            }

            AvailableGuilds.Clear();
            AvailableGuilds.AddRange(guilds.OrderBy(guild => guild.GuildName).ToList());

            // send update events
            OnAvailableGuildsChanged?.Invoke(AvailableGuilds);
        }

        public DiscordGuild SelectGuild(ulong guildId) {
            bool guildHasChanged = this.SelectedGuild != null && this.SelectedGuild.GuildId != guildId;

            // if the selected guild has changed, unsubsribe from all channel specific events
            if(guildHasChanged) {
                foreach((EventType evt, ulong channelId) in subscribedChannels) {
                    messenger.SendChannelUnsubscribeRequest(evt, channelId);
                }
                subscribedChannels.Clear();
            }



            this.SelectedGuild = GetDiscordGuild(guildId);

            // resubscribe to all events from channels
            if(guildHasChanged && this.SelectedGuild != null) {

                foreach(ulong voiceChannelId in this.SelectedGuild.GetOrderedVoiceChannelIDs()) {
                    messenger.SendChannelSubscribeEvent(EventType.VOICE_STATE_CREATE, voiceChannelId);
                    subscribedChannels.Add((EventType.VOICE_STATE_CREATE, voiceChannelId));

                    messenger.SendChannelSubscribeEvent(EventType.VOICE_STATE_UPDATE, voiceChannelId);
                    subscribedChannels.Add((EventType.VOICE_STATE_UPDATE, voiceChannelId));

                    messenger.SendChannelSubscribeEvent(EventType.VOICE_STATE_DELETE, voiceChannelId);
                    subscribedChannels.Add((EventType.VOICE_STATE_DELETE, voiceChannelId));

                    messenger.SendChannelSubscribeEvent(EventType.SPEAKING_START, voiceChannelId);
                    subscribedChannels.Add((EventType.SPEAKING_START, voiceChannelId));

                    messenger.SendChannelSubscribeEvent(EventType.SPEAKING_STOP, voiceChannelId);
                    subscribedChannels.Add((EventType.SPEAKING_STOP, voiceChannelId));
                }
            }



            return this.SelectedGuild;
        }


        // creates a DiscordGuild based on data retrieved from the client
        private DiscordGuild GetDiscordGuild(ulong guildId) {
            if(!IsRunning || !messenger.Connected) return null;

            DiscordGuildInfo discordGuildInfo = null;
            foreach(DiscordGuildInfo info in AvailableGuilds) {
                if(info.GuildId == guildId) {
                    discordGuildInfo = info;
                    break;
                }
            }
            if(discordGuildInfo == null) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Error while trying to Find Guild in ID " + guildId + " in AvailableGuilds");
                return null;
            }

            DiscordGuild guild = new DiscordGuild(discordGuildInfo.GuildId, discordGuildInfo.GuildName, discordGuildInfo.IconUrl);


            IPCMessage channelsMessage = messenger.SendGetChannelsRequest(guildId);
            if(channelsMessage.Error != null) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Error while retrieving channels of Guild " + guildId + ": " +  channelsMessage.Error.ToString());
                return null;
            }

            var channelData = channelsMessage.Data["channels"] as JArray;
            foreach(var channelJToken in channelData) {
                ulong channelId = UInt64.Parse(channelJToken["id"].ToString());
                int type = Int32.Parse(channelJToken["type"].ToString());


                if(type != 0 && type != 2 && type != 4 && type != 5) {
                    continue;
                }

                IPCMessage channelMessage = messenger.SendGetChannelRequest(channelId);
                if(channelMessage.Error != null) {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Error while retrieving text channel " + channelId + ": " + channelMessage.Error.ToString());
                    return null;
                }

                AddChannelToGuild(guild, channelMessage.Data);

            }

            Logger.Instance.LogMessage(TracingLevel.INFO, guild.ToString());

            return guild;
        }


        // creates a TextChannel or VoiceChannel based on the JObject and adds it to the guild. Returns false if this was not successfull
        private bool AddChannelToGuild(DiscordGuild discordGuild, JObject channelData) {
            if(discordGuild == null || channelData == null)
                return false;

            ulong guildId = UInt64.Parse(channelData["guild_id"].ToString());
            ulong channelId = UInt64.Parse(channelData["id"].ToString());
            string channelName = channelData["name"]?.ToString();
            int type = Int32.Parse(channelData["type"].ToString());
            int position = Int32.Parse(channelData["position"].ToString());



            // dont add the channel to the guild if it is not the correct guild
            if(guildId != discordGuild.GuildId)
                return false;

            // text channel
            if(type == 0 || type == 4 || type == 5) {
                // dont add the channel if it already exists inside the guild
                if(discordGuild.GetTextChannel(channelId) != null)
                    return false;

                DiscordTextChannel textChannel = new DiscordTextChannel(discordGuild, channelId, channelName, position);
                discordGuild.AddTextChannel(textChannel);
                return true;
            }
            // voice channel
            else if(type == 2) {
                DiscordVoiceChannel voiceChannel = new DiscordVoiceChannel(discordGuild, channelId, channelName, position);

                var voiceStateData = channelData["voice_states"];
                if(voiceStateData == null) {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Error while creatíng voice state from channel " + channelId + ": " + channelData.ToString());
                    return false;
                }

                // users in channel
                var usersArray = voiceStateData as JArray;
                foreach(var userJToken in usersArray) {
                    ulong userId = UInt64.Parse(userJToken["user"]["id"].ToString());
                    string userName = userJToken["user"]["global_name"]?.ToString();
                    string avatar = userJToken["user"]["avatar"]?.ToString();
                    string iconUrl = null;

                    if(avatar != null) {
                        iconUrl = $"https://cdn.discordapp.com/avatars/{userId}/{avatar}.png";
                    }


                    VoiceStates voiceState = GetVoiceState(userJToken["voice_state"]);

                    DiscordUser discordUser = new DiscordUser(voiceChannel, userId, userName, voiceState, iconUrl);
                    voiceChannel.AddUser(discordUser);
                }

                discordGuild.AddVoiceChannel(voiceChannel);
                return true;
            }

            return false;
        }


        // returns the "VoiceStates" object based on data from the client
        private VoiceStates GetVoiceState(JToken voiceStateInfo) {
            if(voiceStateInfo == null)
                return VoiceStates.DISCONNECTED;

            if(voiceStateInfo["deaf"]?.ToString() == "true" || voiceStateInfo["self_dead"]?.ToString() == "true") 
                return VoiceStates.DEAFENED;

            if(voiceStateInfo["mute"]?.ToString() == "true" || voiceStateInfo["self_mute"]?.ToString() == "true" ) 
                return VoiceStates.MUTED;

            return VoiceStates.UNMUTED;
        }


        /*
         * File Management
         */
        private void SaveTokenToFile(string access_token) {
            if(!Directory.Exists(UserTokenDirectory))
                Directory.CreateDirectory(UserTokenDirectory);

            access_token ??= String.Empty;

            var data = new { token = access_token }; 
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(Path.Combine(UserTokenDirectory, "StoredToken.json"), json);

        }

        private string GetTokenFromFile() {
            if(!File.Exists(Path.Combine(UserTokenDirectory, "StoredToken.json")))
                return String.Empty;

            string json = File.ReadAllText(Path.Combine(UserTokenDirectory, "StoredToken.json"));
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            return data["token"];
        }
    }

}
