using PX.Data;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.AP.Standalone
{
	public sealed class APQuickCheckVisibilityRestriction : PXCacheExtension<APQuickCheck>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region BranchID
		[Branch(typeof(AccessInfo.branchID), IsDetail = false, TabOrder = 0)]
		[PXFormula(typeof(Switch<
				Case<Where<APQuickCheck.vendorLocationID, IsNotNull,
						And<Selector<APQuickCheck.vendorLocationID, Location.vBranchID>, IsNotNull>>,
					Selector<APQuickCheck.vendorLocationID, Location.vBranchID>,
				Case<Where<APQuickCheck.vendorID, IsNotNull,
						And<Not<Selector<APQuickCheck.vendorID, Vendor.vOrgBAccountID>, RestrictByBranch<Current2<APQuickCheck.branchID>>>>>,
					Null,
				Case<Where<Current2<APQuickCheck.branchID>, IsNotNull>,
					Current2<APQuickCheck.branchID>>>>,
				Current<AccessInfo.branchID>>))]
		public int? BranchID { get; set; }
		#endregion

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(branchID: typeof(APQuickCheck.branchID))]
		public int? VendorID { get; set; }
		#endregion
	}
}