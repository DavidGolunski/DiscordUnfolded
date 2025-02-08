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

namespace DiscordUnfolded {
    public class DiscordRPC {

        private static DiscordRPC instance;
        public static DiscordRPC Instance {
            get => instance ??= new DiscordRPC();
            private set => instance = value;
        }

        public bool IsRunning { get => cancellationTokenSource != null; }

        private CancellationTokenSource cancellationTokenSource;
        private const string clientId = "1337102485017595966"; // Replace with your actual Client ID
        private const string clientSecret = "6FWjjmhTxcaNMBwzVCZq_bKuS6KDy-qp";
        private string authorizationCode = null;
        private string accessToken = null; // Retrieved after authenticate
        private NamedPipeClientStream pipe;
        private const string PIPE_NAME = "discord-ipc-0"; // Try 0-9
        private const string redirectUri = "https://127.0.0.1:7393";

        private DiscordRPC() {
            cancellationTokenSource = null;
        }

        public void Start() {
            if(IsRunning) {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            Task.Run(() => StartRPC(token), token);

            BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "BetterDiscordReceiver Started");
        }

        public void Stop() {
            if(!IsRunning) {
                return;
            }

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;

            if(pipe != null) {
                pipe.Close();
                pipe.Dispose();
            }

            BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "BetterDiscordReceiver Stopped");
        }

        private async Task StartRPC(CancellationToken token) {
            try {
                pipe = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.InOut, PipeOptions.Asynchronous);
                await pipe.ConnectAsync(5000, token);

                BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, $"Connected to Discord IPC: {PIPE_NAME}");

                // Send Handshake
                await SendMessageAsync(0, new { v = 1, client_id = clientId });

                // Start listening for incoming messages
                await ListenAsync(token);
            }
            catch(Exception ex) {
                BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.ERROR, $"Failed to connect to Discord IPC: {ex.Message}");
                return;
            }

           
        }

        private async Task ListenAsync(CancellationToken token) {
            byte[] buffer = new byte[2048];
            while(!token.IsCancellationRequested && pipe.IsConnected) {
                BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "Listening For Messages");
                try {
                    int bytesRead = await pipe.ReadAsync(buffer, 0, buffer.Length, token);
                    if(bytesRead > 0) {
                        string response = ParseMessage(buffer, bytesRead);
                        HandleIncomingMessage(response);
                    }
                }
                catch(Exception ex) {
                    BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error in ListenAsync: {ex.Message}");
                    break;
                }
            }
            BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "Stopped Listeing For Messages");
        }

        private void HandleIncomingMessage(string message) {
            var response = JsonConvert.DeserializeObject<JObject>(message);
            string cmd = response["cmd"]?.ToString();

            switch(cmd) {
                case "DISPATCH":
                    BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "Received DISPATCH, sending AUTHORIZE command...");
                    _ = SendAuthorizeRequest();
                    break;
                case "AUTHORIZE":
                    authorizationCode = response["data"]?["code"]?.ToString();
                    BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "Received Code: " + authorizationCode + " Authorize Message: " + message);
                    //_ = SendAuthenticateRequest();
                    try {
                        accessToken = DiscordOAuth2.ExchangeCode(authorizationCode, clientId, clientSecret, redirectUri);
                    }
                    catch (Exception ex) {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, "Received Error while retrieving access token: " + ex.StackTrace);
                        break;
                    }
                    
                    BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "Received AccessToken: " + accessToken);
                    _ = GetGuilds();
                    break;
                case "AUTHENTICATE":
                    BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "Received Authenticate: " + message);
                    _ = GetGuilds();
                    break;
                default:
                    BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "Received Message: " + message);
                    break;
            }
        }

        private async Task SendAuthorizeRequest() {
            var request = new {
                nonce = Guid.NewGuid().ToString(),
                cmd = "AUTHORIZE",
                args = new {
                    client_id = clientId,
                    scopes = new[] { "identify", "rpc", "guilds" }
                }
            };

            await SendMessageAsync(1, request);    
        }

        private async Task SendAuthenticateRequest() {
            // will try to do this later. I did not receive any message on the specificed port, but already got an access token
            // _ = Task.Run(() => StartHttpListener(), CancellationToken.None); 

            var request = new {
                nonce = Guid.NewGuid().ToString(),
                cmd = "AUTHENTICATE",
                args = new {
                    acess_token = accessToken
                }
            };

            await SendMessageAsync(1, request);
        }

        private async Task StartHttpListener() {
            using var listener = new HttpListener();
            listener.Prefixes.Add($"{redirectUri}/");
            listener.Start();
            BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, $"Listening for auth code on {redirectUri}");

            try {
                var context = await listener.GetContextAsync();
                string authCode = context.Request.QueryString["code"];

                if(!string.IsNullOrEmpty(authCode)) {
                    BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, $"Received Auth Code: {authCode}");
                    _ = ExchangeAuthCodeForToken(authCode);
                }

                string responseString = "Authorization successful! You can close this window.";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                context.Response.ContentLength64 = buffer.Length;
                using var output = context.Response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
            }
            catch(Exception ex) {
                BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.ERROR, $"HTTP Listener Error: {ex.Message}");
            }
            finally {
                listener.Stop();
            }
        }

        private async Task ExchangeAuthCodeForToken(string authCode) {
            using var client = new HttpClient();
            var values = new Dictionary<string, string>
            {
            { "client_id", clientId },
            { "client_secret", "YOUR_CLIENT_SECRET" },
            { "grant_type", "authorization_code" },
            { "code", authCode },
            { "redirect_uri", redirectUri }
        };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("https://discord.com/api/oauth2/token", content);
            var responseString = await response.Content.ReadAsStringAsync();

            BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, $"Token Response: {responseString}");
        }

        private async Task GetGuilds() {
            //_ = Task.Run(() => StartHttpListener(), CancellationToken.None);

            var request = new {
                nonce = Guid.NewGuid().ToString(),
                cmd = "GET_GUILDS",
                args = new { }
            };

            await SendMessageAsync(1, request);
        }


        private async Task SendMessageAsync(int op, object payload) {
            var json = JsonConvert.SerializeObject(payload);
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

        private string ParseMessage(byte[] buffer, int length) {
            int op = BitConverter.ToInt32(buffer, 0);
            int jsonLength = BitConverter.ToInt32(buffer, 4);
            string json = Encoding.UTF8.GetString(buffer, 8, jsonLength);
            return json;
        }
    }

}
