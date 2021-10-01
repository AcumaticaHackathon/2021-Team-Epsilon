using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.FA
{
	public class AssetMaintVisibilityRestriction : PXGraphExtension<AssetMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches]
		public virtual void FADetails_LessorID_CacheAttached(PXCache sender)
		{
		}
	}
}
