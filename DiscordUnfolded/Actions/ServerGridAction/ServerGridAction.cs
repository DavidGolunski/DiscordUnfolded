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

namespace DiscordUnfolded {
    [PluginActionId("com.davidgolunski.discordunfolded.servergridaction")]
    public class ServerGridAction : KeypadBase {


        private readonly ServerGridSettings settings;

        private DiscordGuildInfo currentGuildInfo = null;
        private DiscordGuildInfo selectedGuildInfo = null;


        public ServerGridAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.settings = new ServerGridSettings();
                SaveSettings();
            }
            else {
                this.settings = payload.Settings.ToObject<ServerGridSettings>();
            }

            ServerBrowserManager.Instance.SubscribeToPosition(settings.Position, UpdateCurrentGuild, true);
            ServerBrowserManager.Instance.SubscribeToSelectedGuild(UpdateSelectedGuild, true);
        }

        public override void Dispose() {
            ServerBrowserManager.Instance.UnsubscribeFromPosition(settings.Position, UpdateCurrentGuild);
            ServerBrowserManager.Instance.UnsubscribeFromSelectedGuild(UpdateSelectedGuild);
        }

        public override void KeyPressed(KeyPayload payload) {
            ServerBrowserManager.Instance.SelectedGuild = currentGuildInfo;
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) {  }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            int oldPosition = settings.Position;
            Tools.AutoPopulateSettings(settings, payload.Settings);

            if(oldPosition != settings.Position) {
                ServerBrowserManager.Instance.UnsubscribeFromPosition(oldPosition, UpdateCurrentGuild);
                ServerBrowserManager.Instance.SubscribeToPosition(settings.Position, UpdateCurrentGuild, true);
            }

            SaveSettings();
        }


        private void UpdateSelectedGuild(object sender, DiscordGuildInfo selectedGuildInfo) {
            if(this.selectedGuildInfo == null && selectedGuildInfo == null)
                return;

            if(this.selectedGuildInfo == null && selectedGuildInfo != null) {
                this.selectedGuildInfo = selectedGuildInfo;
                UpdateButton(this.currentGuildInfo, selectedGuildInfo);
                return;
            }

            if(this.selectedGuildInfo.Equals(selectedGuildInfo))
                return;

            this.selectedGuildInfo = selectedGuildInfo;
            UpdateButton(this.currentGuildInfo, selectedGuildInfo);
        }

        private void UpdateCurrentGuild(object sender, DiscordGuildInfo currentGuildInfo) {
            if(this.currentGuildInfo == null && currentGuildInfo == null)
                return;

            if(this.currentGuildInfo == null && currentGuildInfo != null) {
                this.currentGuildInfo = currentGuildInfo;
                UpdateButton(currentGuildInfo, selectedGuildInfo);
                return;
            }

            if(this.currentGuildInfo.Equals(currentGuildInfo))
                return;

            this.currentGuildInfo = currentGuildInfo;
            UpdateButton(currentGuildInfo, selectedGuildInfo);
        }



        private void UpdateButton(DiscordGuildInfo currentGuildInfo, DiscordGuildInfo selectedGuildInfo) {
            if(currentGuildInfo == null) {
                Connection.SetDefaultImageAsync().GetAwaiter().GetResult();
                return;
            }

            string imageUrl = currentGuildInfo.IconUrl;
            Bitmap bitmap = ImageTools.GetResizedBitmapFromUrl(imageUrl);
            if(bitmap == null) {
                bitmap = ImageTools.GetBitmapFromFilePath("./Images/RoundRectangle@2x.png");
                string title = ImageTools.SplitString(currentGuildInfo.GuildName, 7);
                bitmap = ImageTools.AddTextToBitmap(bitmap, title, Color.White);
            }

            if(selectedGuildInfo != null && currentGuildInfo.Equals(selectedGuildInfo)) {
                Bitmap highlightColorBitmap = ImageTools.GetResizedBitmapFromUrl("./Images/BigGreenHighlight@2x.png");
                bitmap = ImageTools.MergeBitmaps(bitmap, highlightColorBitmap);
            }

            Connection.SetImageAsync(bitmap).GetAwaiter().GetResult();
            bitmap.Dispose();

        }

        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }
    }
}
