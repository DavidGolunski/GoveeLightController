using BarRaider.SdTools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoveeLightController {

    public class SetBrightnessSettings : DeviceListSettings {

        [JsonProperty(PropertyName = "brightness")]
        public int Brightness { get; set; }

        public SetBrightnessSettings() : base() {
            Brightness = 100;
        }
    }
}
