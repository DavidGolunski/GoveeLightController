﻿using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace GoveeLightController {
    [PluginActionId("com.davidgolunski.goveelightcontroller.counterstrikeeffectssaction")]

    public class CounterStrikeEffectsAction : KeypadBase {
        /*
         * This class represents an action on the Stream Deck.
         * The Action enables/disables the League Effects Manager, which controls GoveeLights based on League Of Legends Events
         */

        private readonly DeviceListSettings localSettings;
        private readonly DeviceListSettings globalSettings;

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
        }

        public override void KeyPressed(KeyPayload payload) {
            if(CounterStrikeEffectsManager.Instance.IsRunning) {
                CounterStrikeEffectsManager.Instance.Stop();
                Connection.SetStateAsync(0).GetAwaiter().GetResult();
                return;
            }

            if(localSettings.UseGlobalSettings) {
                CounterStrikeEffectsManager.Instance.Start(globalSettings.DeviceIpList);
            }
            else {
                CounterStrikeEffectsManager.Instance.Start(localSettings.DeviceIpList);
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