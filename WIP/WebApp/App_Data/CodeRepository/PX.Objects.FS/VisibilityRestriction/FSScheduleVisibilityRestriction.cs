using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.FS
{
    public sealed class FSScheduleVisibilityRestriction : PXCacheExtension<FSSchedule>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
        }

        #region VendorID
        [PXRemoveBaseAttribute(typeof(FSSelectorVendorAttribute))]
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [FSSelectorVendorRestrictVisibilityAttribute]
        public int? VendorID { get; set; }
        #endregion
    }
}