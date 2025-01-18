using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
            get => instance ?? (instance = new CounterStrikeEffectsManager());
            private set => instance = value;
        }


        private Thread updateThread;
        public bool isRunning { get; private set; }

        public CounterStrikeEffectsManager() { }

        private Dictionary<string, List<ScriptCommand>> actionDict;

        // Ensure resources are released
        ~CounterStrikeEffectsManager() {
            Stop();
        }


        // starts the thread that reads the League API and controls the lights
        public void Start(List<string> deviceIpList) {
            if(isRunning)
                return;
            isRunning = true;

            actionDict = GetActionDict();
            CounterStrikeAPI.Instance.Reset();

            Logger.Instance.LogMessage(TracingLevel.INFO, "Started Counter Strike Effects Manager Thread");
            
            updateThread = new Thread(() => {
                try {
                    CounterStrikeAPI.Instance.StartListening();
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, "Listening for Counter Strike Updates");

                    while(isRunning) {
                        Update(deviceIpList);
                        Thread.Sleep(10); // Wait for 0.01 seconds. The CS Buffer is set to 0.05, so we should not miss any updates. The update method will wait anyways on the next update
                    }
                } catch (Exception ex) {
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, ex.ToString());
                }
            });
            updateThread.IsBackground = true;

            updateThread.Start();
        }

        // Stops the thread
        public void Stop() {
            if(!isRunning)
                return;

            isRunning = false;
            CounterStrikeAPI.Instance.StopListening();
            if(updateThread != null && updateThread.IsAlive) {
                updateThread.Join(); // Wait for the thread to terminate
                updateThread = null;
                CounterStrikeAPI.Instance.Reset();
                Logger.Instance.LogMessage(TracingLevel.INFO, "Counter Strike Effects Manager Thread has been stopped successfully");
            }
        }

        // update function called by the thread every 0.1 seconds until the thread is closed
        private void Update(List<string> deviceIpList) {

            bool updateSuccessfull = CounterStrikeAPI.Instance.WaitForUpdate();

            if(!updateSuccessfull) {
                Logger.Instance.LogMessage(TracingLevel.WARN, "CS Update was not successfull");
                return;
            }

            var csEvent = CounterStrikeAPI.Instance.GetEvent();
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Event Found: " + csEvent.ToString());

            if(csEvent == CsEventTypes.NO_EVENT)
                return;


            if(!actionDict.ContainsKey(csEvent.ToString()))
                return;

            List<ScriptCommand> currentAction = actionDict[csEvent.ToString()];

            Logger.Instance.LogMessage(TracingLevel.DEBUG, "CS animation starting: " + csEvent.ToString());
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
    }
}
