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

        private class ActionItem {
            [JsonProperty("text")]
            public string Text { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }

            public ActionItem(string text, string value) {
                Text = text;
                Value = value;
            }
        }

        private class ScriptActionSettings : DeviceListSettings {

            [JsonProperty("actionDropDown")]
            public List<ActionItem> ActionDropDown { get; set; }

            [JsonProperty("selectedAction")]
            public string SelectedAction { get; set; }


            public ScriptActionSettings() : base() {
                ActionDropDown = new List<ActionItem> {
                    new ActionItem("-", "-")
                };
                SelectedAction = "-";
            }
        }


        private ScriptActionSettings localSettings;
        private DeviceListSettings globalSettings;

        public ScriptAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.localSettings = new ScriptActionSettings();
                SaveSettings();
            }
            else {
                this.localSettings = payload.Settings.ToObject<ScriptActionSettings>();
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
            
            string actionString = localSettings.SelectedAction;
            bool actionSuccess = ScriptCommand.StartScriptAction(actionString, globalSettings.deviceIpList);


            if(!actionSuccess) {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"The Action {actionString} does not exist");
                return;
            }
            return;

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

            List<ScriptCommand> commands = ScriptCommand.GetAction("PRIMARY_COLOR_TEST");
            if(commands == null || commands.Count == 0) {
                Logger.Instance.LogMessage(TracingLevel.INFO, "The action \"PRIMARY_COLOR_TEST\" was not found");
                return;
            }
            foreach(var command in commands) {
                Logger.Instance.LogMessage(TracingLevel.INFO, command.ToString());
            }

            ScriptCommand.StartScriptAction("PRIMARY_COLOR_TEST", globalSettings.deviceIpList);
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

            List<string> actionNames = ScriptCommand.GetListOfActions();
            actionNames.Sort();

            List<ActionItem> actions = new List<ActionItem>();
            foreach(var action in actionNames) {
                actions.Add(new ActionItem(action, action));
            }
            localSettings.ActionDropDown = actions;


            Connection.SetSettingsAsync(JObject.FromObject(localSettings));
        }

        #endregion
    }
}
