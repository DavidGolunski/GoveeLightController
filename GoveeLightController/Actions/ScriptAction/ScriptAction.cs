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
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"ScriptAction: Destructor called");
        }

        public override void KeyPressed(KeyPayload payload) {
            
            string actionString = localSettings.SelectedAction;
            bool actionSuccess = ScriptCommand.StartScriptAction(actionString, globalSettings.DeviceIpList);


            if(!actionSuccess) {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"The Action {actionString} does not exist");
            }
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
