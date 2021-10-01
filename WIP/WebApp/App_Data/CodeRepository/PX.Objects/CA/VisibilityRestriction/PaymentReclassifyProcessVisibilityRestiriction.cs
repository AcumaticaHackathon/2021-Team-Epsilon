using PX.Data;
using PX.Objects.CS;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.AP;

namespace PX.Objects.CA
{
	public class PaymentReclassifyProcessVisibilityRestiriction : PXGraphExtension<PaymentReclassifyProcess>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[RestrictCustomerByBranch(source: typeof(BAccountR.cOrgBAccountID),
			WhereType: typeof(Or<Current<PaymentReclassifyProcess.Filter.showReclassified>, Equal<True>>),
			branchID: typeof(AccessInfo.branchID),
			ResetCustomer = true)]
		[RestrictVendorByBranch(source: typeof(BAccountR.vOrgBAccountID),
			WhereType: typeof(Or<Current<PaymentReclassifyProcess.Filter.showReclassified>, Equal<True>>),
			branchID: typeof(AccessInfo.branchID),
			ResetVendor = true)]
		protected virtual void CASplitExt_ReferenceID_CacheAttached(PXCache sender) { }
	}
}