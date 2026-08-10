using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class Router
    {
        public Route? FindRoute(
            NodeControl start,
            NodeControl destination,
            bool allowReverse = false)
        {
            var queue = new Queue<NodeControl>();
            var previous = new Dictionary<NodeControl, NodeControl?>();

            queue.Enqueue(start);
            previous[start] = null;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == destination)
                    break;

                // 순방향
                foreach (var next in current.ConnectedNodes)
                {
                    if (previous.ContainsKey(next))
                        continue;

                    previous[next] = current;
                    queue.Enqueue(next);
                }

                // 역방향
                if (allowReverse)
                {
                    foreach (var next in current.ReverseConnectedNodes)
                    {
                        if (previous.ContainsKey(next))
                            continue;

                        previous[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }

            if (!previous.ContainsKey(destination))
                return null;

            var nodes = new List<NodeControl>();

            var node = destination;

            while (node != null)
            {
                nodes.Add(node);
                node = previous[node];
            }

            nodes.Reverse();

            var route = new Route();

            foreach (var item in nodes)
                route.Nodes.Add(item);

            return route;
        }
    }
}
