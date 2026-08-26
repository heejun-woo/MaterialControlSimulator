using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
            while (true)
            {
                if (_running ==false)
                {
                    session.IsRunning = false;
                    return;
                }
                session.IsRunning = true;

                await Task.Delay(1000); // 지연대기

                var carrier = session.Carrier;
                var currentNode = carrier.CurrentNode;

                if (currentNode == null)
                    return;

                NodeControl? nextNode = null;

                // Route가 있으면 기존 Route 로직
                if (session.Route != null)
                {
                    nextNode = GetNextRouteNode(session);

                    if (nextNode == null)
                        return;
                }
                // Route가 없으면 순방향으로 이동
                else if (currentNode is LocationControl)
                {
                    if (currentNode.ConnectedNodes.Count == 0)
                        return;

                    nextNode = currentNode.ConnectedNodes[0];
                }    
                // Port에서는 목적지가 없으면 대기
                else
                {
                    await Task.Delay(100);
                    continue;
                }

                // 다음 노드가 점유되어 있으면 대기
                if (!nextNode.TryEnter(carrier))
                {
                    await Task.Delay(100);
                    continue;
                }

                // 현재 노드에서 나감
                currentNode.Leave();

                // 현재 노드 변경

                App.CarrierHistory.Add(carrier.Id, carrier.CurrentNode.Id, nextNode.Id, "MOVE");
                carrier.CurrentNode = nextNode;

                // 실제 이동
                var command = new MoveCommand(nextNode);
                await Execute(carrier, command);

                //목적지 도착
                if(session.Destination == nextNode)
                {
                    session.Destination = null;
                    session.Route = null;
                }
            }
        }

        private NodeControl? GetNextRouteNode(CarrierSession session)
        {
            var route = session.Route;

            if (route == null)
                return null;

            var currentIndex =
                route.Nodes.IndexOf(session.Carrier.CurrentNode);

            if (currentIndex < 0)
                return null;

            if (currentIndex + 1 >= route.Nodes.Count)
                return null;

            return route.Nodes[currentIndex + 1];
        }

        private async Task Execute(CarrierControl Carrier, MoveCommand command)
        {
            await Carrier.MoveToAsync(command.Destination);

            Logger.Info($"{Carrier.Id} → {command.Destination.Id}");
        }

    }
}
