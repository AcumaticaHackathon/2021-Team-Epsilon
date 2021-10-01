using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	public sealed class APPPDDebitAdjParametersVisibilityRestriction : PXCacheExtension<APPPDDebitAdjParameters>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(branchID: typeof(APPPDDebitAdjParameters.branchID), ResetVendor = true)]
		public int? VendorID { get; set; }
		#endregion
	}
}