using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;

namespace GoveeLightController {
    public class LeagueEffectManager {

        private static LeagueEffectManager instance;
        public static LeagueEffectManager Instance {
            get => instance ?? (instance = new LeagueEffectManager());
            private set => instance = value;
        }


        private LeagueAPI leagueAPI;
        private Thread updateThread;
        public bool isRunning { get; private set; }

        public LeagueEffectManager() {
            leagueAPI = new LeagueAPI();
        }

        // Ensure resources are released
        ~LeagueEffectManager() {
            Stop();
        }


        public void Start(List<string> deviceIpList) {
            if (isRunning) return;
            isRunning = true;

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
                leagueAPI.Reset();
                Logger.Instance.LogMessage(TracingLevel.INFO, "League Manager Thread has been stopped successfully");
            }
        }


        private void Update(List<string> deviceIpList) {
            if(!leagueAPI.IsInGame()) {
                return;
            }

            var leagueEvent = leagueAPI.GetEvent();
            var isDead = leagueAPI.IsDead();

            switch(leagueEvent) {
                case LeagueEventTypes.NO_EVENT:
                    return;

                case LeagueEventTypes.GAME_STARTED:
                    Logger.Instance.LogMessage(TracingLevel.INFO, "League Game Started");

                    var primaryRuneTree = leagueAPI.GetPrimaryRuneTree();
                    if(primaryRuneTree != null) {
                        switch(primaryRuneTree) {
                            case RuneTreeTypes.DOMINATION:
                                GoveeDeviceController.Instance.SetPrimaryColor(Color.Red);
                                break;
                            case RuneTreeTypes.INSPIRATION:
                                GoveeDeviceController.Instance.SetPrimaryColor(Color.Aqua);
                                break;
                            case RuneTreeTypes.RESOLVE:
                                GoveeDeviceController.Instance.SetPrimaryColor(Color.Green);
                                break;
                            case RuneTreeTypes.SORCERY:
                                GoveeDeviceController.Instance.SetPrimaryColor(Color.Blue);
                                break;
                            case RuneTreeTypes.PRECISION:
                                GoveeDeviceController.Instance.SetPrimaryColor(Color.Yellow);
                                break;
                        }
                    }

                    GoveeDeviceController.Instance.TurnOn(deviceIpList);
                    Task.Delay(100);
                    GoveeDeviceController.Instance.ActivatePrimaryColor(deviceIpList);
                    break;

                case LeagueEventTypes.HAS_KILLED:
                    GoveeDeviceController.Instance.Pulse(Color.Green, 1, 2, 2, isDead, true);
                    break;

                case LeagueEventTypes.HAS_PENTAKILLED:
                    GoveeDeviceController.Instance.Pulse(Color.Green, 5, 0.5, 0.5, isDead, true);
                    break;

                case LeagueEventTypes.HAS_ASSISTED:
                    GoveeDeviceController.Instance.Pulse(Color.Green, 1, 1, 1, isDead, true);
                    break;

                case LeagueEventTypes.HAS_DIED:
                    GoveeDeviceController.Instance.Pulse(Color.Red, 1, 2, 2, true, true);
                    break;

                case LeagueEventTypes.HAS_REVIVED:
                    GoveeDeviceController.Instance.TurnOn(deviceIpList);
                    break;

                case LeagueEventTypes.HAS_KILLED_TURRET:
                    GoveeDeviceController.Instance.Pulse(Color.Green, 1, 4, 4, isDead, true);
                    break;

                case LeagueEventTypes.HAS_ASSISTED_TURRET:
                    GoveeDeviceController.Instance.Pulse(Color.Green, 1, 4, 2, isDead, true);
                    break;

                case LeagueEventTypes.GAME_WON:
                    GoveeDeviceController.Instance.Pulse(Color.Green, 5, 0.4, 0.4, true, false);
                    break;

                case LeagueEventTypes.GAME_LOST:
                    GoveeDeviceController.Instance.Pulse(Color.Red, 5, 0.4, 0.4, true, false);
                    break;

                case LeagueEventTypes.VOID_GRUBS_KILLED:
                    GoveeDeviceController.Instance.Pulse(Color.Purple, 1, 1, 1, isDead, true);
                    break;

                case LeagueEventTypes.HERALD_KILLED:
                    GoveeDeviceController.Instance.Pulse(Color.Purple, 1, 3, 3, isDead, true);
                    break;

                case LeagueEventTypes.BARON_KILLED:
                    GoveeDeviceController.Instance.Pulse(Color.Purple, 1, 5, 5, isDead, true);
                    break;

                case LeagueEventTypes.AIR_DRAGON_KILLED:
                    GoveeDeviceController.Instance.Pulse(Color.White, 1, 3, 3, isDead, true);
                    break;

                case LeagueEventTypes.FIRE_DRAGON_KILLED:
                    GoveeDeviceController.Instance.Pulse(Color.Orange, 1, 3, 3, isDead, true);
                    break;

                case LeagueEventTypes.WATER_DRAGON_KILLED:
                    GoveeDeviceController.Instance.Pulse(Color.Blue, 1, 3, 3, isDead, true);
                    break;

                case LeagueEventTypes.EARTH_DRAGON_KILLED:
                    GoveeDeviceController.Instance.Pulse(Color.Brown, 1, 3, 3, isDead, true);
                    break;

                case LeagueEventTypes.HEXTECH_DRAGON_KILLED:
                    GoveeDeviceController.Instance.Pulse(Color.Aqua, 1, 3, 3, isDead, true);
                    break;

                case LeagueEventTypes.CHEMTECH_DRAGON_KILLED:
                    GoveeDeviceController.Instance.Pulse(Color.Green, 1, 3, 3, isDead, true);
                    break;

                case LeagueEventTypes.ELDER_DRAGON_KILLED:
                    GoveeDeviceController.Instance.Pulse(Color.White, 2, 2, 2, isDead, true);
                    break;

                default:
                    break;
            }
        }
    }
}
