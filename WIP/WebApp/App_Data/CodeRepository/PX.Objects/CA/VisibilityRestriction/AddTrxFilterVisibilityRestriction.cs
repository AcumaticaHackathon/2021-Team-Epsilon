using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.CA
{
	public sealed class AddTrxFilterVisibilityRestriction : PXCacheExtension<AddTrxFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region ReferenceID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByBranch(typeof(BAccountR.cOrgBAccountID), branchID: typeof(AccessInfo.branchID))]
		[RestrictVendorByBranch(typeof(BAccountR.vOrgBAccountID), branchID: typeof(AccessInfo.branchID))]
		public int? ReferenceID { get; set; }
		#endregion
	}
}