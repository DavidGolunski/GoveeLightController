using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace GoveeLightController {
    public class CounterStrikeEffectsManager {

        /*
         * A class that creates a seperate Thread to control GoveeLights based on
         * League of Legends game Events. The Class LeagueAPI is providing the Events
         */

        private static CounterStrikeEffectsManager instance;
        public static CounterStrikeEffectsManager Instance {
            get => instance ??= new CounterStrikeEffectsManager();
            private set => instance = value;
        }


        private CancellationTokenSource cancellationTokenSource;
        public bool IsRunning { get => cancellationTokenSource != null; }

        public CounterStrikeEffectsManager() { }

        // Ensure resources are released
        ~CounterStrikeEffectsManager() {
            Stop();
        }


        // starts the thread that reads the League API and controls the lights
        public void Start(List<string> deviceIpList) {
            if(IsRunning)
                return;

            ScriptCommand.ClearActionCache();

            string cfgPath = GetConfigurationFileTargetLocation();
            if(cfgPath == null) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Could not find the CounterStrike configuration file folder. Please copy the file \"gamestate_integration_goveelightcontroller.cfg\" to \"[Your Steam Folder]/steamapps/common/Counter-Strike Global Offensive/game/csgo/cfg\"");
            }
            else {
                if(CopyConfigurationFile("gamestate_integration_goveelightcontroller.cfg", cfgPath))
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Successfully copied the configuration file to \"{cfgPath}\"");
                else Logger.Instance.LogMessage(TracingLevel.ERROR, "Copying of configuration was not file successfull. Please copy the file \\\"gamestate_integration_goveelightcontroller.cfg\\\" to \\\"[Your Steam Folder]/steamapps/common/Counter-Strike Global Offensive/game/csgo/cfg\\\"\"");

            }


            // Delete GameStateLog file if it exists
            //if(File.Exists("GameStateLog.txt"))
            //   File.Delete("GameStateLog.txt");

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            CounterStrikeAPI.Instance.Reset();

            Logger.Instance.LogMessage(TracingLevel.INFO, "Started Counter Strike Effects Manager Task");
            // run the script that should run, when the cs integration starts
            Logger.Instance.LogMessage(TracingLevel.INFO, "CounterStrike Event detected: INTEGRATION_STARTED");
            bool successfull = ScriptCommand.StartScriptAction("CS@INTEGRATION_STARTED", deviceIpList);
            if(!successfull) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "No effect found called: CS@INTEGRATION_STARTED");
            }

            Task.Run(async () => {
                try {
                    CounterStrikeAPI.Instance.StartListening();

                    while(!cancellationToken.IsCancellationRequested) {
                        Update(deviceIpList);
                        await Task.Delay(10, cancellationToken); // Wait for 0.01 seconds. The CS Buffer is set to 0.05, so we should not miss any updates. The update method will wait anyways on the next update
                    }
                }
                catch(TaskCanceledException) {
                    // Task was cancelled, which is expected during Stop
                }
                catch(Exception ex) {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, ex.ToString());
                }
            });
        }

        // Stops the thread
        public void Stop(List<string> deviceIpList = null) {
            if(!IsRunning)
                return;

            CounterStrikeAPI.Instance.StopListening();

            if(cancellationTokenSource != null) {
                cancellationTokenSource.Cancel(); // Signal the task to stop
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
            
            if(deviceIpList != null) {
                // run the script that should run, when the cs integration stops
                bool successfull = ScriptCommand.StartScriptAction("CS@INTEGRATION_STOPPED", deviceIpList);
                Logger.Instance.LogMessage(TracingLevel.INFO, "CounterStrike Event detected: INTEGRATION_STOPPED");
                if(!successfull) {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "No effect found called: CS@INTEGRATION_STOPPED");
                }
            }
            

            CounterStrikeAPI.Instance.Reset();
            Logger.Instance.LogMessage(TracingLevel.INFO, "Counter Strike Effects Manager Task has been stopped successfully");
        }

        // update function called by the thread every 0.1 seconds until the thread is closed
        private void Update(List<string> deviceIpList) {

            bool updateSuccessfull = CounterStrikeAPI.Instance.WaitForUpdate(); 
            if(!updateSuccessfull) 
                return;
            
            var csEvent = CounterStrikeAPI.Instance.GetEvent(); 
            if(csEvent == CsEventTypes.NO_EVENT)
                return;

            Logger.Instance.LogMessage(TracingLevel.INFO, "CounterStrike Event detected: " + csEvent.ToString());

            bool successfull = ScriptCommand.StartScriptAction("CS@" + csEvent.ToString(), deviceIpList);
            if(!successfull) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "No effect found called: CS@" + csEvent.ToString());
            }
        }

        #region configuration file copying
        

        private static string GetConfigurationFileTargetLocation() {
            // Read out the default steam installation path
            const string steamRegistryKey = @"HKEY_CURRENT_USER\Software\Valve\Steam";
            string steamInstallationPath = (string) Microsoft.Win32.Registry.GetValue(steamRegistryKey, "SteamPath", null);

            if(steamInstallationPath == null)
                return null;

            // the library folders file contains information on the path to all installed steam games 
            string libraryFoldersFilePath = Path.Combine(steamInstallationPath, "steamapps", "libraryfolders.vdf");
            
            if(!File.Exists(libraryFoldersFilePath)) 
                return null;
            

            // start reading the library file
            using var reader = new StreamReader(libraryFoldersFilePath);

            string currentPath = null;
            bool insideApps = false;

            while (!reader.EndOfStream) {
                string line = reader.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // Detect a "path" entry
                if (line.StartsWith("\"path\"")) {
                    // Example line: "path"      "D:\\Program Files (x86)\\Steam"
                    int firstQuote = line.IndexOf('"', 6);
                    int secondQuote = line.IndexOf('"', firstQuote + 1);
                    if (firstQuote > 0 && secondQuote > firstQuote)
                        currentPath = line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                }

                // Detect beginning of "apps"
                if (line == "\"apps\"") {
                    insideApps = true;
                    continue;
                }

                // Detect end of "apps" section
                if (insideApps && line == "}") {
                    insideApps = false;
                    currentPath = null;    
                    continue;
                }

                // Inside apps, check if app ID of CounterStrike exists
                if (insideApps && line.StartsWith("\"730\"")) {
                    if(currentPath == null) {
                        Logger.Instance.LogMessage(TracingLevel.DEBUG, "Found AppID 730, but did not find a path");
                        return null;
                    }
                    break;
                }
            }
            if(currentPath == null)
                return null;

            string[] uncombinedPaths = new string[] { currentPath, "steamapps", "common", "Counter-Strike Global Offensive", "game", "csgo", "cfg" };
            string combinedPath = Path.Combine(uncombinedPaths);

            if(!Directory.Exists(combinedPath)) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Expected to find the folder \"{combinedPath}\", but it did not exist.");
                return null;
            }
            return combinedPath;
        }

        // copies a file to the target folder
        private static bool CopyConfigurationFile(string configurationFileLocation, string targetFolder) {
            if(string.IsNullOrEmpty(configurationFileLocation) || string.IsNullOrEmpty(targetFolder))
                return false;
            
            if(!File.Exists(configurationFileLocation) || !Directory.Exists(targetFolder)) {
                return false;
            }

            try {
                string targetFilePath = Path.Combine(targetFolder, Path.GetFileName(configurationFileLocation));
                File.Copy(configurationFileLocation, targetFilePath, overwrite: true);
            }
            catch(Exception) {
                return false;
            }

            return true;
        }

        #endregion
    }
}
