using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.PO.LandedCosts
{
	public sealed class POReceiptFilterVisibilityRestriction : PXCacheExtension<POReceiptFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(branchID: typeof(POReceipt.branchID), ResetVendor = false)]
		public int? VendorID { get; set; }
		#endregion
	}
}