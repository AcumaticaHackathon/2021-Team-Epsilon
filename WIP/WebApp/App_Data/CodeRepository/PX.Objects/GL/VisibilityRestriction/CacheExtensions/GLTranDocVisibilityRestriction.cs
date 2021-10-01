using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.GL
{
	public sealed class GLTranDocVisibilityRestriction : PXCacheExtension<GLTranDoc>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region BAccountID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByBranch(typeof(BAccountR.cOrgBAccountID), branchID: typeof(GLDocBatch.branchID),
			ResetCustomer = true)]
		[RestrictVendorByBranch(typeof(BAccountR.vOrgBAccountID), branchID: typeof(GLDocBatch.branchID),
			ResetVendor = true)]
		public int? BAccountID { get; set; }
		#endregion
	}
}