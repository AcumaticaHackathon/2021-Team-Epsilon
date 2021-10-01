using PX.Data;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.TX
{
	public sealed class TaxPeriodFilterVisibilityRestriction : PXCacheExtension<TaxPeriodFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByOrganization(orgBAccountID: typeof(TaxPeriodFilter.orgBAccountID))]
		public int? VendorID { get; set; }
		#endregion
	}
}