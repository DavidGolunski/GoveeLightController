using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.IO;
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


        private CancellationTokenSource _cancellationTokenSource;
        public bool IsRunning { get => _cancellationTokenSource != null; }

        public LeagueEffectManager() {}

        private Dictionary<string, List<ScriptCommand>> actionDict;

        // Ensure resources are released
        ~LeagueEffectManager() {
            Stop();
        }


        // starts the thread that reads the League API and controls the lights
        public void Start(List<string> deviceIpList) {
            if(IsRunning)
                return;

            _cancellationTokenSource = new CancellationTokenSource();

            actionDict = GetActionDict();
            LeagueAPI.Instance.Reset();

            Logger.Instance.LogMessage(TracingLevel.INFO, "Started League Effects Manager Task");

            Task.Run(async () =>
            {
                try {
                    while(!_cancellationTokenSource.Token.IsCancellationRequested) {
                        Update(deviceIpList);
                        await Task.Delay(100, _cancellationTokenSource.Token); // Wait for 0.1 second
                    }
                }
                catch(TaskCanceledException) {
                    // Task was cancelled, which is expected during Stop
                }
            }, _cancellationTokenSource.Token);
        }

        // Stops the thread
        public void Stop() {
            if(!IsRunning) return;

            _cancellationTokenSource.Cancel(); // Signal the task to stop
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            LeagueAPI.Instance.Reset();
            Logger.Instance.LogMessage(TracingLevel.INFO, "League Manager Task has been stopped successfully");
        }

        // update function called by the thread every 0.1 seconds until the thread is closed
        private void Update(List<string> deviceIpList) {
            if(!LeagueAPI.Instance.IsInGame())
                return;

            var leagueEvent = LeagueAPI.Instance.GetEvent();
            if(leagueEvent == LeagueEventTypes.NO_EVENT)
                return;

            Logger.Instance.LogMessage(TracingLevel.INFO, "League Event found: " + leagueEvent.ToString());

            if(!actionDict.ContainsKey(leagueEvent.ToString()))
                return;

            List<ScriptCommand> currentAction = actionDict[leagueEvent.ToString()];
            
            Console.WriteLine("LeagueEvent animation starting: " + leagueEvent.ToString());
            ScriptCommand.StartScriptAction(currentAction, deviceIpList);
        }


        private Dictionary<string, List<ScriptCommand>> GetActionDict() {
            Dictionary<string, List<ScriptCommand>> actions = new Dictionary<string, List<ScriptCommand>>();

            List<string> actionNames = ScriptCommand.GetListOfActions();
            foreach(var actionName in actionNames) {
                if(!actionName.StartsWith("LOL@"))
                    continue;

                string actionNameWithoutLol = actionName.Remove(0, 4);
                actions.Add(actionNameWithoutLol, ScriptCommand.GetAction(actionName));
            }
            
            return actions;
        }
    }
}
