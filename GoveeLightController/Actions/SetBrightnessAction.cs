﻿using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace GoveeLightController {
    [PluginActionId("com.davidgolunski.goveelightcontroller.setbrightnessaction")]

    public class SetBrightnessAction : KeypadBase {
        /*
         * This class represents an action on the Stream Deck.
         * The Action sets the brightsness of the lights
         */

        private class SetBrightnessSettings : DeviceListSettings {

            [JsonProperty(PropertyName = "brightness")]
            public int brightness { get; set; }

            public SetBrightnessSettings() : base() {
                brightness = 100;
            }
        }


        private SetBrightnessSettings localSettings;
        private DeviceListSettings globalSettings;


        public SetBrightnessAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.localSettings = new SetBrightnessSettings();
                SaveSettings();
            }
            else {
                this.localSettings = payload.Settings.ToObject<SetBrightnessSettings>();
            }
            this.globalSettings = new DeviceListSettings();
            GlobalSettingsManager.Instance.RequestGlobalSettings();
            Connection.OnPropertyInspectorDidAppear += OnPropertyInspectorOpened;
        }

        public override void Dispose() {
            Connection.OnPropertyInspectorDidAppear -= OnPropertyInspectorOpened;
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"SetBrightnessAction: Destructor called");
        }

        public override void KeyPressed(KeyPayload payload) {
            if(localSettings.useGlobalSettings) {
                GoveeDeviceController.Instance.SetBrightness(localSettings.brightness, globalSettings.deviceIpList);
            }
            else {
                GoveeDeviceController.Instance.SetBrightness(localSettings.brightness, localSettings.deviceIpList);
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