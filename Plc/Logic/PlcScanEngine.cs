using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator.Plc.Logic
{
    public class PlcScanEngine
    {
        private readonly PlcBindingManager _manager;
        private readonly List<IPlcLogic> _logics = new();

        private CancellationTokenSource? _cts;
        private Task? _scanTask;

        public int ScanIntervalMs { get; set; } = 20;

        public bool IsRunning =>
            _scanTask != null &&
            !_scanTask.IsCompleted;

        public PlcScanEngine(PlcBindingManager manager)
        {
            _manager = manager;
        }

        public void AddLogic(
            IPlcLogic logic)
        {
            _logics.Add(logic);
        }

        public void Start()
        {
            if (IsRunning)
                return;

            _cts =
                new CancellationTokenSource();

            _scanTask =
                Task.Run(
                    () => ScanLoopAsync(
                        _cts.Token));
        }

        private async Task ScanLoopAsync(
            CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    foreach (var logic in _logics)
                    {
                        logic.Scan(
                            _manager);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        $"PLC Scan ERROR : {ex}");
                }

                try
                {
                    await Task.Delay(
                        ScanIntervalMs,
                        token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public async Task StopAsync()
        {
            if (_cts == null)
                return;

            _cts.Cancel();

            if (_scanTask != null)
            {
                try
                {
                    await _scanTask;
                }
                catch (OperationCanceledException)
                {
                }
            }

            _cts.Dispose();

            _cts = null;
            _scanTask = null;
        }
    }
}
