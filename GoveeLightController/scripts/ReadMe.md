# Light Scripts
## General
1. Scripts are special JSON files that the user can modify
2. Scripts need to be inside the "Documents/davidgolunski/goveelightcontroller" folder
3. Before modifying any scripts save a copy of the original Scripts as a backup
4. If something goes wrong you can find information about it in the "pluginlog.log" file (inside the plugin folder)
5. Only one script can be active at the same time. If you start another script, it will abort the original script


## JSON Structure
1. The JSON is a collection of "Actions" (String)
2. Each Action is an array of commands that can be executed
3. A Command might need to have additional parameters to be valid
4. You can add some selected if statements using "if" and one of the predefined variables


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
```
{  
"ColorSwitch1": [  
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
  "ColorSwitch2": [    
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
```

### If Conditions
You have access to some selected variables. You can add one of the varibles inside an "if" parameters.  
The command will only be executed when the "if-statement" is true.  
The variables are:  
- "IsLeaguePlayerDead"
- "IsLeaguePlayerNotDead"

__Example:__
```
{ "command": "TurnOff", "if": "IsLeaguePlayerDead" }
```


## Events for Game Integration
For Counter Strike and League of Legends you can create a __custom lightshow based on events within the games__.  
The Effect Managers for the games will look for specific light scripts, that will be executed if a particular event happens.  
You can __change what the light scripts will do__ as well.  
__If multiple events happen at the same time, only the most "relevant" event will be selected!__  
E.g. If you make a "Kill" in League of Legends, which is also a "PentaKill", only the "PentaKill" Event will trigge

### Counter Strike Events
All Counter Strike Events can be found in "Documents/davidgolunski/goveelightcontroller/CounterStrikeEffects.json".  
The following list is _ordered by the priority in which the events will be triggered_.

1. __CS@GAME_WON__: Happens if you win a game.
2. __CS@GAME_LOST__: Happens if you loose a game.
3. __CS@ROUND_WON__: Happens if you win a round.
4. __CS@ROUND_LOST__: Happens if you loose a round.
5. __CS@ROUND_STARTED_T__: Happens if a new round (or warmup) starts in which you are in the "Terrorist" Team.
6. __CS@ROUND_STARTED_CT__: Happens if a new round (or warmup) starts in which you are in the "CounterTerrorist" Team.
7. __CS@HAS_KILLED__: Happens if you kill an enemy.
8. __CS@HAS_DIED__: Happens if you die in game.
9. __CS@HAS_REVIVED__: Happens if you revive in game (this can happen in gamemodes like Deathmatch or Arms Race).
10. __CS@ENTERRED_MAIN_MENU__; Happens if you enter the main menu.


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