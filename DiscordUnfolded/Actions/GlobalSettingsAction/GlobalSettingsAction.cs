using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace DiscordUnfolded {
    [PluginActionId("com.davidgolunski.discordunfolded.globalsettingsaction")]
    public class GlobalSettingsAction : KeypadBase {

        private readonly GlobalSettings settings;

        public GlobalSettingsAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            this.settings = new GlobalSettings();
            GlobalSettingsManager.Instance.RequestGlobalSettings();
            Connection.OnPropertyInspectorDidAppear += OnPropertyInspectorOpened;
        }

        public override void Dispose() {
            Connection.OnPropertyInspectorDidAppear -= OnPropertyInspectorOpened;
        }

        public override void KeyPressed(KeyPayload payload) {
            if(DiscordRPC.Instance.IsRunning) {
                DiscordRPC.Instance.Stop();
            }
            else {
                DiscordRPC.Instance.Start();
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            ChannelGridManager.Instance.Width = settings.MaxChannelWidth;
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) {
            Tools.AutoPopulateSettings(settings, payload.Settings);

            Connection.SetSettingsAsync(JObject.FromObject(settings)).GetAwaiter().GetResult();
        }

        // Save the current settings and send a "GlobalSettingsReceived" message to all other actions
        private Task SaveSettings() {
            GlobalSettingsManager.Instance.SetGlobalSettings(JObject.FromObject(settings));
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private void OnPropertyInspectorOpened(object sender, SDEventReceivedEventArgs<PropertyInspectorDidAppear> e) {
            Connection.SetSettingsAsync(JObject.FromObject(settings)).GetAwaiter().GetResult();
        }

    }
}