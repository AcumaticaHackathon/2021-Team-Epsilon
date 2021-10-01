using PX.Data;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.TX
{
	public sealed class TaxYearFilterVisibilityRestriction : PXCacheExtension<TaxYearMaint.TaxYearFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByCompany(organizationID: typeof(TaxYearMaint.TaxYearFilter.organizationID))]
		public int? VendorID { get; set; }
		#endregion
	}
}