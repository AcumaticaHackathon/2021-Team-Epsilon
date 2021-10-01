using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	public sealed class ApproveBillsFilterVisibilityRestriction : PXCacheExtension<ApproveBillsFilter>
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

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public int? VendorID { get; set; }
		#endregion
	}
}