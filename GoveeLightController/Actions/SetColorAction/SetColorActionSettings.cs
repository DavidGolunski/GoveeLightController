using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace GoveeLightController {
    public class SetColorActionSettings : DeviceListSettings {

        [JsonProperty(PropertyName = "useDynamicIconOption")]
        public string UseDynamicIconOption { get; set; }
        public bool UseDynamicIcon {
            get => UseDynamicIconOption == "dynamic";
        }

        [JsonProperty(PropertyName = "colorRed")]
        public int ColorRed { get; set; }
        [JsonProperty(PropertyName = "colorGreen")]
        public int ColorGreen { get; set; }
        [JsonProperty(PropertyName = "colorBlue")]
        public int ColorBlue { get; set; }


        private string hexCodeString;
        [JsonProperty(PropertyName = "hexCodeString")]
        public string HexCodeString {
            get => hexCodeString;
            set {
                string modifiedHexCode = value.Trim().ToLower();
                modifiedHexCode = Regex.Replace(modifiedHexCode, "[^0-9a-f]", "");

                hexCodeString = modifiedHexCode;
            }
        }

        public SetColorActionSettings() : base() {
            UseDynamicIconOption = "dynamic";

            ColorRed = 255;
            ColorGreen = 255;
            ColorBlue = 255;

            hexCodeString = "ffffff";
        }
    }
}
