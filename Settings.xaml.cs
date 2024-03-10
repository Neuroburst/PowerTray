using System;
using Wpf.Ui.Controls;

namespace PowerTray
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : FluentWindow
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
