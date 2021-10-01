using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.PJ.RequestsForInformation.PJ.DAC;

namespace PX.Objects.PJ
{
    public sealed class RequestForInformationVisibilityRestriction : PXCacheExtension<RequestForInformation>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
        }

        [PXMergeAttributes(Method = MergeMethod.Append)]
        [RestrictVendorByUserBranches(typeof(BAccountR.vOrgBAccountID))]
        [RestrictCustomerByUserBranches(typeof(BAccountR.cOrgBAccountID))]
        public int? BusinessAccountId { get; set; }
    }
}
