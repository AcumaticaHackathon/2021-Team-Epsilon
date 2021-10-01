using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;

namespace PX.Objects.FS
{
    public sealed class FSEquipmentVisibilityRestriction : PXCacheExtension<FSEquipment>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
        }

        #region CustomerID
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [RestrictCustomerByUserBranches]
        public int? CustomerID { get; set; }
        #endregion

        #region OwnerID
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [RestrictCustomerByUserBranches]
        public int? OwnerID { get; set; }
        #endregion

        #region VendorID
        [PXRemoveBaseAttribute(typeof(FSSelectorBusinessAccount_VEAttribute))]
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [FSSelectorBusinessAccount_VEVisibilityRestrictionAttribute]
        public int? VendorID { get; set; }
        #endregion
    }
}
