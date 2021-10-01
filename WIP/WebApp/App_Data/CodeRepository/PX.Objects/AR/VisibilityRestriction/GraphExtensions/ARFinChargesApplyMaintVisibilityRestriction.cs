using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AR
{

	public class ARFinChargesApplyMaintVisibilityRestriction : PXGraphExtension<ARFinChargesApplyMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		public override void Initialize()
		{
			base.Initialize();

			Base.ARFinChargeRecords.WhereAnd<Where<Customer.cOrgBAccountID, RestrictByBranch<Current<AccessInfo.branchID>>>>();
			Base.CustomersInStatementCycle.WhereAnd<Where<Customer.cOrgBAccountID, RestrictByBranch<Current<AccessInfo.branchID>>>>();
		}
	}
}
