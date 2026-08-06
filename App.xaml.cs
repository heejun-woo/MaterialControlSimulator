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
        public static NodeRegistry Nodes { get; private set; } = new NodeRegistry();

        public static CarrierRegistry Carriers { get; } = new();
    }

}
