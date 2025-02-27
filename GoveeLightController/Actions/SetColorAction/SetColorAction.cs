using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Threading.Tasks;

namespace GoveeLightController {
    [PluginActionId("com.davidgolunski.goveelightcontroller.setcoloraction")]

    public class SetColorAction : KeypadBase {
        /*
         * This class represents an action on the Stream Deck.
         * The Action sets the color of the lights
         */


        private readonly SetColorActionSettings localSettings;
        private readonly DeviceListSettings globalSettings;


        public SetColorAction(SDConnection connection, InitialPayload payload) : base(connection, payload) {
            if(payload.Settings == null || payload.Settings.Count == 0) {
                this.localSettings = new SetColorActionSettings();
                SaveSettings();
            }
            else {
                this.localSettings = payload.Settings.ToObject<SetColorActionSettings>();
            }
            this.globalSettings = new DeviceListSettings();
            GlobalSettingsManager.Instance.RequestGlobalSettings();
            Connection.OnPropertyInspectorDidAppear += OnPropertyInspectorOpened;

            UpdateImage();
        }

        public override void Dispose() {
            Connection.OnPropertyInspectorDidAppear -= OnPropertyInspectorOpened;
        }

        public override void KeyPressed(KeyPayload payload) {
            if(localSettings.UseGlobalSettings) {
                GoveeDeviceController.Instance.SetColor(Color.FromArgb(localSettings.ColorRed, localSettings.ColorGreen, localSettings.ColorBlue), globalSettings.DeviceIpList);
            }
            else {
                GoveeDeviceController.Instance.SetColor(Color.FromArgb(localSettings.ColorRed, localSettings.ColorGreen, localSettings.ColorBlue), localSettings.DeviceIpList);
            }

        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload) {
            int previousRed = localSettings.ColorRed;
            int previousGreen = localSettings.ColorGreen;
            int previousBlue = localSettings.ColorBlue;
            string previousHexCode = localSettings.HexCodeString;
            
            Tools.AutoPopulateSettings(localSettings, payload.Settings);

            // if the rgb values have changed, update the hex code
            if(previousRed != localSettings.ColorRed || previousGreen != localSettings.ColorGreen || previousBlue != localSettings.ColorBlue) {
                localSettings.HexCodeString = Color.FromArgb(localSettings.ColorRed, localSettings.ColorGreen, localSettings.ColorBlue).ToHex();
            }
            // if the hex code has changed, update the rgb values
            else if(previousHexCode != localSettings.HexCodeString) {
                string modifiedHex = "ff" + localSettings.HexCodeString.PadRight(6, '0');
                int colorInt = int.Parse(modifiedHex, System.Globalization.NumberStyles.HexNumber);
                Color newColor = Color.FromArgb(colorInt);
                localSettings.ColorRed = newColor.R;
                localSettings.ColorGreen = newColor.G;
                localSettings.ColorBlue = newColor.B;
            }

            SaveSettings();
            UpdateImage();
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

        private void UpdateImage() {
            if(!localSettings.UseDynamicIcon) {
                Connection.SetDefaultImageAsync();
                return;
            }
         
            Bitmap img = ImageTools.GetBitmapFromFilePath("./Actions/SetColorAction/LightbulbColorDynamic.png");
            img = ImageTools.ReplaceColor(img, Color.Black, Color.FromArgb(localSettings.ColorRed, localSettings.ColorGreen, localSettings.ColorBlue));
            Connection.SetImageAsync(img).GetAwaiter().GetResult();
            img.Dispose();
        }

        #endregion
    }
}