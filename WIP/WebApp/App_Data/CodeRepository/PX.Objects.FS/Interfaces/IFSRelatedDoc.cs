using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.FS
{
    public interface IFSRelatedDoc
    {
        string SrvOrdType { get; }

        string ServiceOrderRefNbr { get; }

        Int32? ServiceOrderLineNbr { get; }

        string AppointmentRefNbr { get; }

        Int32? AppointmentLineNbr { get; }

        string ServiceContractRefNbr { get; }

        int? ServiceContractPeriodID { get; }
    }
}
