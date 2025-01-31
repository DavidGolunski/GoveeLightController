using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GoveeLightController {
    internal class Program {

        static void Main(string[] args) {
            // Uncomment this line of code to allow for debugging
            //while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }

            bool runningStandalone = false;
            runningStandalone |= RunStandaloneLeagueEffects(args);
            runningStandalone |= RunStandaloneCounterStrikeEffects(args);


            if(runningStandalone) {

                Console.WriteLine("Write \"stop\" (without the quotation marks) to stop the program");
                while(true) {
                    string userInput = Console.ReadLine();

                    if(userInput?.ToLower() == "stop") {
                        break; // Exit the loop
                    }
                    else {
                        Console.WriteLine("Write \"stop\" (without the quotation marks) to stop the program");
                    }
                }

                ShutDown();
                return;
            }
            // only call the StreamDeck Wrapper function if the arguemnts are not specifically for a standalone version
            SDWrapper.Run(args);
        }


        // to check if a given IP is valid. If an invalid IP is provided the program would crash
        private static bool IsValidIP(string ip) {
            // Regex to match a valid IPv4 address
            string pattern = @"^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\." +
                             @"(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\." +
                             @"(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\." +
                             @"(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";

            return Regex.IsMatch(ip, pattern);
        }

        private static void ShutDown() {
            Console.WriteLine("Exiting the program...");
            LeagueEffectManager.Instance.Stop();
            GoveeDeviceController.Instance.TurnOff();
        }




        /*
         * Valid if the first argument is "LeagueOfLegendsEffects" and all later arguments are ip adresses.
         */
        static bool RunStandaloneLeagueEffects(string[] args) {
            if(args.Length < 1)
                return false;

            if(args[0] != "LeagueOfLegendsEffects")
                return false;

            List<string> deviceIpList = new List<string>();

            // if the input is "0.0.0.0"  we want to look for devices in the network automatically
            if(args.Length == 1 || args[1] == "0.0.0.0") {
                Dictionary<string, GoveeDevice> devices = GoveeDevice.GetDevices(1000);

                if(devices.Count == 0) {
                    Console.WriteLine("No Govee Devices have been found. Make sure LAN Control is enabled using the GoveeApp.\n" +
                        "You can also add the IP of the devices manually as a startup argument."
                        );
                    return true;
                }

                foreach(string deviceIp in devices.Keys) { 
                    deviceIpList.Add(deviceIp);
                    Console.WriteLine("Found Device with IP: " + deviceIp);
                }
            }
            // otherwise try to get ip addresses from the arguments
            else {
                for(int i = 1; i < args.Length; i++) {
                    string deviceIp = args[i];
                    if(IsValidIP(deviceIp)) {
                        deviceIpList.Add(deviceIp);
                        Console.WriteLine("Device with IP \"" + deviceIp + "\" was added successfully.");
                    }
                    else {
                        Console.WriteLine("Argument number " + i + " was not a valid IP adress (" + deviceIp + "). Skipping this argument");
                    }
                }
            }

            if(deviceIpList.Count == 0) {
                Console.WriteLine("No valid IP adresses where given. Terminating program.");
                return true;
            }

            Console.WriteLine("Starting League Effects with " + deviceIpList.Count + " devices.\n");
            
            LeagueEffectManager.Instance.Start(deviceIpList);


            return true;
        }

        /*
         * Valid if the first argument is "CounterStrikeEffects" and all later arguments are ip adresses.
         */
        static bool RunStandaloneCounterStrikeEffects(string[] args) {
            if(args.Length < 1)
                return false;

            if(args[0] != "CounterStrikeEffects")
                return false;

            List<string> deviceIpList = new List<string>();

            // if no ips are given, then try to find govee devices inside the network
            if(args.Length == 1) {
                Dictionary<string, GoveeDevice> devices = GoveeDevice.GetDevices(2000);

                if(devices.Count == 0) {
                    Console.WriteLine("No Govee Devices have been found. Make sure LAN Control is enabled using the GoveeApp.\n" +
                        "You can also add the IP of the devices manually as a startup argument."
                        );
                    return true;
                }

                foreach(string deviceIp in devices.Keys) {
                    deviceIpList.Add(deviceIp);
                    Console.WriteLine("Found Device with IP: " + deviceIp);
                }
            }
            // otherwise try to get ip addresses from the arguments
            else {
                for(int i = 1; i < args.Length; i++) {
                    string deviceIp = args[i];
                    if(IsValidIP(deviceIp)) {
                        deviceIpList.Add(deviceIp);
                        Console.WriteLine("Device with IP \"" + deviceIp + "\" was added successfully.");
                    }
                    else {
                        Console.WriteLine("Argument number " + i + " was not a valid IP adress (" + deviceIp + "). Skipping this argument");
                    }
                }
            }

            if(deviceIpList.Count == 0) {
                Console.WriteLine("No valid IP adresses where given. Terminating program.");
                return true;
            }

            Console.WriteLine("Starting Counter Strike Effects with " + deviceIpList.Count + " devices.\n");

            CounterStrikeEffectsManager.Instance.Start(deviceIpList);


            return true;
        }
    }
}
