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
    public class LotIStartRequestLogic : IPlcLogic
    {

        private readonly int _requestAddress;
        private readonly int _responseAddress;

        private readonly int _timeoutSeconds;

        private readonly int _LotIdLength = 8;

        private HostRequestState _state = HostRequestState.Idle;

        private DateTime _requestTime;
        private readonly Stopwatch _timer = new();
        private bool _firstScan = true;


        public LotIStartRequestLogic(
            int requestAddress,
            int responseAddress,
            int timeoutSeconds = 30)
        {
            _requestAddress = requestAddress;
            _responseAddress = responseAddress;

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
                string? strLotID = manager.GetValue<string>("W3868", _LotIdLength);
                manager.SetValue("W3878", strLotID, _LotIdLength);
                manager.SetValue("W3868", string.Empty, _LotIdLength);

                string? strProdID = manager.GetValue<string>("W3860", 5);
                manager.SetValue("W3870", strProdID, 5);
                manager.SetValue("W3860", string.Empty, 5);

                manager.WriteBit(0x380A, true);

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
