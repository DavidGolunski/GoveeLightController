using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace GoveeLightController {
    public class GoveeDeviceController {

        /*
         * A singleton class that can be used to control one or multiple Govee Devices at once.
         */

        private static GoveeDeviceController instance;
        public static GoveeDeviceController Instance { 
            get => instance ??= new GoveeDeviceController();
            private set => instance = value;
        }

        // store all devices, so no new devices have to be created every time a function is called
        private readonly Dictionary<string, GoveeDevice> _devices;
        private Color _primaryColor = Color.Black;

        public GoveeDeviceController() {

            _devices = new Dictionary<string, GoveeDevice>();
        }

        ~GoveeDeviceController() {
            TurnOff(null);
        }


        public void SetPrimaryColor(Color color) {
            _primaryColor = color;
        }

        public void ActivatePrimaryColor(List<string> ips = null) {
            SetColor(_primaryColor, ips);
        }

        public void TurnOn(List<string> ips = null) {
            AddNonExistingDevices(ips);

            List<string> ipsToUse = ips??_devices.Keys.ToList();
            foreach(var ip in ipsToUse) {
                _devices[ip].TurnOn();
            }
        }

        public void TurnOff(List<string> ips = null) {
            AddNonExistingDevices(ips);

            List<string> ipsToUse = ips ?? _devices.Keys.ToList();
            foreach(var ip in ipsToUse) {
                _devices[ip].TurnOff();
            }
        }

        public void SetBrightness(int brightness, List<string> ips = null) {
            AddNonExistingDevices(ips);

            List<string> ipsToUse = ips ?? _devices.Keys.ToList();
            foreach(var ip in ipsToUse) {
                _devices[ip].SetBrightness(brightness);
            }
        }

        public void SetColor(Color color, List<string> ips = null) {
            if(color == null)
                throw new ArgumentNullException(nameof(color));

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

    }
}
