using MaterialControlSimulator.Plc;
using MaterialControlSimulator.Plc.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class EquipmentPlc
    {
  
        public string Name => Config.Name;
        public int Port => Config.McPort;
        public EquipmentPlcConfig Config { get; }

        public PlcMemory Memory { get; }
        public PlcBindingManager BindingManager { get; }
        public McProtocolServer Server { get; }
        public PlcScanEngine ScanEngine { get; }

        public EquipmentPlc(EquipmentPlcConfig config)
        {
            Config = config;

            Memory = new PlcMemory();
            BindingManager = new PlcBindingManager(Memory);

            Server = new McProtocolServer(
                Memory,
                BindingManager);

            ScanEngine = new PlcScanEngine(
                BindingManager);
        }

        public void Start()
        {
            _ = Server.StartAsync(Config.McPort);
            ScanEngine.Start();
        }

        public void Stop()
        {
            _ = ScanEngine.StopAsync();
            Server.Stop();
        }
    }
}

}
