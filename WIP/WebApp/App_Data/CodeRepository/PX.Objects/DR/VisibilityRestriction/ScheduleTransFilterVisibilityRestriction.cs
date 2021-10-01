using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.DR
{
	public sealed class ScheduleTransFilterVisibilityRestriction : PXCacheExtension<ScheduleTransInq.ScheduleTransFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region BAccountID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByBranch(typeof(BAccountR.cOrgBAccountID), branchID: typeof(ScheduleTransInq.ScheduleTransFilter.branchID),
			ResetCustomer = true)]
		[RestrictVendorByBranch(typeof(BAccountR.vOrgBAccountID), branchID: typeof(ScheduleTransInq.ScheduleTransFilter.branchID),
			ResetVendor = true)]
		public int? BAccountID { get; set; }
		#endregion
	}
}