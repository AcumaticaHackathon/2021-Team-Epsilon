using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.DR
{
	public sealed class DRDraftScheduleProcSchedulesFilterVisibilityRestriction : PXCacheExtension<DRDraftScheduleProc.SchedulesFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region BAccountID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByBranch(typeof(BAccountR.cOrgBAccountID), branchID: typeof(DRDraftScheduleProc.SchedulesFilter.branchID),
			ResetCustomer = true)]
		[RestrictVendorByBranch(typeof(BAccountR.vOrgBAccountID), branchID: typeof(DRDraftScheduleProc.SchedulesFilter.branchID),
			ResetVendor = true)]
		public int? BAccountID { get; set; }
		#endregion
	}
}