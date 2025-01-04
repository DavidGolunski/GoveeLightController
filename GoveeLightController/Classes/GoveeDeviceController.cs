using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using BarRaider.SdTools;
using System.Drawing;

namespace GoveeLightController {
    public class GoveeDeviceController {

        private static GoveeDeviceController instance;
        public static GoveeDeviceController Instance { 
            get => instance ?? (instance = new GoveeDeviceController(40, 60));
            private set => instance = value;
        }

        private int _standardBrightness;
        private int _highlightBrightness;
        private Dictionary<string, GoveeDevice> _devices;
        private Color _primaryColor = Color.Black;
        private bool terminateEffect = false; // Flag to stop special effects
        private Thread _effectThread;          // Placeholder for effect thread

        private GoveeDeviceController(int standardBrightness = 40, int highlightBrightness = 100) {
            _standardBrightness = standardBrightness;
            _highlightBrightness = highlightBrightness;

            _devices = new Dictionary<string, GoveeDevice>();
        }

        ~GoveeDeviceController() {
            TurnOff(null);
        }


        public void SetPrimaryColor(Color color) {
            _primaryColor = color;
        }

        public void ActivatePrimaryColor(List<string> ips = null, bool ignoreThread = true) {
            SetColor(_primaryColor, ips, ignoreThread);
        }

        public void TurnOn(List<string> ips = null, bool ignoreThread = false) {
            if(!ignoreThread)
                StopEffectThread();

            AddNonExistingDevices(ips);

            List<string> ipsToUse = ips??_devices.Keys.ToList();
            foreach(var ip in ipsToUse) {
                _devices[ip].TurnOn();
            }
        }

        public void TurnOff(List<string> ips = null, bool ignoreThread = false) {
            if(!ignoreThread)
                StopEffectThread();

            AddNonExistingDevices(ips);

            List<string> ipsToUse = ips ?? _devices.Keys.ToList();
            foreach(var ip in ipsToUse) {
                _devices[ip].TurnOff();
            }
        }

        public void SetBrightness(int brightness, List<string> ips = null,  bool ignoreThread = false) {
            if(!ignoreThread)
                StopEffectThread();

            AddNonExistingDevices(ips);

            List<string> ipsToUse = ips ?? _devices.Keys.ToList();
            foreach(var ip in ipsToUse) {
                _devices[ip].SetBrightness(brightness);
            }
        }

        public void SetColor(Color color, List<string> ips = null, bool ignoreThread = false) {
            if(color == null)
                throw new ArgumentNullException(nameof(color));
            if(!ignoreThread)
                StopEffectThread();

            AddNonExistingDevices(ips);

            List<string> ipsToUse = ips ?? _devices.Keys.ToList();
            foreach(var ip in ipsToUse) {
                _devices[ip].SetColor(color);
            }
        }

        #region supportive functions

        private void AddNonExistingDevices(List<string> ips) {
            if(ips == null || ips.Count == 0) {
                return;
            }

            foreach(string ip in ips) {
                if(!_devices.ContainsKey(ip)) {
                    _devices.Add(ip, new GoveeDevice(ip, null, null));
                }
            }
        }
        #endregion

        #region Threading Section
        public void Pulse(Color color, int numOfPulses, double onTime, double offTime, bool turnOffAfterFunction = false, bool switchToPrimaryColorAfterFunction = false) {
            if(color == null)
                throw new ArgumentNullException(nameof(color));
            if(numOfPulses <= 0 || onTime <= 0 || offTime <= 0)
                throw new ArgumentException("Invalid pulse parameters");

            StopEffectThread();

            terminateEffect = false;
            _effectThread = new Thread(() =>
                RunPulse(color, numOfPulses, onTime, offTime, turnOffAfterFunction, switchToPrimaryColorAfterFunction)
            );
            _effectThread.Start();
        }

        private void StopEffectThread() {
            if(_effectThread != null && _effectThread.IsAlive) {
                terminateEffect = true;
                _effectThread.Join(); // Wait for the thread to terminate
            }
            terminateEffect = false;
            _effectThread = null;
        }

        private void RunPulse(Color color, int numOfPulses, double onTime, double offTime, bool turnOffAfterFunction, bool switchToPrimaryColorAfterFunction) {
            if(_devices.Count <= 0)
                return;

            TurnOn(null, true);
            SetColor(color, null, true);
            Task.Delay(100).Wait();
            SetBrightness(_highlightBrightness, null, true);

            Task.Delay(TimeSpan.FromSeconds(onTime)).Wait();

            for(int i = 0; i < numOfPulses - 1; i++) {
                if(terminateEffect)
                    return;

                TurnOff(null, true);
                Task.Delay(TimeSpan.FromSeconds(offTime)).Wait();

                if(terminateEffect)
                    return;

                TurnOn(null,true);
                Task.Delay(TimeSpan.FromSeconds(onTime)).Wait();
            }

            SetBrightness(_standardBrightness, null, true);

            if(switchToPrimaryColorAfterFunction && !terminateEffect) {
                Task.Delay(100).Wait();
                ActivatePrimaryColor(null, true);
            }

            if(turnOffAfterFunction && !terminateEffect) {
                Task.Delay(100).Wait();
                TurnOff(null,true);
            }
        }
        #endregion
    }
}
