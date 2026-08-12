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

namespace MaterialControlSimulator.Controls
{
    public enum PortState
    {
        Idle,
        Ready,
        Busy,
        Error
    }
    /// <summary>
    /// PortControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PortControl : NodeControl
    {


        public PortControl()
        {
            InitializeComponent();
            SelectionChanged += OnSelectionChanged;
        }

        public static readonly DependencyProperty PortStateProperty =
            DependencyProperty.Register(
                nameof(PortState),
                typeof(ushort),
                typeof(PortControl),
                new FrameworkPropertyMetadata(
                    (ushort)0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public ushort PortState
        {
            get => (ushort)GetValue(PortStateProperty);
            set => SetValue(PortStateProperty, value);
        }

        private void OnSelectionChanged(bool selected)
        {
            NodeBorder.BorderBrush = selected
                ? Brushes.Cyan
                : new SolidColorBrush(Color.FromRgb(83, 96, 108));

            NodeBorder.BorderThickness =
                selected
                    ? new Thickness(3)
                    : new Thickness(2);
        }
    }
}
