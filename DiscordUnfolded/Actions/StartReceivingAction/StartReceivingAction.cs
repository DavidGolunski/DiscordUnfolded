﻿using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace DiscordUnfolded {
    [PluginActionId("com.davidgolunski.startreceivingaction")]
    public class StartReceivingAction : KeypadBase {


        public StartReceivingAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {

        }

        public override void Dispose() {
        }

        public override void KeyPressed(KeyPayload payload) {
            if(BetterDiscordReceiver.Instance.IsRunning) {
                BetterDiscordReceiver.Instance.Stop();
            }
            else {
                BetterDiscordReceiver.Instance.Start();
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