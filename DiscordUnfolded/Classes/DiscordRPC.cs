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
using WebSocketSharp;
using Newtonsoft.Json;

namespace DiscordUnfolded {
    internal class DiscordRPC {

        private static DiscordRPC instance;
        public static DiscordRPC Instance {
            get => instance ??= new DiscordRPC();
            private set => instance = value;
        }

        // shows if the autoclicker is currently running
        public bool IsRunning { get => cancellationTokenSource != null; }

        private CancellationTokenSource cancellationTokenSource;

        private WebSocket ws;
        private string clientId = "1335023476733513819"; // Replace with your app's Client ID
        private string secret = "XolubfPRlQlM8mqNg7rdzAjmG-cVhVEt";
        private string accessToken; // This will be retrieved after authorization




        private DiscordRPC() {
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

            Task.Run(() => StartRPC(), token);

            BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "BetterDiscordReceiver Started");

        }

        public void Stop() {
            if(!IsRunning) {
                return;
            }


            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;

            BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "BetterDiscordReceiver Stopped");
        }



        public void StartRPC() {
            int[] ports = { 6463, 6464, 6465, 6466, 6467, 6468, 6469, 6470, 6471, 6472 };

            foreach(var port in ports) {
                string wsUrl = $"ws://127.0.0.1:{port}/?v=1&client_id={clientId}&encoding=json";

                try {
                    ws = new WebSocket(wsUrl);
                    ws.OnMessage += OnMessageReceived;
                    ws.Connect();

                    BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, $"Connected to Discord RPC on port {port}");
                    SendAuthorizeRequest();
                    return;
                }
                catch(Exception) {
                    BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, $"Failed to connect on port {port}, trying next...");
                }
            }

            BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "Failed to connect to Discord RPC.");
        }

        private void OnMessageReceived(object sender, MessageEventArgs e) {
            BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "Received: " + e.Data);

            var response = JsonConvert.DeserializeObject<dynamic>(e.Data);
            if(response.cmd == "AUTHORIZE") {
                string authCode = response.data.code;
                BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "Authorization Code: " + authCode);

                // Exchange authorization code for an access token
                Task.Run(() => ExchangeAuthCodeForToken(authCode));
            }

            if(response.cmd == "AUTHENTICATE") {
                BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "Authenticated successfully! Now we can send RPC commands.");
            }
        }

        private void SendAuthorizeRequest() {
            var request = new {
                nonce = Guid.NewGuid().ToString(),
                args = new {
                    client_id = clientId,
                    scopes = new[] { "rpc", "identify" }
                },
                cmd = "AUTHORIZE"
            };

            ws.Send(JsonConvert.SerializeObject(request));
            BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "Sent authorization request...\n" + JsonConvert.SerializeObject(request));
        }

        private async Task ExchangeAuthCodeForToken(string authCode) {
            using(var client = new System.Net.Http.HttpClient()) {
                var values = new System.Collections.Generic.Dictionary<string, string> {
                { "client_id", clientId },
                { "client_secret", secret },  // Get this from Discord Developer Portal
                { "grant_type", "authorization_code" },
                { "code", authCode },
                { "redirect_uri", "https://localhost" } // Must match the one in your Discord app settings
            };

                var content = new System.Net.Http.FormUrlEncodedContent(values);
                var response = await client.PostAsync("https://discord.com/api/oauth2/token", content);
                var responseString = await response.Content.ReadAsStringAsync();

                dynamic jsonResponse = JsonConvert.DeserializeObject(responseString);
                accessToken = jsonResponse.access_token;

                BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "Access Token: " + accessToken);

                SendAuthenticateRequest();
            }
        }

        private void SendAuthenticateRequest() {
            var request = new {
                nonce = Guid.NewGuid().ToString(),
                args = new {
                    access_token = accessToken
                },
                cmd = "AUTHENTICATE"
            };

            ws.Send(JsonConvert.SerializeObject(request));
            BarRaider.SdTools.Logger.Instance.LogMessage(TracingLevel.INFO, "Sent authentication request...");
        }
    }

}
