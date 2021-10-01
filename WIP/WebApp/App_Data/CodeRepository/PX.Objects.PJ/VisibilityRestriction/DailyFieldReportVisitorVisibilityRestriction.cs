using PX.Data;
using PX.Data.Licensing;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.PJ.DailyFieldReports.PJ.DAC;

namespace PX.Objects.PJ
{
    public sealed class DailyFieldReportVisitorVisibilityRestriction : PXCacheExtension<DailyFieldReportVisitor>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
        }

        #region BusinessAccountId
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [RestrictVendorByUserBranches(source: typeof(BAccountR.vOrgBAccountID))]
        [RestrictCustomerByUserBranches(source: typeof(BAccountR.cOrgBAccountID))]
        public int? BusinessAccountId { get; set; }
        #endregion
    }

    public sealed class DailyFieldReportSubcontractorActivityVisibilityRestriction : PXCacheExtension<DailyFieldReportSubcontractorActivity>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
        }

        #region VendorId
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [RestrictVendorByUserBranches]
        public int? VendorId { get; set; }
        #endregion
    }
}
