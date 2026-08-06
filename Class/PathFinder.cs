using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class PathFinder
    {
        public List<NodeModel> FindPath(
            NodeModel start,
            NodeModel target)
        {
            Queue<List<NodeModel>> queue = new();

            queue.Enqueue(new List<NodeModel>
        {
            start
        });


            while (queue.Count > 0)
            {
                var path = queue.Dequeue();

                var current = path.Last();


                if (current == target)
                    return path;


                foreach (var next in current.NextNodes)
                {
                    if (path.Contains(next))
                        continue;


                    var newPath = new List<NodeModel>(path);

                    newPath.Add(next);

                    queue.Enqueue(newPath);
                }
            }


            return null;
        }
    }
}
