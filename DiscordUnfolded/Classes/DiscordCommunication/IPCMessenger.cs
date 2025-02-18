using BarRaider.SdTools;
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
            byte[] buffer = new byte[2048 * 8];

            while(Connected && !cancellationToken.IsCancellationRequested) {
                try {
                    int bytesRead = pipe.ReadAsync(buffer, 0, buffer.Length, cancellationToken).GetAwaiter().GetResult();
                    if(bytesRead <= 0) {
                        Task.Delay(10);
                        continue;
                    }

                    int op = BitConverter.ToInt32(buffer, 0);
                    int jsonLength = BitConverter.ToInt32(buffer, 4);
                    string json = Encoding.UTF8.GetString(buffer, 8, jsonLength);

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
                        messageQueue.Enqueue(messageObject);
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




        /*
         * Request Sending Options
         */
        public IPCMessage SendDispatchRequest(string clientId) {
            if(!Connected || cancellationToken.IsCancellationRequested) {
                DebugLog("SendDispatchRequest failed because the pipe was not connected or a cancellation was requested");
                return IPCMessage.Empty;
            }

            var request = new { v = 1, client_id = clientId };

            // add the request to the pending Request dictionary
            var tcs = new TaskCompletionSource<JObject>(cancellationToken);
            pendingRequests[string.Empty] = tcs; // nonce is null for dispatch requests

            SendMessageAsync(0, request).GetAwaiter().GetResult();

            // validate result
            JObject message = tcs.Task.GetAwaiter().GetResult();
            if(message == null || message["cmd"]?.ToString() != MessageType.DISPATCH.ToString())
                return IPCMessage.Empty;

            return new IPCMessage(MessageType.DISPATCH, null, message["data"] as JObject);
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
                    scopes = new[] { "identify", "rpc", "guilds" }
                }
            };

            // add the request to the pending Request dictionary
            var tcs = new TaskCompletionSource<JObject>(cancellationToken);
            pendingRequests[generatedNonce] = tcs;

            SendMessageAsync(1, request).GetAwaiter().GetResult();

            // validate result
            JObject message = tcs.Task.GetAwaiter().GetResult();
            if(message == null || message["cmd"]?.ToString() != MessageType.AUTHORIZE.ToString())
                return IPCMessage.Empty;

            return new IPCMessage(MessageType.AUTHORIZE, null, message["data"] as JObject);
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

            // add the request to the pending Request dictionary
            var tcs = new TaskCompletionSource<JObject>(cancellationToken);
            pendingRequests[generatedNonce] = tcs;

            SendMessageAsync(1, request).GetAwaiter().GetResult();

            // validate result
            JObject message = tcs.Task.GetAwaiter().GetResult();
            if(message == null || message["cmd"]?.ToString() != MessageType.AUTHENTICATE.ToString())
                return IPCMessage.Empty;

            return new IPCMessage(MessageType.AUTHENTICATE, null, message["data"] as JObject);
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

            // add the request to the pending Request dictionary
            var tcs = new TaskCompletionSource<JObject>(cancellationToken);
            pendingRequests[generatedNonce] = tcs;

            SendMessageAsync(1, request).GetAwaiter().GetResult();

            // validate result
            JObject message = tcs.Task.GetAwaiter().GetResult();
            if(message == null || message["cmd"]?.ToString() != MessageType.SELECT_VOICE_CHANNEL.ToString())
                return IPCMessage.Empty;

            return new IPCMessage(MessageType.SELECT_VOICE_CHANNEL, null, message["data"] as JObject);
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
