using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.PO
{
	public sealed class VendorLocationVisibilityRestriction : PXCacheExtension<VendorLocation>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region BAccountID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public int? BAccountID { get; set; }
		#endregion
	}
}