using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GoveeLightController {

    /*
     * A class that stores information about which devices should be used by an action.
     * Used for Global Settings and as a base class for more advanced settings
     */
    public class DeviceListSettings {

        // this is the string that the user has input as the ip list
        public string _deviceIpListString;

        [JsonProperty(PropertyName = "deviceIpListString")]
        public string DeviceIpListString {
            get => _deviceIpListString; 
            set {
                _deviceIpListString = value;
                UpdateDeviceIpList(value);
            }
        }

        [JsonProperty(PropertyName = "validatedDeviceIpListString")]
        public string ValidatedDeviceIpListString {
            get {
                string deviceIpListString = String.Join(",\n", this.DeviceIpList.ToArray());
                return deviceIpListString.Trim();
            }
        }


        public List<String> DeviceIpList { get; private set; } = new List<String>();



        [JsonProperty(PropertyName = "useGlobalSettingsOption")]
        public string UseGlobalSettingsOption { get; set; }

        public bool UseGlobalSettings {
            get => UseGlobalSettingsOption == "global";
        }



        public DeviceListSettings(){
            _deviceIpListString = "";
            UseGlobalSettingsOption = "global";
        }



        private void UpdateDeviceIpList(string input) {
            input ??= string.Empty;

            // remove any characters that are not part of an ip
            string filteredInput = Regex.Replace(input, @"[^0-9,.\n]", "");

            // Split the input string by comma and whitespace
            string[] ips = filteredInput.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(ip => ip.Trim())
                                .ToArray();

            List<string> validIPs = new List<string>();
            foreach(var ip in ips) {
                if(IsValidIP(ip)) {
                    validIPs.Add(ip);
                }
            }

            DeviceIpList = validIPs;
        }

        private static bool IsValidIP(string ip) {
            // Regex to match a valid IPv4 address
            string pattern = @"^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\." +
                             @"(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\." +
                             @"(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\." +
                             @"(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";

            return Regex.IsMatch(ip, pattern);
        }
    }
}