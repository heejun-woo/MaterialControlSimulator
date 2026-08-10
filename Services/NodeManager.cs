using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class NodeManager
    {
        private readonly Dictionary<string, NodeControl> _nodes = new();

        public IEnumerable<NodeControl> Nodes => _nodes.Values;

        public void Add(NodeControl node)
        {
            _nodes[node.Id] = node;
        }


        public NodeControl Get(string id)
        {
            return _nodes[id];
        }
    }
}
