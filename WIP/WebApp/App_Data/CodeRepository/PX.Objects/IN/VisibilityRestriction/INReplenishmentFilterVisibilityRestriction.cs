using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.IN
{
	public sealed class INReplenishmentFilterVisibilityRestriction : PXCacheExtension<INReplenishmentFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region PreferredVendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public int? PreferredVendorID { get; set; }
		#endregion
	}
}
