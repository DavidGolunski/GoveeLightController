@echo off
REM Batch file to start com.davidgolunski.goveelightcontroller.exe with customizable arguments.

REM Define the first argument: necessary to indicate that this is being run without the stream deck
SET EFFECT_MODE=CounterStrikeEffects

REM Modify this line to change the IP address
SET IP_ADDRESS=0.0.0.0

REM Add additional IP arguments here as needed, separating them with spaces.
REM Example:
REM SET ADDITIONAL_IPS=192.168.1.1 192.168.1.2
SET ADDITIONAL_IPS=

REM Run the program with the specified arguments
com.davidgolunski.goveelightcontroller.exe "%EFFECT_MODE%" "%IP_ADDRESS%" %ADDITIONAL_IPS%

REM Pause the console to view output
PAUSE
