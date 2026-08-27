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
            #endregion

            PkgLoc01.Connect(PkgLoc02);
            PkgLoc02.Connect(PkgLoc03);
            PkgLoc03.Connect(PkgLoc04);
            PkgLoc04.Connect(PkgLoc05);
            PkgLoc05.Connect(PkgLoc06);
            PkgLoc06.Connect(PkgLoc07);
            PkgLoc07.Connect(PkgLoc08);
            PkgLoc08.Connect(PkgLoc01);

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
    }
}
