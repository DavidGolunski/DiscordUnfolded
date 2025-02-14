using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {

    [PluginActionId("com.davidgolunski.discordunfolded.navigationaction")]
    public class NavigationAction : KeypadBase {


        private readonly NavigationSettings settings;


        public NavigationAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.settings = new NavigationSettings();
                SaveSettings();
            }
            else {
                this.settings = payload.Settings.ToObject<NavigationSettings>();
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
            // for server navigation
            if(settings.NavigationType == "server") {
                if(settings.Direction == "up") 
                    ServerBrowserManager.Instance.Offset -= 1;
                else 
                    ServerBrowserManager.Instance.Offset += 1;
            }
            // for channel grid navigation
            else {
                if(settings.Direction == "up") 
                    ChannelGridManager.Instance.YOffset -= 1;
                else 
                    ChannelGridManager.Instance.YOffset += 1;
            }
           
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

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
