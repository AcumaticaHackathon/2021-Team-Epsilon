using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.AR
{
	public sealed class CustomerLocationMaintVisibilityRestriction : PXGraphExtension<CustomerLocationMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		public override void Initialize()
		{
			base.Initialize();

			Base.Location.WhereAnd<Where<BAccount.cOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByUserBranches]
		public void Location_BAccountID_CacheAttached(PXCache sender) { }
	}
}