using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;
using PX.Objects.FS.DAC.ReportParameters;

namespace PX.Objects.FS.VisibilityRestriction
{
    public sealed class FSStaffMemberVisibilityRestriction : PXCacheExtension<FSStaffMemberReportParameters>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
        }

        #region EmployeeID
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [RestrictVendorByUserBranches]
        public int? EmployeeID { get; set; }
        #endregion
    }
}
