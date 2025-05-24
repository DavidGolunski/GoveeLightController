using Newtonsoft.Json;

namespace GoveeLightController {

    public class SetTemperatureSettings : DeviceListSettings {

        [JsonProperty(PropertyName = "temperature")]
        public int Temperature { get; set; }

        public SetTemperatureSettings() : base() {
            Temperature = 6500;
        }
    }
}
