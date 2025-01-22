﻿using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Payloads;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
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


        private SetColorSettings localSettings;
        private DeviceListSettings globalSettings;


        public SetColorDialAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.localSettings = new SetColorSettings("static", "#ffff00");
                SaveSettings();
            }
            else {
                this.localSettings = payload.Settings.ToObject<SetColorSettings>();
            }
            this.globalSettings = new DeviceListSettings();
            GlobalSettingsManager.Instance.RequestGlobalSettings();
            Connection.OnPropertyInspectorDidAppear += OnPropertyInspectorOpened;

            UpdateLayout();
        }

        public override void Dispose() {
            Connection.OnPropertyInspectorDidAppear -= OnPropertyInspectorOpened;
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"SetColorDialAction: Destructor called");
        }

        public async override void DialRotate(DialRotatePayload payload) {
            Color selectedColor = localSettings.selectedColor;

            int hue = (int) selectedColor.GetHue();
            float sat = selectedColor.GetSaturation();

            int stepSize = payload.IsDialPressed ? 10 : 1;

            // adding 360 so modulo operator can work correctly
            hue += payload.Ticks * stepSize + 360;
            hue %= 360;

            Color col = ImageTools.FromHSB((float) hue, sat, 1);
            localSettings.selectedColorHex = col.ToHex();
            
            await SaveSettings();
            UpdateLayout();
            SetColor();
           

        }

        public override void DialDown(DialPayload payload) { }

        public override void DialUp(DialPayload payload) { }

        public override void TouchPress(TouchpadPressPayload payload) {
            SetColor();
        }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            Tools.AutoPopulateSettings(localSettings, payload.Settings);
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
            Color selectedColor = localSettings.selectedColor;

            Bitmap img = ImageTools.GetBitmapFromFilePath("./Actions/SetColorAction/ColorRect.png");
            img = ImageTools.ReplaceColor(img, Color.Black, selectedColor);

            int hue = (int) selectedColor.GetHue();
            string imageString = Tools.ImageToBase64(img, true);
            Logger.Instance.LogMessage(TracingLevel.DEBUG, imageString);

            Dictionary<string, string> dkv = new Dictionary<string, string>();
            dkv["value"] = hue + "°";
            dkv["indicator"] = hue.ToString();
            dkv["colorIcon"] = imageString;
            await Connection.SetFeedbackAsync(dkv);

            img.Dispose();
        }

        private void SetColor() {
            if(localSettings.useGlobalSettings) {
                GoveeDeviceController.Instance.SetColor(localSettings.selectedColor, globalSettings.deviceIpList);
            }
            else {
                GoveeDeviceController.Instance.SetColor(localSettings.selectedColor, localSettings.deviceIpList);
            }
        }

        #endregion
    }
}