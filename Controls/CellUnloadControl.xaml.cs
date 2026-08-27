using MaterialControlSimulator.Plc;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MaterialControlSimulator.Controls
{
    /// <summary>
    /// CellUnloadControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CellUnloadControl : NodeControl
    {
        public string PairNodeId { get; set; } = "";

        public int MagazineSize { get; set; } = 10;

        public int TransferIntervalMs { get; set; } = 1000;

        public bool TransferEnabled { get; set; } = true;

        public string NextNodeId { get; set; } = "";

        public CellUnloadControl()
        {
            InitializeComponent();
        }
    }
}
