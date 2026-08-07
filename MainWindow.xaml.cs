using MaterialControlSimulator.Controls;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Text;
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

namespace MaterialControlSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SimulationManager _simulation = new(App.CarrierManager, App.Nodes); 
        private LayoutManager _layoutManager;
        public ObservableCollection<PropertyItem> Properties { get; } = new();

        public MainWindow()
        {
            InitializeComponent();

            _layoutManager = new LayoutManager();
            _layoutManager.LoadComplete += LayoutManager_LoadComplete;
            PropertyGrid.ItemsSource = Properties;

            DataContext = new MainViewModel();

            Loaded += MainWindow_Loaded; 

            Logger.MessageReceived += Logger_MessageReceived;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 컨트롤 Loaded 완료 이후 실행
            Dispatcher.BeginInvoke(
                () =>
                {
                    _layoutManager.Initialize();
                },
                System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void LayoutManager_LoadComplete()
        {
            Logger.Info("Layout Load Complete");

        }

        private void Logger_MessageReceived(string message)
        {
            Dispatcher.Invoke(() =>
            {
                DebugOutput.AppendText(
                    message + Environment.NewLine);


                DebugOutput.ScrollToEnd();
            });
        }

        public void ShowProperties(NodeControl node)
        {
            Properties.Clear();


            Properties.Add(new PropertyItem
            {
                Name = "ID",
                Value = node.Id
            });


            Properties.Add(new PropertyItem
            {
                Name = "Type",
                Value = node.GetType().Name
            });


            var pos = node.GetPosition();

            Properties.Add(new PropertyItem
            {
                Name = "Position",
                Value = $"{pos.X:F0}, {pos.Y:F0}"
            });


            Properties.Add(new PropertyItem
            {
                Name = "State",
                Value = "Ready"
            });

            Properties.Add(new PropertyItem
            {
                Name = "Carrier ID",
                Value = node.Carrier?.Id ?? "None"
            });
        }


        private async void Start_Click(object sender, RoutedEventArgs e)
        {

            var route = new Route
            {
                Loop = true
            };

            route.Nodes.Add(Port01);
            route.Nodes.Add(Port02);
            route.Nodes.Add(Port03);
            route.Nodes.Add(Port04);
            route.Nodes.Add(Port05);
            route.Nodes.Add(Port06);
            route.Nodes.Add(Port07);
            route.Nodes.Add(Port08);
            route.Nodes.Add(Port09);
            route.Nodes.Add(Port10);
            route.Nodes.Add(Port11);
            route.Nodes.Add(Port12);


            Carrier01.CurrentNode = Port01;

            _simulation._running = true;

            await _simulation.Start(route);
        }
    }
}