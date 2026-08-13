using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private readonly Stopwatch _timer = new();
        private bool _firstScan = true;
        public void Scan(PlcBindingManager manager)
        {
            #region 1초 주기로 실행
            if (!_firstScan && _timer.ElapsedMilliseconds < 1000)
            {
                return;
            }

            _firstScan = false;
            _timer.Restart(); 
            #endregion

            short check = manager.GetValue<short>("W3800");

            if (manager.GetValue<short>("W3800") == 9999)
            {
                manager.SetValue("W3800", (short)1);
            }
            else manager.SetValue("W3800", manager.GetValue<short>("W3800") + 1);

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
            if ((DateTime.Now - _lastRequestTime).TotalSeconds > 30)
            {
                manager.WriteBit(0x3808, false);
                manager.WriteBit(0x3809, true);
            }
        }
    }
}
