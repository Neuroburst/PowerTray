# PowerTray
Welcome! PowerTray is a simple battery monitor that sits in your system tray!
The tooltip also displays useful information.
You can also right click on the icon > Click "Battery Info" to see even more information!

## Features
- Displays battery capacity, charge, lifetime, and status
- Allows you to see more advanced information, like voltage, design capacity, and battery health
- Calculates discharge and charge rate manually for a more accurate reading than the system
- Uses minimal resources and stays out of your way
- Graphs discharge rates and CPU & GPU power usage
- Tray, tooltip, and battery information updates frequently
- Customizable tray display
- And so much more!

## Installation

1. Make sure you have .NET 8.0 installed! (or use the self contained version of PowerTray)
2. Download the latest release

### If you don't want PowerTray to run as admin
    3. To get to your startup folder, press Windows+R, type "shell:startup", then press enter
    4. Put PowerTray.exe in that folder
    5. It will now run every time your computer starts up!

### If you do want PowerTray to run as admin (allows graphing of CPU and GPU power usage)
    3. Put PowerTray into any desired folder (don't move it again otherwise the shortcut will break!)
    4. Open the startup folder by pressing Windows+R, typing "shell:startup", then pressing enter
    5. Create a shortcut of PowerTray in that folder
    6. Right click the shortcut and click properties
    7. Click "Advanced..." and check the "Run as administrator" box
    8. Click OK in that window, as well as the properties window
    9. It will now run as admin every time your computer starts up!



## Creation
This is an open-source project created with Visual Studio.
It was made using the .NET framework and WPF.

Supports Windows 10 & 11

I used some components from LibreHardware Monitor to detect more battery information.