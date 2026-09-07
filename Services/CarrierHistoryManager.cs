
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using WpfApp;

namespace MaterialControlSimulator
{
    public class CarrierHistoryManager
    {
        private readonly object _lock = new();

        private readonly List<CarrierHistory>
            _histories = new();

        private readonly string _folder =
            Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                "CarrierHistory");

        public int MaxCount { get; set; } = 10000;

        public event Action? HistoryChanged;

        public CarrierHistoryManager()
        {
            Directory.CreateDirectory(_folder);

            LoadToday();
        }

        public void Add(string carrierId, string fromNode, string toNode, string eventName, string message = "")
        {
            Logger.Info($"[{carrierId}] : {message}");
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

                Save(history);

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

        public void Add(CarrierHistory history)
        {
            lock (_lock)
            {
                _histories.Add(history);

                Save(history);
            }
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

        private void Save(CarrierHistory history)
        {
            string path =
                GetFilePath(history.Time.Date);

            string json =
                JsonSerializer.Serialize(history);

            File.AppendAllText(
                path,
                json + Environment.NewLine);
        }

        private string GetFilePath(DateTime date)
        {
            return Path.Combine(
                _folder,
                $"{date:yyyy-MM-dd}.jsonl");
        }

        private void LoadToday()
        {
            _histories.Clear();

            string path =
                GetFilePath(DateTime.Today);

            if (!File.Exists(path))
                return;

            foreach (string line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var history =
                    JsonSerializer.Deserialize
                        <CarrierHistory>(line);

                if (history != null)
                    _histories.Add(history);
            }


        }
        public List<CarrierHistory> Load(DateTime date)
        {
            string path =
                GetFilePath(date);

            if (!File.Exists(path))
                return new List<CarrierHistory>();

            return File.ReadLines(path)
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x))
                .Select(x =>
                    JsonSerializer.Deserialize
                        <CarrierHistory>(x))
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();
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
