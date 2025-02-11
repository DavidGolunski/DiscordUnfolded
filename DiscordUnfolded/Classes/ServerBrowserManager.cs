using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using DiscordUnfolded.DiscordStructure;

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

        private readonly List<DiscordGuild> guildList = new List<DiscordGuild>();
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
        private event EventHandler<DiscordGuildInfo> SelectedGuildEvents;


        private ServerBrowserManager() { 

            for(int i= 0; i < maxServerButtons; i++) {
                updateEvents[i] = null;
            }

            DiscordGuild.OnGuildListChanged += OnGuildListChanged;
            OnGuildListChanged(null, DiscordGuild.Guilds);
        }

        ~ServerBrowserManager() {
            DiscordGuild.OnGuildListChanged -= OnGuildListChanged;
        }

        // called automatically when the available Discord Guilds have changed
        private void OnGuildListChanged(object sender, List<DiscordGuild> newGuildList) {

            foreach (var guild in guildList) {
                guild.OnGuildInfoChanged -= OnGuildInfoChanged;
            }

            guildList.Clear();
            guildList.AddRange(newGuildList);

            // if the info or the name has changed the buttons will instantly be updated
            foreach(var guild in newGuildList) {
                guild.OnGuildInfoChanged += OnGuildInfoChanged;
            }

            UpdateAllPositions();
        }

        // if the info or the name has changed the buttons will instantly be updated
        private void OnGuildInfoChanged(object sender, DiscordGuildInfo info) {
            foreach(var guild in guildList) {
                if(guild.GuildId != info.GuildId)
                    continue;

                guild.GuildName = info.GuildName;
                guild.IconUrl = info.IconUrl;
            }

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

            DiscordGuild guildToUpdate = GetGuildAtPosition(position);
            handler.Invoke(this, guildToUpdate.GetInfo());
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
                DiscordGuild guildToUpdate = GetGuildAtPosition(position);

                if(updateEvents.ContainsKey(position) && updateEvents[position] != null) {
                    updateEvents[position].Invoke(this, guildToUpdate.GetInfo());
                }
            }
        }

        // returns the guild that should be displayed at "position". Takes offset and guild count into account
        private DiscordGuild GetGuildAtPosition(int position) {
            if(position < 0 || position >= maxServerButtons || guildList.Count == 0) {
                return null;
            }
            int guildIndex = (position + Offset) % guildList.Count; // Calculate which guild this position corresponds to
            return guildIndex < guildList.Count ? guildList[guildIndex] : null;

        }

        #endregion

        #region Selected Guild

        public void SubscribeToSelectedGuild(EventHandler<DiscordGuildInfo> handler, bool instantlyUpdate = false) {
            SelectedGuildEvents += handler;

            if(!instantlyUpdate)
                return;

            handler.Invoke(this, selectedGuild);
        }

        public void UnsubscribeFromSelectedGuild(EventHandler<DiscordGuildInfo> handler) {
            SelectedGuildEvents -= handler;
        }

        private void OnSelectedGuildChanged() {
            SelectedGuildEvents?.Invoke(this, SelectedGuild);
        }

        #endregion
    }
}
