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

namespace DiscordUnfolded {
    [PluginActionId("com.davidgolunski.discordunfolded.channelgridaction")]
    public class ChannelGridAction : KeypadBase {


        private readonly ChannelGridSettings settings;
        private readonly GlobalSettings globalSettings;

        private ChannelGridInfo channelGridInfo = null;


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


        public void UpdateButton(object sender, ChannelGridInfo newChannelGridInfo) {
            if(newChannelGridInfo.Equals(this.channelGridInfo))
                return;

            this.channelGridInfo = newChannelGridInfo;

            if(newChannelGridInfo.ChannelInfo != null) {
                UpdateButton(newChannelGridInfo.ChannelInfo);
                return;
            }
            if(newChannelGridInfo.UserInfo != null) {
                UpdateButton(newChannelGridInfo.UserInfo);
                return;
            }
            Connection.SetDefaultImageAsync().GetAwaiter().GetResult();
        }

        private void UpdateButton(DiscordChannelInfo channelInfo) {
            if(channelInfo == null) {
                Connection.SetDefaultImageAsync().GetAwaiter().GetResult();
                return;
            }

            Bitmap bitmap = ImageTools.GetBitmapFromFilePath("./Images/RoundRectangle@2x.png");
            string title = ImageTools.SplitString(channelInfo.ChannelName, 7);
            bitmap = ImageTools.AddTextToBitmap(bitmap, title);

            Connection.SetImageAsync(bitmap).GetAwaiter().GetResult();
            bitmap.Dispose();
            return;
        }

        private void UpdateButton(DiscordUserInfo userInfo) {
            if(userInfo == null) {
                Connection.SetDefaultImageAsync().GetAwaiter().GetResult();
                return;
            }

            Bitmap bitmap = ImageTools.GetBitmapFromFilePath("./Images/RoundRectangle@2x.png");
            string title = ImageTools.SplitString(userInfo.UserName, 7);
            bitmap = ImageTools.AddTextToBitmap(bitmap, title);

            Connection.SetImageAsync(bitmap).GetAwaiter().GetResult();
            bitmap.Dispose();
            return;
        }


        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }
    }
}
