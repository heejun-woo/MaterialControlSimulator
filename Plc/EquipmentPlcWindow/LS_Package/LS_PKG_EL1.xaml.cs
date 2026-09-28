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

namespace MaterialControlSimulator.Plc.EquipmentPlcWindow
{
    /// <summary>
    /// PKG_EL1.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LS_PKG_EL1 : Window
    {
        private readonly EquipmentPlc _plc;

        public LS_PKG_EL1(EquipmentPlc plc)
        {
            InitializeComponent();
            _plc = plc;

            PlcNameText.Text = plc.Name;
            PortText.Text = plc.Port.ToString();

            InitializeGrid();
        }

        private void InitializeGrid()
        {
        }
    }
}
