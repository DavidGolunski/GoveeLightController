using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoveeLightController.Actions {
    [PluginActionId("com.davidgolunski.goveelightcontroller.scriptaction")]

    public class ScriptAction : KeypadBase {

        private DeviceListSettings localSettings;
        private DeviceListSettings globalSettings;

        public ScriptAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.localSettings = new DeviceListSettings();
                SaveSettings();
            }
            else {
                this.localSettings = payload.Settings.ToObject<DeviceListSettings>();
            }
            this.globalSettings = new DeviceListSettings();
            GlobalSettingsManager.Instance.RequestGlobalSettings();
            Connection.OnPropertyInspectorDidAppear += OnPropertyInspectorOpened;
            Connection.SetStateAsync(0).GetAwaiter().GetResult();
        }

        public override void Dispose() {
            Connection.OnPropertyInspectorDidAppear -= OnPropertyInspectorOpened;
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"TurnOnAction: Destructor called");
        }

        public override void KeyPressed(KeyPayload payload) {
            List<string> fileNames = ScriptCommand.GetScriptFileNames();
            if(fileNames.Count == 0) {
                Logger.Instance.LogMessage(TracingLevel.INFO, "No Files found");
                return;
            }
            foreach(string fileName in fileNames) {
                Logger.Instance.LogMessage(TracingLevel.INFO, "FileName: " + fileName);
            }

            List<string> actionNames = ScriptCommand.GetListOfActions();
            if(actionNames.Count == 0) {
                Logger.Instance.LogMessage(TracingLevel.INFO, "No Actions found");
                return;
            }

            foreach(string actionName in actionNames) {
                Logger.Instance.LogMessage(TracingLevel.INFO, "ActionName: " + actionName);
            }

            List<ScriptCommand> commands = ScriptCommand.GetAction("GAME_WON");
            if(commands == null || commands.Count == 0) {
                Logger.Instance.LogMessage(TracingLevel.INFO, "The action \"GAME_WON\" was not found");
                return;
            }
            foreach(var command in commands) {
                Logger.Instance.LogMessage(TracingLevel.INFO, command.ToString());
            }

            ScriptCommand.StartScriptAction("GAME_WON", globalSettings.deviceIpList);



        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            Tools.AutoPopulateSettings(localSettings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) {
            Tools.AutoPopulateSettings(globalSettings, payload.Settings);
        }

        #region Private Methods

        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(localSettings));
        }

        private void OnPropertyInspectorOpened(object sender, SDEventReceivedEventArgs<PropertyInspectorDidAppear> e) {
            Connection.SetSettingsAsync(JObject.FromObject(localSettings));
        }

        #endregion
    }
}
