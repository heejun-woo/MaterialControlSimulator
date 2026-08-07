using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class CarrierSession
    {
        public CarrierControl Carrier { get; }

        public Queue<MoveCommand> Queue { get; } = new();

        public Route? Route { get; set; }

        public bool Running { get; set; }
        public NodeControl? CurrentNode => Carrier.CurrentNode;

        public CarrierSession(CarrierControl carrier)
        {
            Carrier = carrier;
        }
    }


}
