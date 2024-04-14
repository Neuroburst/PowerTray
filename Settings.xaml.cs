using System;
using System.Configuration;
using Wpf.Ui.Controls;

using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;

namespace PowerTray
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : FluentWindow
    {

        public Configuration AppConfig = ConfigurationManager.OpenMachineConfiguration();

        public Settings()
        {
            InitializeComponent();
            DefaultTray.ItemsSource = Enum.GetNames(typeof(App.DisplayedInfo));
            TrayFontStyle.ItemsSource = Enum.GetNames(typeof(System.Drawing.FontStyle));
            Load();

            App.RefreshPowerPlans();
            UpdatePlansList();
        }

        public void UpdatePlansList()
        {
            ACPlan.ItemsSource = App.plans.Select(o => o.Name).ToList();
            BatteryPlan.ItemsSource = App.plans.Select(o => o.Name).ToList();
        }

        private void Load(bool update = true)
        {
            if (AppConfig.Sections["Options"] is null)
            {
                AppConfig.Sections.Add("Options", new Options());
            }

            if (update)
            {
                var OptionsSettingsSection = AppConfig.GetSection("Options");
                DataContext = OptionsSettingsSection;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            AppConfig.Save();
            App.LoadSettings();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            AppConfig.Sections.Remove("Options");
            AppConfig.Save();
            Load(false);

            App.LoadSettings();
            DataContext = null;
            Load(true);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AutoSwitch_Click(object sender, RoutedEventArgs e)
        {
            Notif.IsEnabled = (bool)Auto.IsChecked;
            NotifLabel.IsEnabled = (bool)Auto.IsChecked;
            ACPlan.IsEnabled = (bool)Auto.IsChecked;
            BatteryPlan.IsEnabled = (bool)Auto.IsChecked;
            ACPlanLabel.IsEnabled = (bool)Auto.IsChecked;
            BatteryPlanLabel.IsEnabled = (bool)Auto.IsChecked;
        }

        private void DefaultClick(object sender, RoutedEventArgs e)
        {
            Reset.IsEnabled = false;
            App.ManagePlans(false);
        }

        private void BatteryBoostClick(object sender, RoutedEventArgs e)
        {
            Boost.IsEnabled = false;
            App.ManagePlans(true);
        }
    }
}
