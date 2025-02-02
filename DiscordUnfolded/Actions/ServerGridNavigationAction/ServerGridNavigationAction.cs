using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {
    [PluginActionId("com.davidgolunski.discordunfolded.servergridnavigationaction")]
    public class ServerGridNavigationAction : KeypadBase {


        private readonly ServerGridNavigationSettings settings;


        public ServerGridNavigationAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.settings = new ServerGridNavigationSettings();
                SaveSettings();
            }
            else {
                this.settings = payload.Settings.ToObject<ServerGridNavigationSettings>();
            }

            if(this.settings.Direction == "down") {
                Connection.SetStateAsync(0);
            }
            else {
                 Connection.SetStateAsync(1);
            }
        }

        public override void Dispose() { }

        public override void KeyPressed(KeyPayload payload) {
            if(settings.Direction == "up") {
                DiscordDataManager.Instance.ServerBrowserOffset -= 1;
            }
            else {
                DiscordDataManager.Instance.ServerBrowserOffset += 1;
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) {  }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();

            if(this.settings.Direction == "down") {
                Connection.SetStateAsync(0);
            }
            else {
                Connection.SetStateAsync(1);
            }
        }


        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }
    }
}
