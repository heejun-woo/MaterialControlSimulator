using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class CarrierState
    {
        public string CarrierId { get; set; } = "";

        // 마지막으로 확정된 위치
        public string CurrentNodeId { get; set; } = "";

        // 현재 목적지
        public string DestinationNodeId { get; set; } = "";

        public bool Empty { get; set; }
    }
}
