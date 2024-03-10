using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using Wpf.Ui.Controls;

namespace PowerTray
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class BatInfo : FluentWindow
    {
        public class Info
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
        private static ObservableCollection<Info> DataCollection { get; set; }
        private static dynamic This;
        
        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            App.CreateSettingsWindow();
            this.Close();
        }

        private static void ResetBuffer(object sender, RoutedEventArgs e)
        {
            App.ResetBuffer();
            UpdateData(null, null);
        }

        public BatInfo()
        {
            InitializeComponent();
            This = this;
            this.CloseButton.Click += CloseWindow;
            this.SettingsButton.Click += OpenSettings;
            this.ResetButton.Click += ResetBuffer;
            //this.SettingsButton.Click += ;
        }

        public static void UpdateData(object sender, EventArgs e)
        {
            if (This == null)
            {
                return;
            }
            var data = BatteryManagement.GetBatteryInfo(PowerTray.App.batteryTag, PowerTray.App.batteryHandle);
            data.Insert(4, "Calculated Charge Rate mW", PowerTray.App.calcChargeRateMw);
            data.Insert(5, "Calculated Time Delta sec", PowerTray.App.calcTimeDelta / 1000);

            DataCollection = new ObservableCollection<Info> { };
            foreach (DictionaryEntry item in data)
            {
                string key = (string)item.Key;

                string name = DeleteInString(key.ToString(), ["mWh", "mW", "sec"]);

                string value = item.Value.ToString() + ((key.ToString().EndsWith("mWh") ? " mWh" : "") + (key.ToString().EndsWith("mW") ? " mW" : "") + (key.Contains("Volt") ? " volts" : "") + 
                    (key.EndsWith("sec") ? " sec" : ""));

                if (key.Contains("Health") || key.Contains("Percent"))
                {
                    value = item.Value.ToString().Substring(0, 5) + "%";
                }
                DataCollection.Add(new Info { Name = name, Value = value });
            }
            This.Data.ItemsSource = DataCollection;
        }

        public static string DeleteInString(string source, string[] strings)
        {
            string modded_source = source;

            foreach (string item in strings)
            {
                modded_source = modded_source.Replace(item, "");
            }

            return modded_source;
        }
    }
}