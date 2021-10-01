using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	public sealed class APRetainageFilterVisibilityRestriction : PXCacheExtension<APRetainageFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(branchID: typeof(APRetainageFilter.branchID), ResetVendor = true)]
		public int? VendorID { get; set; }
		#endregion
	}
}