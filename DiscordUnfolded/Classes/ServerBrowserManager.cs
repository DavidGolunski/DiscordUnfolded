using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

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
                int newOffset = value;
                if(newOffset < 0) {
                    newOffset *= -1;
                    newOffset %= maxServerButtons;
                    newOffset = (maxServerButtons - newOffset) % maxServerButtons;
                }
                else {
                    newOffset %= maxServerButtons;
                }
                offset = newOffset;
                UpdateAllPositions();
            } 
        }

        private readonly List<DiscordGuildInfo> guildList = new List<DiscordGuildInfo>();
        private readonly Dictionary<int, EventHandler<DiscordGuildInfo>> updateEvents = new Dictionary<int, EventHandler<DiscordGuildInfo>>();



        private DiscordGuildInfo selectedGuild = null;
        public DiscordGuildInfo SelectedGuild {
            get => selectedGuild;
            set {
                if(value == selectedGuild)
                    return;
                selectedGuild = value;
                OnSelectedGuildChanged();
            }
        }
        private event EventHandler<DiscordGuildInfo> selectedGuildEvents;


        private ServerBrowserManager() { 

            for(int i= 0; i < maxServerButtons; i++) {
                updateEvents[i] = null;
            }
        }

        public void UpdateGuildList(List<DiscordGuildInfo> newGuildList) {
            guildList.Clear();
            guildList.AddRange(newGuildList);
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
            for(int i = 0; i < maxServerButtons; i++) {
                UpdatePosition(i);
            }
        }

        private void UpdatePosition(int position) {
            DiscordGuildInfo guildToUpdate = GetGuildAtPosition(position);

            if(updateEvents.ContainsKey(position) && updateEvents[position] != null) {
                updateEvents[position].Invoke(this, guildToUpdate);
            }
        }

        private DiscordGuildInfo GetGuildAtPosition(int position) {
            if(position < 0 || position >= maxServerButtons || guildList.Count == 0) {
                return null;
            }
            int guildIndex = (position + Offset) % guildList.Count; // Calculate which guild this position corresponds to
            return guildIndex < guildList.Count ? guildList[guildIndex] : null;

        }

        #endregion

        #region Selected Guild

        public void SubscribeToSelectedGuild(EventHandler<DiscordGuildInfo> handler) {
            selectedGuildEvents += handler;
        }

        public void UnsubscribeFromSelectedGuild(int position, EventHandler<DiscordGuildInfo> handler) {
            selectedGuildEvents -= handler;
        }

        private void OnSelectedGuildChanged() {
            selectedGuildEvents?.Invoke(this, SelectedGuild);
        }

        #endregion
    }
}
