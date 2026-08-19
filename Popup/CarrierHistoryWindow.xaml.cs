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

namespace MaterialControlSimulator
{
    /// <summary>
    /// CarrierHistoryControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CarrierHistoryWindow : Window
    {
        public CarrierHistoryWindow()
        {
            InitializeComponent();

            Loaded +=
                CarrierHistoryControl_Loaded;
        }

        private void CarrierHistoryControl_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            LoadAll();
        }

        private void LoadAll()
        {
            HistoryGrid.ItemsSource =
                App.CarrierHistory.GetAll();
        }

        private void Search_Click(
            object sender,
            RoutedEventArgs e)
        {
            string carrierId =
                CarrierSearchText.Text.Trim();

            if (string.IsNullOrEmpty(carrierId))
            {
                LoadAll();
                return;
            }

            HistoryGrid.ItemsSource =
                App.CarrierHistory.FindByCarrier(
                    carrierId);
        }

        private void All_Click(
            object sender,
            RoutedEventArgs e)
        {
            CarrierSearchText.Clear();

            LoadAll();
        }
    }
}
