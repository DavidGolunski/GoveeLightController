using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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


        private CancellationTokenSource _cancellationTokenSource;
        public bool IsRunning { get => _cancellationTokenSource != null; }

        public CounterStrikeEffectsManager() { }

        private Dictionary<string, List<ScriptCommand>> actionDict;

        // Ensure resources are released
        ~CounterStrikeEffectsManager() {
            Stop();
        }


        // starts the thread that reads the League API and controls the lights
        public void Start(List<string> deviceIpList) {
            if(IsRunning)
                return;


            string cfgPath = GetConfigurationFileTargetLocation();
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "cfgPath: " + cfgPath);
            if(cfgPath != null) {
                bool successfull = CopyConfigurationFile("gamestate_integration_goveelightcontroller.cfg", cfgPath);
                if(successfull) {
                    Console.WriteLine("Copying of configuration file successfull");
                    Logger.Instance.LogMessage(TracingLevel.INFO, "Copying of configuration file successfull");
                }
                else {
                    Console.WriteLine("Copying of configuration was not file successfull. Please copy the file \\\"gamestate_integration_goveelightcontroller.cfg\\\" to \\\"[Your Steam Folder]/steamapps/common/Counter-Strike Global Offensive/game/csgo/cfg\\\"\"");
                    Logger.Instance.LogMessage(TracingLevel.WARN, "Copying of configuration was not file successfull. Please copy the file \\\"gamestate_integration_goveelightcontroller.cfg\\\" to \\\"[Your Steam Folder]/steamapps/common/Counter-Strike Global Offensive/game/csgo/cfg\\\"\"");
                }
            }
            else {
                Console.WriteLine("Could not find the CounterStrike configuration file folder. Please copy the file \"gamestate_integration_goveelightcontroller.cfg\" to \"[Your Steam Folder]/steamapps/common/Counter-Strike Global Offensive/game/csgo/cfg\"");
                Logger.Instance.LogMessage(TracingLevel.WARN, "Could not find the CounterStrike configuration file folder. Please copy the file \"gamestate_integration_goveelightcontroller.cfg\" to \"[Your Steam Folder]/steamapps/common/Counter-Strike Global Offensive/game/csgo/cfg\"");
            }

            _cancellationTokenSource = new CancellationTokenSource();

            actionDict = GetActionDict();
            CounterStrikeAPI.Instance.Reset();

            Logger.Instance.LogMessage(TracingLevel.INFO, "Started Counter Strike Effects Manager Task");

            Task.Run(async () =>
            {
                try {
                    CounterStrikeAPI.Instance.StartListening();

                    while(!_cancellationTokenSource.Token.IsCancellationRequested) {
                        Update(deviceIpList);
                        await Task.Delay(10, _cancellationTokenSource.Token); // Wait for 0.01 seconds. The CS Buffer is set to 0.05, so we should not miss any updates. The update method will wait anyways on the next update
                    }
                }
                catch(TaskCanceledException) {
                    // Task was cancelled, which is expected during Stop
                }
                catch(Exception ex) {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, ex.ToString());
                }
            }, _cancellationTokenSource.Token);
        }

        // Stops the thread
        public void Stop() {
            if(!IsRunning)
                return;

            CounterStrikeAPI.Instance.StopListening();

            _cancellationTokenSource.Cancel(); // Signal the task to stop
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;

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

            if(!actionDict.ContainsKey(csEvent.ToString()))
                return;

            List<ScriptCommand> currentAction = actionDict[csEvent.ToString()];

            ScriptCommand.StartScriptAction(currentAction, deviceIpList);
        }


        private Dictionary<string, List<ScriptCommand>> GetActionDict() {
            Dictionary<string, List<ScriptCommand>> actions = new Dictionary<string, List<ScriptCommand>>();

            List<string> actionNames = ScriptCommand.GetListOfActions();
            foreach(var actionName in actionNames) {
                if(!actionName.StartsWith("CS@"))
                    continue;

                string actionNameWithoutLol = actionName.Remove(0, 3);
                actions.Add(actionNameWithoutLol, ScriptCommand.GetAction(actionName));
            }

            return actions;
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
