using MaterialControlSimulator.Plc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MaterialControlSimulator.Controls
{

    public class NodeControl : UserControl, INode, IPlcBindable
    {
        #region 그외
        public static readonly DependencyProperty IdProperty =
    DependencyProperty.Register(
        nameof(Id),
        typeof(string),
        typeof(NodeControl));

        public string Id
        {
            get => (string)GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }
        public string DisplayName => Id;

        public List<NodeControl> ConnectedNodes { get; } = new();
        public List<NodeControl> ReverseConnectedNodes { get; } = new();


        public static readonly DependencyProperty BorderBrushProperty =
    DependencyProperty.Register(
        nameof(BorderBrush),
        typeof(Brush),
        typeof(NodeControl),
        new PropertyMetadata(Brushes.Gray));

        public Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public NodeControl()
        {

            MouseDown += NodeControl_MouseDown;

            Loaded += NodeControl_Loaded;
        }
        public void Connect(NodeControl node)
        {
            if (node == this)
                return;

            if (!ConnectedNodes.Contains(node))
                ConnectedNodes.Add(node);

            if (!node.ReverseConnectedNodes.Contains(this))
                node.ReverseConnectedNodes.Add(this);
        }

        private void NodeControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Application.Current.MainWindow
                is MainWindow main)
            {
                main.ShowProperties(this);
            }
        }

        private void NodeControl_Loaded(object sender, RoutedEventArgs e)
        {
            App.Nodes.Register(this);
            App.NodeManager.Add(this);
        }

        public CarrierControl? Carrier { get; private set; }

        public bool IsOccupied =>
            Carrier != null;


        public bool TryEnter(CarrierControl carrier)
        {
            if (Carrier != null)
                return false;

            Carrier = carrier;
            return true;
        }


        public void Leave()
        {
            Carrier = null;
        }

        public Point GetPosition()
        {
            var x = Canvas.GetLeft(this);
            var y = Canvas.GetTop(this);

            if (double.IsNaN(x))
                x = 0;

            if (double.IsNaN(y))
                y = 0;


            var width = ActualWidth;
            var height = ActualHeight;


            return new Point(
                x + width / 2,
                y + height / 2);
        }

        public event Action<bool>? SelectionChanged;

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;
                SelectionChanged?.Invoke(value);
            }
        }
        #endregion

        #region PLC 관련 속성
        public ObservableCollection<PlcBinding> PlcBindings { get; } = new();

        public string CarrierID { get; set; }
        public bool IsExist  { get; set; }
  
        #endregion
    }
}
