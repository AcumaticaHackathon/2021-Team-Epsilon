using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.FS
{
    public sealed class FSEquipmentComponentVisibilityRestriction : PXCacheExtension<FSEquipmentComponent>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
        }

        #region VendorID
        [PXRemoveBaseAttribute(typeof(FSSelectorBusinessAccount_VEAttribute))]
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [FSSelectorBusinessAccount_VEVisibilityRestrictionAttribute]
        public int? VendorID { get; set; }
        #endregion
    }
}