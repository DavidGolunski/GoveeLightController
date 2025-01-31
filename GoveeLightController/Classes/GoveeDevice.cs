using BarRaider.SdTools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel.Channels;
using System.ServiceModel.MsmqIntegration;
using System.Text;
using System.Drawing;

namespace GoveeLightController {


    public class GoveeDevice {
        public string Ip { get; }
        public string DeviceId { get; }
        public string Sku { get; }
        public Color CurrentColor { get; private set; } = Color.Black; // Placeholder for color tracking
        public int CurrentBrightness { get; private set; } = 100;
        private readonly UdpClient _udpClient;

        public GoveeDevice(string ip, string deviceId, string sku) {
            Ip = ip;
            DeviceId = deviceId;
            Sku = sku;
            _udpClient = new UdpClient { Client = { ReceiveTimeout = 2000 } }; // Timeout for operations
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Device Created: {ip} {sku} {deviceId}");
        }

        ~GoveeDevice() {
            // Ensure the socket is closed when the object is deleted
            _udpClient?.Close();
        }

        private void SendMessage(Dictionary<string, object> message) {
            try {
                var jsonMessage = JsonConvert.SerializeObject(message);
                byte[] bytes = Encoding.UTF8.GetBytes(jsonMessage);
                _udpClient.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Parse(Ip), 4003));
            }
            catch(Exception ex) {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Error sending message to {Ip}: {ex.Message}");
            }
        }

        public void TurnOn() {
            var message = new Dictionary<string, object>
            {
                { "msg", new { cmd = "turn", data = new { value = 1 } } }
            };
            //Logger.Instance.LogMessage(TracingLevel.DEBUG, "Turning On: " + Ip);
            SendMessage(message);
        }

        public void TurnOff() {
            var message = new Dictionary<string, object>
            {
                { "msg", new { cmd = "turn", data = new { value = 0 } } }
            };
            //Logger.Instance.LogMessage(TracingLevel.DEBUG, "Turning Off: " + Ip);
            SendMessage(message);
        }

        public void SetBrightness(int brightness) {
            if(brightness < 0 || brightness > 100)
                throw new ArgumentOutOfRangeException(nameof(brightness), "Brightness must be between 0 and 100.");

            var message = new Dictionary<string, object>
            {
            { "msg", new { cmd = "brightness", data = new { value = brightness } } }
        };
            SendMessage(message);
            CurrentBrightness = brightness;
            //Logger.Instance.LogMessage(TracingLevel.DEBUG, "Setting Brightness to " + CurrentBrightness + ": " + Ip);
        } 

        public void SetColor(Color color) {
            if(color == null)
                throw new ArgumentNullException(nameof(color));

            var message = new Dictionary<string, object>
            {
            {
                "msg", new
                {
                    cmd = "colorwc",
                    data = new
                    {
                        color = new { r = color.R, g = color.G, b = color.B },
                        colorTemInKelvin = 0
                    }
                }
            }
        };
            SendMessage(message);
            CurrentColor = color;
            //Logger.Instance.LogMessage(TracingLevel.DEBUG, "Setting Color to " + CurrentColor.ToString() + ": " + Ip);
        }



        #region static functions

        public static Dictionary<string, GoveeDevice> GetDevices(int timeout) {
            var devices = new Dictionary<string, GoveeDevice>();

            // Get the device's local IP address assigned by the router
            string localIP = GetLocalIPAddress();
            if(localIP == null) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Unable to determine local IP address.");
                return devices;
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Using local IP: {localIP} to find devices");


            var requestScanMsg = new Dictionary<string, object>
            {
            { "msg", new { cmd = "scan", data = new { account_topic = "reverse" } } }
            };
            var jsonMessage = JsonConvert.SerializeObject(requestScanMsg);
            byte[] bytes = Encoding.UTF8.GetBytes(jsonMessage);

            try {
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(localIP), 0);

                using(UdpClient udpSender = new UdpClient(localEndPoint))
                using(UdpClient udpReceiver = new UdpClient(4002)) {
                    // Send broadcast message
                    IPEndPoint sendEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 4001);
                    udpSender.Send(bytes, bytes.Length, sendEndPoint);


                    // receive messages from devices
                    udpReceiver.Client.ReceiveTimeout = timeout;
                    while(true) {
                        try {
                            IPEndPoint remoteEP = null;
                            byte[] data = udpReceiver.Receive(ref remoteEP);
                            string receivedMessage = Encoding.UTF8.GetString(data);

                            dynamic response = JsonConvert.DeserializeObject(receivedMessage);

                            string ip = response?.msg?.data?.ip;
                            string device = response?.msg?.data?.device;
                            string sku = response?.msg?.data?.sku;

                            if(ip != null  && device != null && sku != null) {
                                GoveeDevice foundDevice = new GoveeDevice(ip, device, sku);
                                devices.Add(ip, foundDevice);
                            }
                        }
                        catch(SocketException) {
                            break; // Timeout reached, end discovery
                        }
                    }
                }
 
            }
            catch(Exception ex) {
                Logger.Instance.LogMessage(TracingLevel.INFO, "Device Discovering Exception: " + ex.StackTrace);
            }

            return devices;
        }

        private static string GetLocalIPAddress() {
            foreach(var netInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()) {
                if(netInterface.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                    netInterface.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback) {
                    foreach(var address in netInterface.GetIPProperties().UnicastAddresses) {
                        if(address.Address.AddressFamily == AddressFamily.InterNetwork) {
                            return address.Address.ToString();
                        }
                    }
                }
            }
            return null;
        }

        #endregion
    }

}
