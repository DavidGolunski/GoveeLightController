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
            IfCondition = null;
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

        public bool IsValid() {
            switch(IfCondition) {
                case null:
                case "IsLeaguePlayerDead":
                case "IsLeaguePlayerNotDead":
                    break;
                default:
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "The given if condition was not valid: \"" + IfCondition + "\"");
                    return false;
            }

            switch(Command) {
                case Commands.Wait:
                    if(Delay <= 0) {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, "The Script Command \"" + Command.ToString() + "\" had an invalid delay.");
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

        public void Execute(List<string> ips = null) {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Executing: " +  this.ToString());
            
            
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
                    GoveeDeviceController.Instance.ActivatePrimaryColor();
                    break;
                case Commands.Unknown:
                default:
                    break;
            }
        }

        public override string ToString() {
            switch(Command) {
                case Commands.Wait:
                    return $"{Command}, Delay({Delay})";
                case Commands.SetBrightness:
                    return $"{Command}, Value({Value})";
                case Commands.SetPrimaryColor:
                case Commands.SetColor:
                    return $"{Command}, R({R}), G({G}), B({B})";
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
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Script Thread Started");
            foreach(ScriptCommand command in commands) {
                if(terminateScriptAction)
                    return;
                command.Execute(ips);
            }
            isRunning = false;
        }



        #endregion

        /// <summary>
        /// Returns a list of all filenames found in "./scripts/" that have the ".json" file type.
        /// </summary>
        public static List<string> GetScriptFileNames() {
            if(!Directory.Exists(ScriptsDirectory)) {
                Directory.CreateDirectory(ScriptsDirectory);
            }
            return new List<string>(Directory.GetFiles(ScriptsDirectory, "*.json"));
        }

        /// <summary>
        /// Validates if a file follows the JSON rules as specified.
        /// </summary>
        /// <param name="fileName">The name of the file to validate.</param>
        /// <returns>True if the file is valid, false otherwise.</returns>
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
                        
                        /*
                        switch(parsedCommand) {
                            case Commands.SetColor:
                                if(!command.ContainsKey("r") || !command.ContainsKey("g") || !command.ContainsKey("b"))
                                    return false;
                                break;
                            case Commands.SetBrightness:
                                if(!command.ContainsKey("value"))
                                    return false;
                                break;
                            case Commands.Wait:
                                if(!command.ContainsKey("delay"))
                                    return false;
                                break;
                        }

                        switch(commandName) {
                            case "TurnOn":
                            case "TurnOff":
                            case "ActivatePrimaryColor":
                                // No additional parameters expected
                                break;
                            case "SetPrimaryColor":
                            case "SetColor":
                                if(!command.ContainsKey("r") || !command.ContainsKey("g") || !command.ContainsKey("b")) {
                                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Parsing Error: " + fileName + " had a \"SetColor\" command without a \"r\", \"g\" or \"b\"");
                                    return false;
                                }
                                break;
                            case "SetBrightness":
                                if(!command.ContainsKey("value")) {
                                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Parsing Error: " + fileName + " had a \"SetBrightness\" command without a \"value\"");
                                    return false;
                                }
                                break;
                            case "Wait":
                                if(!command.ContainsKey("delay")) {
                                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Parsing Error: " + fileName + " had a \"Wait\" command without a \"delay\"");
                                    return false;
                                }
                                break;
                            default:
                                return false; // Unknown command
                        }*/
                    }
                }
                return true;
            }
            catch {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "Parsing Error: " + fileName + " was unable to be parsed");
                return false; // If parsing fails or structure is invalid
            }
        }

        /// <summary>
        /// Returns a list of all valid actions inside the ".json" files in "./scripts/".
        /// </summary>
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

        /// <summary>
        /// Returns the list of ScriptCommandInfo objects for a specific action name.
        /// </summary>
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
