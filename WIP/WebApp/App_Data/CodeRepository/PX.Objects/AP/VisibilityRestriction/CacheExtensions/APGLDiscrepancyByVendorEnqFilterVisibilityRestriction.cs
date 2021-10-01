using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace ReconciliationTools
{
	public sealed class APGLDiscrepancyByVendorEnqFilterVisibilityRestriction : PXCacheExtension<APGLDiscrepancyByVendorEnqFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(typeof(APGLDiscrepancyByVendorEnqFilter.branchID), ResetVendor = true)]
		public int? VendorID { get; set; }
		#endregion
	}
}