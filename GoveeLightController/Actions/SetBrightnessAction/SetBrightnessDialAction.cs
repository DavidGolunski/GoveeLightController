using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using BarRaider.SdTools.Payloads;

namespace GoveeLightController {
    [PluginActionId("com.davidgolunski.goveelightcontroller.setbrightnessdialaction")]

    public class SetBrightnessDialAction : EncoderBase {
        /*
         * This class represents an action on the Stream Deck.
         * The Action sets the brightsness of the lights
         */

        private readonly SetBrightnessSettings localSettings;
        private readonly DeviceListSettings globalSettings;

        // to distinguish between a dial press and a "rotate press"
        private bool dialWasRotated = false;

        public SetBrightnessDialAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
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

            Dictionary<string, string> dkv = new Dictionary<string, string> {
                ["value"] = localSettings.Brightness + "%",
                ["indicator"] = localSettings.Brightness.ToString()
            };
            Connection.SetFeedbackAsync(dkv);
        }

        public override void Dispose() {
            Connection.OnPropertyInspectorDidAppear -= OnPropertyInspectorOpened;
        }

        public async override void DialRotate(DialRotatePayload payload) {
            dialWasRotated = true;
            int stepSize = payload.IsDialPressed ? 5 : 1;

            localSettings.Brightness += payload.Ticks * stepSize;
            if(localSettings.Brightness < 0)
                localSettings.Brightness = 0;
            if(localSettings.Brightness > 100)
                localSettings.Brightness = 100;

            await SaveSettings();

            Dictionary<string, string> dkv = new Dictionary<string, string> {
                ["value"] = localSettings.Brightness + "%",
                ["indicator"] = localSettings.Brightness.ToString()
            };
            await Connection.SetFeedbackAsync(dkv);

            SetBrightness();
        }

        public override void DialDown(DialPayload payload) {
            dialWasRotated = false;
        }

        public override void DialUp(DialPayload payload) {
            if(dialWasRotated)
                return;

            SetBrightness();
        }

        public override void TouchPress(TouchpadPressPayload payload) {
            SetBrightness();
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

        private void SetBrightness() {
            if(localSettings.UseGlobalSettings) {
                GoveeDeviceController.Instance.SetBrightness(localSettings.Brightness, globalSettings.DeviceIpList);
            }
            else {
                GoveeDeviceController.Instance.SetBrightness(localSettings.Brightness, localSettings.DeviceIpList);
            }
        }
        

        #endregion
    }
}
