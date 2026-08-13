using MaterialControlSimulator.Plc;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace MaterialControlSimulator
{
    public interface IPlcBindable
    {
        ObservableCollection<PlcBinding> PlcBindings { get; }
    }
}