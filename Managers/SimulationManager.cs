using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaterialControlSimulator.Controls;

namespace MaterialControlSimulator
{
    public class SimulationManager
    {
        private readonly CarrierManager _carrierManager;

        private readonly Dictionary<string, Queue<MoveCommand>> _queues = new();


        public SimulationManager(
            CarrierManager carrierManager)
        {
            _carrierManager = carrierManager;
        }


        public void Enqueue(
            MoveCommand command)
        {
            if (!_queues.ContainsKey(command.CarrierId))
            {
                _queues[command.CarrierId] = new();
            }

            _queues[command.CarrierId].Enqueue(command);
        }


        public async Task StartAsync()
        {
            var tasks = _queues.Keys
                .Select(RunCarrierAsync);

            await Task.WhenAll(tasks);
        }


        private async Task RunCarrierAsync(
            string carrierId)
        {
            var queue = _queues[carrierId];


            while (queue.Count > 0)
            {
                var command = queue.Dequeue();

                await _carrierManager.ExecuteAsync(command);
            }
        }
    }
}
