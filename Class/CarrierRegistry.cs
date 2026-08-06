using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaterialControlSimulator.Controls;

namespace MaterialControlSimulator
{
    public class CarrierRegistry
    {
        private readonly Dictionary<string, CarrierControl> _carriers = new();
        
        public int Count => _carriers.Count;

        public void Register(CarrierControl carrier)
        {
            _carriers[carrier.Id] = carrier;
        }


        public CarrierControl Get(string id)
        {
            return _carriers[id];
        }
    }
}
