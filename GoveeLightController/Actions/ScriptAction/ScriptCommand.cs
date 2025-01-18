using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using BarRaider.SdTools;
using System.Runtime.CompilerServices;
using System.Drawing;
using System.Threading;

namespace GoveeLightController {

    public enum Commands {
        TurnOn,
        TurnOff,
        SetColor,
        SetBrightness,
        SetPrimaryColor,
        ActivatePrimaryColor,
        Wait,
        Unknown // For unrecognized commands
    }

    public class ScriptCommand {
        /*
         * This class allows the execution of "Scripts" for Govee Devices.
         * "Scripts" are special JSON Files that contain a series of instructions for the lights.
         * An object of this class represents a single executable line in the JSON
         */
        private const string ScriptsDirectory = "./scripts/";

        public Commands Command { get; set; }
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public int Value { get; set; }
        public int Delay { get; set; }
        public string IfCondition { get; set; }


        public ScriptCommand(Commands command) {
            Command = command;
            R = -1;
            G = -1;
            B = -1;
            Value = -1;
            Delay = -1;
            IfCondition = "";
        }

        public static ScriptCommand FromDictionary(Commands command, Dictionary<string, object> parameters) {
            var commandInfo = new ScriptCommand(command);

            if(parameters.TryGetValue("r", out var r))
                commandInfo.R = Convert.ToInt32(r);
            if(parameters.TryGetValue("g", out var g))
                commandInfo.G = Convert.ToInt32(g);
            if(parameters.TryGetValue("b", out var b))
                commandInfo.B = Convert.ToInt32(b);
            if(parameters.TryGetValue("value", out var value))
                commandInfo.Value = Convert.ToInt32(value);
            if(parameters.TryGetValue("delay", out var delay))
                commandInfo.Delay = Convert.ToInt32(delay);
            if(parameters.TryGetValue("if", out var ifCondition))
                commandInfo.IfCondition = ifCondition.ToString();

            if(!commandInfo.IsValid()) {
                return null;
            }
            return commandInfo;
        }

        // checks if the command and the parameters given are ok/within bounds
        public bool IsValid() {
            switch(IfCondition) {
                case "":
                case "IsLeaguePlayerDead":
                case "IsLeaguePlayerNotDead":
                    break;
                default:
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "The given if condition was not valid: \"" + IfCondition + "\"");
                    return false;
            }

            switch(Command) {
                case Commands.Wait:
                    if(Delay <= 0 || Delay > 10000) {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, "The Script Command \"" + Command.ToString() + "\" had an invalid delay. Delays need to be above 0 an lower than 10001");
                        return false;
                    }
                    break;
                case Commands.SetBrightness:
                    if(Value < 0 || Value > 100) {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, "The Script Command \"" + Command.ToString() + "\" had an invalid value. A Brightness must be between 0 and 100");
                        return false;
                    }
                    break;
                case Commands.SetPrimaryColor:
                case Commands.SetColor:
                    if(R < 0 || R > 255 ||  G < 0 || G > 255 || B < 0 || B > 255) {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, "The Script Command \"" + Command.ToString() + "\" had an invalid value. RGB values need to be between 0 and 255");
                        return false;
                    }
                    break;
                case Commands.ActivatePrimaryColor:
                case Commands.TurnOn:
                case Commands.TurnOff:
                    return true;
                case Commands.Unknown:
                default:
                    return false;
            }
            return true;
        }

        // executes this command
        private void Execute(List<string> ips = null) {
          
            if(IfCondition == "IsLeaguePlayerNotDead" && LeagueAPI.Instance.IsDead) {
                return;
            }
            if(IfCondition == "IsLeaguePlayerDead" && !LeagueAPI.Instance.IsDead) {
                return;
            }

            //Logger.Instance.LogMessage(TracingLevel.DEBUG, "Executing: " + this.ToString());
           Console.WriteLine("Executing: " + this.ToString());

            switch(Command) {
                case Commands.Wait:
                    Task.Delay(Delay).Wait();
                    break;
                case Commands.SetBrightness:
                    GoveeDeviceController.Instance.SetBrightness(Value, ips);
                    break;
                case Commands.SetColor:
                    GoveeDeviceController.Instance.SetColor(Color.FromArgb(255, R, G, B), ips);
                    break;
                case Commands.TurnOn:
                    GoveeDeviceController.Instance.TurnOn(ips);
                    break;
                case Commands.TurnOff:
                    GoveeDeviceController.Instance.TurnOff(ips);
                    break;
                case Commands.SetPrimaryColor:
                    GoveeDeviceController.Instance.SetPrimaryColor(Color.FromArgb(255, R, G, B));
                    break;
                case Commands.ActivatePrimaryColor:
                    GoveeDeviceController.Instance.ActivatePrimaryColor(ips);
                    break;
                case Commands.Unknown:
                default:
                    break;
            }
        }

        public override string ToString() {
            switch(Command) {
                case Commands.Wait:
                    return $"{Command}, Delay({Delay})" + " ifCondition: \"" + IfCondition + "\"";
                case Commands.SetBrightness:
                    return $"{Command}, Value({Value})" + " ifCondition: \"" + IfCondition + "\"";
                case Commands.SetPrimaryColor:
                case Commands.SetColor:
                    return $"{Command}, R({R}), G({G}), B({B})" + " ifCondition: \"" + IfCondition + "\"";
                case Commands.TurnOn:
                case Commands.TurnOff:
                case Commands.ActivatePrimaryColor:
                case Commands.Unknown:
                default:
                    return Command.ToString() + " ifCondition: \"" + IfCondition + "\"";
            }
        }


        #region static functions


        #region thread management

        private static Thread scriptThread = null;
        private static bool terminateScriptAction = false;
        private static bool isRunning = false;
        public static bool IsRunning {
            get => isRunning;
        }
        
        // stops the execution of the current script list
        private static void StopThread() {
            if(!isRunning)
                return;

            terminateScriptAction = true;
            isRunning = false;
            if(scriptThread != null && scriptThread.IsAlive) {
                scriptThread.Join(); // Wait for the thread to terminate
                scriptThread = null;
            }

        }

        // starts executing a list of script commands
        // this is done in a seperate thread, as to not disturb the rest of the program 
        public static bool StartScriptAction(string action, List<string> ips = null) {
            return StartScriptAction(GetAction(action), ips);
        }

        public static bool StartScriptAction(List<ScriptCommand> commands, List<string> ips = null) {
            if(commands == null)
                return false;
            StopThread();
            terminateScriptAction = false;
            isRunning = true;

            scriptThread = new Thread(() => {
                RunScriptAction(commands, ips);
            }) {
                IsBackground = true // Ensure the thread does not block application exit
            };

            scriptThread.Start();
            return true;
        }


        private static void RunScriptAction(List<ScriptCommand> commands, List<string> ips) {
            //Logger.Instance.LogMessage(TracingLevel.DEBUG, "Script Thread Started");
            foreach(ScriptCommand command in commands) {
                if(terminateScriptAction)
                    return;
                command.Execute(ips);
            }
            isRunning = false;
        }



        #endregion

        // Returns a list of all filenames found in "./scripts/" that have the ".json" file type.
        public static List<string> GetScriptFileNames() {
            if(!Directory.Exists(ScriptsDirectory)) {
                Directory.CreateDirectory(ScriptsDirectory);
            }
            return new List<string>(Directory.GetFiles(ScriptsDirectory, "*.json"));
        }

        // Checks if a file and all actions inside are valid 
        public static bool IsValidFile(string fileName) {
            if(!File.Exists(fileName)) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "The file at \"" + fileName + "\" does not exist");
                return false;
            }

            try {
                string jsonContent = File.ReadAllText(fileName);
                var json = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, object>>>>(jsonContent);

                foreach(var action in json.Values) {
                    foreach(var command in action) {
                        if(!command.ContainsKey("command")) {
                            Logger.Instance.LogMessage(TracingLevel.ERROR, "Parsing Error: " + fileName + " did not contain \"command\"");
                            return false;
                        }


                        string commandName = command["command"].ToString();
                        if(!Enum.TryParse(commandName, true, out Commands parsedCommand) || parsedCommand == Commands.Unknown) {
                            Logger.Instance.LogMessage(TracingLevel.ERROR, "Parsing Error: " + fileName + " had an unknown command (" + commandName + ")");
                            return false;
                        }

                        var parameters = new Dictionary<string, object>(command);
                        parameters.Remove("command");
                        ScriptCommand scriptCommand = ScriptCommand.FromDictionary(parsedCommand, parameters);
                        if(scriptCommand == null || !scriptCommand.IsValid()) {
                            Logger.Instance.LogMessage(TracingLevel.ERROR, "The Command " + scriptCommand + " could not be parsed");
                            return false;
                        }
                    }
                }
                return true;
            }
            catch {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Parsing Error: " + fileName + " was unable to be parsed");
                return false; // If parsing fails or structure is invalid
            }
        }

        // Returns a list of all valid actions inside the ".json" files in "./scripts/".
        public static List<string> GetListOfActions() {
            var actions = new List<string>();

            foreach(var fileName in GetScriptFileNames()) {
                if(IsValidFile(fileName)) {
                    string jsonContent = File.ReadAllText(fileName);
                    var json = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, object>>>>(jsonContent);
                    actions.AddRange(json.Keys);
                }
                else {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "The file " + fileName + " was not valid");
                }
            }

            return actions;
        }

        // returns a list of ScriptCommands, based on an actions name
        public static List<ScriptCommand> GetAction(string actionName) {
            foreach(var fileName in GetScriptFileNames()) {
                if(IsValidFile(fileName)) {
                    string jsonContent = File.ReadAllText(fileName);
                    var json = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, object>>>>(jsonContent);

                    if(json != null && json.ContainsKey(actionName)) {
                        var commandList = json[actionName];
                        var result = new List<ScriptCommand>();

                        foreach(var command in commandList) {
                            if(command.ContainsKey("command") &&
                                Enum.TryParse(command["command"].ToString(), true, out Commands parsedCommand)) {
                                var parameters = new Dictionary<string, object>(command);
                                parameters.Remove("command");
                                result.Add(ScriptCommand.FromDictionary(parsedCommand, parameters));
                            }
                        }

                        return result;
                    }
                }
            }

            return null; // Action not found or file is invalid
        }

        #endregion
    }


}
