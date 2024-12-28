using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoveeLightController {
    [PluginActionId("com.davidgolunski.goveelightcontroller.turnoffaction")]

    public class TurnOffAction : KeypadBase {

        private DeviceListSettings settings;


        public TurnOffAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.settings = new DeviceListSettings();
                SaveSettings();
            }
            else {
                this.settings = payload.Settings.ToObject<DeviceListSettings>();
            }
        }

        public override void Dispose() {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"TurnOffAction: Destructor called");
        }

        public override void KeyPressed(KeyPayload payload) {
            GoveeDeviceController.Instance.TurnOff();
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { 
            
        }

        #region Private Methods

        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        #endregion
    }
}