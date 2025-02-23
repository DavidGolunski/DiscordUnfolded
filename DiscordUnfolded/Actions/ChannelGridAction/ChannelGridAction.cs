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
using System.Diagnostics;

namespace DiscordUnfolded {
    [PluginActionId("com.davidgolunski.discordunfolded.channelgridaction")]
    public class ChannelGridAction : KeypadBase {


        private readonly ChannelGridSettings settings;
        private readonly GlobalSettings globalSettings;

        private ChannelGridInfo channelGridInfo = null;

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
            // try starting the Discord RPC connection. This will be run when any action that can use data from DiscordRPC is created
            DiscordRPC.Instance.Start(globalSettings.ClientId, globalSettings.ClientSecret);

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

            // if the button is the current user, mute or unmute the user. Selects the user if the button is not the current user
            UserButtonPressed(channelGridInfo, timeDiff);

            // select the channel if the button is a channel button
            if(channelGridInfo?.ChannelInfo?.ChannelId != null) {
                DiscordRPC.Instance.SelectChannel(channelGridInfo.ChannelInfo.ChannelType, channelGridInfo.ChannelInfo.ChannelId);
            }

            // increase or decrease volume for special cases
            SpecialCaseButtonPressed(channelGridInfo?.SpecialCaseString);


            keyPressedTimestamp = DateTime.MaxValue;
        }

        private void UserButtonPressed(ChannelGridInfo channelGridInfo, double timeDiff) {
            if(channelGridInfo?.UserInfo?.UserId == null || channelGridInfo?.UserInfo?.UserId == 0)
                return;

            //if the button is not the current user, then select/deselect the user
            if(channelGridInfo.UserInfo.UserId != DiscordRPC.Instance.CurrentUserID) {
                ChannelGridManager.Instance.SelectedUserId = ChannelGridManager.Instance.SelectedUserId == channelGridInfo.UserInfo.UserId ? 0 : channelGridInfo.UserInfo.UserId;
                return;
            }

            // if the button is the current user, mute or unmute the user
            VoiceStates currentVoiceState = channelGridInfo.UserInfo.VoiceState;

            if(currentVoiceState == VoiceStates.DEAFENED || currentVoiceState == VoiceStates.MUTED) {
                DiscordRPC.Instance.SetVoiceState(VoiceStates.UNMUTED);
            }
            else if(timeDiff > 1000) {
                DiscordRPC.Instance.SetVoiceState(VoiceStates.DEAFENED);
            }
            else {
                DiscordRPC.Instance.SetVoiceState(VoiceStates.MUTED);
            }
        }

        private void SpecialCaseButtonPressed(string specialCaseString) {
            if(string.IsNullOrEmpty(specialCaseString) || ChannelGridManager.Instance.SelectedUserId == 0 || DiscordRPC.Instance.SelectedGuild == null) return;

            DiscordUser selectedDiscordUser = DiscordRPC.Instance.SelectedGuild.GetUser(ChannelGridManager.Instance.SelectedUserId);
            if(selectedDiscordUser == null) return;

            int oldVolume = selectedDiscordUser.Volume;

            if("PLUS".Equals(specialCaseString)) {
                DiscordRPC.Instance.SetUserVolume(selectedDiscordUser.UserId, oldVolume + 5);
            }
            else {
                DiscordRPC.Instance.SetUserVolume(selectedDiscordUser.UserId, oldVolume - 5);
            }
        }

        /*
         * Update Button Logic
         */
        public void UpdateButton(object sender, ChannelGridInfo newChannelGridInfo) {
            // ignore the update if no relevant information has changed. ChannelGridInfo sent by events can not be null
            if(newChannelGridInfo == null || newChannelGridInfo.Equals(this.channelGridInfo))
                return;

            //Logger.Instance.LogMessage(TracingLevel.DEBUG, "ChannelGridAction: ButtonUpdate requirements fullfilled. XPos: " + settings.XPos + " YPos: " + settings.YPos + " oldChannelGridInfo: " + this.channelGridInfo + " newChannelGridInfo: " + newChannelGridInfo);
            this.channelGridInfo = newChannelGridInfo;

            if(newChannelGridInfo.ChannelInfo != null) {
                UpdateButton(newChannelGridInfo.ChannelInfo, newChannelGridInfo.UsersInChannel);
                return;
            }
            if(newChannelGridInfo.UserInfo != null) {
                UpdateButton(newChannelGridInfo.UserInfo);
                return;
            }
            if(newChannelGridInfo.SpecialCaseString != null) {
                UpdateButton(newChannelGridInfo.SpecialCaseString);
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

                if(usersInChannel.Contains(DiscordRPC.Instance.CurrentUserID)) {
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

            // display the volume if the user is currently selected
            if(userInfo.UserId == ChannelGridManager.Instance.SelectedUserId) {
                bitmap = ImageTools.AddTextToBitmap(bitmap, ((int) userInfo.Volume).ToString(), Color.White, 100);
            }

            Connection.SetImageAsync(bitmap).GetAwaiter().GetResult();
            bitmap.Dispose();
            highlightBitmap?.Dispose();
        }

        private void UpdateButton(string specialCaseString) {
            if(!"PLUS".Equals(specialCaseString) && !"MINUS".Equals(specialCaseString)) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "ChannelGridAction - SpecialCaseString was " +  specialCaseString);
            }

            Bitmap bitmap = null;
            if(specialCaseString == "PLUS") {
                bitmap = ImageTools.GetBitmapFromFilePath("./Actions/ChannelGridAction/Plus@2x.png");
            }
            else {
                bitmap = ImageTools.GetBitmapFromFilePath("./Actions/ChannelGridAction/Minus@2x.png");
            }
            Connection.SetImageAsync(bitmap).GetAwaiter().GetResult();
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "ChannelGridAction - Updated button with Special Case String " + specialCaseString);
            bitmap.Dispose();
        }

       
    }
}
