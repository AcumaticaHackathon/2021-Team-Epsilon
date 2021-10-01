using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	public sealed class APDocumentFilterVisibilityRestriction : PXCacheExtension<APDocumentEnq.APDocumentFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByOrganization(orgBAccountID: typeof(APDocumentEnq.APDocumentFilter.orgBAccountID))]
		public int? VendorID { get; set; }
		#endregion
	}
}