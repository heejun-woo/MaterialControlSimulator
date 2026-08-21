using MaterialControlSimulator.Plc;
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



            // Node / 프로젝트 로딩 완료
            App.PlcBindingManager.Register(Port01);

            Logger.Info("Layout Load Complete");



            Carrier01.SetPosition(Port01);


            var main = Window.GetWindow(this) as MainWindow;
            if (main != null)
            {
                main.InitializeSimulation();

                main._simulation.SetDestination(App.CarrierManager.Get("C01"), Loc06);
            }

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
