using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator.Plc
{
    public class PlcBindingInfo
    {
        public NodeControl Node { get; set; } = null!;

        public PlcBinding Binding { get; set; } = null!;
    }
}
