using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Text;

namespace GoveeLightController {
    public class LeagueEffectManager {

        private static LeagueEffectManager instance;
        public static LeagueEffectManager Instance {
            get => instance ?? (instance = new LeagueEffectManager());
            private set => instance = value;
        }

        private Thread updateThread;
        public bool isRunning { get; private set; }

        public LeagueEffectManager() {}

        private Dictionary<string, List<ScriptCommand>> actionDict;

        // Ensure resources are released
        ~LeagueEffectManager() {
            Stop();
        }


        public void Start(List<string> deviceIpList) {
            if (isRunning) return;
            isRunning = true;

            actionDict = GetActionDict();

            //ToDo: Read all league Effects from file

            Logger.Instance.LogMessage(TracingLevel.INFO, "Started Thread");
            updateThread = new Thread(() => {
                while(isRunning) {
                    Update(deviceIpList);
                    Thread.Sleep(100); // Wait for 0.1 second
                }
            }) {
                IsBackground = true // Ensure the thread does not block application exit
            };

            updateThread.Start();
        }

        // Stops the thread
        public void Stop() {
            if(!isRunning) return;
       
            isRunning = false;
            if(updateThread != null && updateThread.IsAlive) {
                updateThread.Join(); // Wait for the thread to terminate
                updateThread = null;
                LeagueAPI.Instance.Reset();
                Logger.Instance.LogMessage(TracingLevel.INFO, "League Manager Thread has been stopped successfully");
            }
        }


        private void Update(List<string> deviceIpList) {
            if(!LeagueAPI.Instance.IsInGame())
                return;

            var leagueEvent = LeagueAPI.Instance.GetEvent();
            if(leagueEvent == LeagueEventTypes.NO_EVENT)
                return;

            if(!actionDict.ContainsKey(leagueEvent.ToString()))
                return;
            List<ScriptCommand> currentAction = actionDict[leagueEvent.ToString()];
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "LeagueEvent animation starting: " + leagueEvent.ToString());
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
