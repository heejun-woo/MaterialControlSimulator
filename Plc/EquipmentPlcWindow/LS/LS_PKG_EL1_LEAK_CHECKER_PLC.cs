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
        }
    }
}
