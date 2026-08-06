using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class NodeRegistry
    {
        private readonly Dictionary<string, INode> _nodes = new();

        public int Count => _nodes.Count;

        public void Register(INode node)
        {
            if (_nodes.ContainsKey(node.Id))
                throw new Exception($"중복 Node ID : {node.Id}");

            _nodes.Add(node.Id, node);
        }


        public INode Get(string id)
        {
            return _nodes[id];
        }


        public IEnumerable<INode> Nodes => _nodes.Values;
    }
}
