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

        public void Scan(
            PlcMemory memory)
        {
            bool request =
                memory.ReadBit(0x3000);

            // Rising Edge
            if (request != _lastRequest)
            {
                memory.WriteBit(0x3800, request);


                memory.WriteBit(0x3808, true);
                memory.WriteBit(0x3809, false);

                _lastRequestTime = DateTime.Now;
                _lastRequest = request;
            }

            // Timeout
            if ((DateTime.Now - _lastRequestTime).TotalSeconds > 10)
            {
                memory.WriteBit(0x3808, false);
                memory.WriteBit(0x3809, true);
            }
        }
    }
}
