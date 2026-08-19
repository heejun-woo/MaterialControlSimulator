
using System;
using System.Collections.Generic;
using System.Linq;

namespace MaterialControlSimulator
{
    public class CarrierHistoryManager
    {
        private readonly object _lock = new();

        private readonly List<CarrierHistory>
            _histories = new();

        public int MaxCount { get; set; } = 10000;

        public event Action? HistoryChanged;

        public void Add(
            string carrierId,
            string fromNode,
            string toNode,
            string eventName,
            string message = "")
        {
            var history =
                new CarrierHistory
                {
                    Time = DateTime.Now,
                    CarrierId = carrierId,
                    FromNode = fromNode,
                    ToNode = toNode,
                    Event = eventName,
                    Message = message
                };

            lock (_lock)
            {
                _histories.Add(history);

                if (_histories.Count > MaxCount)
                {
                    int removeCount =
                        _histories.Count - MaxCount;

                    _histories.RemoveRange(
                        0,
                        removeCount);
                }
            }

            HistoryChanged?.Invoke();
        }

        public List<CarrierHistory> GetAll()
        {
            lock (_lock)
            {
                return _histories
                    .OrderByDescending(x => x.Time)
                    .ToList();
            }
        }

        public List<CarrierHistory> FindByCarrier(
            string carrierId)
        {
            lock (_lock)
            {
                return _histories
                    .Where(x =>
                        x.CarrierId.Contains(
                            carrierId,
                            StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.Time)
                    .ToList();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _histories.Clear();
            }

            HistoryChanged?.Invoke();
        }
    }
}