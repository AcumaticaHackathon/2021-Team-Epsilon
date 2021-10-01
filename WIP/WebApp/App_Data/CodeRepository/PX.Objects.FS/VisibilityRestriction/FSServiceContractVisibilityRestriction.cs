using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CS;

namespace PX.Objects.FS
{
    public sealed class FSServiceContractVisibilityRestriction : PXCacheExtension<FSServiceContract>
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

        #region BillCustomerID
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [RestrictCustomerByBranch(typeof(FSServiceContract.branchID), ResetCustomer = true)]
        public int? BillCustomerID { get; set; }
        #endregion

        #region VendorID
        [PXRemoveBaseAttribute(typeof(FSSelectorVendorAttribute))]
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [FSSelectorVendorRestrictVisibilityAttribute]
        public int? VendorID { get; set; }
        #endregion
    }
}
