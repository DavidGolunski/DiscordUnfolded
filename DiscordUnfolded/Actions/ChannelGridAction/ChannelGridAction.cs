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

namespace DiscordUnfolded {
    [PluginActionId("com.davidgolunski.discordunfolded.channelgridaction")]
    public class ChannelGridAction : KeypadBase {


        private readonly ChannelGridSettings settings;
        private readonly GlobalSettings globalSettings;

        private ChannelGridInfo channelGridInfo = null;
        private ulong lastUsedUserID = 0; // this contains the last UserID with which the button was updated


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

        public override void KeyPressed(KeyPayload payload) {
            
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) {
            Tools.AutoPopulateSettings(globalSettings, payload.Settings);


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
         * Update Button Logic
         */
        public void UpdateButton(object sender, ChannelGridInfo newChannelGridInfo) {

            if(newChannelGridInfo == null || (newChannelGridInfo.Equals(this.channelGridInfo) && this.lastUsedUserID == globalSettings.UserID))
                return;

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

            Bitmap bitmap = ImageTools.GetResizedBitmapFromUrl(userInfo.IconUrl);

            if(bitmap == null) {
                bitmap = ImageTools.GetBitmapFromFilePath("./Images/RoundRectangle@2x.png");
                string title = ImageTools.SplitString(userInfo.UserName, 7);
                bitmap = ImageTools.AddTextToBitmap(bitmap, title, Color.White);
            }
            
            if(userInfo.VoiceState == VoiceStates.DEAFENED) {
                Bitmap highlightBitmap = ImageTools.GetBitmapFromFilePath("./Images/BigRedHighlight@2x.png");
                bitmap = ImageTools.MergeBitmaps(bitmap, highlightBitmap);
                highlightBitmap.Dispose();
            }
            else if(userInfo.VoiceState == VoiceStates.MUTED) {
                Bitmap highlightBitmap = ImageTools.GetBitmapFromFilePath("./Images/BigYellowHighlight@2x.png");
                bitmap = ImageTools.MergeBitmaps(bitmap, highlightBitmap);
                highlightBitmap.Dispose();
            }


            Connection.SetImageAsync(bitmap).GetAwaiter().GetResult();
            bitmap.Dispose();
            return;
        }


       
    }
}
