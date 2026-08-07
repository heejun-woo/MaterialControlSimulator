using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class SimulationManager
    {
        private readonly CarrierManager _carrierManager;
        private readonly NodeRegistry _nodeRegistry;
        public bool _running = false;

        public SimulationManager(
            CarrierManager carrierManager,
            NodeRegistry nodeRegistry)
        {
            _carrierManager = carrierManager;
            _nodeRegistry = nodeRegistry;
        }

        private async Task StartCarrier(CarrierSession session)
        {
            while (_running)
            {
                var nextNode = session.Route.GetNextNode(session.CurrentNode);


                if (nextNode == null)
                {
                    await Task.Delay(100);
                    continue;
                }


                var command = new MoveCommand(nextNode);
                session.Queue.Enqueue(command);

                if (!session.Running)
                {
                    _ = Run(session);
                }

                session.Carrier.CurrentNode = nextNode;

                await Task.Delay(100);
            }
        }


        public async Task Start(Route route)
        {
            foreach (var session in App.CarrierManager.Sessions)
            {
                session.Route = route;

                _ = StartCarrier(session);
            }
        }


        private async Task Run(CarrierSession session)
        {
            session.Running = true;

            while (true)
            {
                if (session.Queue.Count == 0)
                {
                    break;
                }

                var command = session.Queue.Dequeue();

                var pos = command.Destination.GetPosition();

                await session.Carrier.MoveToAsync(pos);

                Logger.Info(
                    $"{session.Carrier.Id} → {command.Destination.Id}");
            }

            session.Running = false;
        }
    }
}
