using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.AP
{
	public sealed class APPaymentVisibilityRestriction : PXCacheExtension<APPayment>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region BranchID
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[Branch(typeof(AccessInfo.branchID), IsDetail = false, TabOrder = 0)]
		[PXFormula(typeof(Switch<
				Case<Where<APPayment.vendorLocationID, IsNotNull,
						And<Selector<APPayment.vendorLocationID, Location.vBranchID>, IsNotNull>>,
					Selector<APPayment.vendorLocationID, Location.vBranchID>,
				Case<Where<APPayment.vendorID, IsNotNull,
						And<Not<Selector<APPayment.vendorID, Vendor.vOrgBAccountID>, RestrictByBranch<Current2<APPayment.branchID>>>>>,
					Null,
				Case<Where<Current2<APPayment.branchID>, IsNotNull>,
					Current2<APPayment.branchID>>>>,
				Current<AccessInfo.branchID>>))]
		public int? BranchID { get; set; }
		#endregion

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(branchID: typeof(APPayment.branchID))]
		public int? VendorID { get; set; }
		#endregion
	}
}