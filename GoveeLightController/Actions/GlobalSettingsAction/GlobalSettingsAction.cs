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

namespace GoveeLightController {
    [PluginActionId("com.davidgolunski.goveelightcontroller.globalsettingsaction")]

    /*
     * This class represents an action where the user can define global settings for the plugin.
     * The Settings are a device list, which can be used by all other actions by default.
     * OnKeyPressed:
     * When the key is pressed it will look for all compatible Govee Devices in the network and populates the Device List automatically
     */
    public class GlobalSettingsAction : KeypadBase {

        private readonly DeviceListSettings settings;

        public GlobalSettingsAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.settings = new DeviceListSettings();
                SaveSettings();
            }
            else {
                this.settings = payload.Settings.ToObject<DeviceListSettings>();
            }
            Connection.OnPropertyInspectorDidAppear += OnPropertyInspectorOpened;
        }

        public override void Dispose() {
            Connection.OnPropertyInspectorDidAppear -= OnPropertyInspectorOpened;
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
                Logger.Instance.LogMessage(TracingLevel.INFO, "Found Device with IP: " + deviceEntry.Value.Ip);
            }

            // update the list in the property inspector, but don't update the settings. The user has to click the save button to apply the new settings
            string oldIpListString = settings._deviceIpListString;
            settings._deviceIpListString = deviceIpListString;
            Connection.SetSettingsAsync(JObject.FromObject(settings));
            settings._deviceIpListString = oldIpListString;

            
        }

        public async override void KeyReleased(KeyPayload payload) {
            await Connection.ShowOk();
        }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
            
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        // Save the current settings and send a "GlobalSettingsReceived" message to all other actions
        private Task SaveSettings() {
            GlobalSettingsManager.Instance.SetGlobalSettings(JObject.FromObject(settings));
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private void OnPropertyInspectorOpened(object sender, SDEventReceivedEventArgs<PropertyInspectorDidAppear> e) {
            Connection.SetSettingsAsync(JObject.FromObject(settings));
        }
    }
}