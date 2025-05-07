using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GoveeLightController {
    public class LeagueEffectManager {

        /*
         * A class that creates a seperate Thread to control GoveeLights based on
         * League of Legends game Events. The Class LeagueAPI is providing the Events
         */

        private static LeagueEffectManager instance;
        public static LeagueEffectManager Instance {
            get => instance ??= new LeagueEffectManager();
            private set => instance = value;
        }


        private CancellationTokenSource cancellationTokenSource;
        public bool IsRunning { get => cancellationTokenSource != null; }

        public LeagueEffectManager() {}

        // Ensure resources are released
        ~LeagueEffectManager() {
            Stop();
        }


        // starts the thread that reads the League API and controls the lights
        public void Start(List<string> deviceIpList) {
            if(IsRunning)
                return;

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            ScriptCommand.ClearActionCache();
            LeagueAPI.Instance.Reset();

            Logger.Instance.LogMessage(TracingLevel.INFO, "Started League Effects Manager Task");

            Task.Run(async () =>
            {
                try {
                    while(!cancellationToken.IsCancellationRequested) {
                        Update(deviceIpList);
                        await Task.Delay(100, cancellationToken); // Wait for 0.1 second
                    }
                }
                catch(TaskCanceledException) {
                    // Task was cancelled, which is expected during Stop
                }
                catch(Exception ex) {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, ex.ToString());
                }
            }, cancellationToken);
        }

        // Stops the thread
        public void Stop() {
            if(!IsRunning) return;

            cancellationTokenSource.Cancel(); // Signal the task to stop
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
            LeagueAPI.Instance.Reset();
            Logger.Instance.LogMessage(TracingLevel.INFO, "League Manager Task has been stopped successfully");
        }

        // update function called by the thread every 0.1 seconds until the thread is closed
        private void Update(List<string> deviceIpList) {
            LeagueAPI.Instance.RetrieveData();
            if(!LeagueAPI.Instance.IsInGame())
                return;

            var leagueEvent = LeagueAPI.Instance.GetEvent();
            if(leagueEvent == LeagueEventTypes.NO_EVENT)
                return;

            Logger.Instance.LogMessage(TracingLevel.INFO, "League Event found: " + leagueEvent.ToString());
            bool successfull = ScriptCommand.StartScriptAction("LOL@" + leagueEvent.ToString(), deviceIpList);
            if(!successfull) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "No effect found called: LOL@" + leagueEvent.ToString());
            }
        }
    }
}
