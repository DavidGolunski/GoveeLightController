using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GoveeLightController {

    /*
     * A class that stores information about which devices should be used by an action.
     * Used for Global Settings and as a base class for more advanced settings
     */
    public class DeviceListSettings {

        public string _deviceIpListString;
        [JsonProperty(PropertyName = "deviceIpListString")]
        public string deviceIpListString {
            get => _deviceIpListString; 
            set {
                ValidateAndExtractIPs(value);
            }
        }

        public List<String> deviceIpList { get; private set; }

        
        public DeviceListSettings(){
            _deviceIpListString = "";
        }



        private void ValidateAndExtractIPs(string input) {
            if (input == null) { 
                input = string.Empty;
            }

            // remove any characters that are not part of an ip
            string filteredInput = Regex.Replace(input, @"[^0-9,.\n]", "");

            // Split the input string by comma and whitespace
            string[] ips = filteredInput.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(ip => ip.Trim())
                                .ToArray();

            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Validating IPs");

            List<string> validIPs = new List<string>();
            foreach(var ip in ips) {
                Logger.Instance.LogMessage(TracingLevel.DEBUG, "IP: " + ip);
                if(IsValidIP(ip)) {
                    validIPs.Add(ip);
                }
            }

            deviceIpList = validIPs;

            string deviceIpListString = String.Join(",\n", validIPs.ToArray());
            deviceIpListString.Replace("[", "");
            deviceIpListString.Replace("]", "");

            this._deviceIpListString = deviceIpListString.Trim();
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