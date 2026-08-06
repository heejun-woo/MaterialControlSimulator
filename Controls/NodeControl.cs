using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

            MouseDown += NodeControl_MouseDown;

            Loaded += NodeControl_Loaded;
        }

        private void NodeControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Application.Current.MainWindow
                is MainWindow main)
            {
                main.ShowProperties(this);
            }
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
    }
}
