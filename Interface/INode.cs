using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MaterialControlSimulator.Controls;

namespace MaterialControlSimulator
{
    public interface INode
    {
        string Id { get; }

        Point GetPosition();

        bool IsOccupied { get; }

        CarrierControl? Carrier { get; }


        bool Enter(CarrierControl carrier);

        void Leave();
    }
}
