using PX.Data;
using PX.Objects.IN;
using System;

namespace PX.Objects.CN.Subcontracts.SC.DAC
{
    /// <summary>
    /// This class is required as Acumatica use Filters with SELECTOR type across all system.
    /// We needed to override ViewName of the <see cref="T:PX.Data.FilterHeader" /> to specific entity type.
    /// </summary>
    /// 
    [Obsolete(Objects.Common.InternalMessages.ClassIsObsoleteAndWillBeRemoved2021R1)]
    [PXCacheName("Subcontract Inventory Item")]
    public class SubcontractInventoryItem : InventoryItem
    {
        public new abstract class inventoryID : IBqlField
        {
        }

        public new abstract class inventoryCD : IBqlField
        {
        }

        public new abstract class descr : IBqlField
        {
        }
    }
}
