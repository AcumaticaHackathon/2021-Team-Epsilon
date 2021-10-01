using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	public sealed class AP1099YearMasterVisibilityRestriction : PXCacheExtension<AP1099YearMaster>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByOrganization(orgBAccountID: typeof(AP1099YearMaster.orgBAccountID))]
		public int? VendorID { get; set; }
		#endregion
	}
}