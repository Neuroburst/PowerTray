using System.Windows;
using System.Runtime.InteropServices;
using System.Drawing.Text;

using Windows.System.Power;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Drawing;

using System.Windows.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;
using Hardcodet.Wpf.TaskbarNotification;
using Wpf.Ui.Input;
using System.Windows.Input;
using System.Collections.Specialized;

using System.Diagnostics;
using System.Windows.Media.Imaging;

using System.Windows.Interop;

using LiveCharts;
using LiveCharts.Defaults;

using LibreHardwareMonitor.Hardware;
using System.Security.Principal;
using LibreHardwareMonitor.Hardware.Cpu;
using Microsoft.Win32.TaskScheduler;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Xml.Linq;


/// TODO:
// link battery saver with battery boost?
// batterybooost at certain percantage?
// AFTER THESE ARE ADDED AND FATHER DOES TESTING, RELEASE 2.0!!!

// ADD THESE FEATURES TOO?
//# Get Power Settings entries and add/set 'Attributes' to 2 to unhide
//$PowerCfg = (Get - ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerSettings' - Recurse).Name - notmatch '\bDefaultPowerSchemeValues|(\\[0-9]|\b255)$'
//foreach ($item in $PowerCfg) { Set-ItemProperty -Path $item.Replace('HKEY_LOCAL_MACHINE','HKLM:') -Name 'Attributes' -Value 2 -Force }


/// SUFFERING:
// make icon auto-darkmode (doesn't work on publish)

// card expanders have annoyingly small click area
// font is unreadable in light mode :(
// sub-context menu (power plan) broked if not first in the list
// scroll is too sensitive

// make tooltip stay open somehow
// figure out how to use win32 API to make it give weird information (and use same battery as kernel)
// make option for multiple batteries besides the auto-selected one

namespace PowerTray
{
    public class PowerPlan
    {
        public readonly string Name;
        public Guid Guid;

        public PowerPlan(string name, Guid guid)
        {
            Name = name;
            Guid = guid;
        }
    }

    public partial class App : System.Windows.Application
    {

        // import dll for destroy icon
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool DestroyIcon(IntPtr handle);

        public enum DisplayedInfo { Percentage, ReportedChargeRate, CalculatedChargeRate, PowerPlan};


        // PowerPlans
        public static Wpf.Ui.Controls.MenuItem pwrPlans;

        public static Guid active_plan;

        // Params ---
        public static List<PowerPlan> plans = new List<PowerPlan>();

        public static bool startup = false;
        public static bool adminstartup = false;

        public static BatteryStatus? charging = null;
        public static string acplanName = "Balanced";
        public static string batteryPlanName = "Balanced";
        public static bool notifs = true;

        public static DisplayedInfo tray_display = DisplayedInfo.Percentage; // (CHANGEABLE)

        static float trayFontSize = 11f; // (CHANGEABLE)
        public static String trayFontType = "Segoe UI";
        static float trayFontQualityMultiplier = 2.0f;

        public static int maxChargeHistoryLength = 60; // in seconds (CHANGEABLE)
        public static int graphsHistoryLength = 120; // in seconds (CHANGEABLE)

        public static int trayRefreshRate = 1000; // in milliseconds (CHANGEABLE)
        public static int batInfoRefreshRate = 1000; // in milliseconds (CHANGEABLE)

        public static int graphRefreshRate = 2000; // in milliseconds (CHANGEABLE)

        static Color chargingColor = Color.Green;
        static Color highColor = Color.Black;
        static Color highDarkColor = Color.White;
        static Color mediumColor = Color.FromArgb(255, 220, 100, 20);
        static Color lowColor = Color.FromArgb(255, 232, 17, 35);
        static Color boostColor = Color.Cyan;

        public static int highAmount = 40; // (CHANGEABLE)
        public static int mediumAmount = 25; // (CHANGEABLE)
        public static int lowAmount = 0;
        // ---
        public static uint batteryTag = 0;
        public static SafeFileHandle batteryHandle = null;

        public static bool auto_switch = false;

        public static bool firstTime = true;
        public static bool graphFirstTime = true;
        public static List<int> remainChargeHistory = new List<int>();
        public static List<long> chargeHistoryTime = new List<long>();

        public static long graphCreatedTimeStamp = -1;
        public static ChartValues<ObservablePoint> calcChargeRateGraph = new ChartValues<ObservablePoint>();
        public static ChartValues<ObservablePoint> chargeRateGraph = new ChartValues<ObservablePoint>();
        public static ChartValues<ObservablePoint> cpuWattageGraph = new ChartValues<ObservablePoint>();

        // use open hardware to get info about computertron
        static Computer c = new Computer()
        {
            IsCpuEnabled = true,
        };


        public static long calcChargeRateMw = 0;
        public static long calcTimeDelta = 0;

        public static bool windowDarkMode = true;

        public static BatInfo batteryInfoWindow;
        public static Graph graphWindow;
        public static Settings settingsWindow;
        
        public static bool aot = true; // always on top

        static System.Windows.Threading.DispatcherTimer tray_timer = new System.Windows.Threading.DispatcherTimer();
        static System.Windows.Threading.DispatcherTimer info_timer = new System.Windows.Threading.DispatcherTimer();
        static System.Windows.Threading.DispatcherTimer graph_timer = new System.Windows.Threading.DispatcherTimer();

        static TaskbarIcon trayIcon;
        static ToolTip toolTip;
        static Font trayFont = new Font(trayFontType, trayFontSize * trayFontQualityMultiplier, System.Drawing.FontStyle.Bold);

        private static ICommand BatInfoOpen = new RelayCommand<dynamic>(action => CreateInfoWindow(), canExecute => true);
        private static ICommand SettingsOpen = new RelayCommand<dynamic>(action => CreateSettingsWindow(), canExecute => true);
        private static ICommand QuitProgram = new RelayCommand<dynamic>(action => Quit(), canExecute => true);
        private static ICommand TraySwitch = new RelayCommand<dynamic>(action => SwitchTrayInfo(), canExecute => true);
        private static ICommand GraphsOpen = new RelayCommand<dynamic>(action => CreateGraphWindow(), canExecute => true);

        private static string GetExecutableLocation()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }

        public static void ManageAdminStartup(bool delete)
        {
            if (IsAdministrator())
            {
                // Get the service on the local machine
                using (TaskService ts = new TaskService())
                {
                    if (delete)
                    {
                        if (ts.RootFolder.Tasks.Exists("PowerTray"))
                        {
                            ts.RootFolder.DeleteTask("PowerTray");
                        }
                    }
                    else
                    {
                        ManageAdminStartup(true);
                        // Create a new task definition and assign properties
                        TaskDefinition td = ts.NewTask();
                        td.RegistrationInfo.Description = "Run PowerTray on Startup";
                        td.Triggers.Add(new LogonTrigger { });
                        td.Actions.Add(new ExecAction(GetExecutableLocation()));

                        td.Principal.RunLevel = TaskRunLevel.Highest;
                        td.Settings.DisallowStartIfOnBatteries = false;
                        td.Settings.StopIfGoingOnBatteries = false;
                        td.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                        var registeredTask = ts.RootFolder.RegisterTaskDefinition(@"PowerTray", td);
                        registeredTask.Enabled = true;
                    }
                }
            }
        }
        public static void ManageStartup(bool delete)
        {
            var keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
            if (delete)
            {
                var key = Registry.CurrentUser.OpenSubKey(keyPath, true);
                if (key != null && key.GetValue("PowerTray") != null)
                {
                    key.DeleteValue("PowerTray");
                }                    
            }
            else
            {
                ManageStartup(true);
                Registry.SetValue(@"HKEY_CURRENT_USER\" + keyPath, "PowerTray", GetExecutableLocation(), RegistryValueKind.String);
            }
        }

        public static float GetHardwareInfo()
        {
            float cpuWattage = 0;

            foreach (var hardware in c.Hardware)
            {

                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    // only fire the update when found
                    hardware.Update();

                    // loop through the data
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Power && sensor.Name.ToLower().Contains("pack"))
                        {
                            cpuWattage = sensor.Value.GetValueOrDefault();
                        }
                    }
                }
            }

            return cpuWattage;
        }

        public static void ResetGraphs()
        {
            calcChargeRateGraph.Clear();
            chargeRateGraph.Clear();
            cpuWattageGraph.Clear();
            graphCreatedTimeStamp = -1;
        }
        
        public static void ResetBuffer()
        {
            firstTime = true;
            remainChargeHistory = new List<int>();
            chargeHistoryTime = new List<long>();
            calcChargeRateMw = 0;
            calcTimeDelta = 0;
        }

        // ERROR HANDLING
        void ShowUnhandledException(Exception e, string unhandledExceptionType, bool promptUserForShutdown)
        {
            var messageBoxTitle = $"Unexpected Error Occurred: {unhandledExceptionType}";
            var messageBoxMessage = $"The following exception occurred:\n\n{e}";
            var messageBoxButtons = System.Windows.MessageBoxButton.OK;

            if (promptUserForShutdown)
            {
                messageBoxMessage += "\n\nNormally the program would DIE now. Should we let it die painlessly, or let it suffer?";
                messageBoxButtons = System.Windows.MessageBoxButton.YesNo;
            }

            // Let the user decide if the app should die or not (if applicable).
            if (System.Windows.MessageBox.Show(messageBoxMessage, messageBoxTitle, messageBoxButtons) == System.Windows.MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
        private void SetupUnhandledExceptionHandling()
        {
            // Catch exceptions from all threads in the AppDomain.
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                ShowUnhandledException(args.ExceptionObject as Exception, "AppDomain.CurrentDomain.UnhandledException", false);

            // Catch exceptions from each AppDomain that uses a task scheduler for async operations.
            TaskScheduler.UnobservedTaskException += (sender, args) =>
                ShowUnhandledException(args.Exception, "TaskScheduler.UnobservedTaskException", false);

            // Catch exceptions from a single specific UI dispatcher thread.
            Dispatcher.UnhandledException += (sender, args) =>
            {
                // If we are debugging, let Visual Studio handle the exception and take us to the code that threw it.
                if (!Debugger.IsAttached)
                {
                    args.Handled = true;
                    ShowUnhandledException(args.Exception, "Dispatcher.UnhandledException", true);
                }
            };
        }


        public static void LoadSettings()
        {
            var settings = (Options)settingsWindow.AppConfig.Sections["Options"];
            aot = settings.AlwaysOnTop;
            batteryInfoWindow.Topmost = aot;
            trayFontSize = settings.FontSize;
            maxChargeHistoryLength = settings.BufferSize;
            graphsHistoryLength = settings.HistoryLength;

            startup = settings.Startup;
            adminstartup = settings.AdminStartup;

            // prevent overlap
            if (startup && adminstartup)
            {
                startup = false;
            }

            auto_switch = settings.AutoSwitch;
            notifs = settings.Notifs;
            acplanName = settings.ACPlan;
            batteryPlanName = settings.BatteryPlan;

            trayRefreshRate = settings.TrayRefreshRate;
            tray_timer.Interval = new TimeSpan(0, 0, 0, 0, trayRefreshRate);
            batInfoRefreshRate = settings.BatInfoRefreshRate;
            info_timer.Interval = new TimeSpan(0, 0, 0, 0, batInfoRefreshRate);

            graphRefreshRate = settings.GraphRefreshRate;
            graph_timer.Interval = new TimeSpan(0, 0, 0, 0, graphRefreshRate);

            highAmount = settings.MediumCharge;
            mediumAmount = settings.LowCharge;

            tray_display = settings.TrayText;

            // reset trayfont
            trayFont = new Font(trayFontType, trayFontSize * trayFontQualityMultiplier, settings.FontStyle);

            // add startup
            if (startup)
            {
                ManageStartup(false);
            }
            else
            {
                ManageStartup(true);
            }

            if (adminstartup)
            {
                ManageAdminStartup(false);
            }
            else
            {
                ManageAdminStartup(true);
            }
        }
        

        private void App_Startup(object sender, StartupEventArgs e)
        {
            // repair startup
            if (startup)
            {
                ManageStartup(false);
            }

            if (startup)
            {
                ManageAdminStartup(false);
            }

            // debug
            SetupUnhandledExceptionHandling();

            c.Open(); // open hardware for inspection

            batteryInfoWindow = new BatInfo();
            batteryInfoWindow.Topmost = aot;
            graphWindow = new Graph();
            graphWindow.Topmost = aot;
            settingsWindow = new Settings();

            LoadSettings();

            // apply theme
            ApplicationThemeManager.Apply(
                  (checkDarkMode()[1] ? ApplicationTheme.Dark : ApplicationTheme.Light), // Theme type
                  WindowBackdropType.Mica, // Background type
                  true                    // Whether to change accents automatically
            );
            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(batteryInfoWindow);

            // get battery tag from info
            var info = BatteryManagement.GetBatteryTag();
            batteryHandle = info[0];
            batteryTag = info[1];

            var batteryInfo = new Wpf.Ui.Controls.MenuItem()
            {
                Header = "Battery Info",
                Icon = new SymbolIcon(SymbolRegular.Info20, 14, false),
                Command = BatInfoOpen,
            };

            var graphs = new Wpf.Ui.Controls.MenuItem()
            {
                Header = "Graphs",
                Icon = new SymbolIcon(SymbolRegular.DataUsage20, 14, false),
                Command = GraphsOpen,
            };

            var switchInfo = new Wpf.Ui.Controls.MenuItem()
            {
                Header = "Switch Tray Data",
                Icon = new SymbolIcon(SymbolRegular.ArrowRepeatAll20, 14, false),
                ToolTip = "Switch the information displayed on the tray icon",
                Command = TraySwitch,
            };

            pwrPlans = new Wpf.Ui.Controls.MenuItem()
            {
                Header = "Power Plans",
                Icon = new SymbolIcon(SymbolRegular.BatterySaver20, 14, false),
            };

            PowerPlans.RefreshPowerPlans();

            var settings = new Wpf.Ui.Controls.MenuItem()
            {
                Header = "Settings",
                Icon = new SymbolIcon(SymbolRegular.Settings20, 14, false),
                Command = SettingsOpen,
            };

            var exit = new Wpf.Ui.Controls.MenuItem()
            {
                Header = "Quit",
                Icon = new SymbolIcon(SymbolRegular.Power20, 14, false),
                Command = QuitProgram
            };

            var contextMenu = new ContextMenu()
            {
                Items = { pwrPlans, batteryInfo, graphs, switchInfo, settings, exit }
            };

            toolTip = new ToolTip();
            trayIcon = new TaskbarIcon()
            {
                TrayToolTip = toolTip,
                ContextMenu = contextMenu,
                LeftClickCommand = BatInfoOpen,
                DoubleClickCommand = SettingsOpen,
            };

            // Create Update Timer for tray icon
            tray_timer.Interval = new TimeSpan(0, 0, 0, 0, trayRefreshRate);
            tray_timer.Tick += new EventHandler(UpdateTray);
            tray_timer.Start();

            info_timer.Interval = new TimeSpan(0, 0, 0, 0, batInfoRefreshRate);
            info_timer.Tick += new EventHandler(BatInfo.UpdateData);
            info_timer.Start();

            graph_timer.Interval = new TimeSpan(0, 0, 0, 0, graphRefreshRate);
            graph_timer.Tick += new EventHandler(UpdateGraphs);
            graph_timer.Start();

        }

        private void UpdateGraphs(object sender, EventArgs e)
        {
            long timeStamp = DateTime.Now.Ticks;
            var bat_info = BatteryManagement.GetBatteryInfo(batteryTag, batteryHandle);
            int fullChargeCapMwh = (int)bat_info["Battery Capacity mWh"];
            int remainChargeCapMwh = (int)bat_info["Remaining Charge mWh"];
            int chargeRateMw = (int)bat_info["Reported Charge Rate mW"];

            if (graphCreatedTimeStamp == -1)
            {
                graphCreatedTimeStamp = timeStamp;
            }

            long timeDelta = (long)((timeStamp - graphCreatedTimeStamp) / 10000000);

            var keys = chargeRateGraph.Select(p => p.X).ToArray();
            while (keys.Length > 0 && timeDelta - keys[0] > graphsHistoryLength)
            {
                calcChargeRateGraph.RemoveAt(0);
                chargeRateGraph.RemoveAt(0);
                cpuWattageGraph.RemoveAt(0);
                keys = chargeRateGraph.Select(p => p.X).ToArray();
            }


            int timetemp = (int)timeDelta;
            calcChargeRateGraph.Add(new ObservablePoint(timetemp, -calcChargeRateMw / 1000d));
            chargeRateGraph.Add(new ObservablePoint(timetemp, -chargeRateMw / 1000d));

            var hwinfo = GetHardwareInfo();

            cpuWattageGraph.Add(new ObservablePoint(timetemp, hwinfo));

            if (graphFirstTime && keys.Length == 1)
            {
                graphFirstTime = false;
                calcChargeRateGraph.RemoveAt(0);
                chargeRateGraph.RemoveAt(0);
                cpuWattageGraph.RemoveAt(0);

            }
            
        }

        private void UpdateTray(object sender, EventArgs e)
        {
            if (active_plan != PowerPlans.GetActivePlanGuid())
            {
                PowerPlans.RefreshPowerPlans();
            }


            var info = BatteryManagement.GetBatteryTag(); // prevent data from expiring
            batteryHandle = info[0];
            batteryTag = info[1];

            // check if dark mode is enabled ---
            bool darkModeEnabled = checkDarkMode()[0];
            bool appdarkMode = checkDarkMode()[1];

            if (appdarkMode != windowDarkMode)
            {
                windowDarkMode = appdarkMode;
                ApplicationThemeManager.Apply(
                      (appdarkMode ? ApplicationTheme.Dark : ApplicationTheme.Light));

                //Style style = (Style)Resources["Style"];
            }

            // switch between dark and light icons (for taskbar)
            if (darkModeEnabled)
            {
                batteryInfoWindow.Icon = ToImageSource(PowerTray.Resources.DarkIcon);
                settingsWindow.Icon = ToImageSource(PowerTray.Resources.DarkIcon);
                graphWindow.Icon = ToImageSource(PowerTray.Resources.DarkIcon);
            }
            else
            {
                batteryInfoWindow.Icon = ToImageSource(PowerTray.Resources.LightIcon);
                settingsWindow.Icon = ToImageSource(PowerTray.Resources.LightIcon);
                graphWindow.Icon = ToImageSource(PowerTray.Resources.LightIcon);
            }

            // ---

            var bat_info = BatteryManagement.GetBatteryInfo(batteryTag, batteryHandle);
            int remainChargeCapMwh = (int)bat_info["Remaining Charge mWh"];
            int chargeRateMw = (int)bat_info["Reported Charge Rate mW"];

            // plans
            if (auto_switch && charging != (BatteryStatus)bat_info["Status"])
            {
                charging = (BatteryStatus)bat_info["Status"];

                if ((charging == BatteryStatus.Idle && (BatteryStatus)bat_info["Status"] == BatteryStatus.Charging) ||
                    (charging == BatteryStatus.Charging && (BatteryStatus)bat_info["Status"] == BatteryStatus.Idle))
                {
                    // since it's still plugged in, don't do anything (idle is when it's not charging but stil connected to AC)
                }
                else // switch plan
                {
                    var ac = charging != BatteryStatus.Discharging;

                    string name = batteryPlanName;
                    if (ac)
                    {
                        name = acplanName;
                    }

                    Guid guid = plans[0].Guid;
                    foreach (PowerPlan plan in plans)
                    {
                        if (plan.Name == name)
                        {
                            guid = plan.Guid;
                        }
                    }
                    PowerPlans.SetPlan(guid, name);
                }
            }

            // update remainChargeHistory ---
            var historyLength = remainChargeHistory.Count;
            if (historyLength == 0 || remainChargeHistory[historyLength - 1] != remainChargeCapMwh)
            {
                long timeStamp = DateTime.Now.Ticks;
                remainChargeHistory.Add(remainChargeCapMwh);
                chargeHistoryTime.Add(timeStamp);

                // cleanup yucky slurpy paste (misleading data point)
                if (firstTime && historyLength == 1)
                {
                    firstTime = false;
                    remainChargeHistory.RemoveAt(0);
                    chargeHistoryTime.RemoveAt(0);
                }

                // calculate charge rate ---
                long charge_delta_Mws = (long)(remainChargeCapMwh - remainChargeHistory[0]) * 3600;
                calcTimeDelta = (timeStamp - chargeHistoryTime[0]) / 10000; // milliseconds

                if (calcTimeDelta != 0)
                {
                    calcChargeRateMw = charge_delta_Mws * 1000 / calcTimeDelta;

                    while (calcTimeDelta > maxChargeHistoryLength * 1000)
                    {
                        remainChargeHistory.RemoveAt(0);
                        chargeHistoryTime.RemoveAt(0);

                        calcTimeDelta = (timeStamp - chargeHistoryTime[0]) / 10000; // milliseconds
                    }
                }
            }
            // ---

            double batteryPercent = (double)bat_info["Percent Remaining"];


            int roundPercent = (int)Math.Round(batteryPercent, 0);

            Color statusColor = highColor;

            if (batteryHandle != null) {
                if (chargeRateMw > 0)
                {
                    statusColor = chargingColor;
                }

                else if (roundPercent >= highAmount)
                {
                    statusColor = highColor;
                }
                else if (roundPercent >= mediumAmount)
                {
                    statusColor = mediumColor;
                }
                else if (roundPercent >= lowAmount)
                {
                    statusColor = lowColor;
                }
            }

            if (PowerPlans.ReadFriendlyName(active_plan) == "BatteryBoost")
            {
                statusColor = boostColor;
            }

            // Lighter text for darkmode
            if (darkModeEnabled)
            {
                if (statusColor == highColor)
                {
                    statusColor = highDarkColor;
                }
                else
                {
                    statusColor = LightenColor(statusColor);
                }
            }
            var toolTipText = CreateTooltipText(bat_info);
                
                
            // Tray Icon ---
            String trayIconText = "!!";

            dynamic decimalMode = null;
            if (tray_display == DisplayedInfo.Percentage)
            {
                trayIconText = roundPercent == 100 ? ":)" : roundPercent.ToString();
            }else if (tray_display == DisplayedInfo.ReportedChargeRate)
            {
                trayIconText = string.Format("{0:F" + (Math.Abs(chargeRateMw / 1000) >= 10 ? 0 : 1) + "}", Math.Abs(chargeRateMw / 1000f));

                decimalMode = chargeRateMw;
            }
            else if (tray_display == DisplayedInfo.CalculatedChargeRate)
            {
                trayIconText = string.Format("{0:F" + (Math.Abs(calcChargeRateMw / 1000) >= 10 ? 0 : 1) + "}", Math.Abs(calcChargeRateMw / 1000f));

                decimalMode = calcChargeRateMw;
            }
            else if (tray_display == DisplayedInfo.PowerPlan)
            {
                trayIconText = PowerPlans.ReadFriendlyName(active_plan).Substring(0, 2);
                //decimalMode = 1.1;
            }

            if (trayFontSize > 8.5 && decimalMode != null)
            {
                var font_size = trayFontSize;
                if (Math.Abs(decimalMode / 1000) < 10)
                {
                    font_size = 8.5f;
                }
                var settings = (Options)settingsWindow.AppConfig.Sections["Options"];
                trayFont = new Font(trayFontType, font_size * trayFontQualityMultiplier, settings.FontStyle);
            }



            SolidBrush trayFontColor = new SolidBrush(statusColor);

            float dpi;
            int textWidth, textHeight;

            // Measure the rendered size of tray icon text under the current system DPI setting.
            using (var bitmap = new Bitmap(1, 1))
            {
                SizeF size;
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    // Measure the rendering size of the tray icon text using this font.
                    size = graphics.MeasureString(trayIconText, trayFont);
                    dpi = graphics.DpiX * trayFontQualityMultiplier;
                }

                // Round the size to integer.
                textWidth = (int)Math.Round(size.Width);
                textHeight = (int)Math.Round(size.Height);
            }

            var iconDimension = (int)Math.Round(16 * (dpi / 96));

            // Draw the tray icon
            using (var bitmap = new Bitmap(iconDimension, iconDimension))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    if (!darkModeEnabled)
                    {
                        // Anti-Aliasing looks the best when the taskbar is in light mode
                        graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                    }

                    // Draw the text, centering it, and padding it with 1%
                    graphics.DrawString(trayIconText, trayFont, trayFontColor,
                        (iconDimension - textWidth) / 2f,
                        (iconDimension - textHeight) / 2f);

                    // The above scaling and start position alignments aim to remove the
                    // padding of the font so that the text fills the tray icon edge to edge.
                }

                // Set tray icon from the drawn bitmap image.
                System.IntPtr intPtr = bitmap.GetHicon();
                try
                {
                    trayIcon.Icon = Icon.FromHandle(intPtr);
                    toolTip.Content = toolTipText;
                }
                finally
                {
                    // Destroy icon hand to release it from memory as soon as it's set to the tray.
                    DestroyIcon(intPtr);
                    // This should be the very last call when updating the tray icon.
                }
            }
            //---
        }

        public static void SwitchTrayInfo()
        {
            if ((int)tray_display + 1 > Enum.GetValues(typeof(DisplayedInfo)).Length - 1)
            {
                tray_display = (DisplayedInfo)0;
            }
            else
            {
                tray_display = (DisplayedInfo)((int)tray_display + 1);
            }
        }
        private static Color LightenColor(Color color)
        {
            var amount = 30;
            Color lightColor = Color.FromArgb(color.A,
                Math.Min((int)(color.R + amount), 255), Math.Min((int)(color.G + amount), 255), Math.Min((int)(color.B + amount), 255));
            return lightColor;
        }

        // Tray Icon Helper Functions ---
        private static void Quit() // check if the exit button was pressed
        {
            trayIcon.Dispose();
            Current.Shutdown();
        }

        public static string GetCalculatedTimeLeft(int remainChargeCapMwh, int fullChargeCapMwh)
        {
            double ctimeLeft = 0;
            if (calcChargeRateMw < 0)
            {
                ctimeLeft = (remainChargeCapMwh / -(double)calcChargeRateMw) * 60;
            }

            if (calcChargeRateMw > 0)
            {
                ctimeLeft = ((fullChargeCapMwh - remainChargeCapMwh) / (double)calcChargeRateMw) * 60;

            }
            return EasySecondsToTime((int)ctimeLeft);
        }

        private string CreateTooltipText(OrderedDictionary bat_info)
        {
            // use battery info
            bool isCharging = (BatteryStatus)bat_info["Status"] == BatteryStatus.Charging;

            int fullChargeCapMwh = (int)bat_info["Battery Capacity mWh"];
            int remainChargeCapMwh = (int)bat_info["Remaining Charge mWh"];
            int chargeRateMw = (int)bat_info["Reported Charge Rate mW"];

            double batteryPercent = (double)bat_info["Percent Remaining"];

            string rtimeLeft = (string)bat_info["Reported Time Left"];

            string reported_charge_time_text = (chargeRateMw > 0 ? (isCharging ? "Charging: " + rtimeLeft
                + " until fully charged" : "Not charging") + "" : rtimeLeft + " remaining");

            string ctimeLeft = GetCalculatedTimeLeft(remainChargeCapMwh, fullChargeCapMwh);

            string calculated_charge_time_text = (chargeRateMw > 0 ? (isCharging ? "Charging: " + ctimeLeft
                + " until fully charged" : "Not charging") + "" : ctimeLeft + " remaining");

            String toolTipText =
                Math.Round(batteryPercent, 3).ToString() + "% " + 
                (isCharging ? "connected to AC\n" : "on battery\n") +
                "Current Charge: " + remainChargeCapMwh.ToString() + " mWh" +
                
                "\n\nReported Data:\n" + 
                reported_charge_time_text + 
                "\n" + (chargeRateMw > 0 ? "Charge Rate: " : "Discharge Rate: ") + Math.Abs(chargeRateMw).ToString() + " mW" +

                "\n\nCalulated Data:\n" +
                calculated_charge_time_text +
                "\n" + (calcChargeRateMw > 0 ? "Charge Rate: " : "Discharge Rate: ") + Math.Abs(calcChargeRateMw).ToString() + " mW" +
                "\n" + "Buffer Size: " + ((int)(calcTimeDelta / 1000)).ToString() + " sec" + 
                "\n\n(The tray is currently\ndisplaying " + tray_display.ToString() + ")";
            return toolTipText;
        }

        private static void CreateInfoWindow()
        {
            batteryInfoWindow = new BatInfo();
            batteryInfoWindow.Topmost = aot;
            batteryInfoWindow.Show();
        }

        public static void CreateGraphWindow()
        {
            graphWindow = new Graph();
            graphWindow.Topmost = aot;
            graphWindow.Show();
        }
        public static void CreateSettingsWindow()
        {
            settingsWindow = new Settings();
            settingsWindow.Show();
        }

        public static bool[] checkDarkMode()
        {
            string RegistryKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\";
            int appLightKey = (int)Registry.GetValue(RegistryKey, "AppsUseLightTheme", -1);
            int trayLightKey = (int)Registry.GetValue(RegistryKey, "SystemUsesLightTheme", -1);
            
            bool appdark = !Convert.ToBoolean(appLightKey == -1 ? true : appLightKey);
            bool traydark = !Convert.ToBoolean(trayLightKey == -1 ? false : trayLightKey);
            
            // zero is taskbar, one is system
            return [traydark, appdark];
        }

        public static System.Windows.Media.ImageSource ToImageSource(Icon icon)
        {
            System.Windows.Media.ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return imageSource;
        }

        public static String EasySecondsToTime(int seconds) // Convert seconds to a readable format
        {
            String time = "_";

            if (seconds < 60)
            {
                time = seconds.ToString() + " mins";
            }
            else
            {
                time = (seconds / 60).ToString() + " hr" + (seconds / 60 == 1 ? "" : "s") + " and " + (seconds % 60).ToString() + " mins";
            }

            if (seconds == -1 || seconds == 0)
            {
                time = "Unknown";
            }
            return time;
        }
        public static bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                      .IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
