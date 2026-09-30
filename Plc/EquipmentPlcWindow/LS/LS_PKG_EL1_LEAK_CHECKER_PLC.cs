using MaterialControlSimulator.Plc.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator.Plc.EquipmentPlcWindow
{
    public class LS_PKG_EL1_LEAK_CHECKER_PLC : EquipmentPlc
    {
        public LS_PKG_EL1_LEAK_CHECKER_PLC(EquipmentPlcConfig config) : base(config)
        {
            InitializeLogic();
        }

        private void InitializeLogic()
        {
            ScanEngine.AddLogic(new CommunicationCheckLogic());
            ScanEngine.AddLogic(new LotInfoRequestLogic(0x3811, 0x3011, 0x3128));
            ScanEngine.AddLogic(new LotIStartRequestLogic(0x3812, 0x3012));
            ScanEngine.AddLogic(new LotIEndRequestLogic(0x3813, 0x3013));
        }
    }
}
