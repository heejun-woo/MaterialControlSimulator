using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class NodeRegistry
    {
        private readonly Dictionary<string, NodeControl> _nodes = new();


        public ObservableCollection<NodeControl> NodeList { get; }
            = new();


        public int Count => _nodes.Count;


        public void Register(NodeControl node)
        {
            if (string.IsNullOrEmpty(node.Id))
                return;

            if (_nodes.ContainsKey(node.Id))
                return;


            _nodes.Add(node.Id, node);

            NodeList.Add(node);

            Logger.Info(
                $"Node Registered : {node.Id}");
        }


        public NodeControl? Get(string id)
        {
            if (_nodes.TryGetValue(id, out var node))
                return node;

            return null;
        }


        public bool Remove(string id)
        {
            if (!_nodes.TryGetValue(id, out var node))
                return false;


            _nodes.Remove(id);
            NodeList.Remove(node);

            return true;
        }
    }


}
