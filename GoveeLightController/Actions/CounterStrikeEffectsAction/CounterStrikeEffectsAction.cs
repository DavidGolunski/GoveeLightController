using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace GoveeLightController {
    [PluginActionId("com.davidgolunski.goveelightcontroller.counterstrikeeffectssaction")]

    public class CounterStrikeEffectsAction : KeypadBase {
        /*
         * This class represents an action on the Stream Deck.
         * The Action enables/disables the League Effects Manager, which controls GoveeLights based on League Of Legends Events
         */

        private DeviceListSettings localSettings;
        private DeviceListSettings globalSettings;

        public CounterStrikeEffectsAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.localSettings = new DeviceListSettings();
                SaveSettings();
            }
            else {
                this.localSettings = payload.Settings.ToObject<DeviceListSettings>();
            }
            this.globalSettings = new DeviceListSettings();
            GlobalSettingsManager.Instance.RequestGlobalSettings();
            Connection.OnPropertyInspectorDidAppear += OnPropertyInspectorOpened;
            Connection.SetStateAsync(0).GetAwaiter().GetResult();
        }

        public override void Dispose() {
            Connection.OnPropertyInspectorDidAppear -= OnPropertyInspectorOpened;
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"CounterStrikeEffectsAction: Destructor called");
        }

        public override void KeyPressed(KeyPayload payload) {
            if(CounterStrikeEffectsManager.Instance.isRunning) {
                CounterStrikeEffectsManager.Instance.Stop();
                Connection.SetStateAsync(0).GetAwaiter().GetResult();
                return;
            }

            if(localSettings.useGlobalSettings) {
                CounterStrikeEffectsManager.Instance.Start(globalSettings.deviceIpList);
            }
            else {
                CounterStrikeEffectsManager.Instance.Start(localSettings.deviceIpList);
            }
            Connection.SetStateAsync(1).GetAwaiter().GetResult();

        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            Tools.AutoPopulateSettings(localSettings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) {
            Tools.AutoPopulateSettings(globalSettings, payload.Settings);
        }

        #region Private Methods

        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(localSettings));
        }

        private void OnPropertyInspectorOpened(object sender, SDEventReceivedEventArgs<PropertyInspectorDidAppear> e) {
            Connection.SetSettingsAsync(JObject.FromObject(localSettings));
        }

        #endregion
    }
}