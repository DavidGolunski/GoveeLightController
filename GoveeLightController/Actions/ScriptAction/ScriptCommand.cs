using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using BarRaider.SdTools;
using System.Drawing;
using System.Threading;
using static System.Collections.Specialized.BitVector32;

namespace GoveeLightController {

    public enum Commands {
        TurnOn,
        TurnOff,
        SetColor,
        SetBrightness,
        SetPrimaryColor,
        ActivatePrimaryColor,
        RandomWait,
        Wait,
        CallOtherAction,
        Unknown // For unrecognized commands
    }

    public class ScriptCommand {
        /*
         * This class allows the execution of "Scripts" for Govee Devices.
         * "Scripts" are special JSON Files that contain a series of instructions for the lights.
         * An object of this class represents a single executable line in the JSON
         */
        private static readonly string ScriptsDirectory = Path.Combine(".", "scripts");
        private static readonly string UserScriptsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "davidgolunski", "goveelightcontroller");
        private static readonly Dictionary<string, List<ScriptCommand>> ScriptActionCache = new Dictionary<string, List<ScriptCommand>>();
        private static readonly Mutex ScriptActionCacheMutex = new Mutex();
        private static readonly Random RandomNum = new Random();

        public static void ClearActionCache() {
            ScriptActionCacheMutex.WaitOne();
            ScriptActionCache.Clear();
            ScriptActionCacheMutex.ReleaseMutex();
        }


        public Commands Command { get; set; }
        public int R { get; set; } // used for SetColor
        public int G { get; set; } // used for SetColor
        public int B { get; set; } // used for SetColor
        public int Value { get; set; } // used for SetBrightness
        public int MinDelay { get; set; } // used for RandomWait
        public int MaxDelay { get; set; } // used for RandomWait
        public int Delay { get; set; } // used for Wait
        public string OtherScriptName { get; set; } // used for CallOtherAction
        public string IfCondition { get; set; } // optional for all
        


        public ScriptCommand(Commands command) {
            Command = command;
            R = -1;
            G = -1;
            B = -1;
            Value = -1;
            MinDelay = -1;
            MaxDelay = -1;
            Delay = -1;
            OtherScriptName = "";
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

            if(parameters.TryGetValue("min", out var minDelay))
                commandInfo.MinDelay = Convert.ToInt32(minDelay);
            if(parameters.TryGetValue("max", out var maxDelay))
                commandInfo.MaxDelay = Convert.ToInt32(maxDelay);
            if(parameters.TryGetValue("delay", out var delay))
                commandInfo.Delay = Convert.ToInt32(delay);

            if(parameters.TryGetValue("name", out var name))
                commandInfo.OtherScriptName = name.ToString();
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
                case "IsCounterStrikePlayerDead":
                case "IsCounterStrikePlayerNotDead":
                    break;
                default:
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "The given if condition was not valid: \"" + IfCondition + "\"");
                    return false;
            }

            switch(Command) {
                case Commands.Wait:
                    if(Delay <= 0 || Delay > 600000) {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, "The Script Command \"" + Command.ToString() + "\" had an invalid delay. Delays need to be above 0 an lower than 600001");
                        return false;
                    }
                    break;
                case Commands.RandomWait:
                    if(MinDelay <= 0 || MaxDelay <= 0 || MinDelay > 600000 || MaxDelay > 600000) {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, "The Script Command \"" + Command.ToString() + "\" had an invalid min or max delay. Delays need to be above 0 an lower than 600001");
                        return false;
                    }
                    if(MinDelay >= MaxDelay) {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, "The Script Command \"" + Command.ToString() + "\" must have a min delay that is lower than the max delay");
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
                case Commands.CallOtherAction:
                    if(string.IsNullOrEmpty(OtherScriptName)) {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, "The Script Command \"" + Command.ToString() + "\" had an invalid value. A name of a different action is needed.");
                        return false;
                    }
                    return true;
                case Commands.Unknown:
                default:
                    return false;
            }
            return true;
        }

        // executes this command
        private string Execute(CancellationToken cancellationToken, List<string> ips = null) {
          
            if(IfCondition == "IsLeaguePlayerNotDead" && LeagueAPI.Instance.IsDead) {
                return null;
            }
            if(IfCondition == "IsLeaguePlayerDead" && !LeagueAPI.Instance.IsDead) {
                return null;
            }
            if(IfCondition == "IsCounterStrikePlayerNotDead" && CounterStrikeAPI.Instance.IsProviderDead) {
                return null;
            }
            if(IfCondition == "IsCounterStrikePlayerDead" && !CounterStrikeAPI.Instance.IsProviderDead) {
                return null;
            }

            switch(Command) {
                case Commands.Wait:
                    Task.Delay(Delay, cancellationToken).Wait(cancellationToken);
                    break;
                case Commands.RandomWait:
                    int randomDelay = RandomNum.Next(MinDelay, MaxDelay + 1);
                    Task.Delay(randomDelay, cancellationToken).Wait(cancellationToken);
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
                case Commands.CallOtherAction:
                    return OtherScriptName;
                case Commands.Unknown:
                default:
                    break;
            }
            return null;
        }

        public override string ToString() {
            switch(Command) {
                case Commands.Wait:
                    return $"{Command}, Delay({Delay})" + " ifCondition: \"" + IfCondition + "\"";
                case Commands.RandomWait:
                    return $"{Command}, Min({MinDelay}), Max({MaxDelay})" + " ifCondition: \"" + IfCondition + "\"";
                case Commands.SetBrightness:
                    return $"{Command}, Value({Value})" + " ifCondition: \"" + IfCondition + "\"";
                case Commands.SetPrimaryColor:
                case Commands.SetColor:
                    return $"{Command}, R({R}), G({G}), B({B})" + " ifCondition: \"" + IfCondition + "\"";
                case Commands.CallOtherAction:
                    return $"{Command}, Name({OtherScriptName})" + " ifCondition: \"" + IfCondition + "\"";
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

        private static CancellationTokenSource cancellationTokenSource = null;
        private static Task ScriptActionTask;
        public static bool IsRunning {
            get => ScriptActionTask != null && !ScriptActionTask.IsCompleted;
        }

        // Newly created script actions can get the current active script from here
        private static string activeScriptActionName = null;
        private static readonly Mutex activeScriptActionNameMutex = new Mutex();
        public static string ActiveScriptActionName {
            set {
                activeScriptActionNameMutex.WaitOne();
                activeScriptActionName = value;
                activeScriptActionNameMutex.ReleaseMutex();
            }
            get {
                activeScriptActionNameMutex.WaitOne();
                string val = activeScriptActionName;
                activeScriptActionNameMutex.ReleaseMutex();
                return val;
            }
        }

        // To inform script actions that the active script has changed
        public static event Action OnActiveScriptActionChanged;
        


        // stops the execution of the current script list
        public static void StopScriptAction() {
            if(!IsRunning) {
                return;
            }
            
            if(cancellationTokenSource != null) {
                cancellationTokenSource.Cancel(); // Signal cancellation
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }

        // starts executing a list of script commands
        // this is done in a seperate thread, as to not disturb the rest of the program 
        public static bool StartScriptAction(string action, List<string> ips = null) {
            List<ScriptCommand> commands = GetAction(action);
            if(commands == null)
                return false;

            StopScriptAction(); // Ensure any running task is stopped

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            

            ScriptActionTask = Task.Run(async () => {
                ActiveScriptActionName = action;
                OnActiveScriptActionChanged?.Invoke();

                try {
                    await RunScriptAction(commands, ips, cancellationToken);
                }
                catch(TaskCanceledException) {
                    // Expected during cancellation
                }
                finally {
                    ActiveScriptActionName = null;
                    OnActiveScriptActionChanged?.Invoke();
                }
                
            }, cancellationToken);

            return true;
        }


        private static async Task RunScriptAction(List<ScriptCommand> commands, List<string> ips, CancellationToken cancellationToken) {
            for(int i = 0; i < commands.Count; i++) {
                if(cancellationToken.IsCancellationRequested) {
                    break;
                }

                string result = commands[i].Execute(cancellationToken, ips); // Execute the command

                // if the function returned a string, then a new action should be called
                if(!string.IsNullOrEmpty(result)) {
                    List<ScriptCommand> newCommands = GetAction(result);
                    if(newCommands != null && newCommands.Count > 0) {
                        // Insert the new commands right after the current one
                        commands.InsertRange(i + 1, newCommands);
                    }
                }

                await Task.Yield(); // Yield to allow responsiveness in async context
            }
        }


        #endregion


        #region FileManagement


        // Creates the necessary folders if they do not exists yet and copies predefined script files into them
        private static void CreateDirectories() {
            // Check if the target scripts folder exists in the user's Documents folder
            if(!Directory.Exists(UserScriptsDirectory)) {
                // Create the folder if it doesn't exist
                Directory.CreateDirectory(UserScriptsDirectory);

                // Copy all files from the source folder to the target folder
                if(Directory.Exists(ScriptsDirectory)) {
                    string[] files = Directory.GetFiles(ScriptsDirectory);

                    foreach(string file in files) {
                        // Get the file name
                        string fileName = Path.GetFileName(file);

                        // Define the destination file path
                        string destFile = Path.Combine(UserScriptsDirectory, fileName);

                        // Copy the file
                        File.Copy(file, destFile, true);
                    }

                    Logger.Instance.LogMessage(TracingLevel.INFO, "Scripts folder created and files copied successfully!");
                }
                else {
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Source folder '{ScriptsDirectory}' does not exist. No files were copied.");
                }
            }
        }

        // Returns a list of all filenames found in "./scripts/" that have the ".json" file type.
        public static List<string> GetScriptFileNames() {
            CreateDirectories();
            return new List<string>(Directory.GetFiles(UserScriptsDirectory, "*.json"));
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
            // actions that start with an '_' are hidden, but can be called with or without the '_' in front
            if(actionName.StartsWith("_")) {
                actionName = actionName.Substring(1);
            }

            // try to get the action from the cache
            ScriptActionCacheMutex.WaitOne();
            if(ScriptActionCache.TryGetValue(actionName, out var cachedScriptAction)) {
                ScriptActionCacheMutex.ReleaseMutex();
                return cachedScriptAction;
            }
            ScriptActionCacheMutex.ReleaseMutex();
            
            foreach(var fileName in GetScriptFileNames()) {
                if(IsValidFile(fileName)) {
                    string jsonContent = File.ReadAllText(fileName);
                    var json = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, object>>>>(jsonContent);

                    if(json != null) {
                        List<Dictionary<string, object>> commandList = null;
                        if(json.ContainsKey(actionName)) {
                            commandList = json[actionName];
                        }
                        else if(json.ContainsKey("_" + actionName)) {
                            commandList = json["_" + actionName];
                        }
                        else {
                            continue;
                        }

                        var result = new List<ScriptCommand>();

                        foreach(var command in commandList) {
                            if(command.ContainsKey("command") &&
                                Enum.TryParse(command["command"].ToString(), true, out Commands parsedCommand)) {
                                var parameters = new Dictionary<string, object>(command);
                                parameters.Remove("command");
                                result.Add(ScriptCommand.FromDictionary(parsedCommand, parameters));
                            }
                        }

                        // write the result into cache
                        ScriptActionCacheMutex.WaitOne();
                        ScriptActionCache.Add(actionName, result);
                        ScriptActionCacheMutex.ReleaseMutex();

                        return result;
                    }
                }
            }

            return null; // Action not found or file is invalid
        }

        #endregion
    }

    #endregion

}
