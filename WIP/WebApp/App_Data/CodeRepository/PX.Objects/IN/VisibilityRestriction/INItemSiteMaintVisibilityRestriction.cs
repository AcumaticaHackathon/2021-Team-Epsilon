using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.IN
{
	public class INItemSiteMaintVisibilityRestriction : PXGraphExtension<INItemSiteMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public virtual void INItemSite_PreferredVendorID_CacheAttached(PXCache sender)
		{
		}
	}
}