using BarRaider.SdTools;
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
    [PluginActionId("com.davidgolunski.goveelightcontroller.setcoloraction")]

    public class SetColorAction : KeypadBase {

        private class SetColorSettings : DeviceListSettings {

            [JsonProperty(PropertyName = "selectedColorHex")]
            public string selectedColorHex { get; set; }

            public Color selectedColor {
                get => new Color(selectedColorHex);
            }

            public SetColorSettings() : base() {
                selectedColorHex = "#ffffff";
            }
        }


        private SetColorSettings localSettings;
        private DeviceListSettings globalSettings;


        public SetColorAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.localSettings = new SetColorSettings();
                SaveSettings();
            }
            else {
                this.localSettings = payload.Settings.ToObject<SetColorSettings>();
            }
            this.globalSettings = new DeviceListSettings();
            GlobalSettingsManager.Instance.RequestGlobalSettings();
            Connection.OnPropertyInspectorDidAppear += OnPropertyInspectorOpened;
        }

        public override void Dispose() {
            Connection.OnPropertyInspectorDidAppear -= OnPropertyInspectorOpened;
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"SetColorAction: Destructor called");
        }

        public override void KeyPressed(KeyPayload payload) {
            if(localSettings.useGlobalSettings) {
                GoveeDeviceController.Instance.SetColor(localSettings.selectedColor, globalSettings.deviceIpList);
            }
            else {
                GoveeDeviceController.Instance.SetColor(localSettings.selectedColor, localSettings.deviceIpList);
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