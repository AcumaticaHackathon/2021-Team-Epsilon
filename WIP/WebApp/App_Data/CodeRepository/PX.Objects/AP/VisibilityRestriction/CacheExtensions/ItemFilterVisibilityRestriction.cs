using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	public sealed class ItemFilterVisibilityRestriction : PXCacheExtension<APUpdateDiscounts.ItemFilter>
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
