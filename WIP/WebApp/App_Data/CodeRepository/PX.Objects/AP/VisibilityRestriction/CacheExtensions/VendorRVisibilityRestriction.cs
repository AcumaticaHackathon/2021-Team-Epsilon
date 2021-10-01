using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	public sealed class VendorRVisibilityRestriction : PXCacheExtension<VendorR>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region AcctCD
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[VendorRaw(typeof(
				Where2<
					Where<Vendor.type, Equal<BAccountType.vendorType>,
						Or<Vendor.type, Equal<BAccountType.combinedType>>>,
					And<Where<Vendor.vOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>>),
				DescriptionField = typeof(Vendor.acctName),
				IsKey = true, DisplayName = "Vendor ID")]
		public string AcctCD { get; set; }
		#endregion
	}
}
