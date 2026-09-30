using MaterialControlSimulator.Plc;
using MaterialControlSimulator.Plc.EquipmentPlcWindow;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MaterialControlSimulator.Controls.HMI
{
    /// <summary>
    /// LS_PKG_EL1_LEAK_CHECKER_Main.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LS_PKG_EL1_LEAK_CHECKER_Main : UserControl
    {
        public LS_PKG_EL1_LEAK_CHECKER_Main()
        {
            InitializeComponent();
        }

        private LS_PKG_EL1_LEAK_CHECKER_PLC _plc;

        public void Initialize(LS_PKG_EL1_LEAK_CHECKER_PLC plc)
        {
            _plc = plc;
            _plc.BindingManager.Bind(NextLotId, TextBlock.TextProperty, "W3868", PlcDataType.String, 8);
            _plc.BindingManager.Bind(NextModel, TextBlock.TextProperty, "W3860", PlcDataType.String, 5);

            _plc.BindingManager.Bind(CurrentLotId, TextBlock.TextProperty, "W3878", PlcDataType.String, 8);
            _plc.BindingManager.Bind(CurrentModel, TextBlock.TextProperty, "W3870", PlcDataType.String, 5);

            _plc.BindingManager.Bind(LotInfoButton, Button.BackgroundProperty, "B3811", PlcDataType.Bool, value => (bool)value ? Brushes.Yellow : Brushes.LightGray);
            _plc.BindingManager.Bind(LotStartButton, Button.BackgroundProperty, "B3812", PlcDataType.Bool, value => (bool)value ? Brushes.Yellow : Brushes.LightGray);
            _plc.BindingManager.Bind(LotEndButton, Button.BackgroundProperty, "B3813", PlcDataType.Bool, value => (bool)value ? Brushes.Yellow : Brushes.LightGray);

            //_plc.BindingManager.Bind(LotInfoButton, Button.ContentProperty, "B3811", PlcDataType.Bool, value => (bool)value ? "LOT INFO REQ" : "LOT INFO");
        }

        private void LotInfoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_plc.BindingManager.ReadBit(0x3011) == false)
            {
                _plc.BindingManager.SetValue("W3816", 1);

                _plc.BindingManager.WriteBit(0x3811, true);
            }
        }

        private void LotStartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_plc.BindingManager.ReadBit(0x3012) == false)
                _plc.BindingManager.WriteBit(0x3812, true);
        }

        private void LotEndButton_Click(object sender, RoutedEventArgs e)
        {
            bool isLotRunning = _plc.BindingManager.ReadBit(0x380A);

            if (_plc.BindingManager.ReadBit(0x3013) == false && isLotRunning)
            {
                _plc.BindingManager.WriteBit(0x380A, false);
                _plc.BindingManager.SetValue("W3817", 1);

                string? strLotID = _plc.BindingManager.GetValue<string>("W3878", 8);
                _plc.BindingManager.SetValue("W3898", strLotID, 8);
                _plc.BindingManager.SetValue("W3878", string.Empty, 8);

                string? strProdID = _plc.BindingManager.GetValue<string>("W3870", 5);
                _plc.BindingManager.SetValue("W3890", strProdID, 5);
                _plc.BindingManager.SetValue("W3870", string.Empty, 5);
                _plc.BindingManager.WriteBit(0x3813, true);
            }
        }

        private void ForceOutButton_Click(object sender, RoutedEventArgs e)
        {
            // Material Force Out PLC 요청
        }
    }
}
