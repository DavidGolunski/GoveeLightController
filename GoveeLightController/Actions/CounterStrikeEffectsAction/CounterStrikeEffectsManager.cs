using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.IO;
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
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "cfgPath: " + cfgPath);
            if(cfgPath != null) {
                bool successfullCopy = CopyConfigurationFile("gamestate_integration_goveelightcontroller.cfg", cfgPath);
                if(successfullCopy) {
                    Logger.Instance.LogMessage(TracingLevel.INFO, "Copying of configuration file successfull");
                }
                else {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "Copying of configuration was not file successfull. Please copy the file \\\"gamestate_integration_goveelightcontroller.cfg\\\" to \\\"[Your Steam Folder]/steamapps/common/Counter-Strike Global Offensive/game/csgo/cfg\\\"\"");
                }
            }
            else {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Could not find the CounterStrike configuration file folder. Please copy the file \"gamestate_integration_goveelightcontroller.cfg\" to \"[Your Steam Folder]/steamapps/common/Counter-Strike Global Offensive/game/csgo/cfg\"");
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            CounterStrikeAPI.Instance.Reset();

            Logger.Instance.LogMessage(TracingLevel.INFO, "Started Counter Strike Effects Manager Task");
            // run the script that should run, when the cs integration starts
            bool successfull = ScriptCommand.StartScriptAction("CS@INTEGRATION_STARTED", deviceIpList);
            if(!successfull) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "No effect found called: CS@INTEGRATION_STARTED");
            }

            Task.Run(async () =>
            {
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

            if(!updateSuccessfull) {
                return;
            }

            var csEvent = CounterStrikeAPI.Instance.GetEvent();
           

            if(csEvent == CsEventTypes.NO_EVENT)
                return;

            Logger.Instance.LogMessage(TracingLevel.INFO, "CounterStrike Event Found: " + csEvent.ToString());

            bool successfull = ScriptCommand.StartScriptAction("CS@" + csEvent.ToString(), deviceIpList);
            if(!successfull) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "No effect found called: CS@" + csEvent.ToString());
            }
        }

        #region configuration file copying

        private static string GetConfigurationFileTargetLocation() {
            const string steamRegistryKey = @"HKEY_CURRENT_USER\Software\Valve\Steam";
            string steamInstallationPath = (string) Microsoft.Win32.Registry.GetValue(steamRegistryKey, "SteamPath", null);

            if(steamInstallationPath == null)
                return null;

            string[] uncombinedPaths = new string[] { steamInstallationPath, "steamapps", "common", "Counter-Strike Global Offensive", "game", "csgo", "cfg" };
            string combinedPath = Path.Combine(uncombinedPaths);

            if(!Directory.Exists(combinedPath))
                return null;

            return combinedPath;
        }

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
