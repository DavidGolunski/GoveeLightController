using Newtonsoft.Json;
using System.Drawing;

namespace GoveeLightController {
    public class SetColorSettings : DeviceListSettings {

        [JsonProperty(PropertyName = "useDynamicIconOption")]
        public string useDynamicIconOption { get; set; }

        public bool useDynamicIcon {
            get => useDynamicIconOption == "dynamic";
        }


        [JsonProperty(PropertyName = "selectedColorHex")]
        public string selectedColorHex { get; set; }

        public Color selectedColor {
            get {
                // remove the "#" at the beginning
                string modifiedColorHex = selectedColorHex.Remove(0, 1);
                // add the alpha channel
                modifiedColorHex = "ff" + modifiedColorHex;
                int colorInt = int.Parse(modifiedColorHex, System.Globalization.NumberStyles.HexNumber);
                return Color.FromArgb(colorInt);
            }
        }

        public SetColorSettings() : base() {
            useDynamicIconOption = "dynamic";
            selectedColorHex = "#ffffff";
        }

        public SetColorSettings(string useDynamicIconOption, string selectedColorHex) : base() {
            this.useDynamicIconOption = useDynamicIconOption;
            this.selectedColorHex = selectedColorHex;
        }
    }
}
