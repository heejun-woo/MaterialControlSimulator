using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace MaterialControlSimulator.Controls
{

    public class NodeControl : UserControl, INode
    {
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
        public NodeControl()
        {
            Loaded += NodeControl_Loaded;
        }


        private void NodeControl_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            App.Nodes.Register(this);
        }

        public CarrierControl? Carrier { get; private set; }


        public bool IsOccupied =>
            Carrier != null;


        public bool Enter(CarrierControl carrier)
        {
            if (IsOccupied)
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
            return TranslatePoint(
                new Point(
                    ActualWidth / 2,
                    ActualHeight / 2),
                Parent as UIElement);
        }
    }
}
