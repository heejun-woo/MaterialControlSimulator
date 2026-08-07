using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MaterialControlSimulator.Controls;
using System.Diagnostics;

namespace MaterialControlSimulator
{
    public class CarrierManager
    {
        private readonly Dictionary<string, CarrierSession> _sessions = new();


        public IEnumerable<CarrierSession> Sessions
            => _sessions.Values;


        public void Register(CarrierControl carrier)
        {
            if (_sessions.ContainsKey(carrier.Id))
                return;

            _sessions.Add(carrier.Id, new CarrierSession(carrier));
        }

        public CarrierSession Get(string carrierId)
        {
            return _sessions[carrierId];
        }
    }
}
