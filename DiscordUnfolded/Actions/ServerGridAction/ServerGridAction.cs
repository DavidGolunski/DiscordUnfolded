using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {
    [PluginActionId("com.davidgolunski.discordunfolded.servergridaction")]
    public class ServerGridAction : KeypadBase {


        private readonly ServerGridNavigationSettings settings;

        public readonly ulong ActionId;


        public ServerGridAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            ActionId = DiscordDataManager.GetNextActionId();
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.settings = new ServerGridNavigationSettings();
                SaveSettings();
            }
            else {
                this.settings = payload.Settings.ToObject<ServerGridNavigationSettings>();
            }
        }

        public override void Dispose() { }

        public override void KeyPressed(KeyPayload payload) {

        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) {  }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }


        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }
    }
}
