using PX.Data;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.TX
{
	public sealed class TaxHistoryMasterVisibilityRestriction : PXCacheExtension<TaxHistoryMaster>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByOrganization(orgBAccountID: typeof(TaxHistoryMaster.orgBAccountID))]
		public int? VendorID { get; set; }
		#endregion
	}
}