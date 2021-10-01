using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.PM
{
    public sealed class PMTranVisibilityRestriction : PXCacheExtension<PMTran>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
        }

        #region BAccountID
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [RestrictCustomerByUserBranches(typeof(BAccountR.cOrgBAccountID))]
        [RestrictVendorByUserBranches(typeof(BAccountR.vOrgBAccountID))]
        public int? BAccountID { get; set; }
        #endregion
    }
}