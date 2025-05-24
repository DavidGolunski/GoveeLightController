# Light Scripts
## General
1. Scripts are special JSON files that the user can modify
2. Scripts need to be inside the "Documents/davidgolunski/goveelightcontroller" folder
3. Before modifying any scripts save a copy of the original Scripts as a backup
4. If something goes wrong you can find information about it in the "pluginlog.log" file (inside the plugin folder)
5. Only one script can be active at the same time. If you start another script, it will abort the original script


## JSON Structure
1. The JSON is a collection of "Actions" (String)
2. "Actions" can be hidden from the "ScriptAction" dropdown selection if the name starts with '_'
3. Each Action is an array of commands that can be executed
4. A Command might need to have additional parameters to be valid
5. You can add some selected if statements using "if" and one of the predefined variables


## Commands
### Command: "TurnOn"
__Description:__  
Turns the light on  
__Parameters:__  
(optional) "ip": "[ip address of a light]" - By adding this, this command will only change the light at the ip address  
  

### Command: "TurnOff"
__Description:__  
Turns the light off  
__Parameters:__  
(optional) "ip": "[ip address of a light]" - By adding this, this command will only change the light at the ip address  

### Command: "SetColor"
__Description:__  
Sets the color of the light  
__Parameters:__  
"r": 0-255  
"g": 0-255  
"b": 0-255  
(optional) "ip": "[ip address of a light]" - By adding this, this command will only change the light at the ip address  

### Command: "SetTemperature"
__Description:__  
Sets the color of the light based on a temperature value
__Parameters:__  
"temperature": 2000-9000  
(optional) "ip": "[ip address of a light]" - By adding this, this command will only change the light at the ip address  

### Command: "SetBrightness"
__Description:__  
Sets the brightness of the light  
__Parameters:__  
"value": 0-100
(optional) "ip": "[ip address of a light]" - By adding this, this command will only change the light at the ip address  

### Command: "SetPrimaryColor"
__Description:__  
Saves one color as "Primary" for all lights. The primary color can be activated with the "ActivatePrimaryColor" command.  
The primary color gets saved while the program (stream deck software) is running.  
__Parameters:__  
"r": 0-255  
"g": 0-255  
"b": 0-255  

### Command: "ActivatePrimaryColor"
__Description:__  
Activates the primary color that was set with the "SetPrimaryColor" command. Restarting the program (stream deck software) resets the saved primary color.  
__Parameters:__  
(optional) "ip": "[ip address of a light]" - By adding this, this command will only change the light at the ip address  

### Command: "CallOtherAction"
__Description:__  
Calls another action. The script will fail while it is running if the other action does not exist.  
_Warning: This has the potential to create endless loops, forcing you to cancel the action manually._  
__Parameters:__  
"name": "[Name of Other Action]"  

### Command: "Wait"
__Description:__  
Introduces a delay before the execution of the next command  
__Parameters:__  
"delay": 1-600000 (time in milliseconds)

### Command: "RandomWait"
__Description:__  
Introduces a delay before the execution of the next command. The delay is randomly generated within the given boundries.  
__Parameters:__  
"min": 1-600000 (time in milliseconds)  
"max": 1-600000 (time in milliseconds)

## Example
```json
{  
"ColorSwitch1": [ 
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
    { "command": "CallOtherAction", "name": "ColorSwitch2" }  
  ],  
  "ColorSwitch2": [    
    { "command": "SetColor", "r": 255, "g": 255, "b": 0 },  
    { "command": "RandomWait", "min": 1000, "max": 4000 },  
    { "command": "SetBrightness", "value": 75 },  
    { "command": "RandomWait", "min": 1000, "max": 4000 },  
    { "command": "SetColor", "r": 0, "g": 255, "b": 255 },  
    { "command": "RandomWait", "min": 1000, "max": 4000 },  
    { "command": "SetBrightness", "value": 100 },  
    { "command": "RandomWait", "min": 1000, "max": 4000 },  
    { "command": "TurnOff" }  
  ]
}
```

### If Conditions
You have access to some selected variables. You can add one of the varibles inside an "if" parameters.  
The command will only be executed when the "if-statement" is true.  
The variables are:  
- "IsLeaguePlayerDead"
- "IsLeaguePlayerNotDead"
- "IsCounterStrikePlayerDead"
- "IsCounterStrikePlayerNotDead"

__Example:__
```
{ "command": "TurnOff", "if": "IsLeaguePlayerDead" }
```


## Events for Game Integration
For Counter Strike and League of Legends you can create a __custom lightshow based on events within the games__.  
The Effect Managers for the games will look for specific light scripts, that will be executed if a particular event happens.  
You can __change what the light scripts will do__ as well.  
__If multiple events happen at the same time, only the most "relevant" event will be selected!__  
E.g. If you make a "Kill" in League of Legends, which is also a "PentaKill", only the "PentaKill" Event will trigger  
E.g. If you win a round in CS, but this was the last round, only the "GameWon" Event will trigger

### Counter Strike Events
All Counter Strike Events can be found in "Documents/davidgolunski/goveelightcontroller/CounterStrikeEffects.json".  
The following list is _ordered by the priority in which the events will be triggered_.

1. __CS@ENTERRED_MAIN_MENU__: Happens when you go into the main menu of Counterstrike.
2. __CS@SPECTATOR_GAME_WON_T__: Happens if you are spectating and the terrorits win the game.
3. __CS@SPECATOR_GAME_WON_CT__: Happens if you are spectating and the counter-terrorists wind the game.
4. __CS@SPECTATOR_GAME_TIED__: Happens if you are spectating and the game ends in a tie.
5. __CS@SPECTATOR_ROUND_WON_T_BOMB_EXPLODED__: Happens if you are spectating and the terrorists win the round by letting the bomb explode.
6. __CS@SPECTATOR_ROUND_WON_T__: Happens if you are spectating and the terrorists win the round.
7. __CS@SPECTATOR_ROUND_WON_CT_BOMB_EXPLODED__: Happens if you are spectating and the counter-terrorists win the round by defusing the bomb.
8. __CS@SPECTATOR_ROUND_WON_CT__: Happens if you are spectating and the counter-terrorists win the round.
9. __CS@SPECTATOR_BOMB_PLANTED__: Happens if you are spectating and the terrorists plant the bomb. 
10. __CS@SPECTATOR_FREECAM__: Happens if you are spectating and you switch to the free camera.
11. __CS@SPECTATOR_WATCHING_T__: Happens if you are spectating and you switch to a terrorists camera.
12. __CS@SPECTATOR_WATCHING_CT__: Happens if you are spectating and you switch to a counter-terrorists.
13. __CS@GAME_WON__: Happens if you win a game.
14. __CS@GAME_LOST__: Happens if you loose a game.
15. __CS@GAME_TIED__: Happens if you tie a game.
16. __CS@ROUND_WON_BOMB_EXPLODED__: Happens if you win a round by letting the bomb explode.
17. __CS@ROUND_LOST_BOMB_EXPLODED__: Happens if you loose a round by letting the bomb explode.
18. __CS@ROUND_WON_BOMB_DEFUSED__: Happens if you win a round by defusing the bomb.
19. __CS@ROUND_LOST_BOMB_DEFUSED__: Happens if you loose a round by letting the bomb get defused.
20. __CS@ROUND_WON__: Happens if you win a round.
21. __CS@ROUND_LOST__: Happens if you loose a round.
22. __CS@ROUND_STARTED_T__: Happens if a new round (or warmup) starts in which you are in the "Terrorist" team.
23. __CS@ROUND_STARTED_CT__: Happens if a new round (or warmup) starts in which you are in the "Terrorist" team.
24. __CS@HAS_DIED__: Happens when you die in game.
25. __CS@HAS_REVIVED_T__: Happens if you revive in game in the "Terrorist" team (this can happen in gamemodes like Deathmatch or Arms Race, or when in warm-ups).
26. __CS@HAS_REVIVED_CT__: Happens if you revive in game in the "Counter-Terrorist" team (this can happen in gamemodes like Deathmatch or Arms Race, or when in warm-ups).
27. __CS@HAS_BOUGH_EQUIPMENT__: Happens when you buy equipment during the buy phase.
28. __CS@STARTED_FLASHED__: Happens when you get flashed. (you need to be alive for this event to happen).
29. __CS@HAS_KILLED_HEADSHOT__: Happens if you kill an enemy with a headshot. (you need to be alive for this event to happen).
30. __CS@HAS_KILLED__: Happens if you kill an enemy. (you need to be alive for this event to happen).
31. __CS@STOPPED_SMOKED__: Happens if you step out of a smoke. (you need to be alive for this event to happen).
32. __CS@STARTED_SMOKED__: Happens if you step into a smoke. (you need to be alive for this event to happen).
33. __CS@STOPPED_BURNING__: Happens if you step out of a molotov. (you need to be alive for this event to happen).
34. __CS@STARTED_BURNING__: Happens if you step into a molotov. (you need to be alive for this event to happen).
35. __CS@INTEGRAITON_STARTED__: Happens when you start the integration.
36. __CS@INTEGRATION_STOPPED__: Happens when you stop the integration.



### League of Legends Events
All League of Legends Events can be found in "Documents/davidgolunski/goveelightcontroller/LeagueEffects.json".  
The following list is _ordered by the priority in which the events will be triggered_.

1. __LOL@HAS_REVIVED__: Happens if you revive.
2. __LOL@GAME_WON__: Happens if you win the game.
3. __LOL@GAME_LOST__: Happens if you loose the game.
4. __LOL@GAME_STARTED_DOMINATION__: Happens if the game starts and you have "Domination" (Red) Runes selected your primary rune tree.
5. __LOL@GAME_STARTED_INSPIRATION__: Happens if the game starts and you have "Inspiration" (Aqua) Runes selected your primary rune tree.
6. __LOL@GAME_STARTED_RESOLVE__: Happens if the game starts and you have "Resolve" (Green) Runes selected your primary rune tree.
7. __LOL@GAME_STARTED_SORCERY__: Happens if the game starts and you have "Sorcery" (Blue) Runes selected your primary rune tree.
8. __LOL@GAME_STARTED_PRECISION__: Happens if the game starts and you have "Precision" (Yellow) Runes selected your primary rune tree.  
9. __LOL@BARON_KILLED__: Happens if either team kills the Baron.
10. __LOL@HERALD_KILLED__: Happens if either team kills the Herald.
11. __LOL@VOID_GRUBS_KILLED__: Happens if you have killed or assisted with killing a Void Grub.
12. __LOL@ATAKHAN_KILLED__: Happens if either team kills the Atakhan.
13. __LOL@AIR_DRAGON_KILLED__: Happens if either team kills the Air Dragon.
14. __LOL@EARTH_DRAGON_KILLED__: Happens if either team kills the Earth Dragon.
15. __LOL@FIRE_DRAGON_KILLED__: Happens if either team kills the Fire Dragon.
16. __LOL@WATER_DRAGON_KILLED__: Happens if either team kills the Water Dragon.
17. __LOL@HEXTECH_DRAGON_KILLED__: Happens if either team kills the Hextech Dragon.
18. __LOL@CHEMTECH_DRAGON_KILLED__: Happens if either team kills the Chemtech Dragon.
19. __LOL@ELDER_DRAGON_KILLED__: Happens if either team kills the Elder Dragon.
20. __LOL@HAS_PENTAKILLED__: Happens if you have a Penta Kill.
21. __LOL@HAS_DIED__: Happens if you die.
22. __LOL@HAS_KILLED__: Happens if you kill an enemy.
23. __LOL@HAS_ASSISTED__: Happens if you assist in killing an enemy.
24. __LOL@HAS_KILLED_TURRET__: Happens if you destroy an enemy turret.
25. __LOL@HAS_ASSISTED_TURRET__: Happens if you assist in destroying an enemy turret.
26. __LOL@HAS_KILLED_INHIB__: Happens if you destroy an enemy Inhibitor.
27. __LOL@HAS_ASSISTED_INHIB__: Happens if you assist in destroying an enemy Inhibitor.
28. __LOL@TEAM_HAS_ACED__: Happens if your team aces the enemy team.
29. __LOL@ENEMY_TEAM_HAS_ACED__: Happens if the enemy team aces your team.
30. __LOL@INTEGRAITON_STARTED__: Happens when you start the integration.
31. __LOL@INTEGRATION_STOPPED__: Happens when you stop the integration.