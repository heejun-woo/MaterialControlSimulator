using MaterialControlSimulator.Plc;
using MaterialControlSimulator.Plc.Logic;
using System.Configuration;
using System.Data;
using System.Windows;

namespace MaterialControlSimulator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static NodeRegistry Nodes { get; } = new();

        public static CarrierRegistry Carriers { get; } = new();

        public static CarrierManager CarrierManager { get; } = new();

        public static NodeManager NodeManager { get; } = new();

        public static PlcMemory plcMemory = new PlcMemory();
        public static PlcBindingManager PlcBindingManager { get; } = new PlcBindingManager(plcMemory);
        public static McProtocolServer PlcServer { get; } = new McProtocolServer(plcMemory, PlcBindingManager);

        public static PlcScanEngine scanEngine = new PlcScanEngine(PlcBindingManager);
        public static CarrierHistoryManager CarrierHistory { get; } = new CarrierHistoryManager();
        public static CarrierRouteManager _routeManager { get; } = new(CarrierManager, Nodes);

        public static CarrierStateManager CarrierStateManager { get; } = new();

        public App()
        {
            InitializeComponent();

        }
    }

}
