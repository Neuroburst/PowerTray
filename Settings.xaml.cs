using System;
using System.Configuration;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using Wpf.Ui.Controls;

namespace PowerTray
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : FluentWindow
    {

        public Configuration AppConfig;

        public Settings()
        {
            string configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PowerTray",
                "PowerTray.config"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(configPath));

            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = configPath
            };

            AppConfig = ConfigurationManager.OpenMappedExeConfiguration(
                fileMap,
                ConfigurationUserLevel.None  // Note: None, not PerUserRoamingAndLocal since we maually set the path
            );



            InitializeComponent();
            DefaultTray.ItemsSource = Enum.GetNames(typeof(App.DisplayedInfo));
            TrayFontStyle.ItemsSource = Enum.GetNames(typeof(System.Drawing.FontStyle));
            Load();

            AdminLabel.IsEnabled = App.IsAdministrator();
            Admin.IsEnabled = App.IsAdministrator();

            PowerPlans.RefreshPowerPlans();
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

                var optionsSection = AppConfig.GetSection("Options");
                //UnlockSection(optionsSection);
                DataContext = optionsSection;
            }
        }

        private void Save()
        {
            AppConfig.Save();
        }

        private void ResetOptions()
        {
            AppConfig.Sections.Remove("Options");
            Save();
        }



        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
            App.LoadSettings();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetOptions();
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
            PowerPlans.ManagePlans(false);
        }

        private void BatteryBoostClick(object sender, RoutedEventArgs e)
        {
            Boost.IsEnabled = false;
            PowerPlans.ManagePlans(true);
        }

        private void AdvancedClick(object sender ,RoutedEventArgs e)
        {
            PowerPlans.Unlock();
        }
    }
}
