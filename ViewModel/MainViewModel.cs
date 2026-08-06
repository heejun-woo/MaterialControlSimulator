using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class MainViewModel
    {
        public ObservableCollection<NodeControl> Nodes
        {
            get
            {
                return App.Nodes.NodeList;
            }
        }
    }
}
