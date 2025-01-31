using Newtonsoft.Json;

namespace GoveeLightController {

    public class SetBrightnessSettings : DeviceListSettings {

        [JsonProperty(PropertyName = "brightness")]
        public int Brightness { get; set; }

        public SetBrightnessSettings() : base() {
            Brightness = 100;
        }
    }
}
