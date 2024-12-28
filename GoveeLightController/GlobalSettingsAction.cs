using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace GoveeLightController {
    [PluginActionId("com.davidgolunski.goveelightcontroller.globalsettingsaction")]

    public class GlobalSettingsAction : KeypadBase {

        private DeviceListSettings settings;

        public GlobalSettingsAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.settings = new DeviceListSettings();
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "Received Settings in Constructor");
                SaveSettings();
            }
            else {
                this.settings = payload.Settings.ToObject<DeviceListSettings>();
            }
        }

        public override void Dispose() {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"GlobalSettingsAction Destructor called");
        }

        public override void KeyPressed(KeyPayload payload) {
            Dictionary<string, GoveeDevice> foundDevices = GoveeDevice.GetDevices(500);
            Logger.Instance.LogMessage(TracingLevel.INFO, "Found " +  foundDevices.Count + " Govee Devices");

            if(foundDevices.Count <= 0) {
                this.settings._deviceIpListString = "No Govee Devices were found.\n" +
                    "You can add devices manually inside the box here. Example:\n" +
                    "192.168.178.40,\n" +
                    "192.168.178.69";
                return;
            }

            string deviceIpListString = "";
            bool isFirst = true;
            foreach(var deviceEntry in foundDevices) {
                if(!isFirst) {
                    deviceIpListString += ",\n";
                }
                else {
                    isFirst = false;
                }
                deviceIpListString += deviceEntry.Value.Ip;
            }

            this.settings._deviceIpListString = deviceIpListString;
        }

        public override void KeyReleased(KeyPayload payload) { }
        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "GlobalSettingsAction: Received Settings");
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
            
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "GlobalSettingsAction: Received Global Settings");
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }


        private Task SaveSettings() {
            GlobalSettingsManager.Instance.SetGlobalSettings(JObject.FromObject(settings));
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }
    }
}