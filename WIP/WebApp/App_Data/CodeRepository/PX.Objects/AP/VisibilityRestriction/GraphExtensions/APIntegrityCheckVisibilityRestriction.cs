using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	public class APIntegrityCheckVisibilityRestriction : PXGraphExtension<APIntegrityCheck>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}
		public override void Initialize()
		{
			base.Initialize();

			Base.APVendorList_ByVendorClassID.WhereAnd<Where<Vendor.vOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>();
		}

		public PXFilteredProcessing<
			Vendor, APIntegrityCheckFilter,
			Where<Vendor.vOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>,
				And<Match<Current<AccessInfo.userName>>>>>
			APVendorList;
	}

	public sealed class APIntegrityCheckFilterVisibilityRestriction : PXCacheExtension<APIntegrityCheckFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorClassID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorClassByUserBranches]
		public string VendorClassID { get; set; }
		#endregion
	}
}
