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
    [PluginActionId("com.davidgolunski.goveelightcontroller.leagueeffectsaction")]

    public class LeagueEffectsAction : KeypadBase {

        private DeviceListSettings localSettings;
        private DeviceListSettings globalSettings;

        public LeagueEffectsAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
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
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Set State to 0");
        }

        public override void Dispose() {
            Connection.OnPropertyInspectorDidAppear -= OnPropertyInspectorOpened;
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"TurnOnAction: Destructor called");
        }

        public override void KeyPressed(KeyPayload payload) {
            if(LeagueEffectManager.Instance.isRunning) {
                LeagueEffectManager.Instance.Stop();
                Connection.SetStateAsync(1).GetAwaiter().GetResult();
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "Set State to 1");
            }
            else {
                if(localSettings.useGlobalSettings) {
                    LeagueEffectManager.Instance.Start(globalSettings.deviceIpList);
                }
                else {
                    LeagueEffectManager.Instance.Start(localSettings.deviceIpList);
                }
                Connection.SetStateAsync(0).GetAwaiter().GetResult();
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "Set State to 0");
            }
            
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