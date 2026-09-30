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
        }

        private void LotInfoButton_Click(object sender, RoutedEventArgs e)
        {
            // LOT INFO 화면 또는 팝업
        }

        private void LotStartButton_Click(object sender, RoutedEventArgs e)
        {
            // LOT START PLC 요청
        }

        private void LotEndButton_Click(object sender, RoutedEventArgs e)
        {
            // LOT END PLC 요청
        }

        private void ForceOutButton_Click(object sender, RoutedEventArgs e)
        {
            // Material Force Out PLC 요청
        }
    }
}
