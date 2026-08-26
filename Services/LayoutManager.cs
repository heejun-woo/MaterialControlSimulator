using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class LayoutManager
    {
        public event Action? LoadComplete;


        public void Initialize()
        {
            //if (App.Nodes.Count == 0)
            //    return;

            //if (App.Carriers.Count == 0)
            //    return;


            LoadComplete?.Invoke();
        }
    }
}
