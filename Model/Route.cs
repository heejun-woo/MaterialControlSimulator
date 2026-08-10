using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class Route
    {
        public List<NodeControl> Nodes { get; } = new();

        public int CurrentIndex { get; set; }

        public NodeControl? CurrentNode =>
            CurrentIndex < Nodes.Count
                ? Nodes[CurrentIndex]
                : null;
    }
}
