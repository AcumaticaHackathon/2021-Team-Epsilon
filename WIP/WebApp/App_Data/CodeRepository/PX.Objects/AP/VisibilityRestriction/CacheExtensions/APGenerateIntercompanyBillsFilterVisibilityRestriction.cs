using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AP.VisibilityRestriction
{
	public sealed class APGenerateIntercompanyBillsFilterVisibilityRestriction : PXCacheExtension<APGenerateIntercompanyBillsFilter>
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