using System;
using System.Configuration;
using Wpf.Ui.Controls;

using System.Windows;
using System.Windows.Controls.Primitives;

namespace PowerTray
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : FluentWindow
    {
        bool can_reset = true;

        public Configuration AppConfig = ConfigurationManager.OpenMachineConfiguration();

        public Settings()
        {
            InitializeComponent();
            DefaultTray.ItemsSource = Enum.GetNames(typeof(App.DisplayedInfo));
            TrayFontStyle.ItemsSource = Enum.GetNames(typeof(System.Drawing.FontStyle));
            Load();
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
    }
}
