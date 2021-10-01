using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	public class APDiscountMaintVisibilityRestriction : PXGraphExtension<APDiscountMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[VendorRaw(typeof(Where<Vendor.vOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>),
				IsKey = true)]
		public virtual void Vendor_AcctCD_CacheAttached(PXCache sender)
		{
		}
	}
}