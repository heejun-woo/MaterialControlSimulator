using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace MaterialControlSimulator.Controls
{
    /// <summary>
    /// CarrierControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CarrierControl : UserControl
    {
        public static readonly DependencyProperty IdProperty =
           DependencyProperty.Register(
               nameof(Id),
               typeof(string),
               typeof(CarrierControl));

        private readonly TranslateTransform _moveTransform = new TranslateTransform();

        public string Id
        {
            get => (string)GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }
        public bool IsNotEmpty { get; private set; } = true;

        public NodeControl? CurrentNode { get; set; }

        public CarrierControl()
        {
            InitializeComponent();
            RenderTransform = _moveTransform;

            Loaded += CarrierControl_Loaded;
        }

        private void CarrierControl_Loaded(object sender, RoutedEventArgs e)
        {
            App.Carriers.Register(this);
            App.CarrierManager.Register(this);
        }

        public Task MoveToAsync(NodeControl node)
        {
            var target = GetNodePosition(node);

            return App.CarrierManager.MoveToAsync(
                this,
                target.X,
                target.Y,
                500);
        }



        public void SetPosition(NodeControl node)
        {
            CurrentNode = node;
            var position = GetNodePosition(node);
            Canvas.SetLeft(this, position.X);
            Canvas.SetTop(this, position.Y);
        }

        public Point GetNodePosition(NodeControl node)
        {
            var nodeX = Canvas.GetLeft(node);
            var nodeY = Canvas.GetTop(node);

            if (double.IsNaN(nodeX))
                nodeX = 0;

            if (double.IsNaN(nodeY))
                nodeY = 0;

            var nodeWidth = node.ActualWidth > 0
                ? node.ActualWidth
                : node.Width;

            var nodeHeight = node.ActualHeight > 0
                ? node.ActualHeight
                : node.Height;

            var carrierWidth = ActualWidth > 0
                ? ActualWidth
                : Width;

            var carrierHeight = ActualHeight > 0
                ? ActualHeight
                : Height;

            if (double.IsNaN(nodeWidth))
                nodeWidth = 0;

            if (double.IsNaN(nodeHeight))
                nodeHeight = 0;

            if (double.IsNaN(carrierWidth))
                carrierWidth = 80; // Carrier가 생성되기전에 접근하는 경우 

            if (double.IsNaN(carrierHeight))
                carrierHeight = 80; // Carrier가 생성되기전에 접근하는 경우 

            return new Point(
                nodeX + (nodeWidth - carrierWidth) / 2,
                nodeY + (nodeHeight - carrierHeight) / 2);
        }
    }
}
