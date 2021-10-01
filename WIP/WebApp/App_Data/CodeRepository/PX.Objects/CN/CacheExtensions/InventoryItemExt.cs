using PX.Data;
using PX.Objects.CN.Subcontracts.SC.DAC;
using PX.Objects.CS;
using System;

namespace PX.Objects.CN.CacheExtensions
{
    [Obsolete(Objects.Common.InternalMessages.ClassIsObsoleteAndWillBeRemoved2021R1)]
    public sealed class InventoryItemExt : PXCacheExtension<SubcontractInventoryItem>
    {
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(Visibility = PXUIVisibility.SelectorVisible, Visible = false)]
        public bool? StkItem
        {
            get;
            set;
        }

        [PXBool]
        [PXUIField(DisplayName = "Used in Project")]
        public bool? IsUsedInProject
        {
            get;
            set;
        }

        public static bool IsActive()
        {
            return false;
        }

        public abstract class isUsedInProject : IBqlField
        {
        }
    }
}