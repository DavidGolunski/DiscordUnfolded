using BarRaider.SdTools;
using Discord.Audio.Streams;
using Discord.WebSocket;
using DiscordUnfolded.DiscordStructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordUnfolded.DiscordCommunication {
    internal class IPCMessenger : IDisposable {

        private const string PIPE_NAME = "discord-ipc-";
        private NamedPipeClientStream pipe;


        public bool Connected { get => pipe != null && pipe.IsConnected; }

        private CancellationToken cancellationToken = CancellationToken.None;
        private readonly bool enableExtensiveLogging = false;


        private readonly Queue<JObject> messageQueue = new Queue<JObject>();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>> pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<JObject>>();


        // called when IPC received an event. The first parameter will be the event type and the second parameter will be the "data" from the event as a JObject
        public event Action<EventType, JObject> OnEventReceived;


        public IPCMessenger(bool enableExtensiveLogging = false) { 
            this.enableExtensiveLogging = enableExtensiveLogging;
        }

        // tries to connect to discords client via ipc. It runs listens for messages and events in an endless loop
        public void Connect(CancellationToken token) {
            if(Connected) {
                DebugLog("Pipe was already connected. Disconnect Pipe first, before calling the connect function again");
                return;
            }

            cancellationToken = token;

            // create the pipe connection to Discord RPC via IPC. Try indeces between 0 and 9
            for(int i = 0; i < 10 && !cancellationToken.IsCancellationRequested && !Connected; i++) {
                try {
                    // Attempt to connect to the pipe with the current index
                    pipe = new NamedPipeClientStream(".", PIPE_NAME + i, PipeDirection.InOut, PipeOptions.Asynchronous);
                    pipe.ConnectAsync(1000, cancellationToken).GetAwaiter().GetResult(); // 1000 milliseconds = 1 second

                    // If we reach here, the connection was successful
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Connected to pipe: {PIPE_NAME + i}");
                }
                catch(TimeoutException) {
                    // This pipe is not available (it may already be in use)
                    DebugLog($"Pipe {PIPE_NAME + i} is in use.");
                }
                catch(IOException) {
                    // This pipe is not available (it may not exist)
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Pipe {PIPE_NAME + i} does not exist.");
                    return;
                }
            }

            if(!Connected) {
                DebugLog("Pipe was not successfully connected!");
                return;
            }

            while(!cancellationToken.IsCancellationRequested && Connected) {
                ListenForMessages();
            }
        }

        public void Disconnect() {
            if(!Connected)
                return;

            pipe.Close();
            pipe.Dispose();
            pipe = null;
        }

        public void Dispose() {
            Disconnect();
        }

        /*
         * Pipe Reading Options
         */
        private void ListenForMessages() {
            byte[] buffer = new byte[1024 * 8];

            while(Connected && !cancellationToken.IsCancellationRequested) {
                
                try {
                    string json = ReadJson(buffer);
                    if(json == null) {
                        Task.Delay(10);
                        continue;
                    }

                    DebugLog("Received Message: " + json);

                    JObject messageObject = JsonConvert.DeserializeObject<JObject>(json);
                    string nonce = messageObject["nonce"]?.ToString();
                    nonce ??= string.Empty;

                    // if a request is waiting on a response, then the "nonce" will be found inside of the "pendingRequests" dictionary
                    if(pendingRequests.TryRemove(nonce, out var tcs)) {
                        tcs.SetResult(messageObject); // Unblock the waiting SendDisposeRequest
                    }
                    // if no one is waiting on a response then we queue the message up, so it can be interpreted later
                    else {
                        HandleMessage(messageObject);
                    }
                    return;
                }
                catch(Exception ex) {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error in ListenAsync: {ex.Message} \n {ex.StackTrace}");
                    break;
                }
            }
            return;
        }

        // sometimes the payload of channels can go over 1gb. This method allows us to not constantly block a big buffer for these cases
        private string ReadJson(byte[] standardBuffer) {
            byte[] metadataBuffer = new byte[8];

            int bytesRead = pipe.ReadAsync(metadataBuffer, 0, metadataBuffer.Length, cancellationToken).GetAwaiter().GetResult();
            if(bytesRead <= 0) {
                return null;
            }

            int op = BitConverter.ToInt32(metadataBuffer, 0);
            int jsonLength = BitConverter.ToInt32(metadataBuffer, 4);

            int currentIndex = 8;
            StringBuilder stringBuilder = new StringBuilder();
            do {
                int jsonBytesRead = pipe.ReadAsync(standardBuffer, 0, standardBuffer.Length, cancellationToken).GetAwaiter().GetResult();

                stringBuilder.Append(Encoding.UTF8.GetString(standardBuffer, 0, jsonBytesRead));
                currentIndex += jsonBytesRead;

            } while(currentIndex < jsonLength);

            return stringBuilder.ToString();
        }

        private void HandleMessage(JObject messageObject) {
            string eventTypeString = messageObject?["evt"]?.ToString();
            if(eventTypeString == null) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Received an unexpected message: " + messageObject.ToString());
                return;
            }

            EventType eventType = (EventType) Enum.Parse(typeof(EventType), eventTypeString);
            JObject data = messageObject["data"] as JObject;
            OnEventReceived?.Invoke(eventType, data);
        }


        /*
         * Request Sending Options
         */
        public IPCMessage SendDispatchRequest(string clientId) {
            if(!Connected || cancellationToken.IsCancellationRequested) {
                DebugLog("SendDispatchRequest failed because the pipe was not connected or a cancellation was requested");
                return IPCMessage.Empty;
            }

            var request = new { v = 1, client_id = clientId };

            return SendMessageAndGetIPCMessageResponse(MessageType.DISPATCH, string.Empty, request, 0);
        }

        public IPCMessage SendAuthorizeRequest(string clientId) {
            if(!Connected || cancellationToken.IsCancellationRequested) {
                DebugLog("SendAuthorizeRequest failed because the pipe was not connected or a cancellation was requested");
                return IPCMessage.Empty;
            }
            var generatedNonce = Guid.NewGuid().ToString();
            var request = new {
                nonce = generatedNonce,
                cmd = MessageType.AUTHORIZE.ToString(),
                args = new {
                    client_id = clientId,
                    scopes = new[] { "identify", "guilds", "rpc" }
                }
            };

            return SendMessageAndGetIPCMessageResponse(MessageType.AUTHORIZE, generatedNonce, request);
        }

        public IPCMessage SendAuthenticateRequest(string accessToken) {
            if(!Connected || cancellationToken.IsCancellationRequested) {
                DebugLog("SendAuthenticateRequest failed because the pipe was not connected or a cancellation was requested");
                return IPCMessage.Empty;
            }
            var generatedNonce = Guid.NewGuid().ToString();
            var request = new {
                nonce = generatedNonce,
                cmd = MessageType.AUTHENTICATE.ToString(),
                args = new {
                    acess_token = accessToken
                }
            };

            return SendMessageAndGetIPCMessageResponse(MessageType.AUTHENTICATE, generatedNonce, request);
        }

        public IPCMessage SendSelectVoiceChannelRequest(ulong channelId) {
            if(!Connected || cancellationToken.IsCancellationRequested) {
                DebugLog("SendSelectVoiceChannelRequest failed because the pipe was not connected or a cancellation was requested");
                return IPCMessage.Empty;
            }

            string channelIdString = channelId == 0 ? null : channelId.ToString();

            var generatedNonce = Guid.NewGuid().ToString();
            var request = new {
                nonce = generatedNonce,
                cmd = MessageType.SELECT_VOICE_CHANNEL.ToString(),
                args = new {
                    channel_id = channelIdString
                }
            };

            return SendMessageAndGetIPCMessageResponse(MessageType.SELECT_VOICE_CHANNEL, generatedNonce, request);
        }

        public IPCMessage SendSelectTextChannelRequest(ulong channelId) {
            if(!Connected || cancellationToken.IsCancellationRequested) {
                DebugLog("SendSelectTextChannelRequest failed because the pipe was not connected or a cancellation was requested");
                return IPCMessage.Empty;
            }

            string channelIdString = channelId == 0 ? null : channelId.ToString();

            var generatedNonce = Guid.NewGuid().ToString();
            var request = new {
                nonce = generatedNonce,
                cmd = MessageType.SELECT_TEXT_CHANNEL.ToString(),
                args = new {
                    channel_id = channelIdString
                }
            };

            return SendMessageAndGetIPCMessageResponse(MessageType.SELECT_TEXT_CHANNEL, generatedNonce, request);
        }

        public IPCMessage SendGetSelectedVoiceChannelRequest() {
            if(!Connected || cancellationToken.IsCancellationRequested) {
                DebugLog("SendGetSelectedVoiceChannelRequest failed because the pipe was not connected or a cancellation was requested");
                return IPCMessage.Empty;
            }

            var generatedNonce = Guid.NewGuid().ToString();
            var request = new {
                nonce = generatedNonce,
                cmd = MessageType.GET_SELECTED_VOICE_CHANNEL.ToString(),
                args = new { }
            };

            return SendMessageAndGetIPCMessageResponse(MessageType.GET_SELECTED_VOICE_CHANNEL, generatedNonce, request);
        }

        public IPCMessage SendGetGuildsRequest() {
            if(!Connected || cancellationToken.IsCancellationRequested) {
                DebugLog("SendGetGuildsRequest failed because the pipe was not connected or a cancellation was requested");
                return IPCMessage.Empty;
            }

            var generatedNonce = Guid.NewGuid().ToString();
            var request = new {
                nonce = generatedNonce,
                cmd = MessageType.GET_GUILDS.ToString(),
                args = new { }
            };

            return SendMessageAndGetIPCMessageResponse(MessageType.GET_GUILDS, generatedNonce, request);
        }

        public IPCMessage SendGetGuildRequest(ulong guildId) {
            if(!Connected || cancellationToken.IsCancellationRequested) {
                DebugLog("SendGetGuildResponse failed because the pipe was not connected or a cancellation was requested");
                return IPCMessage.Empty;
            }

            var generatedNonce = Guid.NewGuid().ToString();
            var request = new {
                nonce = generatedNonce,
                cmd = MessageType.GET_GUILD.ToString(),
                args = new {
                    guild_id = guildId.ToString()
                }
            };

            return SendMessageAndGetIPCMessageResponse(MessageType.GET_GUILD, generatedNonce, request);
        }

        public IPCMessage SendGetChannelsRequest(ulong guildId) {
            if(!Connected || cancellationToken.IsCancellationRequested) {
                DebugLog("SendGetChannelsRequest failed because the pipe was not connected or a cancellation was requested");
                return IPCMessage.Empty;
            }

            var generatedNonce = Guid.NewGuid().ToString();
            var request = new {
                nonce = generatedNonce,
                cmd = MessageType.GET_CHANNELS.ToString(),
                args = new {
                    guild_id = guildId.ToString()
                }
            };

            return SendMessageAndGetIPCMessageResponse(MessageType.GET_CHANNELS, generatedNonce, request);
        }

        public IPCMessage SendGetChannelRequest(ulong channelId) {
            if(!Connected || cancellationToken.IsCancellationRequested) {
                DebugLog("SendGetChannelRequest failed because the pipe was not connected or a cancellation was requested");
                return IPCMessage.Empty;
            }

            var generatedNonce = Guid.NewGuid().ToString();
            var request = new {
                nonce = generatedNonce,
                cmd = MessageType.GET_CHANNEL.ToString(),
                args = new {
                    channel_id = channelId.ToString()
                }
            };

            return SendMessageAndGetIPCMessageResponse(MessageType.GET_CHANNEL, generatedNonce, request);
        }


        /*
         * Subscribing and Unsibscribing from Events
         */

        // sends a subscribe event for options that do not need any special parameters
        public IPCMessage SendGeneralSubscribeEvent(EventType eventType) {
            if(!Connected || cancellationToken.IsCancellationRequested) {
                DebugLog("SendGeneralSubscribeEvent failed because the pipe was not connected or a cancellation was requested");
                return IPCMessage.Empty;
            }

            if(eventType != EventType.GUILD_CREATE && eventType != EventType.CHANNEL_CREATE) {
                DebugLog("SendGeneralSubscribeEvent failed because the Event " + eventType + " is not supported in a general request");
                return IPCMessage.Empty;
            }

            var generatedNonce = Guid.NewGuid().ToString();
            var request = new {
                nonce = generatedNonce,
                cmd = MessageType.SUBSCRIBE.ToString(),
                args = new { },
                evt = eventType.ToString()
            };

            return SendMessageAndGetIPCMessageResponse(MessageType.SUBSCRIBE, generatedNonce, request);
        }


        // sends a subscribe event for options that only need a channel_id as a parameter
        public IPCMessage SendChannelSubscribeEvent(EventType eventType, ulong channelID) {
            if(!Connected || cancellationToken.IsCancellationRequested) {
                DebugLog("SendChannelSubscribeEvent failed because the pipe was not connected or a cancellation was requested");
                return IPCMessage.Empty;
            }

            if(eventType != EventType.VOICE_STATE_CREATE && eventType != EventType.VOICE_STATE_DELETE && eventType != EventType.VOICE_STATE_UPDATE) {
                DebugLog("SendChannelSubscribeEvent failed because the Event " + eventType + " is not supported in a channel request");
                return IPCMessage.Empty;
            }

            var generatedNonce = Guid.NewGuid().ToString();
            var request = new {
                nonce = generatedNonce,
                cmd = MessageType.SUBSCRIBE.ToString(),
                args = new {
                    channel_id = channelID.ToString()
                },
                evt = eventType.ToString()
            };

            return SendMessageAndGetIPCMessageResponse(MessageType.SUBSCRIBE, generatedNonce, request);
        }

        public IPCMessage SendGeneralUnsubscribeRequest(EventType eventType) {
            if(!Connected || cancellationToken.IsCancellationRequested) {
                DebugLog("SendGeneralUnsubscribeRequest failed because the pipe was not connected or a cancellation was requested");
                return IPCMessage.Empty;
            }

            if(eventType != EventType.GUILD_CREATE && eventType != EventType.CHANNEL_CREATE) {
                DebugLog("SendGeneralUnsubscribeRequest failed because the Event " + eventType + " is not supported in a general request");
                return IPCMessage.Empty;
            }

            var generatedNonce = Guid.NewGuid().ToString();
            var request = new {
                nonce = generatedNonce,
                cmd = MessageType.UNSUBSCRIBE.ToString(),
                args = new { },
                evt = eventType.ToString()
            };

            return SendMessageAndGetIPCMessageResponse(MessageType.UNSUBSCRIBE, generatedNonce, request);
        }

        public IPCMessage SendChannelUnsubscribeRequest(EventType eventType, ulong channelID) {
            if(!Connected || cancellationToken.IsCancellationRequested) {
                DebugLog("SendChannelUnsubscribeRequest failed because the pipe was not connected or a cancellation was requested");
                return IPCMessage.Empty;
            }

            if(eventType != EventType.VOICE_STATE_CREATE && eventType != EventType.VOICE_STATE_DELETE && eventType != EventType.VOICE_STATE_UPDATE) {
                DebugLog("SendChannelUnsubscribeRequest failed because the Event " + eventType + " is not supported in a channel request");
                return IPCMessage.Empty;
            }

            var generatedNonce = Guid.NewGuid().ToString();
            var request = new {
                nonce = generatedNonce,
                cmd = MessageType.UNSUBSCRIBE.ToString(),
                args = new {
                    channel_id = channelID.ToString()
                },
                evt = eventType.ToString()
            };

            return SendMessageAndGetIPCMessageResponse(MessageType.UNSUBSCRIBE, generatedNonce, request);
        }


        /*
         * General IPC helper functions
         */

        // Creates the IPC Message from a JObject
        private IPCMessage SendMessageAndGetIPCMessageResponse(MessageType messageType, string nonce, object request, int op = 1) {
            // add the request to the pending Request dictionary
            var tcs = new TaskCompletionSource<JObject>(cancellationToken);
            pendingRequests[nonce] = tcs;

            SendMessageAsync(op, request).GetAwaiter().GetResult();

            // create the IPC Message
            JObject message = tcs.Task.GetAwaiter().GetResult();

            if(message == null || message["cmd"]?.ToString() != messageType.ToString())
                return IPCMessage.Empty;

            // check if an error has occured
            if(message["evt"]?.ToString() == EventType.ERROR.ToString())
                return new IPCMessage(messageType, message["data"] as JObject, null);

            return new IPCMessage(messageType, null, message["data"] as JObject);
        }

        // general function for sending data via IPC
        private async Task SendMessageAsync(int op, object payload) {
            var json = JsonConvert.SerializeObject(payload);
            DebugLog("Send Message: " + json);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var length = BitConverter.GetBytes(jsonBytes.Length);
            var opBytes = BitConverter.GetBytes(op);

            using var ms = new MemoryStream();
            ms.Write(opBytes, 0, 4);
            ms.Write(length, 0, 4);
            ms.Write(jsonBytes, 0, jsonBytes.Length);

            await pipe.WriteAsync(ms.ToArray(), 0, (int) ms.Length);
            await pipe.FlushAsync();
        }


        /*
         * Debugging
         */
        private void DebugLog(string message) {
            if(!enableExtensiveLogging)
                return;
            Logger.Instance.LogMessage(TracingLevel.DEBUG, message);
        }
    }
}
