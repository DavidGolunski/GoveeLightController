# Light Scripts
## General
1. Scripts are special JSON files that the user can modify
2. Scripts need to be inside the "%appdata%/elgato/com.davidgolunski.goveelightcontroller/scripts" folder
3. Before modifying any scripts save a copy of the original Scripts as a backup
4. If something goes wrong you can find information about it in the "pluginlog.log" file (inside the plugin folder)
5. Only one script can be active at the same time. If you start another script, it will abort the original script


## JSON Structure
1. The JSON is a collection of "Actions" (String)
2. Each Action is an array of commands that can be executed
3. A Command might need to have additional parameters to be valid


## Commands
### Command: "TurnOn"
__Description:__  
Turns the light on  
__Parameters:__  
No Additional Parameters Required

### Command: "TurnOff"
__Description:__  
Turns the light off  
__Parameters:__  
No Additional Parameters Required

### Command: "SetColor"
__Description:__  
Sets the color of the light  
__Parameters:__  
"r": 0-255  
"g": 0-255  
"b": 0-255  

### Command: "SetBrightness"
__Description:__  
Sets the brightness of the light  
__Parameters:__  
"value": 0-100

### Command: "Wait"
__Description:__  
Introduces a delay before the execution of the next command
__Parameters:__  
"delay": 0-10000 (time in milliseconds)


## Example
{  
"GAME_LOST": [  
    { "command": "TurnOn" },  
    { "command": "Wait", "delay": 1000 },  
    { "command": "SetColor", "r": 255, "g": 0, "b": 0 },  
    { "command": "Wait", "delay": 1000 },  
    { "command": "SetBrightness", "value": 75 },  
    { "command": "Wait", "delay": 1000 },  
    { "command": "SetColor", "r": 0, "g": 255, "b": 0 },  
    { "command": "Wait", "delay": 1000 },  
    { "command": "SetBrightness", "value": 100 },  
    { "command": "Wait", "delay": 2000 },  
    { "command": "TurnOff" }  
  ],  
  "GAME_WON": [  
    { "command": "TurnOn" },  
    { "command": "Wait", "delay": 1000 },  
    { "command": "SetColor", "r": 255, "g": 255, "b": 0 },  
    { "command": "Wait", "delay": 1000 },  
    { "command": "SetBrightness", "value": 75 },  
    { "command": "Wait", "delay": 1000 },  
    { "command": "SetColor", "r": 0, "g": 255, "b": 255 },  
    { "command": "Wait", "delay": 1000 },  
    { "command": "SetBrightness", "value": 100 },  
    { "command": "Wait", "delay": 2000 },  
    { "command": "TurnOff" }  
  ]
}