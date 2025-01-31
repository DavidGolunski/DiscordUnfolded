using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BarRaider.SdTools;

namespace DiscordUnfolded {
    internal class BetterDiscordReceiver {

        private static BetterDiscordReceiver instance;
        public static BetterDiscordReceiver Instance {
            get => instance ??= new BetterDiscordReceiver();
            private set => instance = value;
        }

        // shows if the autoclicker is currently running
        public bool IsRunning { get => cancellationTokenSource != null; }

        private CancellationTokenSource cancellationTokenSource;


        public BetterDiscordReceiver() {
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

            Logger.Instance.LogMessage(TracingLevel.INFO, "BetterDiscordReceiver Started");

        }

        public void Stop() {
            if(!IsRunning) {
                return;
            }
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;

            Logger.Instance.LogMessage(TracingLevel.INFO, "BetterDiscordReceiver Stopped");
        }

        private async Task ListenForMessages(CancellationToken cancellationToken) {
            using UdpClient udpClient = new UdpClient(8223);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8223);

            while(!cancellationToken.IsCancellationRequested) {
                try {
                    if(udpClient.Available > 0) {
                        UdpReceiveResult result = await udpClient.ReceiveAsync();
                        string message = Encoding.UTF8.GetString(result.Buffer);
                        Logger.Instance.LogMessage(TracingLevel.DEBUG, "Received: " + message);
                    }
                }
                catch(Exception ex) {
                    Console.WriteLine($"Error: {ex.Message}");
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Error: " + ex.StackTrace);
                }
                await Task.Delay(100, cancellationToken);
            }
        }
    }
}
