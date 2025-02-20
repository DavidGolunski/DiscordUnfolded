using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using DiscordUnfolded.DiscordStructure;
using DiscordUnfolded.DiscordCommunication;

namespace DiscordUnfolded {
    public class ServerBrowserManager {

        private static ServerBrowserManager instance;
        public static ServerBrowserManager Instance {
            get => instance ??= new ServerBrowserManager();
            private set => instance = value;
        }


        private const int maxServerButtons = 8;

        private int offset = 0;
        public int Offset {
            get => offset;
            set {
                if(DiscordRPC.Instance.AvailableGuilds.Count == 0) {
                    offset = 0;
                    UpdateAllPositions();
                    return;
                }

                int newOffset = value;
                if(newOffset < 0) {
                    newOffset *= -1;
                    newOffset %= DiscordRPC.Instance.AvailableGuilds.Count;
                    newOffset = (DiscordRPC.Instance.AvailableGuilds.Count - newOffset) % DiscordRPC.Instance.AvailableGuilds.Count;
                }
                else {
                    newOffset %= DiscordRPC.Instance.AvailableGuilds.Count;
                }
                offset = newOffset;
                UpdateAllPositions();
            } 
        }

        private readonly Dictionary<int, EventHandler<DiscordGuildInfo>> updateEvents = new Dictionary<int, EventHandler<DiscordGuildInfo>>();

        private ServerBrowserManager() { 

            for(int i= 0; i < maxServerButtons; i++) {
                updateEvents[i] = null;
            }

            DiscordRPC.Instance.OnAvailableGuildsChanged += OnGuildListChanged;
            OnGuildListChanged(DiscordRPC.Instance.AvailableGuilds);
        }

        ~ServerBrowserManager() {
            DiscordRPC.Instance.OnAvailableGuildsChanged -= OnGuildListChanged;
        }

        // called automatically when the available Discord Guilds have changed
        private void OnGuildListChanged(List<DiscordGuildInfo> availableGuilds) {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "On Guild List Changed: Updating all buttons in ServerBrowserManager");
            Offset = Offset;
            UpdateAllPositions();
        }

        #region Position

        public void SubscribeToPosition(int position, EventHandler<DiscordGuildInfo> handler, bool instantlyUpdate = false) {
            if(position < 0 || position >= maxServerButtons) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "ServerButton wanted to subsribe to position " + position);
                return;
            }
            updateEvents[position] += handler;

            if(!instantlyUpdate)
                return;

            DiscordGuildInfo guildToUpdate = GetGuildAtPosition(position);
            handler.Invoke(this, guildToUpdate);
        }

        public void UnsubscribeFromPosition(int position, EventHandler<DiscordGuildInfo> handler) {
            if(position < 0 || position >= maxServerButtons) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "ServerButton wanted to unsubsribe from position " + position);
                return;
            }
            updateEvents[position] -= handler;
        }

        private void UpdateAllPositions() {
            for(int position = 0; position < maxServerButtons; position++) {
                DiscordGuildInfo guildToUpdate = GetGuildAtPosition(position);

                if(updateEvents.ContainsKey(position) && updateEvents[position] != null) {
                    updateEvents[position].Invoke(this, guildToUpdate);
                }
            }
        }

        // returns the guild info that should be displayed at "position". Takes offset and guild count into account
        private DiscordGuildInfo GetGuildAtPosition(int position) {
            if(position < 0 || position >= maxServerButtons || DiscordRPC.Instance.AvailableGuilds.Count == 0) {
                return null;
            }
            int guildIndex = (position + Offset) % DiscordRPC.Instance.AvailableGuilds.Count; // Calculate which guild this position corresponds to
            return guildIndex < DiscordRPC.Instance.AvailableGuilds.Count ? DiscordRPC.Instance.AvailableGuilds[guildIndex] : null;
        }

        #endregion
    }
}
