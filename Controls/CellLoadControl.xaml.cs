using MaterialControlSimulator.Plc;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MaterialControlSimulator.Controls
{
    /// <summary>
    /// CellLoadControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CellLoadControl : NodeControl
    {
        public string PairNodeId { get; set; } = "";
        public string NextNodeId { get; set; } = "";
        public CellLoadControl()
        {
            InitializeComponent();
        }
    }
}
