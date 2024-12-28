using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using BarRaider.SdTools;


namespace GoveeLightController {
    public class GoveeDeviceController {

        private static GoveeDeviceController instance;
        public static GoveeDeviceController Instance { 
            get => instance ?? (instance = new GoveeDeviceController(3, 40, 60));
            private set => instance = value;
        }

        private readonly int _deviceConnectTimeout;
        private readonly int _standardBrightness;
        private readonly int _highlightBrightness;
        private Dictionary<string, GoveeDevice> _devices;
        private Color _primaryColor = Color.BLACK;
        private bool _terminateEffect = false; // Flag to stop special effects
        private Thread _effectThread;          // Placeholder for effect thread

        private GoveeDeviceController(int deviceConnectTimeout = 3, int standardBrightness = 40, int highlightBrightness = 100, List<string> deviceIpList = null) {
            _deviceConnectTimeout = deviceConnectTimeout;
            _standardBrightness = standardBrightness;
            _highlightBrightness = highlightBrightness;

            if(deviceIpList == null) {
                _devices = GoveeDevice.GetDevices(_deviceConnectTimeout);
                if(_devices.Count > 0) {
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Found {_devices.Count} Govee Devices");
                }
                else {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "Have not found any Govee Devices");
                }
            }
            else {
                _devices = deviceIpList.ToDictionary(ip => ip, ip => new GoveeDevice(ip, null, null));
            }

            Task.Delay(1000).Wait();
            SetBrightness(_standardBrightness);
            Task.Delay(100).Wait();
            SetColor(_primaryColor);
            Task.Delay(100).Wait();
            TurnOff();
        }

        ~GoveeDeviceController() {
            TurnOff();
        }

        public void SetPrimaryColor(Color color) {
            _primaryColor = color ?? throw new ArgumentNullException(nameof(color));
        }

        public void ActivatePrimaryColor(bool ignoreThread = true) {
            SetColor(_primaryColor, ignoreThread);
        }

        public void TurnOn(bool ignoreThread = false) {
            if(!ignoreThread)
                StopEffectThread();
            foreach(var device in _devices.Values) {
                device.TurnOn();
            }
        }

        public void TurnOff(bool ignoreThread = false) {
            if(!ignoreThread)
                StopEffectThread();
            foreach(var device in _devices.Values) {
                device.TurnOff();
            }
        }

        public void SetBrightness(int brightness, bool ignoreThread = false) {
            if(!ignoreThread)
                StopEffectThread();
            foreach(var device in _devices.Values) {
                device.SetBrightness(brightness);
            }
        }

        public void SetColor(Color color, bool ignoreThread = false) {
            if(color == null)
                throw new ArgumentNullException(nameof(color));
            if(!ignoreThread)
                StopEffectThread();
            foreach(var device in _devices.Values) {
                device.SetColor(color);
            }
        }

        public void Pulse(Color color, int numOfPulses, double onTime, double offTime, bool turnOffAfterFunction = false, bool switchToPrimaryColorAfterFunction = false) {
            if(color == null)
                throw new ArgumentNullException(nameof(color));
            if(numOfPulses <= 0 || onTime <= 0 || offTime <= 0)
                throw new ArgumentException("Invalid pulse parameters");

            StopEffectThread();

            _terminateEffect = false;
            _effectThread = new Thread(() =>
                RunPulse(color, numOfPulses, onTime, offTime, turnOffAfterFunction, switchToPrimaryColorAfterFunction)
            );
            _effectThread.Start();
        }

        private void StopEffectThread() {
            if(_effectThread != null && _effectThread.IsAlive) {
                _terminateEffect = true;
                _effectThread.Join(); // Wait for the thread to terminate
            }
            _terminateEffect = false;
            _effectThread = null;
        }

        private void RunPulse(Color color, int numOfPulses, double onTime, double offTime, bool turnOffAfterFunction, bool switchToPrimaryColorAfterFunction) {
            if(_devices.Count <= 0)
                return;

            TurnOn(true);
            SetColor(color, true);
            Task.Delay(100).Wait();
            SetBrightness(_highlightBrightness, true);

            Task.Delay(TimeSpan.FromSeconds(onTime)).Wait();

            for(int i = 0; i < numOfPulses - 1; i++) {
                if(_terminateEffect)
                    return;

                TurnOff(true);
                Task.Delay(TimeSpan.FromSeconds(offTime)).Wait();

                if(_terminateEffect)
                    return;

                TurnOn(true);
                Task.Delay(TimeSpan.FromSeconds(onTime)).Wait();
            }

            SetBrightness(_standardBrightness, true);

            if(switchToPrimaryColorAfterFunction && !_terminateEffect) {
                Task.Delay(100).Wait();
                ActivatePrimaryColor(true);
            }

            if(turnOffAfterFunction && !_terminateEffect) {
                Task.Delay(100).Wait();
                TurnOff(true);
            }
        }
    }
}
