using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.DR
{
	public sealed class SchedulesInqSchedulesFilterVisibilityRestriction : PXCacheExtension<SchedulesInq.SchedulesFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region BAccountID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictCustomerByBranch(typeof(BAccountR.cOrgBAccountID), branchID: typeof(SchedulesInq.SchedulesFilter.branchID),
			ResetCustomer = true)]
		[RestrictVendorByBranch(typeof(BAccountR.vOrgBAccountID), branchID: typeof(SchedulesInq.SchedulesFilter.branchID),
			ResetVendor = true)]
		public int? BAccountID { get; set; }
		#endregion
	}
}