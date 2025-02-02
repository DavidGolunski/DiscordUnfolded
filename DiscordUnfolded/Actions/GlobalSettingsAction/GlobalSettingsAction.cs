using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace DiscordUnfolded {
    [PluginActionId("com.davidgolunski.globalsettingsaction")]
    public class GlobalSettingsAction : KeypadBase {


        public GlobalSettingsAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {

        }

        public override void Dispose() {
        }

        public override void KeyPressed(KeyPayload payload) {
            if(DiscordBot.Instance.IsRunning) {
                DiscordBot.Instance.Stop();
            }
            else {
                DiscordBot.Instance.Start();
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(null));
        }

        #endregion
    }
}