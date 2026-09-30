using EMA.ExtendedWPFConverters;
using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using WpfApp;

namespace MaterialControlSimulator.Plc.Logic
{
    public class LotInfoRequestLogic : IPlcLogic
    {

        private readonly int _requestAddress;
        private readonly int _responseAddress;
        private readonly int _lotIdAddress;
        private readonly int _nextLotIdAddress;

        private readonly int _timeoutSeconds;

        private readonly int _LotIdLength = 8;

        private HostRequestState _state = HostRequestState.Idle;

        private DateTime _requestTime;
        private readonly Stopwatch _timer = new();
        private bool _firstScan = true;


        public LotInfoRequestLogic(
            int requestAddress,
            int responseAddress,
            int lotIdAddress,
            int nextLotIdAddress,
            int timeoutSeconds = 30)
        {
            _requestAddress = requestAddress;
            _responseAddress = responseAddress;
            _lotIdAddress = lotIdAddress;
            _nextLotIdAddress = nextLotIdAddress;

            _timeoutSeconds = timeoutSeconds;
        }

        public void Scan(PlcBindingManager manager)
        {
            if (manager.ReadBit(_requestAddress) == false)
            {
                _state = HostRequestState.Idle;
                return;
            }
            else if (_state == HostRequestState.Idle)
            {
                _requestTime = DateTime.Now;
                _state = HostRequestState.Waiting;
            }

            #region 1초 주기로 실행
            if (!_firstScan && _timer.ElapsedMilliseconds < 1000)
            {
                return;
            }

            _firstScan = false;
            _timer.Restart();
            #endregion

            switch (_state)
            {
                case HostRequestState.Idle:
                    break;


                case HostRequestState.Waiting:
                    CheckResponse(manager);
                    break;


                case HostRequestState.Alarm:

                    manager.WriteBit(_requestAddress, false);
                    _state = HostRequestState.Idle;

                    break;
            }
        }



        private void CheckResponse(PlcBindingManager manager)
        {
            bool response = manager.ReadBit(_responseAddress);

            if (response)
            {
                string strLotID = manager.ReadStringFromMemory(_lotIdAddress, _LotIdLength);
                manager.WriteStringToMemory(_nextLotIdAddress, strLotID, _LotIdLength);

                manager.WriteBit(_requestAddress, false);
                return;
            }

            if ((DateTime.Now - _requestTime).TotalSeconds >= _timeoutSeconds)
            {
                SetAlarm(true);
                
                _state = HostRequestState.Alarm;
                manager.WriteBit(_requestAddress, false);
            }

        }


        private void SetAlarm(bool value)
        {

        }


    }
}
