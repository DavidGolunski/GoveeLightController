# Govee Light Controller (By David Golunski)
## General
This plugin allows you to control your Govee lights using LAN Control (without an API key).  
This also includes custom lightshow scripts and light effects for League of Legends and Counter Strike.  
You can find a full list of devices that support LAN Control [here](https://app-h5.govee.com/user-manual/wlan-guide).

## Basic Functionality
- Turning Lights on and off
- Changing the color
- Changing the Brightness
- Running a custom lightshow script. More information can be found in ["GoveeLightController/scripts/ReadMe.md"](https://github.com/DavidGolunski/GoveeLightController/tree/main/GoveeLightController/scripts) folder
- Light effects based on events in Counter Strike or League of Legends (effects can be customized as well!)

## Setup Instructions
1. Connect your compatible Govee Devices to the Wifi (the lights and the pc need to be in the same local network)
2. Inside of the Govee mobile app go to the settings of each light and enable "LAN Control". This settings is not available on all devices. You can check [here](https://app-h5.govee.com/user-manual/wlan-guide) if your device supports this feature.
3. Recommended: Use the "Global Settings" Action to search for Govee Lights automatically or manually input the IP adresses of the lights

## Credits and Support
Thank you to [BarRaider](https://barraider.com/) and their [Streamdeck Tools](https://github.com/BarRaider/streamdeck-tools) which allowed quicker and easier development.
Some Icons have been taken from [uxwing](https://uxwing.com/).

If you like the plugin please consider [supporting me via PayPal](https://www.paypal.com/donate/?hosted_button_id=ZN3URG59JBRVJ) and [joining the Discord](https://discord.gg/9qMPNxRhqt).   .   
This will allow me to keep the applications alive for a little bit longer :)

## Troubleshooting
- __The program can not find the lights:__  
If the lights are not recognized make sure that the lights are within the same network and they have "LAN Control" enabled. If lights are connected with a Wifi-Repeater, try to connect them to the router directly.
You can always specify IPs manually if needed
- __The lights are reacting "slowly" or multiple lights are out of sync:__  
Govee Lights do not perform actions instantly. It sometimes can take some time until a change has been fully executed (usually around 500ms). E.g. if you change the color from blue to red, you will see purple for a split second.  
This is just how Govee Lights work. If you have multiple different lights models, the lights might appear "out of sync", since the time it takes the lights to perform an action depends on the model.
- __The CounterStrike Effects are not working:__  
If the CounterStrike effects are not working, please make sure that the file called _"gamestate_integration_goveelightcontroller.cfg"_ is present in _"[Your Steam Installation Folder]/steamapps/common/Counter-Strike Global Offensive/game/csgo/csg"_.  
The Plugin will try to automatically copy the file into that directory if it finds it, but this can always fail.
If it still does not work, make sure that no other application is using the port 3000, as this is the port that CounterStrike uses to communicate all game updates.
The Plugin does not work if you are only spectating a game.
- __The LeagueOfLegends Effects are not working:__  
League of Legends effects are using port 2999 for communication. Make sure no other application is using that port.
- __Something else is going wrong:__  
The plugin logs any issues inside the log file. You can find the log file at:  
```%appdata%/Elgato/StreamDeck/Plugins/com.davidgolunski.goveelightcontroller.sdPlugin/pluginlog.log```.  
If you face any issues with the plugin you can always start ask for help on the [official Discord](https://discord.gg/9qMPNxRhqt).

## Using Game Effects without a Streamdeck
You can use the Game Effects for Counter Strike and League of Legends also without a Streamdeck.  
Download this Repo, unzip it and execute one of the two _.bat_ files:  
__StartStandaloneCounterStrikeEffects.bat__ and __StartStandaloneLeagueEffects.bat__  
It will try to find all compatible Govee Devices inside the network and use them to display the in game effects.  
If you are facing troubles with the automatic detection or you want to only use specific devices you can edit the _.bat_ file and provide one or multiple ips yourself.


## Change and Release Notes
### Version 1.0.2
- Added "Support" buttons now link to the Elgato Marketplace. Users can join the discord from the link on the marketplace.

### Version 1.0.1
- Added "Support" buttons to the actions PropertyInspector
- Fixed some spelling mistakes

### Version 1.0.0
- Implementation of __"Global Settings Action"__ (Button)
- Implementation of __"Turn On/Off Action"__ (Button)
- Implementation of __"Change Color Action"__ (Button) 
- Implementation of __"Change Brightness Action"__ (Button) 
- Implementation of __"Script Action"__ (Button) 
- Implementation of __"League Of Legends Game Effects Action"__ (Button) 
- Implementation of __"Counter Strike Game Effects Action"__ (Button) 
- Implementation of __"Counter Strike Game Effects Action"__ (Button) 
- Implementation of __"Color Control Action"__ (Encoder) 
- Implementation of __"Brightness Control Action"__ (Encoder) 