using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.FS
{
    public class SrvManagementEmployeeMaintVisibilityRestriction : PXGraphExtension<SrvManagementEmployeeMaint>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
        }

        public override void Initialize()
        {
            base.Initialize();

            Base.SrvManagementStaffRecords.WhereAnd<Where<Vendor.vOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>();
        }
    }
}