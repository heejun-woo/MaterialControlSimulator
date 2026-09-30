using MaterialControlSimulator.Controls;
using MaterialControlSimulator.Plc;
using MaterialControlSimulator.Plc.EquipmentPlcWindow;
using MaterialControlSimulator.Plc.Logic;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MaterialControlSimulator.SimulationType
{
    /// <summary>
    /// SampleType.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class EL_1 : UserControl
    {
        private LayoutManager _layoutManager;

        private EquipmentPlcWindow _equipmentPlcWindow;

        private readonly SimulatorConfig _config;

        public EL_1(SimulatorConfig config)
        {
            InitializeComponent();

            _layoutManager = new LayoutManager();
            _layoutManager.LoadComplete += LayoutManager_LoadComplete;

            Loaded += Control_Loaded;
            _config = config;

            InitializeEquipmentPlc();
            OpenEquipmentPlc();

        }

        private void InitializeEquipmentPlc()
        {
            var config = _config.EquipmentPlcs.FirstOrDefault(x => x.Name == "EQ01");

            if (config == null) return;

            _equipmentPlcWindow = new EquipmentPlcWindow(config);
        }

        private void OpenEquipmentPlc()
        {

            if (_equipmentPlcWindow == null)
            {
                InitializeEquipmentPlc();

                _equipmentPlcWindow.Closed += (s, e) =>
                {
                    _equipmentPlcWindow = null;
                };
                _equipmentPlcWindow.Show();
            }
            else
            {
                _equipmentPlcWindow.Activate();
                _equipmentPlcWindow.Show();
            }
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
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


            #region 실구간 노드연결

            ConnectNodes(Loc2001, Loc2011);
            ConnectNodes(Loc2011, Loc2021);
            ConnectNodes(Loc2021, Loc2031);
            ConnectNodes(Loc2031, Loc2041);

            ConnectNodes(Loc2003, Loc2013);
            ConnectNodes(Loc2013, Loc2023);
            ConnectNodes(Loc2023, Loc2033);
            ConnectNodes(Loc2033, Loc2049);

            ConnectNodes(Loc2041, Loc2042);
            ConnectNodes(Loc2042, Loc2043);
            ConnectNodes(Loc2043, Loc2044);
            ConnectNodes(Loc2044, Loc2045);
            ConnectNodes(Loc2045, Loc2046);
            ConnectNodes(Loc2046, Loc2047);
            ConnectNodes(Loc2047, Loc2048);
            ConnectNodes(Loc2048, Loc2049);
            ConnectNodes(Loc2049, Loc2050);
            ConnectNodes(Loc2050, Loc2051);

            ConnectNodes(Loc2051, Loc2061);
            ConnectNodes(Loc2061, Loc2071);
            ConnectNodes(Loc2071, Loc2081);
            ConnectNodes(Loc2081, Loc2091);


            ConnectNodes(Loc2002, Loc2012);
            ConnectNodes(Loc2012, Loc2022);
            ConnectNodes(Loc2022, Loc2032);
            ConnectNodes(Loc2032, Loc3002);

            ConnectNodes(Loc2004, Loc2014);
            ConnectNodes(Loc2014, Loc2024);
            ConnectNodes(Loc2024, Loc2034);
            ConnectNodes(Loc2034, Loc3010);


            ConnectNodes(Loc3001, Loc3002);
            ConnectNodes(Loc3002, Loc3003);
            ConnectNodes(Loc3003, Loc3004);
            ConnectNodes(Loc3004, Loc3005);
            ConnectNodes(Loc3005, Loc3006);
            ConnectNodes(Loc3006, Loc3007);
            ConnectNodes(Loc3007, Loc3008);
            ConnectNodes(Loc3008, Loc3009);
            ConnectNodes(Loc3009, Loc3010);
            ConnectNodes(Loc3010, Loc3011);
            ConnectNodes(Loc3011, Loc3012);
            ConnectNodes(Loc3012, Loc2062);
            ConnectNodes(Loc2062, Loc2072);
            ConnectNodes(Loc2072, Loc2082);
            ConnectNodes(Loc2082, Loc2092);
            ConnectNodes(Loc2092, Loc2102);
            ConnectNodes(Loc2102, Loc2101);


            #endregion

            #region 공구간 노드연결

            ConnectNodes(Loc1104, Loc1103);
            ConnectNodes(Loc1103, Loc1101);
            ConnectNodes(Loc1101, Loc1111);
            ConnectNodes(Loc1111, Loc1112);
            ConnectNodes(Loc1112, Loc1102);
            ConnectNodes(Loc1102, Loc1092);
            ConnectNodes(Loc1092, Loc1082);
            ConnectNodes(Loc1082, Loc1072);
            ConnectNodes(Loc1072, Loc1062);
            ConnectNodes(Loc1062, Loc1052);

            ConnectNodes(Loc1094, Loc1093);
            ConnectNodes(Loc1093, Loc1091);
            ConnectNodes(Loc1091, Loc1081);
            ConnectNodes(Loc1081, Loc1061);
            ConnectNodes(Loc1061, Loc1051);

            ConnectNodes(Loc1052, Loc1051);
            ConnectNodes(Loc1051, Loc1050);
            ConnectNodes(Loc1050, Loc1049);
            ConnectNodes(Loc1049, Loc1048);
            ConnectNodes(Loc1048, Loc1047);
            ConnectNodes(Loc1047, Loc1046);
            ConnectNodes(Loc1046, Loc1045);
            ConnectNodes(Loc1045, Loc1044);
            ConnectNodes(Loc1044, Loc1043);
            ConnectNodes(Loc1043, Loc1042);
            ConnectNodes(Loc1042, Loc1041);

            ConnectNodes(Loc1050, Loc1034);
            ConnectNodes(Loc1034, Loc1024);
            ConnectNodes(Loc1024, Loc1014);
            ConnectNodes(Loc1014, Loc1004);

            ConnectNodes(Loc1049, Loc1033);
            ConnectNodes(Loc1033, Loc1023);
            ConnectNodes(Loc1023, Loc1013);
            ConnectNodes(Loc1013, Loc1003);

            ConnectNodes(Loc1042, Loc1032);
            ConnectNodes(Loc1032, Loc1022);
            ConnectNodes(Loc1022, Loc1012);
            ConnectNodes(Loc1012, Loc1002);

            ConnectNodes(Loc1041, Loc1031);
            ConnectNodes(Loc1031, Loc1021);
            ConnectNodes(Loc1021, Loc1011);
            ConnectNodes(Loc1011, Loc1001);


            #endregion


            ConnectNodes(Loc2091, Loc1093);
            ConnectNodes(Loc2101, Loc1103);


            ConnectNodes(Loc1001, Loc2001);
            ConnectNodes(Loc1002, Loc2002);
            ConnectNodes(Loc1003, Loc2003);
            ConnectNodes(Loc1004, Loc2004);


            SetPlcAddress();



            //// Node / 프로젝트 로딩 완료
            //App.PlcBindingManager.Register(Port01);

            Logger.Info("Layout Load Complete");

            var main = Window.GetWindow(this) as MainWindow;
            if (main != null)
            {
                main.InitializeSimulation();

                App.CarrierStateManager.RestoreCarriers(main._simulation, SimulationCanvas);

            }


            App.scanEngine.AddLogic(new CommunicationCheckLogic());
            //App.scanEngine.AddLogic(new CellTransferLogic(Loc05, Loc05.Id, PkgLoc01.Id, Loc05.NextNodeId, PkgLoc01.NextNodeId, 10, 10));
            //App.scanEngine.AddLogic(new HostRequestLogic(Loc03.Id, 0x3A49, 0x3149, 0x3419, Loc04.Id, Loc09.Id, string.Empty, 30));

            App.scanEngine.Start();
        }

        private void SetPlcAddress()
        {
            //Port01.PlcBindings.Add(new PlcBinding
            //{
            //    PropertyName = "CarrierID",
            //    Address = "W100",
            //    DataType = PlcDataType.String,
            //    WordCount = 16
            //});

            //Port01.PlcBindings.Add(new PlcBinding
            //{
            //    PropertyName = "IsExist",
            //    Address = "B100",
            //    DataType = PlcDataType.Bool
            //});

            //Port01.PlcBindings.Add(new PlcBinding
            //{
            //    PropertyName = "PortStatus",
            //    Address = "W200",
            //    DataType = PlcDataType.UInt16
            //});


            //Port01.PlcBindings.Add(new PlcBinding
            //{
            //    PropertyName = "PortStatus",
            //    Address = "W200",
            //    DataType = PlcDataType.UInt16
            //});


            //Debug.WriteLine(App.PlcBindingManager.GetValue("B100"));
        }


        private void ConnectNodes(NodeControl source, NodeControl target)
        {
            source.Connect(target);
            var line = CreateConnectionLine(source, target);
            SimulationCanvas.Children.Add(line);
            Panel.SetZIndex(line, 0);
        }
        private Line CreateConnectionLine(NodeControl source, NodeControl target)
        {
            var start = GetNodeCenter(source);
            var end = GetNodeCenter(target);

            var line = new Line
            {
                X1 = start.X,
                Y1 = start.Y,

                X2 = end.X,
                Y2 = end.Y,

                Stroke = source.BorderBrush,
                StrokeThickness = 3,

                IsHitTestVisible = false,
                Opacity = 0.4

            };

            return line;
        }

        private Point GetNodeCenter(FrameworkElement control)
        {
            double left =
                Canvas.GetLeft(control);

            double top =
                Canvas.GetTop(control);

            if (double.IsNaN(left))
                left = 0;

            if (double.IsNaN(top))
                top = 0;

            return new Point(
                left + control.ActualWidth / 2,
                top + control.ActualHeight / 2);
        }
    }
}
