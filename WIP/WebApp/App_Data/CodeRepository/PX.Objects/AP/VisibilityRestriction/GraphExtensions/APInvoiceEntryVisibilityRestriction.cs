using PX.Data;
using PX.Objects.CS;
using System.Collections.Generic;

namespace PX.Objects.AP
{
	public class APInvoiceEntryVisibilityRestriction : PXGraphExtension<APInvoiceEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[RestrictVendorByBranch(branchID: typeof(APInvoice.branchID))]
		protected virtual void APInvoice_VendorID_CacheAttached(PXCache sender) { }
		
		public delegate void CopyPasteGetScriptDelegate(bool isImportSimple, List<Api.Models.Command> script, List<Api.Models.Container> containers);

		[PXOverride]
		public void CopyPasteGetScript(bool isImportSimple, List<Api.Models.Command> script, List<Api.Models.Container> containers,
			CopyPasteGetScriptDelegate baseMethod)
		{
			// We need to process fields together that are related to the Branch and Customer for proper validation. For this:
			// 1) set the right order of the fields
			// 2) insert dependent fields after the BranchID field
			// 3) all fields must belong to the same view.

			string branchViewName = nameof(APInvoiceEntry.CurrentDocument);
			string vendorViewName = nameof(APInvoiceEntry.Document);

			(string name, string viewName) branch = (nameof(APInvoice.BranchID), branchViewName);

			List<(string name, string viewName)> fieldList = new List<(string name, string viewName)>();
			fieldList.Add((nameof(APInvoice.VendorID), vendorViewName));
			fieldList.Add((nameof(APInvoice.VendorLocationID), vendorViewName));

			Common.Utilities.SetDependentFieldsAfterBranch(script, branch, fieldList);
		}
	}
}
