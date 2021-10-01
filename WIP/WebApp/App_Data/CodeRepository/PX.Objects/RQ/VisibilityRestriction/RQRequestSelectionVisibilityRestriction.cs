using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.RQ
{
	public sealed class RQRequestSelectionVisibilityRestriction : PXCacheExtension<RQRequestSelection>
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
