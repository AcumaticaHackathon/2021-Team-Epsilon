using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.RQ
{
	public sealed class RQRequisitionSelectionVisibilityRestriction : PXCacheExtension<RQRequisitionProcess.RQRequisitionSelection>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(branchID: typeof(AccessInfo.branchID))]
		public int? VendorID { get; set; }
		#endregion
	}
}