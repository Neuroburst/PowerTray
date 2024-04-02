using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using System.ComponentModel;
using System.Management;

namespace PowerTray
{
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>
    public partial class Graph : FluentWindow
    {
        private Func<double, string> _yFormatter;
        public Graph()
        {
            InitializeComponent();

            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Reported Charge Rate",
                    Values = App.chargeRateGraph,
                    StrokeThickness = 5,
                    LineSmoothness = 1,
                    PointGeometrySize = 15,
                },

                new LineSeries
                {
                    Title = "Calculated Charge Rate",
                    Values = App.calcChargeRateGraph,
                    StrokeThickness = 5,
                    LineSmoothness = 1,
                    PointGeometrySize = 15,
                },
            };

            XFormatter = val => val.ToString();
            YFormatter = val => (val/1000).ToString() + " W";

            DataContext = this;
        }

        public SeriesCollection SeriesCollection { get; set; }
        public Func<double, string> XFormatter { get; set; }

        public Func<double, string> YFormatter
        {
            get { return _yFormatter; }
            set
            {
                _yFormatter = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            App.ResetGraphs();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
