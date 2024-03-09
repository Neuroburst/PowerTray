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
using Wpf.Ui.Controls;

namespace PowerTray
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class BatInfo : FluentWindow
    {
        private static StackPanel dataStack;
        private static StackPanel valuesStack;
        public BatInfo()
        {
            InitializeComponent();
            dataStack = this.Data;
            valuesStack = this.Values;
        }

        public static void UpdateData(object sender, EventArgs e)
        {
            var data = BatteryManagement.GetBatteryInfo(PowerTray.App.batteryTag, PowerTray.App.batteryHandle);

            dataStack.Children.Clear();
            valuesStack.Children.Clear();
            foreach (KeyValuePair<string, dynamic>item in data)
            {
                var margin = new Thickness();
                margin.Bottom = 5;
                margin.Top = 5;
                
                Card itemcard = new Card();
                Card valuecard = new Card();
                itemcard.Margin = margin;
                valuecard.Margin = margin;

                itemcard.Content = (item.Key.ToString().EndsWith("mWh") ? item.Key.Remove(item.Key.Length-3, 3) : item.Key);
                valuecard.Content = item.Value.ToString() + (item.Key.ToString().EndsWith("mWh") ? " mWh" : "");
                if (item.Key.Contains("Health"))
                {
                    valuecard.Content = item.Value.ToString().Substring(0, 5) + "%";
                }

                dataStack.Children.Add(itemcard);
                valuesStack.Children.Add(valuecard);
            }
            dataStack.UpdateLayout();
            valuesStack.UpdateLayout();
        }
    }
}