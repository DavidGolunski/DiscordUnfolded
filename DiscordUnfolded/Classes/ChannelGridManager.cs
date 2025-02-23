using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordUnfolded.DiscordStructure;
using Discord;
using DiscordUnfolded.DiscordCommunication;

namespace DiscordUnfolded {

    public class ChannelGridManager {

        private static ChannelGridManager instance;
        public static ChannelGridManager Instance {
            get {
                if (instance == null) {
                    instance = new ChannelGridManager();
                    Logger.Instance.LogMessage(TracingLevel.WARN, "ChannelGridManager Instance had to create new Object");
                }
                //instance ??= new ChannelGridManager();
                return instance;
            }
            private set => instance = value;
        }

        private int width = 8;
        public int Width {
            get => width; 
            set {
                if(width == value ) 
                    return;
                width = value;

                UpdateChannelGrid();
            }
        }
        private const int Height = 4;

        private int xOffset = 0;
        public int XOffset {
            get => xOffset;
            set {
                int newValue = Math.Min(value, Width);
                newValue = Math.Max(newValue, 0);

                if(xOffset == newValue)
                    return;
                xOffset = newValue;
                UpdateChannelGrid();
            }
        }

        private int yOffset = 0;
        public int YOffset {
            get => yOffset;
            set {
                int newValue = Math.Min(value, MaxYOffset);
                newValue = Math.Max(newValue, 0);

                if(yOffset == newValue)
                    return;
                yOffset = newValue;
                UpdateChannelGrid();
            }
        }

        // this is to calculate what the max yOffset should be. We do not want the user to scroll into endlessness, but also do not want to prevent the users to see any data
        private int MaxYOffset {
            get {
                int maxSubscribedButtonYPos = 0;
                foreach((int xPos, int yPos) in updateEvents.Keys) {
                    maxSubscribedButtonYPos = Math.Max(maxSubscribedButtonYPos, yPos);
                }
                return channelGrid.Count - maxSubscribedButtonYPos - 1;
            }
        }

        private readonly Dictionary<(int, int), EventHandler<ChannelGridInfo>> updateEvents = new Dictionary<(int, int), EventHandler<ChannelGridInfo>>();


        // a representation of all information that is available from a guild on a grid
        private readonly List<List<ChannelGridInfo>> channelGrid = new List<List<ChannelGridInfo>>();

        // the selected user id allows the 
        private ulong selectedUserId = 0;
        public ulong SelectedUserId {
            get => selectedUserId;
            set {
                if (value == selectedUserId) return;
                selectedUserId = value;
                UpdateChannelGrid();
            }
        }


        private ChannelGridManager() {

            for(int y = 0; y < Height; y++) {
                for(int x = 0; x < width; x++) {
                    updateEvents[(x, y)] = null;
                }
            }

            DiscordRPC.Instance.OnSelectedGuildChanged += OnSelectedGuildChanged;
            OnSelectedGuildChanged(DiscordRPC.Instance.SelectedGuild);
        }

        ~ChannelGridManager() {
            Logger.Instance.LogMessage(TracingLevel.WARN, "ChannelGridManager Deconstructor called");
            DiscordRPC.Instance.OnSelectedGuildChanged -= OnSelectedGuildChanged;
        }



        /*
         * Channel Buttons can subsribe to a static position on the streamdeck here. They will be updated with the mapped dynamic information (which takes offsets into account)
         */
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

        /*
         * Functions called by subscribed events
         */

        // unsubscribe from previous guild and subscribe to all events in the new guild
        private void OnSelectedGuildChanged(DiscordGuild discordGuild) {

            // unsubscribe from all channels and users in the old guild
            // DiscordRPC automatically unsubscribes all events when the selected guild changes

            SelectedUserId = 0;
            // null handling
            if(discordGuild == null) {
                UpdateChannelGrid();
                return;
            }

            // subscribe to events in new guild
            discordGuild.OnTextChannelInfoChanged += OnTextChannelInfoChanged;
            discordGuild.OnTextChannelChanged += OnTextChannelChanged;

            discordGuild.OnVoiceChannelInfoChanged += OnVoiceChannelInfoChanged;
            discordGuild.OnTextChannelChanged += OnVoiceChannelChanged;

            discordGuild.OnUserInfoChanged += OnUserInfoChanged;
            discordGuild.OnUserChanged += OnUserChanged;

            Logger.Instance.LogMessage(TracingLevel.DEBUG, "ChannelGridManager - on Selected Guild Changed: " + discordGuild);

            UpdateChannelGrid();
        }

        private void OnTextChannelInfoChanged(object sender, DiscordChannelInfo discordChannelInfo) {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "ChannelGridManager: OnTextChannelInfoChanged");
            UpdateChannelGrid();
        }
        private void OnTextChannelChanged(object sender, (bool wasAdded, ulong voiceChannelID) info) {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "ChannelGridManager: OnTextChannelChanged");
            UpdateChannelGrid();
        }

        private void OnVoiceChannelInfoChanged(object sender, DiscordChannelInfo discordChannelInfo) {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "ChannelGridManager: OnVoiceChannelInfoChanged");
            UpdateChannelGrid();
        }
        public void OnVoiceChannelChanged(object sender, (bool wasAdded, ulong voiceChannelID) info) {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "ChannelGridManager: OnVoiceChannelChanged");
            UpdateChannelGrid();
        }

        public void OnUserInfoChanged(object sender, DiscordUserInfo discordUserInfo) {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "ChannelGridManager: OnUserInfoChanged called");
            UpdateChannelGrid(); 
        }
        public void OnUserChanged(object sender, (bool wasAdded, ulong userId) info) {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "ChannelGridManager: OnUserChanged called");
            UpdateChannelGrid();
        }


        /*
         * Updating information and buttons
         */
        public void UpdateChannelGrid() {
            ClearChannelGrid();

            if(DiscordRPC.Instance.SelectedGuild == null) {
                UpdateAllButtons();
                return;
            }

            List<ulong> voiceChannelIDs = DiscordRPC.Instance.SelectedGuild.GetOrderedVoiceChannelIDs();
            foreach(ulong voiceChannelID in voiceChannelIDs) {
                DiscordVoiceChannel voiceChannel = DiscordRPC.Instance.SelectedGuild.GetVoiceChannel(voiceChannelID);

                channelGrid.Add(new List<ChannelGridInfo>());
                List<ulong> userIDsInVoiceChannel = voiceChannel.GetUserIDs();

                // add the voice channel info in first position
                channelGrid.Last().Add(new ChannelGridInfo(voiceChannel.GetInfo(), userIDsInVoiceChannel));


                DiscordUser selectedDiscordUser = null;
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
                    else if(discordUserInVoiceChannel.UserId == SelectedUserId) {
                        selectedDiscordUser = discordUserInVoiceChannel;
                        continue;
                    }

                    channelGrid.Last().Add(new ChannelGridInfo(discordUserInVoiceChannel.GetInfo()));
                }

                // create a new row in which the volume of the selected user can be modified
                if(selectedDiscordUser == null)
                    continue;

                // if the last user that should have been added was the selected user and a new row was created for that user, then we need to remove that row again
                if(channelGrid.Last().Count == 1 && new ChannelGridInfo().Equals(channelGrid.Last()[0]))
                    channelGrid.RemoveAt(channelGrid.Count - 1);

                // if there is not enough space to show the plus and minus buttons, then create a new row
                if(Width - channelGrid.Last().Count < 3) {
                    channelGrid.Add(new List<ChannelGridInfo>());
                    // add an empty space if the width allows for it
                    if(Width > 3)
                        channelGrid.Last().Add(new ChannelGridInfo());
                }

                channelGrid.Last().Add(new ChannelGridInfo("MINUS"));
                channelGrid.Last().Add(new ChannelGridInfo(selectedDiscordUser.GetInfo()));
                channelGrid.Last().Add(new ChannelGridInfo("PLUS"));

            }

            List<ulong> textChannelIDs = DiscordRPC.Instance.SelectedGuild.GetOrderedTextChannelIDs();
            for(int i = 0; i < textChannelIDs.Count; i++) {
                if(i % Width == 0) {
                    channelGrid.Add(new List<ChannelGridInfo>());
                }
                DiscordTextChannel textChannel = DiscordRPC.Instance.SelectedGuild.GetTextChannel(textChannelIDs[i]);
                channelGrid.Last().Add(new ChannelGridInfo(textChannel.GetInfo()));
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

        // sends out an update event to all buttons
        private void UpdateAllButtons() {
            foreach((int xPos, int yPos) in updateEvents.Keys) {
                UpdateButton(xPos, yPos);
            }
        }

        // sends out an update event to buttons at the specified position
        private void UpdateButton(int xPos, int yPos) {
            //Logger.Instance.LogMessage(TracingLevel.DEBUG, "ChannelGridManager.UpdatingButton: (" + xPos + ", " + yPos + ") " + GetChannelInfoForPosition(xPos, yPos) );
            updateEvents[(xPos, yPos)]?.Invoke(this, GetChannelInfoForPosition(xPos, yPos)); 
        }


        // returns what information is currently saved for a specific static x and y position
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

    }
}
