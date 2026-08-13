using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator.Plc.Logic
{
    public class CommunicationCheckLogic
      : IPlcLogic
    {
        private bool _lastRequest;
        private DateTime _lastRequestTime;

        public void Scan(PlcBindingManager manager)
        {
            bool request = manager.ReadBit(0x3000);

            // Rising Edge
            if (request != _lastRequest)
            {
                manager.WriteBit(0x3800, request);


                manager.WriteBit(0x3808, true);
                manager.WriteBit(0x3809, false);

                _lastRequestTime = DateTime.Now;
                _lastRequest = request;
            }

            // Timeout
            if ((DateTime.Now - _lastRequestTime).TotalSeconds > 10)
            {
                manager.WriteBit(0x3808, false);
                manager.WriteBit(0x3809, true);
            }
        }
    }
}
