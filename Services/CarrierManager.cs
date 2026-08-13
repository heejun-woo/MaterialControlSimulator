using MaterialControlSimulator.Class;
using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        public CarrierControl GetCarrierControl(string carrierId)
        {
            return _sessions[carrierId].Carrier;
        }

        public bool ContainKey(string carrierId)
        {
            return _sessions.ContainsKey(carrierId);
        }

        private readonly List<CarrierMoveInfo> _movingCarriers = new();

        private bool _renderHooked;



        // =========================================================
        // MOVE
        // =========================================================

        public Task MoveToAsync(
            CarrierControl carrier,
            double targetX,
            double targetY,
            double durationMs = 500)
        {
            double startX = Canvas.GetLeft(carrier);
            double startY = Canvas.GetTop(carrier);

            if (double.IsNaN(startX))
                startX = 0;

            if (double.IsNaN(startY))
                startY = 0;

            // 이미 이동 중이면 기존 이동 제거
            for (int i = _movingCarriers.Count - 1; i >= 0; i--)
            {
                if (_movingCarriers[i].Carrier == carrier)
                {
                    _movingCarriers[i].Completion.TrySetResult(false);

                    _movingCarriers.RemoveAt(i);
                }
            }

            var completion =
                new TaskCompletionSource<bool>();

            _movingCarriers.Add(
                new CarrierMoveInfo
                {
                    Carrier = carrier,

                    StartX = startX,
                    StartY = startY,

                    TargetX = targetX,
                    TargetY = targetY,

                    DurationMs = durationMs,

                    StartTime = TimeSpan.Zero,

                    Completion = completion
                });

            if (!_renderHooked)
            {
                CompositionTarget.Rendering += OnRendering;

                _renderHooked = true;
            }

            return completion.Task;
        }

        // =========================================================
        // RENDER
        // =========================================================

        private void OnRendering(
            object? sender,
            EventArgs e)
        {
            if (e is not RenderingEventArgs args)
                return;

            if (_movingCarriers.Count == 0)
            {
                CompositionTarget.Rendering -= OnRendering;

                _renderHooked = false;

                return;
            }

            TimeSpan now = args.RenderingTime;

            for (int i = _movingCarriers.Count - 1; i >= 0; i--)
            {
                CarrierMoveInfo move =
                    _movingCarriers[i];

                // 첫 Rendering 시점
                if (move.StartTime == TimeSpan.Zero)
                {
                    move.StartTime = now;
                }

                double elapsed =
                    (now - move.StartTime)
                    .TotalMilliseconds;

                double progress =
                    Math.Min(
                        elapsed / move.DurationMs,
                        1.0);

                double x =
                    move.StartX +
                    (move.TargetX - move.StartX) *
                    progress;

                double y =
                    move.StartY +
                    (move.TargetY - move.StartY) *
                    progress;

                Canvas.SetLeft(
                    move.Carrier,
                    x);

                Canvas.SetTop(
                    move.Carrier,
                    y);

                if (progress >= 1.0)
                {
                    // 정확한 최종 위치
                    Canvas.SetLeft(
                        move.Carrier,
                        move.TargetX);

                    Canvas.SetTop(
                        move.Carrier,
                        move.TargetY);

                    _movingCarriers.RemoveAt(i);

                    move.Completion.TrySetResult(true);
                }
            }
        }
    }
}
