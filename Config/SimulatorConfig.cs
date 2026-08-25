using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class SimulatorConfig
    {
        public string SimulationType { get; set; } = "Sample";

        public int McPort { get; set; } = 5000;

        public int PlcIndex { get; set; } = 0;
    }
}
