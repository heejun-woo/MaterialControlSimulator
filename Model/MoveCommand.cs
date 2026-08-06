using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaterialControlSimulator.Controls;

namespace MaterialControlSimulator
{
    public class MoveCommand
    {
        public string CarrierId { get; set; }

        public string DestinationId { get; set; }

        public double Speed { get; set; } = 200;
    }
}
