using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp
{
    public enum NodeState
    {
        Run,
        Wait,
        Trouble,
        Stop
    }

    public enum HostRequestState
    {
        Idle,
        Waiting,
        Alarm,
        Set
    }
    public enum CarrierHistoryType
    {
        Created,
        DestinationSet,
        Arrived,
        CellTransfer,
        Removed,

        HostRequest,
        HostResponse,
        HostTimeout,
        HostRetry
    }
}
