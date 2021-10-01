using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;

namespace PX.Objects.PO
{
    public sealed class POReceiptVisibilityRestriction : PXCacheExtension<POReceipt>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		#region VendorID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(source: typeof(Vendor.vOrgBAccountID),
			WhereType: typeof(Or<Current<POReceipt.receiptType>, Equal<POReceiptType.transferreceipt>>),
			branchID: typeof(POReceipt.branchID),
			ResetVendor = false)]
		public int? VendorID { get; set; }
		#endregion
	}
}