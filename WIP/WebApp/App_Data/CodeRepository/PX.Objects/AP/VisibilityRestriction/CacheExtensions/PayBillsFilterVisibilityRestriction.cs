using PX.Data;
using PX.Objects.CS;
using System;

namespace PX.Objects.AP
{
	public sealed class PayBillsFilterVisibilityRestriction : PXCacheExtension<PayBillsFilter>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(typeof(PayBillsFilter.branchID), ResetVendor = true)]
		public int? VendorID { get; set; }
		#endregion

		#region VendorClassID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorClassByUserBranches]
		public String VendorClassID { get; set; }
		#endregion
	}
}
