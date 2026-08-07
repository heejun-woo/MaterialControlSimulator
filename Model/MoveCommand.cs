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
        public NodeControl Destination { get; }

        public MoveCommand(NodeControl destination)
        {
            Destination = destination;
        }
    }
}
