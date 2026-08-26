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
    /// CarrierCreateWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CarrierCreateWindow : Window
    {
        public CarrierCreateWindow()
        {
            InitializeComponent();

            InitialNodeComboBox.ItemsSource = App.NodeManager.Nodes;
        }
        public CarrierControl? CreatedCarrier { get; private set; }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            var id = CarrierIdTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                MessageBox.Show("Carrier ID를 입력하세요.");
                return;
            }

            if (App.CarrierManager.ContainKey(id) != false)
            {
                MessageBox.Show("이미 존재하는 Carrier입니다.");
                return;
            }

            if (InitialNodeComboBox.SelectedItem is not NodeControl destination)
            {
                MessageBox.Show("초기 위치 선택하세요.");
                return;
            }
            NodeControl InitNode = InitialNodeComboBox.SelectedItem as NodeControl;

            CreatedCarrier = new CarrierControl
            {
                Id = id, CurrentNode = InitNode
            };

            InitNode?.TryEnter(CreatedCarrier);
            CreatedCarrier.SetPosition(InitNode);

            App.CarrierManager.Register(CreatedCarrier);

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
   
}
