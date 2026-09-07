using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp;

namespace MaterialControlSimulator.Plc.Logic
{
    public class CellTransferLogic : IPlcLogic
    {
        private readonly CellUnloadControl _unload;

        private readonly string _unloadNodeId;
        private readonly string _loadNodeId;
        private readonly string _unloadNextNodeId;
        private readonly string _loadNextNodeId;
        private readonly int _magazineSize;
        private readonly int _transferIntervalMs;

        private readonly Stopwatch _timer =
            Stopwatch.StartNew();

        public CellTransferLogic(
                CellUnloadControl unload,
                string unloadNodeId,
                string loadNodeId,
                string unloadNextNodeId,
                string loadNextNodeId,
                int magazineSize,
                int transferIntervalMs)
        {
            _unload = unload;
            _unloadNodeId = unloadNodeId;
            _loadNodeId = loadNodeId;

            _unloadNextNodeId = unloadNextNodeId;
            _loadNextNodeId = loadNextNodeId;

            _magazineSize = magazineSize;
            _transferIntervalMs = transferIntervalMs;
        }

        public void Scan(
            PlcBindingManager manager)
        {
            if (!_unload.TransferEnabled)
                return;

            if (_timer.ElapsedMilliseconds <
                _unload.TransferIntervalMs)
            {
                return;
            }

            _timer.Restart();

            ProcessTransfer();
        }

        private void ProcessTransfer()
        {
            if (string.IsNullOrWhiteSpace(
                    _unload.PairNodeId))
            {
                return;
            }

            // Pair Load Node 찾기
            var pairNode =
                App.Nodes.Get(
                    _unload.PairNodeId)
                as CellLoadControl;

            if (pairNode == null)
                return;

            // Unload 위치의 Carrier Session
            var sourceSession =
               App.CarrierManager.FindSessionAtNode(
                   _unloadNodeId);

            // Load 위치의 Carrier Session
            var targetSession =
                 App.CarrierManager.FindSessionAtNode(
                    _loadNodeId);

            if (sourceSession == null ||
                targetSession == null)
            {
                return;
            }

            ProcessTransfer(
              sourceSession,
              targetSession);
        }

        private void ProcessTransfer(
            CarrierSession sourceSession,
            CarrierSession targetSession)
        {
            var dispatcher =
                sourceSession.Carrier.Dispatcher;

            dispatcher.BeginInvoke(
                new Action(() =>
                {
                    var source =
                        sourceSession.Carrier;

                    var target =
                        targetSession.Carrier;

                    // -----------------------------
                    // 이동 가능 수량 계산
                    // -----------------------------

                    int transferCount =
                        Math.Min(
                            _magazineSize,
                            Math.Min(
                                source.CurrentCellCount,
                                target.MaxCellCount -
                                target.CurrentCellCount));

                    if (transferCount > 0)
                    {
                        source.CurrentCellCount -=
                            transferCount;

                        target.CurrentCellCount +=
                            transferCount;
                    }

                    // -----------------------------
                    // 주는 쪽 Empty
                    // -----------------------------

                    if (source.CurrentCellCount == 0)
                    {
                        App.CarrierHistory.Add(
                            sourceSession.Carrier.Id,
                            string.Empty,
                            string.Empty,
                            CarrierHistoryType.CellTransfer.ToString(),
                            $"CellTransfer : Empty");

                        App._routeManager.SetDestination(
                            sourceSession.Carrier,
                            _unloadNextNodeId);
                    }

                    // -----------------------------
                    // 받는 쪽 Full
                    // -----------------------------

                    if (target.CurrentCellCount >=
                        target.MaxCellCount)
                    {
                        App.CarrierHistory.Add(
                            sourceSession.Carrier.Id,
                            string.Empty,
                            string.Empty,
                            CarrierHistoryType.CellTransfer.ToString(),
                            $"CellTransfer : {target.CurrentCellCount}");

                        App._routeManager.SetDestination(
                            targetSession.Carrier,
                            _loadNextNodeId);
                    }
                }));
        }

    }

}
