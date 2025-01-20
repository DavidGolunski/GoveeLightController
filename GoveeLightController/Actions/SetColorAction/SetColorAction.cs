﻿using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Threading.Tasks;

namespace GoveeLightController {
    [PluginActionId("com.davidgolunski.goveelightcontroller.setcoloraction")]

    public class SetColorAction : KeypadBase {
        /*
         * This class represents an action on the Stream Deck.
         * The Action sets the color of the lights
         */


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

            UpdateImage();
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
            UpdateImage();
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

        private void UpdateImage() {
            if(!localSettings.useDynamicIcon) {
                //Connection.SetStateAsync(0).GetAwaiter().GetResult();
                Connection.SetDefaultImageAsync();
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "Setting Static Icon for Color");
                return;
            }
         
            Bitmap img = ImageTools.GetBitmapFromFilePath("./Actions/SetColorAction/LightbulbColorDynamic.png");
            img = ImageTools.ReplaceColor(img, Color.Black, localSettings.selectedColor);
            Connection.SetImageAsync(img).GetAwaiter().GetResult();
            img.Dispose();
        }

        #endregion
    }
}