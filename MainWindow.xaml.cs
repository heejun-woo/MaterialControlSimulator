using MaterialControlSimulator.Controls;
using MaterialControlSimulator.Plc;
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
using WpfApp;

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

        #region 초기화 관련
        
        private async void PlcStart(int PortID)
        {
            await App.PlcServer.StartAsync(PortID);
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

            SetPlcAddress();

            _simulation = new(App.CarrierManager, App.Nodes);
            RefreshEquipmentTree();

            // Node / 프로젝트 로딩 완료
            App.PlcBindingManager.RebuildCache();
            // 그 다음 MC Server 시작
            PlcStart(5000);

            Logger.Info("Layout Load Complete");



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
        #endregion

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

        #region 메뉴 이벤트
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

        private double _zoom = 1.0;

        private void SetZoom(double zoom)
        {
            _zoom = Math.Clamp(zoom, 0.3, 3.0);

            SimulationScale.ScaleX = _zoom;
            SimulationScale.ScaleY = _zoom;
        }

        private void ZoomIn(object sender, RoutedEventArgs e)
        {
            SetZoom(_zoom + 0.1);
            //App.PlcBindingManager.SetValue("B100", true);
            App.PlcBindingManager.SetValue("W100", "ABCDrtr23");
            App.PlcBindingManager.SetValue("W300", "TTTTT");

            Logger.Info(App.PlcBindingManager.GetValue<string>("W300", 10));
        }

        private void ZoomOut(object sender, RoutedEventArgs e)
        {
            SetZoom(_zoom - 0.1);

            //App.PlcBindingManager.SetValue("B100", false);
            App.PlcBindingManager.SetValue("W100", "");

            ushort[] words =App.PlcBindingManager.GetWords("W300",10);
            Logger.Info(words.ToString());
        }

        private void ZoomReset(object sender, RoutedEventArgs e)
        {
            SetZoom(1.0);
        }
        #endregion

        #region TreeView 이벤트
        private void RefreshEquipmentTree()
        {
            var groups = new ObservableCollection<EquipmentGroup>();

            var locations = new EquipmentGroup
            {
                Name = "Location"
            };

            var ports = new EquipmentGroup
            {
                Name = "Port"
            };

            foreach (var node in App.Nodes.GetAll())
            {
                if (node is LocationControl)
                {
                    locations.Items.Add(node);
                }
                else if (node is PortControl)
                {
                    ports.Items.Add(node);
                }
            }

            if (locations.Items.Count > 0)
                groups.Add(locations);

            if (ports.Items.Count > 0)
                groups.Add(ports);

            EquipmentTree.ItemsSource = groups;
        }

        private NodeControl? _selectedNode;

        private void EquipmentTree_SelectedItemChanged(
            object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is not NodeControl node)
                return;

            // 기존 선택 해제
            if (_selectedNode != null)
                _selectedNode.IsSelected = false;

            // 새 노드 선택
            _selectedNode = node;
            _selectedNode.IsSelected = true;

            // 화면에서 해당 노드로 이동
            MoveToNode(node);

            ShowProperties(node);
        }
        private void MoveToNode(NodeControl node)
        {
            var x = Canvas.GetLeft(node);
            var y = Canvas.GetTop(node);

            if (double.IsNaN(x))
                x = 0;

            if (double.IsNaN(y))
                y = 0;

            node.BringIntoView();
        } 
        #endregion


        private void SetPlcAddress()
        {
            Port01.PlcBindings.Add(new PlcBinding
            {
                PropertyName = "CarrierID",
                Address = "W100",
                DataType = PlcDataType.String,
                WordCount = 16
            });

            Port01.PlcBindings.Add(new PlcBinding
            {
                PropertyName = "IsExist",
                Address = "B100",
                DataType = PlcDataType.Bool
            });

            Port01.PlcBindings.Add(new PlcBinding
            {
                PropertyName = "PortState",
                Address = "W200",
                DataType = PlcDataType.UInt16
            });


            Debug.WriteLine(App.PlcBindingManager.GetValue("B100"));
        }

    }
}