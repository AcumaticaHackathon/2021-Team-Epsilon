using PX.Common;
using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PO
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Method, AllowMultiple = false)]
	public class POReceiptEntryReportsAttribute : PXStringListAttribute
	{
		public static class Values
		{
			public const string PurchaseReceipt = "PO646000";
			public const string BillingDetails = "PO632000";
			public const string Allocated = "PO622000";
		}

		[PXLocalizable]
		public static class DisplayNames
		{
			public const string PurchaseReceipt = "Print Purchase Receipt"; //from AUStepAction instead of "Purchase Receipt";
			public const string BillingDetails = "View Purchase Receipts Billing History"; //from AUStepAction instead of "Purchase Receipt Billing Details"(Messages.ReportPOReceiptBillingDetails)
			public const string Allocated = Messages.ReportPOReceipAllocated;
		}

		private static Tuple<string, string>[] ValuesToLabels => new Tuple<string, string>[]
		{
			Pair(Values.PurchaseReceipt, DisplayNames.PurchaseReceipt),
			Pair(Values.BillingDetails, DisplayNames.BillingDetails),
			Pair(Values.Allocated, DisplayNames.Allocated),
		};

		public POReceiptEntryReportsAttribute() : base(ValuesToLabels)
		{ }
	}
}
