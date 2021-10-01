using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.CN.Compliance.CL.DAC
{
	public sealed class ProcessLienWaiversFilterVisibilityRestriction : PXCacheExtension<ProcessLienWaiversFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorId
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public int? VendorId { get; set; }
		#endregion
	}
}