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
        NA = 0,
        LR = 1,
        LC = 3,
        UR = 4,
        UC = 6,
        PL = 7
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

        public static readonly DependencyProperty PortStatusProperty =
            DependencyProperty.Register(
                nameof(PortStatus),
                typeof(ushort),
                typeof(PortControl),
                new PropertyMetadata(
                    (ushort)0,
                    OnPortStatusChanged));

        public ushort PortStatus
        {
            get => (ushort)GetValue(PortStatusProperty);
            set => SetValue(PortStatusProperty, value);
        }

        private static void OnPortStatusChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (PortControl)d;
            ushort value = (ushort)e.NewValue;

            control.PortStatusText = value switch
            {
                0 => "NA",
                1 => "LR",
                3 => "LC",
                4 => "UR",
                6 => "UC",
                7 => "PL",
                _ => $"?{value}"
            };
        }

        public static readonly DependencyProperty PortStatusTextProperty =
                DependencyProperty.Register(
                    nameof(PortStatusText),
                    typeof(string),
                    typeof(PortControl),
                    new PropertyMetadata("NA"));

        public string PortStatusText
        {
            get => (string)GetValue(PortStatusTextProperty);
            private set => SetValue(PortStatusTextProperty, value);
        }
    }
}
