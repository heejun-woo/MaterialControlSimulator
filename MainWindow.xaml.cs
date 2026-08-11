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
using System.Xml.Linq;

namespace MaterialControlSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SimulationManager _simulation; 
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

            Loc01.Connect(Loc02);
            Loc02.Connect(Loc03);
            Loc03.Connect(Loc04);
            Loc04.Connect(Loc05);
            Loc05.Connect(Loc06);
            Loc06.Connect(Loc07);
            Loc07.Connect(Loc08);
            Loc08.Connect(Loc09);
            Loc09.Connect(Loc10);
            Loc10.Connect(Loc11);
            Loc11.Connect(Loc12);
            Loc12.Connect(Loc01);

            Port01.Connect(Loc01);
            Loc01.Connect(Port01);

            Port02.Connect(Loc07);
            Loc07.Connect(Port02);

            Logger.Info("Layout Load Complete");
            _simulation = new(App.CarrierManager, App.Nodes);

            Carrier01.SetPosition(Port01);
            _simulation.SetDestination(App.CarrierManager.Get("C01"), Loc06);

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
            _simulation._running = true;

            await _simulation.Start();
        }

        private async void Pause_Click(object sender, RoutedEventArgs e)
        {
            _simulation._running = false;

        }
        private void CreateCarrier_Click(object sender, RoutedEventArgs e)
        {
            var window = new CarrierCreateWindow
            {
                Owner = this
            };

            if (window.ShowDialog() != true)
                return;

            if (window.CreatedCarrier == null)
                return;

            SimulationCanvas.Children.Add(
                window.CreatedCarrier);

        }
        private async void SetCarrierDestination_Click(object sender, RoutedEventArgs e)
        {
            var window = new CarrierDestinationWindow(_simulation)
            {
                Owner = this
            };

            window.ShowDialog();

        }
    }
}