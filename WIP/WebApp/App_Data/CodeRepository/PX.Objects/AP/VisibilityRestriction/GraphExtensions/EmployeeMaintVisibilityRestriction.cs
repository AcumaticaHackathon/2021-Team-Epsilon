using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.GL.DAC;
using CRLocation = PX.Objects.CR.Standalone.Location;

namespace PX.Objects.EP
{
    public class EmployeeMaintVisibilityRestriction: PXGraphExtension<EmployeeMaint>
    {
	    public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

	    #region VBranchID
	    [Branch(useDefaulting: false, IsDetail = false, PersistingCheck = PXPersistingCheck.Nothing, DisplayName = "Receiving Branch", IsEnabledWhenOneBranchIsAccessible = true)]
	    public void Location_VBranchID_CacheAttached(PXCache sender) { }
	    #endregion
	}
}