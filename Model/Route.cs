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
        public List<NodeControl> Nodes { get; set; }
         = new();
        public bool Loop { get; set; }

        public NodeControl? GetNextNode(NodeControl currentNode)
        {
            var index = Nodes.IndexOf(currentNode);

            if (index < 0)
                return null;


            // 마지막 노드인 경우
            if (index == Nodes.Count - 1)
            {
                if (Loop)
                    return Nodes[0];

                return null;
            }


            return Nodes[index + 1];
        }
    }
}
