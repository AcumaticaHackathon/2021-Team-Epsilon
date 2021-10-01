using PX.Data;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.TX
{
	public sealed class TaxAdjustmentVisibilityRestriction : PXCacheExtension<TaxAdjustment>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(branchID: typeof(TaxAdjustment.branchID))]
		public int? VendorID { get; set; }
		#endregion
	}
}