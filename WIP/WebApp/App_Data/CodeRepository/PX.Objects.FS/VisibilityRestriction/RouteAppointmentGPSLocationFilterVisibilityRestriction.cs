using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;

namespace PX.Objects.FS
{
    public class RouteAppointmentGPSLocationFilterVisibilityRestriction : PXCacheExtension<RouteAppointmentGPSLocationFilter>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
        }

        #region CustomerID
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [RestrictCustomerByUserBranches]
        public virtual int? CustomerID { get; set; }
        #endregion
    }
}
