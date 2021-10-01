using PX.Data;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.CN.JointChecks.AP.DAC
{
	public sealed class JointPayeeVisibilityRestriction : PXCacheExtension<JointPayee>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region JointPayeeInternalId
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByUserBranches(typeof(BAccountR.vOrgBAccountID))]
		public int? JointPayeeInternalId { get; set; }
		#endregion
	}
}