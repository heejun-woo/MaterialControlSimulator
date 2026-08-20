using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class CarrierRouteManager
    {
        private readonly CarrierManager _carrierManager;
        private readonly NodeRegistry _nodeRegistry;
        public Router _router;

        public CarrierRouteManager(
            CarrierManager carrierManager,
            NodeRegistry nodeRegistry)
        {
            _carrierManager = carrierManager;
            _nodeRegistry = nodeRegistry;
            _router = new Router();
        }

        public bool SetDestination(
            CarrierControl carrier,
            string destinationNodeId)
        {
            var session =
                _carrierManager.Get(
                    carrier.Id);

            if (session == null)
                return false;

            var destination =
                _nodeRegistry.Get(
                    destinationNodeId);

            if (destination == null)
                return false;

            // 여기서 기존 SimulatorManager에 있던
            // Router 검색 로직 사용
            Route? route =
                FindRoute(
                    session,
                    destination);

            if (route == null)
                return false;

            // 네 실제 Session 구조에 맞춰 이름만 변경
            session.Route = route;
            session.Destination = destination;

            return true;
        }

        private Route? FindRoute(
            CarrierSession session,
            NodeControl destination)
        {
            return _router.FindRoute(session.Carrier.CurrentNode, destination);
        }
    }
}
