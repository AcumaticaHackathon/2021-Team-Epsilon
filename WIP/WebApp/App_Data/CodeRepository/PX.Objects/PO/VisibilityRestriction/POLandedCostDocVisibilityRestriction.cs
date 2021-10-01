using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.PO
{
	public sealed class POLandedCostDocVisibilityRestriction : PXCacheExtension<POLandedCostDoc>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(branchID: typeof(POLandedCostDoc.branchID), ResetVendor = false)]
		public int? VendorID { get; set; }
		#endregion
	}
}