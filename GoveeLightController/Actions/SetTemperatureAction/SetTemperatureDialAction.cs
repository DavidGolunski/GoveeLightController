using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using BarRaider.SdTools.Payloads;

namespace GoveeLightController {
    [PluginActionId("com.davidgolunski.goveelightcontroller.settemperaturedialaction")]

    public class SetTemperatureDialAction : EncoderBase {
        /*
         * This class represents an action on the Stream Deck.
         * The Action sets the brightsness of the lights
         */

        private readonly SetTemperatureSettings localSettings;
        private readonly DeviceListSettings globalSettings;

        // to distinguish between a dial press and a "rotate press"
        private bool dialWasRotated = false;

        public SetTemperatureDialAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
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

            int normalizedTemperature = (localSettings.Temperature - 2000) / 70; // normalizong the temperature to a value between 0 and 100, as the normal range from 2000 to 9000 breaks the layout
            Dictionary<string, string> dkv = new Dictionary<string, string> {
                ["value"] = localSettings.Temperature + "K",
                ["indicator"] = normalizedTemperature.ToString()
            };
            Connection.SetFeedbackAsync(dkv);
        }

        public override void Dispose() {
            Connection.OnPropertyInspectorDidAppear -= OnPropertyInspectorOpened;
        }

        public async override void DialRotate(DialRotatePayload payload) {
            dialWasRotated = true;
            int stepSize = payload.IsDialPressed ? 100 : 10;

            localSettings.Temperature += payload.Ticks * stepSize;
            if(localSettings.Temperature < 2000)
                localSettings.Temperature = 2000;
            if(localSettings.Temperature > 9000)
                localSettings.Temperature = 9000;

            await SaveSettings();

            int normalizedTemperature = (localSettings.Temperature - 2000) / 70; // normalizong the temperature to a value between 0 and 100, as the normal range from 2000 to 9000 breaks the layout
            Dictionary<string, string> dkv = new Dictionary<string, string> {
                ["value"] = localSettings.Temperature + "K",
                ["indicator"] = normalizedTemperature.ToString()
            };
            await Connection.SetFeedbackAsync(dkv);

            SetTemperature();
        }

        public override void DialDown(DialPayload payload) {
            dialWasRotated = false;
        }

        public override void DialUp(DialPayload payload) {
            if(dialWasRotated)
                return;

            SetTemperature();
        }

        public override void TouchPress(TouchpadPressPayload payload) {
            SetTemperature();
        }


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

        private void SetTemperature() {
            if(localSettings.UseGlobalSettings) {
                GoveeDeviceController.Instance.SetTemperature(localSettings.Temperature, globalSettings.DeviceIpList);
            }
            else {
                GoveeDeviceController.Instance.SetTemperature(localSettings.Temperature, localSettings.DeviceIpList);
            }
        }
        

        #endregion
    }
}
