using MaterialControlSimulator.Controls;
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
    /// CarrierDestinationWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CarrierDestinationWindow : Window
    {
        private readonly SimulationManager _simulationManager;

        public CarrierDestinationWindow(SimulationManager simulationManager)
        {
            InitializeComponent();

            _simulationManager = simulationManager;
            CarrierComboBox.ItemsSource = App.CarrierManager.Sessions;
            DestinationComboBox.ItemsSource = App.NodeManager.Nodes;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (CarrierComboBox.SelectedItem is not CarrierSession session)
            {
                MessageBox.Show("Carrier를 선택하세요.");
                return;
            }

            if (DestinationComboBox.SelectedItem
                is not NodeControl destination)
            {
                MessageBox.Show("목적지를 선택하세요.");
                return;
            }

            _simulationManager.SetDestination(session, destination);

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

}
