using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.CA
{
	public class CashAccountMaintVisibilityRestriction : PXGraphExtension<CashAccountMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public void _(Events.CacheAttached<CashAccount.referenceID> e)
		{
		}
	}
}