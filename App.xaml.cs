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

// TODO:
// fix weird spike when collected battery buffer info

// scroll is too sensitive
// make tooltip stay open somehow

// INFORMATION GATHERING
// figure out how to use win32 API to make it give weird information (and use same battery as kernel)
// make option for multiple batteries besides the auto-selected one

// MORE MENUS AND OPTIONS
// make option to switch tray info displayed on the icon
// add settings menu (for public options and update speed, auto-update, better discharge calc, and default tray view)
// graph calcDischarge rate and other things

namespace PowerTray
{
    public partial class App : System.Windows.Application
    {
        // import dlls
        [DllImport("user32.dll", CharSet = CharSet.Auto)]

        static extern bool DestroyIcon(IntPtr handle);
        // Params ---
        static float trayFontSize = 11.5f;
        public static String trayFontType = "Segoe UI";
        static float trayFontQualityMultiplier = 2.0f;

        public static int remainChargeHistorySize = 60;

        public static int trayRefreshRate = 1000; // in milliseconds
        public static int batInfoRefreshRate = 500;
        public static bool batInfoAutoRefresh = true;

        static Color chargingColor = Color.Green;
        static Color highColor = Color.Black;
        static Color highDarkColor = Color.White;
        static Color mediumColor = Color.FromArgb(255, 220, 100, 20);
        static Color lowColor = Color.FromArgb(255, 232, 17, 35);

        public static int highAmount = 40;
        public static int mediumAmount = 30;
        public static int lowAmount = 0;
        // ---
        public static uint batteryTag = 0;
        public static SafeFileHandle batteryHandle = null;

        public static int[] remainChargeHistory = new int[remainChargeHistorySize];
        public static long[] chargeHistoryTime = new long[remainChargeHistorySize];
        static int bufferSize = 0;
        static int bufferEndIdx = 0;
        public static long calcChargeRateMw = 0;

        static TaskbarIcon trayIcon;
        static ToolTip toolTip;
        static Font trayFont = new Font(trayFontType, trayFontSize * trayFontQualityMultiplier, System.Drawing.FontStyle.Bold);

        private static ICommand BatInfoOpen = new RelayCommand<dynamic>(action => CreateInfoWindow(), canExecute => true);
        private static ICommand SettingsOpen = new RelayCommand<dynamic>(action => CreateSettingsWindow(), canExecute => true);
        private static ICommand QuitProgram = new RelayCommand<dynamic>(action => Quit(), canExecute => true);

        public static void ResetBuffer()
        {
            remainChargeHistory = new int[remainChargeHistorySize];
            chargeHistoryTime = new long[remainChargeHistorySize];
            bufferSize = 0;
            bufferEndIdx = 0;
            calcChargeRateMw = 0;
        }
        private void App_Startup(object sender, StartupEventArgs e)
        {
            // get battery tag from info
            var info  = BatteryManagement.GetBatteryTag();
            batteryHandle = info[0];
            batteryTag = info[1];

            var batteryInfo = new Wpf.Ui.Controls.MenuItem()
            {
                Header = "Battery Info",
                Icon = new SymbolIcon(SymbolRegular.Info20, 14, false),
                Command = BatInfoOpen,
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
                Items = { batteryInfo, settings, exit }
            };

            toolTip = new ToolTip();
            trayIcon = new TaskbarIcon()
            {
                TrayToolTip = toolTip,
                ContextMenu = contextMenu,
                //LeftClickCommand = toggleTooltipPin,
                //DoubleClickCommand = CommonCommands.OpenSettingsWindowCommand
            };

            // Create Update Timer for tray icon
            System.Windows.Threading.DispatcherTimer tray_timer = new System.Windows.Threading.DispatcherTimer();
            tray_timer.Interval = new TimeSpan(0, 0, 0, 0, trayRefreshRate);
            tray_timer.Tick += new EventHandler(UpdateTray);
            tray_timer.Start();

            System.Windows.Threading.DispatcherTimer info_timer = new System.Windows.Threading.DispatcherTimer();
            info_timer.Interval = new TimeSpan(0, 0, 0, 0, batInfoRefreshRate);
            info_timer.Tick += new EventHandler(BatInfo.UpdateData);
            info_timer.Start();

            
        }
        
        private void UpdateTray(object sender, EventArgs e)
        {
            // check if dark mode is enabled ---
            bool darkModeEnabled = checkDarkMode()[0];

            ApplicationThemeManager.Apply(
                  (checkDarkMode()[1] ? ApplicationTheme.Dark : ApplicationTheme.Light),   // Theme type
                  WindowBackdropType.Mica, // Background type
                  true                    // Whether to change accents automatically
            );
            // ---

            var bat_info = BatteryManagement.GetBatteryInfo(batteryTag, batteryHandle);
            int fullChargeCapMwh = (int)bat_info["Battery Capacity mWh"];
            int remainChargeCapMwh = (int)bat_info["Remaining Charge mWh"];
            int chargeRateMw = (int)bat_info["Charge Rate mW"];


            // update remainChargeHistory ---
            int oldIndex = bufferEndIdx - 1;
            if (oldIndex < 0)
            {
                oldIndex += remainChargeHistorySize;
            }
            if (remainChargeHistory[oldIndex] != remainChargeCapMwh)
            {
                long timeStamp = DateTime.Now.Ticks;
                remainChargeHistory.SetValue(remainChargeCapMwh, bufferEndIdx);
                chargeHistoryTime.SetValue(timeStamp, bufferEndIdx);
                // calculate charge rate ---
                if (bufferSize < remainChargeHistorySize)
                {
                    bufferSize += 1;
                }
                int start_idx = bufferEndIdx - bufferSize + 1;
                if (start_idx < 0)
                {
                    start_idx += remainChargeHistorySize;
                }
                long time_delta_ms = (chargeHistoryTime[bufferEndIdx] - chargeHistoryTime[start_idx]) / 10000;
                long charge_delta_Mws = (long)(remainChargeHistory[bufferEndIdx] - remainChargeHistory[start_idx]) * 3600;

                if (time_delta_ms != 0)
                {
                    calcChargeRateMw = charge_delta_Mws * 1000 / time_delta_ms;
                }

                bufferEndIdx += 1;
                if (bufferEndIdx >= remainChargeHistorySize)
                {
                    bufferEndIdx = 0;
                }
            }
            // ---

            double batteryPercent = (double)bat_info["Percent Remaining"];

            double timeLeft = 0;
            if (chargeRateMw < 0)
            {
                timeLeft = (remainChargeCapMwh / -(double)chargeRateMw) * 60;
            }

            if (chargeRateMw > 0)
            {
                timeLeft = ((fullChargeCapMwh - remainChargeCapMwh) / (double)chargeRateMw) * 60;
            }
            // ---


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
            String trayIconText = roundPercent == 100 ? ":)" : roundPercent.ToString();
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

        private string CreateTooltipText(OrderedDictionary bat_info)
        {
            // use battery info
            bool isCharging = (BatteryStatus)bat_info["Status"] == BatteryStatus.Charging;

            int fullChargeCapMwh = (int)bat_info["Battery Capacity mWh"];
            int remainChargeCapMwh = (int)bat_info["Remaining Charge mWh"];
            int chargeRateMw = (int)bat_info["Charge Rate mW"];

            double batteryPercent = (double)bat_info["Percent Remaining"];

            double timeLeft = 0;
            if (chargeRateMw < 0)
            {
                timeLeft = (remainChargeCapMwh / -(double)chargeRateMw) * 60;
            }

            if (chargeRateMw > 0)
            {
                timeLeft = ((fullChargeCapMwh - remainChargeCapMwh) / (double)chargeRateMw) * 60;

            }
            String toolTipText =
                Math.Round(batteryPercent, 3).ToString() + "% " + (isCharging ? "connected to AC" : "on battery\n" +
                EasySecondsToTime((int)timeLeft) + " remaining") +
                (chargeRateMw > 0 ? (isCharging ? "\nCharging: " + EasySecondsToTime((int)timeLeft) + " until fully charged" : "\nnot charging") + "" : "") +
                "\n\n" + "Current Charge: " + remainChargeCapMwh.ToString() + " mWh" +
                "\n" + (chargeRateMw > 0 ? "Charge Rate: " : "Discharge Rate: ") + Math.Abs(chargeRateMw).ToString() + " mW" +
                "\n" + (calcChargeRateMw > 0 ? "Calculated Charge Rate: " : "Calculated Discharge Rate: ") + Math.Abs(calcChargeRateMw).ToString() + " mW";
                //"\n\n(click on the tray icon to " + (tooltipPinned? "unpin" : "pin") + " this tooltip)";
            return toolTipText;
        }

        private static void CreateInfoWindow()
        {
            BatInfo dialog = new BatInfo();
            dialog.Show();
        }

        public static void CreateSettingsWindow()
        {
            Settings dialog = new Settings();
            dialog.Show();
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
        public static String EasySecondsToTime(int seconds) // Convert seconds to a readable format
        {
            String time = "_";

            if (seconds < 60)
            {
                time = seconds.ToString() + " minutes";
            }
            else
            {
                time = (seconds / 60).ToString() + " Hour" + (seconds / 60 == 1 ? "" : "s") + " and " + (seconds % 60).ToString() + " Minutes";
            }

            if (seconds == -1 || seconds == 0)
            {
                time = "Unknown minutes";
            }
            return time;
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
