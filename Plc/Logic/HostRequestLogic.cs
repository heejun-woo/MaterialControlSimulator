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
    public class HostRequestLogic : IPlcLogic
    {
        private readonly string _nodeId;

        private readonly int _requestAddress;
        private readonly int _responseAddress;
        private readonly int _destinationAddress;

        private readonly string _destination1;
        private readonly string _destination2;
        private readonly string _destination3;

        private readonly int _timeoutSeconds;

        private HostRequestState _state =
            HostRequestState.Idle;

        private DateTime _requestTime;
        private readonly Stopwatch _timer = new();
        private bool _firstScan = true;



        public HostRequestLogic(
            string nodeId,
            int requestAddress,
            int responseAddress,
            int destinationAddress,
            string destination1,
            string destination2,
            string destination3,
            int timeoutSeconds)
        {
            _nodeId = nodeId;

            _requestAddress = requestAddress;
            _responseAddress = responseAddress;
            _destinationAddress = destinationAddress;

            _destination1 = destination1;
            _destination2 = destination2;
            _destination3 = destination3;

            _timeoutSeconds = timeoutSeconds;
        }

        public void Scan(PlcBindingManager manager)
        {


            var session = App.CarrierManager.FindSessionAtNode(_nodeId);

            // Carrier 없음
            if (session == null)
            {
                _state = HostRequestState.Idle;
                return;
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

                    if (manager.ReadBit(_responseAddress))                         
                        break;

                    if (manager.ReadBit(_requestAddress))
                    {
                        manager.WriteBit(_requestAddress, false);
                        break;
                    }

                    StartRequest(manager, session);
                    break;


                case HostRequestState.Waiting:

                    CheckResponse(manager, session);
                    break;


                case HostRequestState.Alarm:

                    if (_timer.ElapsedMilliseconds < 5000) return;
                    // Carrier 정지
                    // Retry 할 때까지 아무것도 안 함
                    Retry(manager);
                    break;
            }
        }

        private void StartRequest(PlcBindingManager manager, CarrierSession session)
        {    
            manager.WriteBit(_requestAddress, true);

            _requestTime = DateTime.Now;

            _state = HostRequestState.Waiting;

            Application.Current.Dispatcher.BeginInvoke(
               new Action(() =>
               {
                   App.CarrierHistory.Add(
                        session.Carrier.Id,
                        _nodeId,
                        "",
                        CarrierHistoryType.HostRequest.ToString(),
                        $"Request ON : {_requestAddress}");
               }));
        }


        private void CheckResponse(PlcBindingManager manager, CarrierSession session)
        {
            bool response = manager.ReadBit(_responseAddress);

            if (response)
            {
                int destination = manager.ReadWord(_destinationAddress);

                ProcessResponse(
                    manager,
                    session,
                    destination);


                manager.WriteBit(_requestAddress, false);
                return;
            }

            if ((DateTime.Now - _requestTime)
                    .TotalSeconds >= _timeoutSeconds)
            {
                SetAlarm(true);

                
                _state = HostRequestState.Alarm;

                manager.WriteBit(_requestAddress, false);
            }

        }

        private void ProcessResponse(PlcBindingManager manager, CarrierSession session, int respRoute)
        {
            string destination;

            switch (respRoute)
            {
                case 1:
                    destination = _destination1;
                    break;

                case 2:
                    destination = _destination2;
                    break;

                case 3:
                    destination = _destination3;
                    break;

                default:
                    return;
            }

            // 기존 SimulatorManager의 목적지 등록
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    App.CarrierHistory.Add(
                        session.Carrier.Id,
                        _nodeId,
                        destination,
                        CarrierHistoryType.HostResponse.ToString(),
                        $"HostResponse ON : {_nodeId}");

                    App._routeManager.SetDestination(
                        session.Carrier,
                        destination);

                    _state = HostRequestState.Set;
                }));
        }


        public void Retry(PlcBindingManager manager)
        {
            if (_state != HostRequestState.Alarm)
                return;

            SetAlarm(false);

            manager.WriteBit(_requestAddress, true);

            _requestTime = DateTime.Now;

            _state =
                HostRequestState.Waiting;
        }

        private void SetAlarm(bool value)
        {
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    var node =
                        App.Nodes.Get(_nodeId)
                            as HostRequestControl;

                    if (node != null)
                        node.IsAlarm = value;
                }));
        }


    }
}
