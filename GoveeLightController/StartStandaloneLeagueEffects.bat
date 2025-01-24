@echo off
REM Batch file to start com.davidgolunski.goveelightcontroller.exe with customizable arguments.

REM Define the first argument: necessary to indicate that this is being run without the stream deck
SET EFFECT_MODE=LeagueOfLegendsEffects

REM Modify this line to change the IP addresses (seperated by a "space")
REM Leave this empty to find Govee Devices automatically inside the network
REM Example with 3 IP addresses: 
REM SET IP_ADDRESSES=192.168.178.51 192.168.178.40 192.168.178.41
SET IP_ADDRESSES=

REM Run the program with the specified arguments
com.davidgolunski.goveelightcontroller.exe "%EFFECT_MODE%" %IP_ADDRESSES%

REM Pause the console to view output
PAUSE
