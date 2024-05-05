![Alt text](Branding.png?raw=true "")

# PowerTray
Welcome! PowerTray is a powerful battery monitor that sits in your system tray!
The tooltip also displays useful information.
You can also right click on the icon > Click "Battery Info" to see even more information!


## Features
- Power Plan management and switching from taskbar
- Special BatteryBoost profile to optimize battery life
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
2. Go to the Releases section of the Github page (middle right) and click on the latest release
2. Download it
3. If your browser blocked it, choose "download anyway"
4. Put it into a folder and don't move it again (this will be explained later)
5. Run PowerTray.exe, and an icon should appear on your system tray (near bottom right of screen) with a number on it  

(note: if you don't see the notification icon, it's likely hidden)  
![Alt text](HiddenTray.png?raw=true "")  
[Click the arrow icon to reveal the hidden tray icon menu and drag it down to the system tray area]  

6. Right click it, which should show a lot of useful options, but right now, click settings
7. Choose Run on Startup or Run as Admin on Startup (requires administrator to enable)  
(note: the benefit of running as admin is so that it can graph CPU usage in the Graphs window)


## Usage
Simply right click the Tray Icon to see all of the available windows of PowerTray  
Battery Info > Information of Battery usage  
Power Plans > Switch Power Plans  
Graphs > Graphs of battery discharge rates and CPU power usage  
Switch Tray Data > Cycle the information displayed on the Tray icon  
Settings > A plethora of options to customize PowerTray!  
Quit > Quit the program  


## Creation
This is an open-source project created with Visual Studio  
It was made using the .NET framework and WPF  

Supports Windows 10 & 11  

I used some components from other open source softwares:  
- LibreHardware Monitor to detect more battery information
- another PowerTray for Power Plan switching and fixing
- Unlock-PowerCfg 1.1.0 for unlocking power plans commands