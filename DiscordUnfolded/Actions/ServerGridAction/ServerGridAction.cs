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
    [PluginActionId("com.davidgolunski.discordunfolded.servergridaction")]
    public class ServerGridAction : KeypadBase {


        private readonly ServerGridSettings settings;

        private DiscordGuildInfo currentGuildInfo = null;


        public ServerGridAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.settings = new ServerGridSettings();
                SaveSettings();
            }
            else {
                this.settings = payload.Settings.ToObject<ServerGridSettings>();
            }

            ServerBrowserManager.Instance.SubscribeToPosition(settings.Position, UpdateButton, true);
        }

        public override void Dispose() {
            ServerBrowserManager.Instance.UnsubscribeFromPosition(settings.Position, UpdateButton);
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
                ServerBrowserManager.Instance.UnsubscribeFromPosition(oldPosition, UpdateButton);
                ServerBrowserManager.Instance.SubscribeToPosition(settings.Position, UpdateButton, true);
            }

            SaveSettings();
        }


        public void UpdateButton(object sender, DiscordGuildInfo discordGuildInfo) {
            // if the info has not changed, then we do not need to update
            if(currentGuildInfo == null && discordGuildInfo == null)
                return;
            if(currentGuildInfo != null && currentGuildInfo.Equals(discordGuildInfo))
                return;


            currentGuildInfo = discordGuildInfo;

            if(discordGuildInfo == null) {
                Connection.SetDefaultImageAsync().GetAwaiter().GetResult();
                return;
            }

            string imageUrl = discordGuildInfo.IconUrl;
            Bitmap bitmap = ImageTools.GetResizedBitmapFromUrl(imageUrl);
            if(bitmap == null) {
                bitmap = ImageTools.GetBitmapFromFilePath("./Images/RoundRectangle@2x.png");
                string title = ImageTools.SplitString(discordGuildInfo.GuildName, 7);
                bitmap = ImageTools.AddTextToBitmap(bitmap, title);
            }

            Connection.SetImageAsync(bitmap).GetAwaiter().GetResult();
            bitmap.Dispose();
        }


        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }
    }
}
