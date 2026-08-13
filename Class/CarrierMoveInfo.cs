using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator.Class
{
    public class CarrierMoveInfo
    {
        public CarrierControl Carrier { get; set; } = null!;

        public double StartX { get; set; }
        public double StartY { get; set; }

        public double TargetX { get; set; }
        public double TargetY { get; set; }

        public TimeSpan StartTime { get; set; }

        public double DurationMs { get; set; }

        public TaskCompletionSource<bool> Completion { get; set; } = null!;
    }
}
