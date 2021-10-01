using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.TX
{
	public sealed class TaxVisibilityRestriction : PXCacheExtension<Tax>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region TaxVendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public int? TaxVendorID { get; set; }
		#endregion
	}
}