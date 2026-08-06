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
        SimulationManager _simulation = new(new CarrierManager()); 
        private LayoutManager _layoutManager;
        public MainWindow()
        {
            InitializeComponent();

            _layoutManager = new LayoutManager();
            _layoutManager.LoadComplete += LayoutManager_LoadComplete;

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

        private async void LayoutManager_LoadComplete()
        {
            Logger.Info("Layout Load Complete");


            _simulation = new SimulationManager(
                new CarrierManager());


            _simulation.Enqueue(new MoveCommand
            {
                CarrierId = "C01",
                DestinationId = "Port01",
                Speed = 200
            });


            _simulation.Enqueue(new MoveCommand
            {
                CarrierId = "C01",
                DestinationId = "Port02",
                Speed = 200
            });

            _simulation.Enqueue(new MoveCommand
            {
                CarrierId = "C01",
                DestinationId = "Port03",
                Speed = 200
            });

            _simulation.Enqueue(new MoveCommand
            {
                CarrierId = "C01",
                DestinationId = "Port04",
                Speed = 200
            });

            await _simulation.StartAsync();
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
    }
}