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
        public Router _router;

        public SimulationManager(CarrierManager carrierManager, NodeRegistry nodeRegistry)
        {
            _carrierManager = carrierManager;
            _nodeRegistry = nodeRegistry;

            _router = new Router();
        }

        public void SetDestination(CarrierSession session, NodeControl destination)
        {
            session.Destination = destination;

            session.Route = _router.FindRoute(session.Carrier.CurrentNode, destination);

            if (session.Route == null)
            {
                Logger.Write(
                    $"[{session.Carrier.Id}] 경로를 찾을 수 없습니다.");
                return;
            }

            session.RouteIndex = 0;
        }
        public async Task Start()
        {
            foreach (var session in App.CarrierManager.Sessions)
            {
                if (session.Route == null)
                    continue;
                if (session.IsRunning) continue;

                _ = StartCarrier(session);
            }
        }

        public void Pause()
        {
            if (!_running)
                return;
            _running = false;
        }

        private async Task StartCarrier(CarrierSession session)
        {
            try
            {
                while (session.Route != null)
                {
                    if (_running == false) return;
                    session.IsRunning = true;

                    var nextIndex = session.RouteIndex + 1;

                    if (nextIndex >= session.Route.Nodes.Count)
                        break;

                    var nextNode = session.Route.Nodes[nextIndex];

                    if(nextNode.TryEnter(session.Carrier) == false)
                    {
                        Logger.Write($"[{session.Carrier.Id}] {nextNode.Id}에 진입할 수 없습니다.");
                        break;
                    }

                    var command = new MoveCommand(nextNode);

                    await Execute(session.Carrier, command);

                    session.Carrier.CurrentNode?.Leave();
                    session.Carrier.CurrentNode = nextNode; 
                    session.RouteIndex = nextIndex;
                }
            }
            finally
            {
                session.IsRunning = false;
            }
        }


        private async Task Execute(CarrierControl Carrier, MoveCommand command)
        {
            var pos = command.Destination.GetPosition();

            await Carrier.MoveToAsync(pos);

            Logger.Info($"{Carrier.Id} → {command.Destination.Id}");
        }

    }
}
