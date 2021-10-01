using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.AP.InvoiceRecognition
{
	public class APInvoiceRecognitionEntryVisibilityRestriction : PXGraphExtension<APInvoiceRecognitionEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(branchID: typeof(AccessInfo.branchID), ResetVendor = true)]
		public virtual void APRecognizedInvoice_VendorID_CacheAttached(PXCache sender)
		{
		}
	}
}