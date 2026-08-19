using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator
{
    public class CarrierHistory
    {
        public DateTime Time { get; set; }

        public string CarrierId { get; set; } = "";

        public string FromNode { get; set; } = "";

        public string ToNode { get; set; } = "";

        public string Event { get; set; } = "";

        public string Message { get; set; } = "";
    }
}
