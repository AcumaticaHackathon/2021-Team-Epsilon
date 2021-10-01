using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.CA
{
	public sealed class CASplitExtMultipleBaseCurrencies : PXCacheExtension<CASplitExtVisibilityRestiriction, CASplitExt>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		#region ReferenceID
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[RestrictCustomerByBranch(typeof(BAccountR.cOrgBAccountID), branchID: typeof(PaymentReclassifyProcess.Filter.branchID))]
		[RestrictVendorByBranch(typeof(BAccountR.vOrgBAccountID), branchID: typeof(PaymentReclassifyProcess.Filter.branchID))]
		public int? ReferenceID { get; set; }
		#endregion
	}
}
