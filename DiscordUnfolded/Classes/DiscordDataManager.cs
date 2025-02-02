using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {
    internal class DiscordDataManager {


        private static DiscordDataManager instance;
        public static DiscordDataManager Instance {
            get => instance ??= new DiscordDataManager();
            private set => instance = value;
        }

        // actions that want to subscribe to events from the DiscordDataManager need to retrieve 
        private static ulong SenderActionId = 1;
        public static ulong GetNextActionId() { return SenderActionId++; }


        // for the server browser
        private int serverGridOffset = 0;
        public int ServerBrowserOffset {
            get {
                return serverGridOffset;
            }
            set {
                if(discordGuildInfos.Count == 0)
                    serverGridOffset = 0;

                serverGridOffset = value;
                // make sure serverBrowserOffset is within 
                serverGridOffset += discordGuildInfos.Count * 64; // 64 is just a random high number that aims to make sure the serverBrowserOffset is positive
                serverGridOffset %= discordGuildInfos.Count;
            }
        }
        private readonly List<DiscordGuildInfo> discordGuildInfos = new List<DiscordGuildInfo>();

        private const int maxServerButtons = 8;
        // an array that holds a dictionary of subscribers
        private readonly Dictionary<ulong, ServerGridAction>[] serverSubscribers = new Dictionary<ulong, ServerGridAction>[maxServerButtons];
        




        // for the channel browser/grid
        private const int Width = 8;
        private const int Height = 4;

        private int OffsetX = 0;
        private int OffsetY = 0;



        private DiscordDataManager() {
            for(int i = 0; i < maxServerButtons; i++) {
                serverSubscribers[i] = new Dictionary<ulong, ServerGridAction>();
            }
        }



        public void UpdateGuildInfo(List<DiscordGuildInfo> newGuildInfo) {
            if(newGuildInfo == null) {
                discordGuildInfos.Clear();
                // ToDo: Sent out updates to the buttons
                return;
            }

            for(int i = 0; i < newGuildInfo.Count; i++) {

                // if the new guild was not in the old list because the list was too short, just add it
                if(discordGuildInfos.Count <= i) {
                    discordGuildInfos.Add(newGuildInfo[i]);
                    // ToDo: Sent out updates to the buttons
                }

                // if the guild on index i has changed, update the guild information
                else if(!discordGuildInfos[i].Equals(newGuildInfo[i])) {
                    discordGuildInfos[i] = newGuildInfo[i];
                    // ToDo: Sent out updates to the buttons
                }
            }

            // remove guild
            if(this.discordGuildInfos.Count > newGuildInfo.Count) {
                this.discordGuildInfos.RemoveRange(newGuildInfo.Count, this.discordGuildInfos.Count - newGuildInfo.Count);
            }
        }

        public void SubscribeToServerBrowser(int serverButtonIndex, ServerGridAction serverGridAction) {
            if (serverGridAction == null) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "ServerGridAction was null");
                return;
            }

            if (serverButtonIndex < 0 || serverButtonIndex >= maxServerButtons) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "ServerbuttonIndex was out of range. ID: " + serverGridAction.ActionId + "  ServerButtonIndex: " + serverButtonIndex);
                return;
            }

            if(serverSubscribers[serverButtonIndex].ContainsKey(serverGridAction.ActionId)) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Serverbutton already existed for index: " + serverButtonIndex + " ID: " + serverGridAction.ActionId );
                return;
            }

            serverSubscribers[serverButtonIndex].Add(serverGridAction.ActionId, serverGridAction);
        }

        public void UnsubscribeFromServerBrowser(int serverButtonIndex, ServerGridAction serverGridAction) {
            if (serverGridAction == null) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "ServerGridAction was null");
                return;
            }

            if(serverButtonIndex < 0 || serverButtonIndex >= maxServerButtons) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "ServerbuttonIndex was out of range. ID: " + serverGridAction.ActionId + "  ServerButtonIndex: " + serverButtonIndex);
                return;
            }

            if(!serverSubscribers[serverButtonIndex].ContainsKey(serverGridAction.ActionId)) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Serverbutton didnt exist for index: " + serverButtonIndex + " ID: " + serverGridAction.ActionId);
                return;
            }

            serverSubscribers[serverButtonIndex].Remove(serverGridAction.ActionId);

        }
             

    }
}
