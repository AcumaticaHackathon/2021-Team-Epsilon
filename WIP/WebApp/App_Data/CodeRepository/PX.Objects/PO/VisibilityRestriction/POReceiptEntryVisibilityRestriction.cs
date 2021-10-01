using PX.Data;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;

namespace PX.Objects.PO
{
	public class POReceiptEntryVisibilityRestriction : PXGraphExtension<POReceiptEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		[Branch(typeof(AccessInfo.branchID), IsDetail = false, TabOrder = 0)]
		[PXFormula(typeof(Switch<
			Case<Where<POReceipt.receiptType, Equal<POReceiptType.transferreceipt>>,
				Selector<POReceipt.siteID, Selector<INSite.branchID, Branch.branchCD>>,
			Case<Where<POReceipt.vendorLocationID, IsNotNull,
					And<Selector<POReceipt.vendorLocationID, Location.vBranchID>, IsNotNull>>,
				Selector<POReceipt.vendorLocationID, Selector<Location.vBranchID, Branch.branchCD>>,
			Case<Where<POReceipt.vendorID, IsNotNull,
					And<Not<Selector<POReceipt.vendorID, Vendor.vOrgBAccountID>, RestrictByBranch<Current2<POReceipt.branchID>>>>>,
				Null,
			Case<Where<Current2<POReceipt.branchID>, IsNotNull>,
				Current2<POReceipt.branchID>>>>>,
			Current<AccessInfo.branchID>>))]
		public virtual void POReceipt_BranchID_CacheAttached(PXCache sender)
		{
		}
	}
}
