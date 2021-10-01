using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	public sealed class APHistoryFilterVisibilityRestriction: PXCacheExtension<APVendorBalanceEnq.APHistoryFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorClassID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorClassByUserBranches]
		public string VendorClassID { get; set; }
		#endregion
	}
}
