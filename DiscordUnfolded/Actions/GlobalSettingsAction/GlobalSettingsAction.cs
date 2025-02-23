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
                Logger.Instance.LogMessage(TracingLevel.WARN, "Discord RPC Stopping was requested by user, by pressing the \"GlobalSettings\" action");
                DiscordRPC.Instance.Stop();
            }
            else {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Discord RPC Starting was requested by user, by pressing the \"GlobalSettings\" action");
                DiscordRPC.Instance.Start(settings.ClientId, settings.ClientSecret, settings.DefaultGuildIdString);
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        // if local settings are received then set them and send out a "GlobalSettingsReceived" message
        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            string previousClientID = settings.ClientId;
            string previousClientSecret = settings.ClientSecret;

            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();

            ChannelGridManager.Instance.Width = settings.MaxChannelWidth;

            if(previousClientID != settings.ClientId || previousClientSecret != settings.ClientSecret) {
                Logger.Instance.LogMessage(TracingLevel.INFO, "Updated ClientID or Client Secret. Restarting IPC Connection");
                DiscordRPC.Instance.Stop();
                DiscordRPC.Instance.Start(settings.ClientId, settings.ClientSecret, settings.DefaultGuildIdString);
            }
            
        }

        // if global settings are received, then load the settings, but do not send out another "GlobalSettingsReceived" message
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