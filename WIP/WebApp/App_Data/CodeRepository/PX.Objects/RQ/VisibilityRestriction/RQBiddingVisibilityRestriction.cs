using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.RQ
{
	public sealed class RQBiddingVisibilityRestriction : PXCacheExtension<RQBidding>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public int? VendorID { get; set; }
		#endregion
	}

	public sealed class RQBiddingVendorVisibilityRestriction : PXCacheExtension<RQBiddingVendor>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public int? VendorID { get; set; }
		#endregion
	}
}