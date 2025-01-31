using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoveeLightController {
    [PluginActionId("com.davidgolunski.goveelightcontroller.turnonoffaction")]

    public class TurnOnOffAction : KeypadBase {
        /*
         * This class represents an action on the Stream Deck.
         * The Action toggles between turning lights on and off
         */

        private readonly DeviceListSettings localSettings;
        private readonly DeviceListSettings globalSettings;


        private bool IsOn = false;

        public TurnOnOffAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
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
            List<string> deviceIpList = null;
            if(localSettings.UseGlobalSettings) {
                deviceIpList = globalSettings.DeviceIpList;
            }
            else {
                deviceIpList = localSettings.DeviceIpList;
            }
            if(IsOn) {

                GoveeDeviceController.Instance.TurnOff(deviceIpList);
                IsOn = false;
                Connection.SetStateAsync(0).GetAwaiter().GetResult();
            }
            else {
                GoveeDeviceController.Instance.TurnOn(deviceIpList);
                IsOn = true;
                Connection.SetStateAsync(1).GetAwaiter().GetResult();
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