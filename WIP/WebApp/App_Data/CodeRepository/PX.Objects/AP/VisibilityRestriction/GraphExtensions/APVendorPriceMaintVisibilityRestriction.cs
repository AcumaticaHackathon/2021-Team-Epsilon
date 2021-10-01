using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	public class APVendorPriceMaintVisibilityRestriction : PXGraphExtension<APVendorPriceMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}
		public override void Initialize()
		{
			base.Initialize();

			Base.Records.WhereAnd<Where<Vendor.vOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>();
		}
	}

	public sealed class APVendorPriceFilterVisibilityRestriction : PXCacheExtension<APVendorPriceFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public int? VendorID { get; set; }
		#endregion
	}
}
