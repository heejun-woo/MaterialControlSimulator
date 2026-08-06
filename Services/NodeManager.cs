using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class NodeManager
    {
        private readonly Dictionary<string, NodeModel> _nodes = new();


        public void Add(NodeModel node)
        {
            _nodes[node.Id] = node;
        }


        public NodeModel Get(string id)
        {
            return _nodes[id];
        }
    }
}
