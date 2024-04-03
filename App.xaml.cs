using System.Windows;
using System.Runtime.InteropServices;
using System.Drawing.Text;

using Windows.Devices.Power;
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

using System.Configuration;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;

using LibreHardwareMonitor.Hardware;
using System.Security.Principal;

/// TODO NOW:

// make tooltip stay open somehow
// make icon auto-darkmode (doesn't work on publish)
// scroll is too sensitive

// INFORMATION GATHERING
// figure out how to use win32 API to make it give weird information (and use same battery as kernel)
// make option for multiple batteries besides the auto-selected one

namespace PowerTray
{
    public partial class App : System.Windows.Application
    {

        // import dll for destroy icon
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool DestroyIcon(IntPtr handle);

        public enum DisplayedInfo { percentage, chargeRate, calcChargeRate };

        // Params ---
        public static DisplayedInfo tray_display = DisplayedInfo.percentage; // (CHANGEABLE)

        static float trayFontSize = 11f; // (CHANGEABLE)
        public static String trayFontType = "Segoe UI";
        static float trayFontQualityMultiplier = 2.0f;

        public static int maxChargeHistoryLength = 60; // in seconds (CHANGEABLE)
        public static int graphsHistoryLength = 120; // in seconds (CHANGEABLE)

        public static int trayRefreshRate = 1000; // in milliseconds (CHANGEABLE)
        public static int batInfoRefreshRate = 500; // in milliseconds (CHANGEABLE)

        static Color chargingColor = Color.Green;
        static Color highColor = Color.Black;
        static Color highDarkColor = Color.White;
        static Color mediumColor = Color.FromArgb(255, 220, 100, 20);
        static Color lowColor = Color.FromArgb(255, 232, 17, 35);

        public static int highAmount = 40; // (CHANGEABLE)
        public static int mediumAmount = 25; // (CHANGEABLE)
        public static int lowAmount = 0;
        // ---
        public static uint batteryTag = 0;
        public static SafeFileHandle batteryHandle = null;

        public static bool firstTime = true;
        public static bool graphFirstTime = true;
        public static List<int> remainChargeHistory = new List<int>();
        public static List<long> chargeHistoryTime = new List<long>();

        public static long graphCreatedTimeStamp = -1;
        public static ChartValues<ObservablePoint> calcChargeRateGraph = new ChartValues<ObservablePoint>();
        public static ChartValues<ObservablePoint> chargeRateGraph = new ChartValues<ObservablePoint>();
        public static ChartValues<ObservablePoint> cpuWattageGraph = new ChartValues<ObservablePoint>();
        public static ChartValues<ObservablePoint> gpuWattageGraph = new ChartValues<ObservablePoint>();

        // use open hardware to get info about computertron
        static Computer c = new Computer()
        {
            IsGpuEnabled = true,
            IsCpuEnabled = true,
        };


        public static long calcChargeRateMw = 0;
        public static long calcTimeDelta = 0;

        public static bool windowDarkMode = false;

        public static BatInfo batteryInfoWindow;
        public static Graph graphWindow;
        public static Settings settingsWindow;
        
        public static bool aot = true; // always on top

        static System.Windows.Threading.DispatcherTimer tray_timer = new System.Windows.Threading.DispatcherTimer();
        static System.Windows.Threading.DispatcherTimer info_timer = new System.Windows.Threading.DispatcherTimer();

        static TaskbarIcon trayIcon;
        static ToolTip toolTip;
        static Font trayFont = new Font(trayFontType, trayFontSize * trayFontQualityMultiplier, System.Drawing.FontStyle.Bold);

        private static ICommand BatInfoOpen = new RelayCommand<dynamic>(action => CreateInfoWindow(), canExecute => true);
        private static ICommand SettingsOpen = new RelayCommand<dynamic>(action => CreateSettingsWindow(), canExecute => true);
        private static ICommand QuitProgram = new RelayCommand<dynamic>(action => Quit(), canExecute => true);
        private static ICommand TraySwitch = new RelayCommand<dynamic>(action => SwitchTrayInfo(), canExecute => true);
        private static ICommand GraphsOpen = new RelayCommand<dynamic>(action => CreateGraphWindow(), canExecute => true);

        public static float[] GetHardwareInfo()
        {
            float cpuWattage = 0;
            float gpuWattage = 0;

            foreach (var hardware in c.Hardware)
            {

                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    // only fire the update when found
                    hardware.Update();

                    // loop through the data
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Power && sensor.Name.Contains("CPU Package"))
                        {
                            // store
                            cpuWattage = sensor.Value.GetValueOrDefault();
                        }
                    }
                }
                // Targets AMD & Nvidia GPUS
                if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd || hardware.HardwareType == HardwareType.GpuIntel)
                {
                    // only fire the update when found
                    hardware.Update();

                    // loop through the data
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Power && sensor.Name.Contains("GPU Power"))
                        {
                            // store
                            gpuWattage = sensor.Value.GetValueOrDefault();
                        }
                    }
                }
            }

            return [cpuWattage, gpuWattage];
        }

        public static void ResetGraphs()
        {
            calcChargeRateGraph.Clear();
            chargeRateGraph.Clear();
            cpuWattageGraph.Clear();
            gpuWattageGraph.Clear();
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

            trayRefreshRate = settings.TrayRefreshRate;
            tray_timer.Interval = new TimeSpan(0, 0, 0, 0, trayRefreshRate);
            batInfoRefreshRate = settings.BatInfoRefreshRate;
            info_timer.Interval = new TimeSpan(0, 0, 0, 0, batInfoRefreshRate);

            highAmount = settings.MediumCharge;
            mediumAmount = settings.LowCharge;

            tray_display = settings.TrayText;

            // reset trayfont
            trayFont = new Font(trayFontType, trayFontSize * trayFontQualityMultiplier, settings.FontStyle);
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
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
                Items = {batteryInfo, graphs, switchInfo, settings, exit }
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

        }

        private void UpdateTray(object sender, EventArgs e)
        {
            var info = BatteryManagement.GetBatteryTag(); // prevent data 
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
            }

            // switch between dark and light icons (for taskbar)
            if (darkModeEnabled)
            {
                batteryInfoWindow.Icon = ToImageSource(PowerTray.Resources.DarkIcon);
                settingsWindow.Icon = ToImageSource(PowerTray.Resources.DarkIcon);
            }
            else
            {
                batteryInfoWindow.Icon = ToImageSource(PowerTray.Resources.LightIcon);
                settingsWindow.Icon = ToImageSource(PowerTray.Resources.LightIcon);
            }

            // ---

            var bat_info = BatteryManagement.GetBatteryInfo(batteryTag, batteryHandle);
            int fullChargeCapMwh = (int)bat_info["Battery Capacity mWh"];
            int remainChargeCapMwh = (int)bat_info["Remaining Charge mWh"];
            int chargeRateMw = (int)bat_info["Reported Charge Rate mW"];

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
                    gpuWattageGraph.RemoveAt(0);
                    keys = chargeRateGraph.Select(p => p.X).ToArray();
                }


                int timetemp = (int)timeDelta;
                calcChargeRateGraph.Add(new ObservablePoint(timetemp, -calcChargeRateMw / 1000));
                chargeRateGraph.Add(new ObservablePoint(timetemp, -chargeRateMw / 1000));

                var hwinfo = GetHardwareInfo();

                cpuWattageGraph.Add(new ObservablePoint(timetemp, hwinfo[0]));
                gpuWattageGraph.Add(new ObservablePoint(timetemp, hwinfo[1]));

                if (graphFirstTime && keys.Length == 1)
                {
                    graphFirstTime = false;
                    calcChargeRateGraph.RemoveAt(0);
                    chargeRateGraph.RemoveAt(0);
                    cpuWattageGraph.RemoveAt(0);
                    gpuWattageGraph.RemoveAt(0);

                }
            }
            // ---

            double batteryPercent = (double)bat_info["Percent Remaining"];


            int roundPercent = (int)Math.Round(batteryPercent, 0);

            Color statusColor = highColor;

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

            
            if (tray_display == DisplayedInfo.percentage)
            {
                trayIconText = roundPercent == 100 ? ":)" : roundPercent.ToString();
            }else if (tray_display == DisplayedInfo.chargeRate)
            {
                trayIconText = string.Format("{0:F" + (Math.Abs(chargeRateMw / 1000) >= 10 ? 0 : 1) + "}", Math.Abs(chargeRateMw / 1000f));
            }
            else if (tray_display == DisplayedInfo.calcChargeRate)
            {
                trayIconText = string.Format("{0:F" + (Math.Abs(calcChargeRateMw / 1000) >= 10 ? 0 : 1) + "}", Math.Abs(calcChargeRateMw / 1000f));
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
            System.Windows.Application.Current.Shutdown();
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

            string reported_charge_time_text = (chargeRateMw > 0 ? (isCharging ? "\nCharging: " + rtimeLeft
                + " until fully charged" : "\nnot charging") + "" : rtimeLeft + " remaining");

            string ctimeLeft = GetCalculatedTimeLeft(remainChargeCapMwh, fullChargeCapMwh);

            string calculated_charge_time_text = (chargeRateMw > 0 ? (isCharging ? "\nCharging: " + ctimeLeft
                + " until fully charged" : "\nnot charging") + "" : ctimeLeft + " remaining");

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
                "\n\n(The tray is currently displaying " + tray_display.ToString() + ")";
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
        T ExecuteWithRetry<T>(Func<T> function, bool throwWhenFail = true)
        {
            for (var i = 0; ;)
            {
                try
                {
                    return function();
                }
                catch when (i++ < 5)
                {
                    // Swallow exception if retry is possible.
                }
                catch when (!throwWhenFail)
                {
                    // Return default value if not throwing exception.
                    return default;
                }
            }
        }
    }
}
