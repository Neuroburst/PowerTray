﻿using System.Configuration;
using System.Data;
using System.Windows;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime;

using Windows.Devices.Power;
using Windows.System.Power;
using Microsoft.Win32;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

// TODO:
// aggregate battery updates too slowly && add charge calc to tooltip

// add back battery info menu and make it auto-darkmode
// make option to switch tray view
// add settings menu (for public options like update speed, auto-update, better discharge calc, and default tray view)

// remove winform dependency completely
// make tray font size bigger and look better

// Important: Support Situations in which:
// No System Battery
// Multiple Batteries

namespace PowerTray
{
    public partial class App : System.Windows.Application
    {
        // import dll
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool DestroyIcon(IntPtr handle);
        // Params ---
        static int trayFontSize = 10;
        public static String trayFontType = "Segoe UI";
        static float trayFontQualityMultiplier = 2.0f;

        public static int remainChargeHistorySize = 600;

        public static int trayRefreshRate = 500;
        public static int batInfoRefreshRate = 250;
        public static bool batInfoAutoRefresh = true;

        static Color chargingColor = Color.Green;
        static Color highColor = Color.Black;
        static Color highDarkColor = Color.White;
        static Color mediumColor = Color.FromArgb(255, 220, 100, 20);
        static Color lowColor = Color.FromArgb(255, 232, 17, 35);

        public static int highAmount = 50;
        public static int mediumAmount = 30;
        public static int lowAmount = 0;
        // ---

        public static int[] remainChargeHistory = new int[remainChargeHistorySize];
        public static long[] chargeHistoryTime = new long[remainChargeHistorySize];
        static int bufferSize = 0;
        static int bufferEndIdx = 0;

        static NotifyIcon trayIcon;
        static Font trayFont = new Font(trayFontType, trayFontSize * trayFontQualityMultiplier, System.Drawing.FontStyle.Bold);

        private void App_Startup(object sender, StartupEventArgs e)
        {
            // Create Context Menu
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            ToolStripItem batteryInfo = contextMenu.Items.Add("Battery Info");
            batteryInfo.Click += new System.EventHandler(CreateInfoWindow);
            ToolStripItem settings = contextMenu.Items.Add("Settings");
            //settings.Click += 
            ToolStripItem exit = contextMenu.Items.Add("Exit");
            exit.Click += new System.EventHandler(Quit);

            // Create tray button
            trayIcon = new NotifyIcon();
            trayIcon.ContextMenuStrip = contextMenu;
            trayIcon.Visible = true;
            //trayIcon.Click += new System.EventHandler(CreateInfoWindow); // weirdly also inputs right clicks and context menu clicks

            // Create Update Timer for tray icon
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = trayRefreshRate;
            timer.Tick += new EventHandler(UpdateTray);
            timer.Start();
        }
        
        private void UpdateTray(object sender, EventArgs e)
        {
            // check if dark mode is enabled ---
            bool darkModeEnabled = checkDarkMode()[0];
            // ---

            var bat_info = GetBatteryInfo();
            Debug.Print(bat_info.ToString());
            int fullChargeCapMwh = bat_info["fullChargeCapMwh"];
            int remainChargeCapMwh = bat_info["remainChargeCapMwh"];
            int chargeRateMwh = bat_info["chargeRateMwh"];

            // update remainChargeHistory ---
            long timeStamp = DateTime.Now.Ticks;
            remainChargeHistory.SetValue(remainChargeCapMwh, bufferEndIdx);
            chargeHistoryTime.SetValue(timeStamp, bufferEndIdx);
            // ---
            
            // calculate charge rate ---
            int start_idx = bufferEndIdx - bufferSize;
            long time_delta_ms = (chargeHistoryTime[bufferEndIdx] - chargeHistoryTime[start_idx]) / 10000;
            long charge_delta = (long)(remainChargeHistory[bufferEndIdx] - remainChargeHistory[start_idx]);
            long calcChargeRateMwh = 0;

            if (time_delta_ms != 0)
            {
                calcChargeRateMwh = charge_delta / time_delta_ms;
            }

            Debug.Print(remainChargeCapMwh.ToString());

            if (bufferSize < remainChargeHistorySize)
            {
                bufferSize += 1;
            }
            bufferEndIdx += 1;
            if (bufferEndIdx >= remainChargeHistorySize)
            {
                bufferEndIdx = 0;
            }
            // ---

            double batteryPercent = (remainChargeCapMwh / (double)fullChargeCapMwh) * 100;

            double timeLeft = 0;
            if (chargeRateMwh < 0)
            {
                timeLeft = (remainChargeCapMwh / -(double)chargeRateMwh) * 60;
            }

            if (chargeRateMwh > 0)
            {
                timeLeft = ((fullChargeCapMwh - remainChargeCapMwh) / (double)chargeRateMwh) * 60;
            }
            // ---


            int roundPercent = (int)Math.Round(batteryPercent, 0);

            Color statusColor = highColor;

            if (chargeRateMwh > 0)
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
            var toolTipText = CreateTooltipText(sender, e);
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
                    using (Icon icon = Icon.FromHandle(intPtr))
                    {
                        trayIcon.Icon?.Dispose();
                        trayIcon.Icon = icon;
                        SetNotifyIconText(trayIcon, toolTipText);
                        //trayIcon.Text = toolTipText;
                    }
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
        private void Quit(object sender, EventArgs e) // check if the exit button was pressed
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private string CreateTooltipText(object sender, EventArgs e)
        {
            var bat_info = GetBatteryInfo();

            // use battery info
            bool isPlugged = SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online;
            bool isCharging = bat_info["Status"] == BatteryStatus.Charging;

            var fullChargeCapMwh = bat_info["fullChargeCapMwh"];
            var remainChargeCapMwh = bat_info["remainChargeCapMwh"];
            var chargeRateMwh = bat_info["chargeRateMwh"];

            double batteryPercent = (remainChargeCapMwh / (double)fullChargeCapMwh) * 100;

            double timeLeft = 0;
            if (chargeRateMwh < 0)
            {
                timeLeft = (remainChargeCapMwh / -(double)chargeRateMwh) * 60;
            }

            if (chargeRateMwh > 0)
            {
                timeLeft = ((fullChargeCapMwh - remainChargeCapMwh) / (double)chargeRateMwh) * 60;

            }
            String toolTipText =
                Math.Round(batteryPercent, 3).ToString() + "% " + (isPlugged ? "connected to AC" : "on battery\n" +
            EasySecondsToTime((int)timeLeft) + " remaining") +
                (chargeRateMwh > 0 ? (isCharging ? "\nCharging: " + EasySecondsToTime((int)timeLeft) + " until fully charged" : "\nnot charging") + "" : "") +
                "\n\n" + "Current Charge: " + remainChargeCapMwh.ToString() + " mWh" +
                "\n" + (chargeRateMwh > 0 ? "Charge Rate: " : "Discharge Rate: ") + Math.Abs((int)chargeRateMwh).ToString() + " mWh";
            return toolTipText;
        }
        private void CreateInfoWindow(object sender, System.EventArgs e)
        {
            BatInfo dialog = new BatInfo();
            dialog.Show();
        }

        public static bool[] checkDarkMode()
        {
            string RegistryKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\";
            int appDarkKey = (int)Registry.GetValue(RegistryKey, "AppsUseLightTheme", -1);
            int trayDarkKey = (int)Registry.GetValue(RegistryKey, "SystemUsesLightTheme", -1);

            bool appdark = !Convert.ToBoolean(appDarkKey == -1 ? true : appDarkKey);
            bool traydark = !Convert.ToBoolean(trayDarkKey == -1 ? false : trayDarkKey);

            // zero is taskbar, one is system
            return [true, true];
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

        public static Dictionary<string, dynamic> GetBatteryInfo()
        {
            var dataDict = new Dictionary<string, dynamic>();
            var batteryReport = Battery.AggregateBattery.GetReport(); // get battery info
            dataDict.Add("designChargeCapMwh", batteryReport.DesignCapacityInMilliwattHours);
            dataDict.Add("fullChargeCapMwh", batteryReport.FullChargeCapacityInMilliwattHours);
            dataDict.Add("remainChargeCapMwh", batteryReport.RemainingCapacityInMilliwattHours);
            dataDict.Add("chargeRateMwh", batteryReport.ChargeRateInMilliwatts);
            dataDict.Add("Status", batteryReport.Status);

            return dataDict;
        }
        public static void SetNotifyIconText(NotifyIcon ni, string text) // bypass 63 character limit for tooltips
        {
            ni.Text = text;
            //if (text.Length >= 128) throw new ArgumentOutOfRangeException("Text limited to 127 characters");
            //Type t = typeof(NotifyIcon);
            //BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
            //t.GetField("text", hidden).SetValue(ni, text);
            //if ((bool)t.GetField("added", hidden).GetValue(ni))
            //    t.GetMethod("UpdateIcon", hidden).Invoke(ni, new object[] { true });
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
