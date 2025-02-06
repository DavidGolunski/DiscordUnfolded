using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {

    public class ChannelGridManager {

        private static ChannelGridManager instance;
        public static ChannelGridManager Instance {
            get => instance ??= new ChannelGridManager();
            private set => instance = value;
        }

        private int width = 8;
        public int Width {
            get => width; 
            set {
                if(width == value ) 
                    return;
                width = value;
            }
        }
        private const int Height = 4;

        private int xOffset = 0;
        public int XOffset {
            get => xOffset;
            set {
                if(xOffset == value)
                    return;

                int newValue = Math.Min(value, Width);
                newValue = Math.Max(newValue, 0);
                xOffset = newValue;
                UpdateAllButtons();
            }
        }
        private int yOffset = 0;
        public int YOffset {
            get => yOffset;
            set {
                if(yOffset == value)
                    return;

                int newValue = Math.Min(value, Height);
                newValue = Math.Max(newValue, 0);
                yOffset = newValue;
                UpdateAllButtons();
            }
        }

        private readonly Dictionary<(int, int), EventHandler<ChannelGridInfo>> updateEvents = new Dictionary<(int, int), EventHandler<ChannelGridInfo>>();

        private DiscordGuildInfo guildInfo = null;
        private readonly Dictionary<ulong, DiscordChannelInfo> voiceChannels = new Dictionary<ulong, DiscordChannelInfo>();
        private readonly Dictionary<ulong, DiscordChannelInfo> textChannels = new Dictionary<ulong, DiscordChannelInfo>();
        private readonly Dictionary<(ulong, ulong), DiscordUserInfo> userInfos = new Dictionary<(ulong, ulong), DiscordUserInfo>();



        // a representation of all information that is available from a guild on a grid
        private readonly List<List<ChannelGridInfo>> channelGrid = new List<List<ChannelGridInfo>>();





        private ChannelGridManager() {

            for(int y = 0; y < Height; y++) {
                for(int x = 0; x < width; x++) {
                    updateEvents[(x, y)] = null;
                }
            }

            ServerBrowserManager.Instance.SubscribeToSelectedGuild(OnSelectedGuildChanged, true);
        }

        ~ChannelGridManager() {
            ServerBrowserManager.Instance.UnsubscribeFromSelectedGuild(OnSelectedGuildChanged);
        }

        private void OnSelectedGuildChanged(object sender, DiscordGuildInfo discordGuildInfo) {
            if(discordGuildInfo == this.guildInfo)
                return;


            guildInfo = discordGuildInfo;

            voiceChannels.Clear();
            textChannels.Clear();
            userInfos.Clear();

            foreach(var updateEvent in updateEvents.Values) {
                updateEvent?.Invoke(this, new ChannelGridInfo());
            }

            if(guildInfo == null) {
                return;
            }

            DiscordGuild discordGuild = DiscordGuild.GetGuild(guildInfo.GuildId);

            List<ulong> voiceChannelIDs = discordGuild.GetOrderedVoiceChannelIDs();
            foreach(ulong voiceChannelID in voiceChannelIDs) {
                DiscordVoiceChannel voiceChannel = discordGuild.GetVoiceChannel(voiceChannelID);

            }


        }

        public void UpdateChannelGrid() {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "ChannelGridManager called UpdateChannelGrid()");
            ClearChannelGrid();

            if(guildInfo == null) {
                UpdateAllButtons();
                return;
            }

            DiscordGuild discordGuild = DiscordGuild.GetGuild(guildInfo.GuildId);
            if(discordGuild == null) {
                UpdateAllButtons();
                return;
            }

            List<ulong> voiceChannelIDs = discordGuild.GetOrderedVoiceChannelIDs();
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "ChannelGridManager voiceChannelIds " + voiceChannelIDs.Count);
            foreach(ulong voiceChannelID in voiceChannelIDs) {
                DiscordVoiceChannel voiceChannel = discordGuild.GetVoiceChannel(voiceChannelID);

                channelGrid.Add(new List<ChannelGridInfo>());
                if(voiceChannel == null)
                    continue;

                List<ulong> userIDsInVoiceChannel = voiceChannel.GetUserIDs();

                // add the voice channel info in first position
                channelGrid.Last().Add(new ChannelGridInfo(voiceChannel.GetInfo(), userIDsInVoiceChannel));

                
                for(int i = 0; i < userIDsInVoiceChannel.Count; i++) {
                    // if the number of users exceeds the width, then add a new row and add an empty button to the first position
                    if(i > 0 && i % Width == 0) {
                        channelGrid.Add(new List<ChannelGridInfo>());
                        channelGrid.Last().Add(new ChannelGridInfo());
                        continue;
                    }

                    DiscordUser discordUserInVoiceChannel = voiceChannel.GetUser(userIDsInVoiceChannel[i]);
                    if(discordUserInVoiceChannel == null) {
                        channelGrid.Last().Add(new ChannelGridInfo());
                        continue;
                    }

                    channelGrid.Last().Add(new ChannelGridInfo(discordUserInVoiceChannel.GetInfo()));
                }
            }

            UpdateAllButtons();
        }

        // clears the Channel Grid. DOES NOT UPDATE THE BUTTONS AUTOMATICALLY
        private void ClearChannelGrid() {
            foreach(List<ChannelGridInfo> channelGridRow in channelGrid) {
                channelGridRow.Clear();
            }
            channelGrid.Clear();
        }

        private void UpdateAllButtons() {
            for(int y = 0; y < Height; y++) {
                for(int x = 0; x < Width; x++) {
                    UpdateButton(x, y);
                }
            }
        }

        private void UpdateButton(int xPos, int yPos) {
            updateEvents[(xPos, yPos)]?.Invoke(this, GetChannelInfoForPosition(xPos, yPos)); 
        }

        private ChannelGridInfo GetChannelInfoForPosition(int xPos, int yPos) {
            int realXPos = xPos + XOffset;
            int realYPos = yPos + YOffset;

            if(realXPos < 0 || realXPos >= Width || realYPos < 0 || realYPos >= channelGrid.Count)
                return new ChannelGridInfo();

            List<ChannelGridInfo> channelGridRow = channelGrid[realYPos];
            if(channelGridRow == null || realXPos >= channelGridRow.Count)
                return new ChannelGridInfo();


            return channelGridRow[realXPos];
        }

        public void SubscribeToPosition(int xPos, int yPos, EventHandler<ChannelGridInfo> handler, bool instantlyUpdate = false) {
            if(xPos < 0 || xPos >= width || yPos < 0 || yPos >= Height) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "ChannelButton wanted to subsribe to position (" + xPos + "," + yPos + ")");
                return;
            }
            updateEvents[(xPos, yPos)] += handler;

            if(!instantlyUpdate)
                return;

            handler.Invoke(this, GetChannelInfoForPosition(xPos, yPos));
        }

        public void UnsubscribeFromPosition(int xPos, int yPos, EventHandler<ChannelGridInfo> handler) {
            if(!updateEvents.ContainsKey((xPos, yPos))) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "ChannelButton wanted to unsubsribe from position (" + xPos + "," + yPos + ")");
                return;
            }
            updateEvents[(xPos, yPos)] -= handler;
        }



    }
}
