using BarRaider.SdTools;
using Newtonsoft.Json;
using System.Drawing;

namespace GoveeLightController {

    class SetColorDialActionSettings : DeviceListSettings {

        [JsonProperty(PropertyName = "colorHue")]
        public int ColorHue { get; set; }
        [JsonProperty(PropertyName = "colorSaturation")]
        public int ColorSaturation { get; set; }
        [JsonProperty(PropertyName = "colorBrightness")]
        public int ColorBrightness { get; set; }

        [JsonProperty(PropertyName = "hexCodeString")]
        public string HexCodeString { get => CurrentColor.ToHex(); set { } }

        public Color CurrentColor {
            get {
                return ImageTools.FromHSB(ColorHue, 0.01f * ColorSaturation, 0.01f * ColorBrightness);
            }
        }

        public SetColorDialActionSettings() : base() {
            ColorHue = 0;
            ColorSaturation = 100;
            ColorBrightness = 100;
        }
    }

}
