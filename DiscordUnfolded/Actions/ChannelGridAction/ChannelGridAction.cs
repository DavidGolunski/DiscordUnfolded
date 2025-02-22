using BarRaider.SdTools;
using DiscordUnfolded.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordUnfolded.DiscordStructure;
using System.Drawing.Imaging;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using DiscordUnfolded.DiscordCommunication;

namespace DiscordUnfolded {
    [PluginActionId("com.davidgolunski.discordunfolded.channelgridaction")]
    public class ChannelGridAction : KeypadBase {


        private readonly ChannelGridSettings settings;
        private readonly GlobalSettings globalSettings;

        private ChannelGridInfo channelGridInfo = null;
        private ulong lastUsedUserID = 0; // this contains the last UserID with which the button was updated

        private DateTime keyPressedTimestamp = DateTime.MaxValue;


        public ChannelGridAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.settings = new ChannelGridSettings();
                SaveSettings();
            }
            else {
                this.settings = payload.Settings.ToObject<ChannelGridSettings>();
            }

            this.globalSettings = new GlobalSettings();
            GlobalSettingsManager.Instance.RequestGlobalSettings();

            ChannelGridManager.Instance.SubscribeToPosition(settings.XPos, settings.YPos, UpdateButton, true);
        }

        public override void Dispose() {
            ChannelGridManager.Instance.UnsubscribeFromPosition(settings.XPos, settings.YPos, UpdateButton);
        }

        public override void OnTick() { }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) {
            Tools.AutoPopulateSettings(globalSettings, payload.Settings);

            int oldXPos = settings.XPos;
            settings.UpdateXPosDropdown(globalSettings.MaxChannelWidth);
            

            if(oldXPos != settings.XPos) {
                ChannelGridManager.Instance.UnsubscribeFromPosition(oldXPos, settings.YPos, UpdateButton);
                ChannelGridManager.Instance.SubscribeToPosition(settings.XPos, settings.YPos, UpdateButton, true);
            }
            ChannelGridManager.Instance.Width = globalSettings.MaxChannelWidth;
            SaveSettings();

            UpdateButton(null, this.channelGridInfo);  
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            int oldXPos = settings.XPos;
            int oldYPos = settings.YPos;
            Tools.AutoPopulateSettings(settings, payload.Settings);

            if(oldXPos != settings.XPos || oldYPos != settings.YPos) {
                ChannelGridManager.Instance.UnsubscribeFromPosition(oldXPos, oldYPos, UpdateButton);
                ChannelGridManager.Instance.SubscribeToPosition(settings.XPos, settings.YPos, UpdateButton, true);
            }

            SaveSettings();
        }

        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        /*
         * Key Pressed Actions
         */
        public override void KeyPressed(KeyPayload payload) {
            keyPressedTimestamp = DateTime.Now;
        }

        public override void KeyReleased(KeyPayload payload) {
            double timeDiff = (DateTime.Now - keyPressedTimestamp).TotalMilliseconds;
            // timeDiff can only be lower than 0 if the "key" was never pressed in the first place or the "OnTick" method already has triggered the action automatically
            if(timeDiff < 0) {
                keyPressedTimestamp = DateTime.MaxValue;
                return;
            }

            // if the button is the current user, mute or unmute the user. Does nothing if the button is not the current user
            UserButtonPressed(channelGridInfo, timeDiff);

            if(channelGridInfo?.ChannelInfo?.ChannelId != null) {
                DiscordRPC.Instance.SelectChannel(channelGridInfo.ChannelInfo.ChannelType, channelGridInfo.ChannelInfo.ChannelId);
            }


            keyPressedTimestamp = DateTime.MaxValue;
        }

        private void UserButtonPressed(ChannelGridInfo channelGridInfo, double timeDiff) {
            // if the button is the current user, mute or unmute the user
            if(channelGridInfo?.UserInfo?.UserId != globalSettings.UserID)
                return;
            
            // deafen if the button was hold down for longer than a second of the user is currently deafened
            if(timeDiff > 1000 || channelGridInfo.UserInfo.VoiceState == VoiceStates.DEAFENED) {
                KeyBindAction.KeyBindActions["DEAFEN"].Execute();
            }
            // mute if the button was hold down less than a second
            else {
                KeyBindAction.KeyBindActions["MUTE"].Execute();
            } 
        }

        /*
         * Update Button Logic
         */
        public void UpdateButton(object sender, ChannelGridInfo newChannelGridInfo) {
            

            // ignore the update if no relevant information has changed. ChannelGridInfo sent by events can not be null
            if(newChannelGridInfo == null || (newChannelGridInfo.Equals(this.channelGridInfo) && this.lastUsedUserID == globalSettings.UserID))
                return;

            //Logger.Instance.LogMessage(TracingLevel.DEBUG, "ChannelGridAction: ButtonUpdate requirements fullfilled. XPos: " + settings.XPos + " YPos: " + settings.YPos + " oldChannelGridInfo: " + this.channelGridInfo + " newChannelGridInfo: " + newChannelGridInfo);
            this.channelGridInfo = newChannelGridInfo;
            this.lastUsedUserID = globalSettings.UserID;

            if(newChannelGridInfo.ChannelInfo != null) {
                UpdateButton(newChannelGridInfo.ChannelInfo, newChannelGridInfo.UsersInChannel);
                return;
            }
            if(newChannelGridInfo.UserInfo != null) {
                UpdateButton(newChannelGridInfo.UserInfo);
                return;
            }

            Bitmap whiteCornerBitmap = ImageTools.GetBitmapFromFilePath("./Images/WhiteCorners@2x.png");
            Connection.SetImageAsync(whiteCornerBitmap).GetAwaiter().GetResult();
            whiteCornerBitmap.Dispose();
        }

        // update button with channel information
        private void UpdateButton(DiscordChannelInfo channelInfo, List<ulong> usersInChannel) {
            if(channelInfo == null) {
                Bitmap whiteCornerBitmap = ImageTools.GetBitmapFromFilePath("./Images/WhiteCorners@2x.png");
                Connection.SetImageAsync(whiteCornerBitmap).GetAwaiter().GetResult();
                whiteCornerBitmap.Dispose();
                return;
            }

            string bitmapPath;
            Color textColor = Color.White;
            if(channelInfo.ChannelType == ChannelTypes.TEXT) {
                bitmapPath = "./Images/RoundRectangle@2x.png";
            }
            else if(channelInfo.ChannelType == ChannelTypes.VOICE) {

                if(usersInChannel.Contains(globalSettings.UserID)) {
                    bitmapPath = "./Images/RedRectangle@2x.png";
                }
                else {
                    bitmapPath = "./Images/GreenRectangle@2x.png";
                    textColor = Color.Black;
                }
            }
            else {
                bitmapPath = "./Images/RoundRectangle@2x.png";
            }

            Bitmap bitmap = ImageTools.GetBitmapFromFilePath(bitmapPath);
            string title = ImageTools.SplitString(channelInfo.ChannelName, 7);
            bitmap = ImageTools.AddTextToBitmap(bitmap, title, textColor);

            Connection.SetImageAsync(bitmap).GetAwaiter().GetResult();
            bitmap.Dispose();
            return;
        }

        // update button with user information
        private void UpdateButton(DiscordUserInfo userInfo) {
            if(userInfo == null) {
                Bitmap whiteCornerBitmap = ImageTools.GetBitmapFromFilePath("./Images/WhiteCorners@2x.png");
                Connection.SetImageAsync(whiteCornerBitmap).GetAwaiter().GetResult();
                whiteCornerBitmap.Dispose();
                return;
            }

            Bitmap highlightBitmap = null;
            Bitmap bitmap = ImageTools.GetResizedBitmapFromUrl(userInfo.IconUrl);

            if(bitmap == null) {
                bitmap = ImageTools.GetBitmapFromFilePath("./Images/RoundRectangle@2x.png");
                string title = ImageTools.SplitString(userInfo.UserName, 7);
                bitmap = ImageTools.AddTextToBitmap(bitmap, title, Color.White);
            }
            
            if(userInfo.IsSpeaking) {
                highlightBitmap = ImageTools.GetBitmapFromFilePath("./Images/BigGreenHighlight@2x.png");
                bitmap = ImageTools.MergeBitmaps(bitmap, highlightBitmap);
            }
            else if(userInfo.VoiceState == VoiceStates.DEAFENED) {
                highlightBitmap = ImageTools.GetBitmapFromFilePath("./Images/BigRedHighlight@2x.png");
                bitmap = ImageTools.MergeBitmaps(bitmap, highlightBitmap);
            }
            else if(userInfo.VoiceState == VoiceStates.MUTED) {
                highlightBitmap = ImageTools.GetBitmapFromFilePath("./Images/BigYellowHighlight@2x.png");
                bitmap = ImageTools.MergeBitmaps(bitmap, highlightBitmap);
            }


            Connection.SetImageAsync(bitmap).GetAwaiter().GetResult();
            bitmap.Dispose();
            highlightBitmap?.Dispose();
            return;
        }

       
    }
}
