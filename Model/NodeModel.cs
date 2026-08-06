using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MaterialControlSimulator
{
    public class NodeModel
    {
        public string Id { get; set; }

        public List<NodeModel> NextNodes { get; set; } = new();
    }
}
