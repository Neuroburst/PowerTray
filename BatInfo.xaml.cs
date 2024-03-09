using System.Collections;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Management.Update;
using Windows.UI.Core;
using Wpf.Ui.Controls;
using Wpf.Ui.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

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
            DataCollection = new ObservableCollection<Info> { };
            foreach (DictionaryEntry item in data)
            {
                string key = (string)item.Key;

                string name = (key.ToString().EndsWith("mWh") ? key.Remove(key.Length-3, 3) : (key.ToString().EndsWith("mW") ? key.Remove(key.Length - 2, 2) : key));
                string value = item.Value.ToString() + (key.ToString().EndsWith("mWh") ? " mWh" : "") + (key.ToString().EndsWith("mW") ? " mW" : "");
                if (key.Contains("Health") || key.Contains("Percent"))
                {
                    value = item.Value.ToString().Substring(0, 10) + "%";
                }
                else if (key.Contains("Volt"))
                {
                    value = item.Value.ToString() + " volts";
                }
                DataCollection.Add(new Info { Name = name, Value = value });
            }
            This.Data.ItemsSource = DataCollection;
        }
    }
}