using PX.Data;
using PX.Objects.CS;
using System.Collections.Generic;

namespace PX.Objects.AP
{
    public class APPaymentEntryVisibilityRestriction : PXGraphExtension<APPaymentEntry>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
        }

        public delegate void CopyPasteGetScriptDelegate(bool isImportSimple, List<Api.Models.Command> script, List<Api.Models.Container> containers);

        [PXOverride]
        public void CopyPasteGetScript(bool isImportSimple, List<Api.Models.Command> script, List<Api.Models.Container> containers,
            CopyPasteGetScriptDelegate baseMethod)
        {
            // We need to process fields together that are related to the Branch and Customer for proper validation. For this:
            // 1) set the right order of the fields
            // 2) insert dependent fields after the BranchID field
            // 3) all fields must belong to the same view.

            string branchViewName = nameof(APPaymentEntry.CurrentDocument);
            string vendorViewName = nameof(APPaymentEntry.Document);

            (string name, string viewName) branch = (nameof(APPayment.BranchID), branchViewName);

            List<(string name, string viewName)> fieldList = new List<(string name, string viewName)>();
            fieldList.Add((nameof(APPayment.VendorID), vendorViewName));
            fieldList.Add((nameof(APPayment.VendorLocationID), vendorViewName));
            fieldList.Add((nameof(APPayment.CashAccountID), vendorViewName));

            Common.Utilities.SetDependentFieldsAfterBranch(script, branch, fieldList);
        }
    }
}