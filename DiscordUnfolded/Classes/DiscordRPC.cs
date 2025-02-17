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

namespace DiscordUnfolded {
    public class DiscordRPC {

        private static DiscordRPC instance;
        public static DiscordRPC Instance {
            get => instance ??= new DiscordRPC();
            private set => instance = value;
        }
        private const string clientId = "1337102485017595966"; // Replace with your actual Client ID
        private const string clientSecret = "6FWjjmhTxcaNMBwzVCZq_bKuS6KDy-qp";
        private const string PIPE_NAME = "discord-ipc-"; // Try 0-9 -> "discord-ipc-0", "discord-ipc-1", ...




        public ulong CurrentUserID { get; private set; } = 0;

        public bool IsRunning { get => cancellationTokenSource != null; }

        private CancellationTokenSource cancellationTokenSource;
        
        private string authorizationCode = null;
        private string accessToken = null; // Retrieved after authenticate
        private NamedPipeClientStream pipe;
        private const string redirectUri = "https://127.0.0.1:7393/callback";

        private DiscordRPC() {
            cancellationTokenSource = null;
        }

        public void Start() {
            if(IsRunning) {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            Task.Run(() => Connect(token), token);

           Logger.Instance.LogMessage(TracingLevel.INFO, "DiscordRPC Started");
        }

        public void Stop() {
            if(!IsRunning) {
                return;
            }

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;

            Disconnect();

            Logger.Instance.LogMessage(TracingLevel.INFO, "DiscordRPC Stopped");
        }

        private async Task Connect(CancellationToken token) {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Started Connect function " + token);
            if(token == null)
                return;

            Disconnect();

            // create the pipe connection to Discord RPC via IPC. Try indeces between 0 and 9
            for(int i = 0; i < 10 && !token.IsCancellationRequested && (pipe == null || !pipe.IsConnected); i++) {
                try {
                    // Attempt to connect to the pipe with the current index
                    pipe = new NamedPipeClientStream(".", PIPE_NAME + i, PipeDirection.InOut, PipeOptions.Asynchronous);

                    // Try to connect with a timeout (e.g., 1 second)
                    await pipe.ConnectAsync(1000, token); // 1000 milliseconds = 1 second

                    // If we reach here, the connection was successful
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Connected to pipe: {PIPE_NAME + i}");
                }
                catch(TimeoutException) {
                    // This pipe is not available (it may already be in use)
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Pipe {PIPE_NAME + i} is in use.");
                }
                catch(IOException) {
                    // This pipe is not available (it may not exist)
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Pipe {PIPE_NAME + i} does not exist.");
                    Stop();
                    return;
                }
            }


            // Send Handshake
            await SendMessageAsync(0, new { v = 1, client_id = clientId });
            
            JObject dispatchMessage = ListenForMessages(token);
            string dispatchCommand = dispatchMessage["cmd"]?.ToString();
            if(dispatchCommand != "DISPATCH") {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "DiscordRPC expected a DISPATCH command, but received \"" + dispatchCommand + "\" instead!");
                Stop();
                return;
            }
            if(token.IsCancellationRequested)
                return;


            // Send Authorize Request
            var authorizeRequest = new {
                nonce = Guid.NewGuid().ToString(),
                cmd = "AUTHORIZE",
                args = new {
                    client_id = clientId,
                    scopes = new[] { "identify", "rpc", "guilds" }
                }
            };
            await SendMessageAsync(1, authorizeRequest);

            JObject authorizeMessage = ListenForMessages(token);
            string authorizeCommand = authorizeMessage["cmd"]?.ToString();
            if(authorizeCommand != "AUTHORIZE") {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "DiscordRPC expected a AUTHORIZE command, but received \"" + authorizeCommand + "\" instead!");
                Stop();
                return;
            }
 

            // Send authorization Code to OAuth2 to get Bearer Access Token
            string authorizationCode = authorizeMessage["data"]?["code"]?.ToString();
            string accessToken = null;
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


            // Authenticate with the OAuth2 Access Token via IPC
            var authenticateRequest = new {
                nonce = Guid.NewGuid().ToString(),
                cmd = "AUTHENTICATE",
                args = new {
                    acess_token = accessToken
                }
            };
            await SendMessageAsync(1, authenticateRequest);

            JObject authenticateMessage = ListenForMessages(token);
            string authenticateCommand = authenticateMessage["cmd"]?.ToString();
            if(authenticateCommand != "AUTHENTICATE") {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "DiscordRPC expected a AUTHENTICATE command, but received \"" + authenticateCommand + "\" instead!");
                Stop();
                return;
            }

            CurrentUserID = UInt64.Parse(authenticateMessage["data"]["user"]["id"].ToString());

            Logger.Instance.LogMessage(TracingLevel.DEBUG, authenticateMessage.ToString());


            if(token.IsCancellationRequested)
                return;

            GetAvailableGuilds();
        }

        private void Disconnect() {
            CurrentUserID = 0;
            if(pipe == null)
                return;
            pipe.Close();
            pipe.Dispose();
        }

        private JObject ListenForMessages(CancellationToken token) {
            byte[] buffer = new byte[2048 * 8];

            while(pipe != null && pipe.IsConnected && token != null && !token.IsCancellationRequested) {
                try {
                    int bytesRead = pipe.ReadAsync(buffer, 0, buffer.Length, token).GetAwaiter().GetResult();
                    if(bytesRead <= 0) {
                        Task.Delay(10);
                        continue;
                    }

                    int op = BitConverter.ToInt32(buffer, 0);
                    int jsonLength = BitConverter.ToInt32(buffer, 4);
                    string json = Encoding.UTF8.GetString(buffer, 8, jsonLength);
                    return JsonConvert.DeserializeObject<JObject>(json);
                }
                catch(Exception ex) {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error in ListenAsync: {ex.StackTrace}");
                    break;
                }
            }

            return null;
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

        public List<DiscordGuild> GetAvailableGuilds() {
            if(pipe == null || !pipe.IsConnected) {
                return null;
            }

            List<DiscordGuild> guilds = new List<DiscordGuild>();


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
    }

}
