using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using DiscordUnfolded.DiscordCommunication;

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
                DiscordRPC.Instance.Start(settings.ClientId, settings.ClientSecret);
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        // if local settings are received then set them and send out a "GlobalSettingsReceived" message
        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "GlobalSettingsAction: Received Settings: " + settings);
            ChannelGridManager.Instance.Width = settings.MaxChannelWidth;
            DiscordRPC.Instance.Stop();
            DiscordRPC.Instance.Start(settings.ClientId, settings.ClientSecret);
        }

        // if global settings are received, then load the settings, but do not send out another "GlobalSettingsReceived" message
        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "GlobalSettingsAction: Received Global Settings: " + settings);
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