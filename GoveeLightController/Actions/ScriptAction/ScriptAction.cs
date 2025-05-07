using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoveeLightController.Actions {
    [PluginActionId("com.davidgolunski.goveelightcontroller.scriptaction")]


    public class ScriptAction : KeypadBase {
        /*
         * This class represents an action on the Stream Deck.
         * The Action runs through a given action (special JSON files) which controls Govee Lights
         */

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


        private readonly ScriptActionSettings localSettings;
        private readonly DeviceListSettings globalSettings;

        private readonly RaceTimer timer; // timer that takes care of running seperate action depending on how long the key was pressed

        public ScriptAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.localSettings = new ScriptActionSettings();
                SaveSettings();
            }
            else {
                this.localSettings = payload.Settings.ToObject<ScriptActionSettings>();
            }
            this.globalSettings = new DeviceListSettings();


            timer = new RaceTimer(300);
            timer.StoppedPrematurely += OnButtonTapped;
            timer.TimeElapsed += OnButtonHold;

            GlobalSettingsManager.Instance.RequestGlobalSettings();
            Connection.OnPropertyInspectorDidAppear += OnPropertyInspectorOpened;
            ScriptCommand.OnActiveScriptActionChanged += OnActiveScriptActionChanged;

            OnActiveScriptActionChanged();
        }

        public override void Dispose() {
            timer.StoppedPrematurely -= OnButtonTapped;
            timer.TimeElapsed -= OnButtonHold;

            Connection.OnPropertyInspectorDidAppear -= OnPropertyInspectorOpened;
            ScriptCommand.OnActiveScriptActionChanged -= OnActiveScriptActionChanged;
        }

        public override void KeyPressed(KeyPayload payload) {
            timer.Start();
        }

        public override void KeyReleased(KeyPayload payload) {
            timer.Stop();
        }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            Tools.AutoPopulateSettings(localSettings, payload.Settings);
            SaveSettings();

            // Update the image if the selected action has changed
            OnActiveScriptActionChanged();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) {
            Tools.AutoPopulateSettings(globalSettings, payload.Settings);
        }

        #region Private Methods

        // called by RaceTimer
        private void OnButtonTapped() {
            string actionString = localSettings.SelectedAction;
            bool actionSuccess;

            ScriptCommand.ClearActionCache();
            if(localSettings.UseGlobalSettings) {
                actionSuccess = ScriptCommand.StartScriptAction(actionString, globalSettings.DeviceIpList);
            }
            else {
                actionSuccess = ScriptCommand.StartScriptAction(actionString, localSettings.DeviceIpList);
            }

            if(!actionSuccess) {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"The Action {actionString} does not exist");
                Connection.ShowAlert().GetAwaiter().GetResult();
            }
        }

        // called by RaceTimer
        private void OnButtonHold() {
            ScriptCommand.StopScriptAction();

            if(localSettings.UseGlobalSettings) {
                GoveeDeviceController.Instance.TurnOff(globalSettings.DeviceIpList);
            }
            else {
                GoveeDeviceController.Instance.TurnOff(localSettings.DeviceIpList);
            }
        }


        private void OnActiveScriptActionChanged() {
            string newActionName = ScriptCommand.ActiveScriptActionName;
            if(string.IsNullOrEmpty(localSettings.SelectedAction) || string.IsNullOrEmpty(newActionName) || localSettings.SelectedAction != newActionName) {
                Connection.SetStateAsync(0);
                return;
            }

            Connection.SetStateAsync(1);
        }

        private Task SaveSettings() {
            return Connection.SetSettingsAsync(JObject.FromObject(localSettings));
        }

        private void OnPropertyInspectorOpened(object sender, SDEventReceivedEventArgs<PropertyInspectorDidAppear> e) {

            // filter out times that start with _
            List<string> actionNames = ScriptCommand.GetListOfActions().Where(item => !item.StartsWith("_")).ToList();
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
