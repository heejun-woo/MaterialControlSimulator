using MaterialControlSimulator.Controls;
using MaterialControlSimulator.Plc;
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
    public partial class SampleType : UserControl
    {
        private LayoutManager _layoutManager;

        public SampleType()
        {
            InitializeComponent();

            _layoutManager = new LayoutManager();
            _layoutManager.LoadComplete += LayoutManager_LoadComplete;

            Loaded += Control_Loaded;
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


            #region Loader 노드연결

            ConnectNodes(Loc01, Loc02);
            ConnectNodes(Loc02, Loc03);
            ConnectNodes(Loc03, Loc04);
            ConnectNodes(Loc04, Loc05);
            ConnectNodes(Loc05, Loc06);
            ConnectNodes(Loc06, Loc07);
            ConnectNodes(Loc07, Loc08);
            ConnectNodes(Loc08, Loc09);
            ConnectNodes(Loc09, Loc10);
            ConnectNodes(Loc10, Loc11);
            ConnectNodes(Loc11, Loc12);
            ConnectNodes(Loc12, Loc01);
            
            ConnectNodes(Port01, Loc01);
            ConnectNodes(Loc01, Port01);
            ConnectNodes(Port02, Loc07);
            ConnectNodes(Loc07, Port02);

            #endregion


            ConnectNodes(PkgLoc01, PkgLoc02);
            ConnectNodes(PkgLoc02, PkgLoc03);
            ConnectNodes(PkgLoc03, PkgLoc04);
            ConnectNodes(PkgLoc04, PkgLoc05);
            ConnectNodes(PkgLoc05, PkgLoc06);
            ConnectNodes(PkgLoc06, PkgLoc07);
            ConnectNodes(PkgLoc07, PkgLoc08);
            ConnectNodes(PkgLoc08, PkgLoc01);

            Loc05.NextNodeId = "Loc06";
            Loc05.PairNodeId = "PkgLoc01";

            PkgLoc01.NextNodeId = "PkgLoc02";
            PkgLoc01.PairNodeId = "Loc06";

            SetPlcAddress();



            // Node / 프로젝트 로딩 완료
            App.PlcBindingManager.Register(Port01);

            Logger.Info("Layout Load Complete");

            var main = Window.GetWindow(this) as MainWindow;
            if (main != null)
            {
                main.InitializeSimulation();

                App.CarrierStateManager.RestoreCarriers(main._simulation, SimulationCanvas);

            }


            App.scanEngine.AddLogic(new CommunicationCheckLogic());
            App.scanEngine.AddLogic(new CellTransferLogic(Loc05, Loc05.Id, PkgLoc01.Id, Loc05.NextNodeId, PkgLoc01.NextNodeId, 10, 10));
            App.scanEngine.Start();

        }

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
                PropertyName = "PortStatus",
                Address = "W200",
                DataType = PlcDataType.UInt16
            });


            Port01.PlcBindings.Add(new PlcBinding
            {
                PropertyName = "PortStatus",
                Address = "W200",
                DataType = PlcDataType.UInt16
            });


            Debug.WriteLine(App.PlcBindingManager.GetValue("B100"));
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

                IsHitTestVisible = false
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
