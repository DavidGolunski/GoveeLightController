using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace GoveeLightController {
    [PluginActionId("com.davidgolunski.goveelightcontroller.settemperatureaction")]

    public class SetTemperatureAction : KeypadBase {
        /*
         * This class represents an action on the Stream Deck.
         * The Action sets the color of the lights based on a temperature value
         */

        private readonly SetTemperatureSettings localSettings;
        private readonly DeviceListSettings globalSettings;


        public SetTemperatureAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.localSettings = new SetTemperatureSettings();
                SaveSettings();
            }
            else {
                this.localSettings = payload.Settings.ToObject<SetTemperatureSettings>();
            }
            this.globalSettings = new DeviceListSettings();
            GlobalSettingsManager.Instance.RequestGlobalSettings();
            Connection.OnPropertyInspectorDidAppear += OnPropertyInspectorOpened;
        }

        public override void Dispose() {
            Connection.OnPropertyInspectorDidAppear -= OnPropertyInspectorOpened;
        }

        public override void KeyPressed(KeyPayload payload) {
            if(localSettings.UseGlobalSettings) {
                GoveeDeviceController.Instance.SetTemperature(localSettings.Temperature, globalSettings.DeviceIpList);
            }
            else {
                GoveeDeviceController.Instance.SetTemperature(localSettings.Temperature, localSettings.DeviceIpList);
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