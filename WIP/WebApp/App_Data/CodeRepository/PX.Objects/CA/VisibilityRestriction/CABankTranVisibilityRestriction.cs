using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.CA
{
	public sealed class CABankTranVisibilityRestriction : PXCacheExtension<CABankTran>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region PayeeBAccountID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByBranch(source: typeof(BAccountR.cOrgBAccountID), branchID: typeof(AccessInfo.branchID))]
		[RestrictVendorByBranch(source: typeof(BAccountR.vOrgBAccountID), branchID: typeof(AccessInfo.branchID))]
		public int? PayeeBAccountID { get; set; }
		#endregion
	}
}