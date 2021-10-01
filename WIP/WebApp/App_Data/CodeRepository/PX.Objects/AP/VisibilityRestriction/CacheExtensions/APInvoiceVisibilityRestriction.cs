using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CR;

namespace PX.Objects.AP
{
	public sealed class APInvoiceVisibilityRestriction : PXCacheExtension<APInvoice>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region BranchID
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[Branch(typeof(AccessInfo.branchID), IsDetail = false, TabOrder = 0)]
		[PXFormula(typeof(Switch<
				Case<Where<APInvoice.vendorLocationID, IsNotNull,
						And<Selector<APInvoice.vendorLocationID, Location.vBranchID>, IsNotNull>>,
					Selector<APInvoice.vendorLocationID, Location.vBranchID>,
				Case<Where<APInvoice.vendorID, IsNotNull,
						And<Not<Selector<APInvoice.vendorID, Vendor.vOrgBAccountID>, RestrictByBranch<Current2<APInvoice.branchID>>>>>,
					Null,
				Case<Where<Current2<APInvoice.branchID>, IsNotNull>,
					Current2<APInvoice.branchID>>>>,
				Current<AccessInfo.branchID>>))]
		public int? BranchID { get; set; }
		#endregion

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(branchID: typeof(APInvoice.branchID))]
		public int? VendorID { get; set; }
		#endregion
	}
}