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
    [PluginActionId("com.davidgolunski.discordunfolded.channelgridnavigationaction")]
    public class ChannelGridNavigationAction : KeypadBase {


        private readonly ChannelGridNavigationSettings settings;


        public ChannelGridNavigationAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.settings = new ChannelGridNavigationSettings();
                SaveSettings();
            }
            else {
                this.settings = payload.Settings.ToObject<ChannelGridNavigationSettings>();
            }

            switch(this.settings.Direction) {
                case "left":
                    Connection.SetStateAsync(2);
                    break;
                case "right":
                    Connection.SetStateAsync(3);
                    break;
                case "up":
                    Connection.SetStateAsync(1);
                    break;
                case "down":
                default:
                    Connection.SetStateAsync(0);
                    break;
            }
        }

        public override void Dispose() { }

        public override void KeyPressed(KeyPayload payload) {
            switch(this.settings.Direction) {
                case "left":
                    ChannelGridManager.Instance.XOffset -= 1;
                    break;
                case "right":
                    ChannelGridManager.Instance.XOffset += 1;
                    break;
                case "up":
                    ChannelGridManager.Instance.YOffset -= 1;
                    break;
                case "down":
                default:
                    ChannelGridManager.Instance.YOffset += 1;
                    break;
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) {  }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();

            switch(this.settings.Direction) {
                case "left":
                    Connection.SetStateAsync(2);
                    break;
                case "right":
                    Connection.SetStateAsync(3);
                    break;
                case "up":
                    Connection.SetStateAsync(1);
                    break;
                case "down":
                default:
                    Connection.SetStateAsync(0);
                    break;
            }
        }


        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }
    }
}
