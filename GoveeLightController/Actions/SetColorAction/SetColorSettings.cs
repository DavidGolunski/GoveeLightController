using Newtonsoft.Json;
using System.Drawing;

namespace GoveeLightController {
    public class SetColorSettings : DeviceListSettings {

        [JsonProperty(PropertyName = "useDynamicIconOption")]
        public string UseDynamicIconOption { get; set; }

        public bool UseDynamicIcon {
            get => UseDynamicIconOption == "dynamic";
        }


        [JsonProperty(PropertyName = "selectedColorHex")]
        public string SelectedColorHex { get; set; }

        public Color SelectedColor {
            get {
                // remove the "#" at the beginning
                string modifiedColorHex = SelectedColorHex.Remove(0, 1);
                // add the alpha channel
                modifiedColorHex = "ff" + modifiedColorHex;
                int colorInt = int.Parse(modifiedColorHex, System.Globalization.NumberStyles.HexNumber);
                return Color.FromArgb(colorInt);
            }
        }

        public SetColorSettings() : base() {
            UseDynamicIconOption = "dynamic";
            SelectedColorHex = "#ffffff";
        }

        public SetColorSettings(string useDynamicIconOption, string selectedColorHex) : base() {
            this.UseDynamicIconOption = useDynamicIconOption;
            this.SelectedColorHex = selectedColorHex;
        }
    }
}
