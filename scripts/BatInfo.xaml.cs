using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;

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

        private void OpenGraph(object sender, RoutedEventArgs e)
        {
            App.CreateGraphWindow();
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
            this.GraphButton.Click += OpenGraph;
            this.ResetButton.Click += ResetBuffer;
        }

        public static void UpdateData(object sender, EventArgs e)
        {
            if (This == null)
            {
                return;
            }
            var data = BatteryManagement.GetBatteryInfo(PowerTray.App.batteryTag, PowerTray.App.batteryHandle);
            if (data != null)
            {
                data.Insert(5, "Calculated Time Left", App.GetCalculatedTimeLeft((int)data["Remaining Charge mWh"], (int)data["Battery Capacity mWh"]));
                data.Insert(6, "Calculated Charge Rate mW", PowerTray.App.calcChargeRateMw);
                data.Insert(7, "Calculated Time Delta sec", PowerTray.App.calcTimeDelta / 1000);

                DataCollection = new ObservableCollection<Info> { };
                foreach (DictionaryEntry item in data)
                {
                    string key = (string)item.Key;

                    string name = DeleteInString(key.ToString(), ["mWh", "mW", "sec"]);

                    string value = item.Value.ToString() + ((key.ToString().EndsWith("mWh") ? " mWh" : "") + (key.ToString().EndsWith("mW") ? " mW" : "") + (key.Contains("Volt") ? " volts" : "") +
                        (key.EndsWith("sec") ? " sec" : ""));

                    if (key.Contains("Health") || key.Contains("Percent"))
                    {
                        if (item.Value.ToString().Length > 5)
                        {
                            value = item.Value.ToString().Substring(0, 6);
                        }
                        else
                        {
                            value = item.Value.ToString();
                        }
                        value += "%";
                    }
                    DataCollection.Add(new Info { Name = name, Value = value });
                }
                This.Data.ItemsSource = DataCollection;
            }
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