using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Payloads;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace GoveeLightController {
    [PluginActionId("com.davidgolunski.goveelightcontroller.setcolordialaction")]

    public class SetColorDialAction : EncoderBase {
        /*
         * This class represents a dial action on the Stream Deck.
         * The Action sets the color of the lights and to "scroll" through
         */


        private readonly SetColorDialActionSettings localSettings;
        private readonly DeviceListSettings globalSettings;

        // to distinguish between a dial press and a "rotate press"
        private bool dialWasRotated = false; 


        public SetColorDialAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.localSettings = new SetColorDialActionSettings();
                SaveSettings();
            }
            else {
                this.localSettings = payload.Settings.ToObject<SetColorDialActionSettings>();
            }
            this.globalSettings = new DeviceListSettings();
            GlobalSettingsManager.Instance.RequestGlobalSettings();
            Connection.OnPropertyInspectorDidAppear += OnPropertyInspectorOpened;

            UpdateLayout();
        }

        public override void Dispose() {
            Connection.OnPropertyInspectorDidAppear -= OnPropertyInspectorOpened;
        }

        public async override void DialRotate(DialRotatePayload payload) {
            dialWasRotated = true;


            int hue = localSettings.ColorHue;
            int stepSize = payload.IsDialPressed ? 10 : 1;

            hue += payload.Ticks * stepSize;
            if(hue < 0)
                hue += 360;
            if(hue >= 360)
                hue -= 360;

            localSettings.ColorHue = hue;

            UpdateLayout();
            SetColor();
            await SaveSettings();
        }

        public override void DialDown(DialPayload payload) { 
            dialWasRotated = false; 
        }

        public override void DialUp(DialPayload payload) {
            if(dialWasRotated)
                return;
            SetColor();
        }

        public override void TouchPress(TouchpadPressPayload payload) {
            SetColor();
        }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            Tools.AutoPopulateSettings(localSettings, payload.Settings);
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "SetColorDialAction - " + localSettings.HexCodeString);
            SaveSettings();
            UpdateLayout();
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

        private async void UpdateLayout() {
            Bitmap img = ImageTools.GetBitmapFromFilePath("./Actions/SetColorAction/ColorRect.png");
            img = ImageTools.ReplaceColor(img, Color.Black, localSettings.CurrentColor);


            int hue = localSettings.ColorHue;
            string imageString = Tools.ImageToBase64(img, true);

            Dictionary<string, string> dkv = new Dictionary<string, string> {
                ["value"] = hue + "°",
                ["indicator"] = hue.ToString(),
                ["colorIcon"] = imageString
            };
            await Connection.SetFeedbackAsync(dkv);

            img.Dispose();
        }

        private void SetColor() {
            if(localSettings.UseGlobalSettings) {
                GoveeDeviceController.Instance.SetColor(localSettings.CurrentColor, globalSettings.DeviceIpList);
            }
            else {
                GoveeDeviceController.Instance.SetColor(localSettings.CurrentColor, localSettings.DeviceIpList);
            }
        }

        #endregion
    }
}