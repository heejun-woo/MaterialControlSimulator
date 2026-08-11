using MaterialControlSimulator.Plc;
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

        public static PlcBindingManager PlcBindingManager { get; } = new(Nodes);
        public static McProtocolServer PlcServer { get; } = new(PlcBindingManager);

        public App()
        {
            InitializeComponent();
        }
    }

}
