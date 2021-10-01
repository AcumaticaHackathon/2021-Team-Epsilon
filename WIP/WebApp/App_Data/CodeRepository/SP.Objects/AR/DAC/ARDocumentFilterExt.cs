using PX.Data;
using PX.Objects.AR;
using PX.Objects.CM;

namespace SP.Objects.AR
{
	public class ARDocumentFilterExt : PXCacheExtension<ARDocumentEnq.ARDocumentFilter>
	{
		#region ShowAllDocs
		public abstract class showAllDocs : PX.Data.IBqlField
		{
		}

		[PXDBBool()]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Show All Documents")]
		public virtual bool? ShowAllDocs { get; set; }
		#endregion
		#region CustomerBalance
		public abstract class spCustomerBalance : PX.Data.IBqlField
		{
		}

		[PXUIField(DisplayName = "Current Balance", Enabled = false)]
		public virtual string SPCustomerBalance { get; set; }
		#endregion
		#region CustomerDepositsBalance
		public abstract class spcustomerDepositsBalance : PX.Data.IBqlField
		{
		}

		[PXUIField(DisplayName = "Prepayments Balance", Enabled = false)]
		public virtual decimal? SPCustomerDepositsBalance { get; set; }
		#endregion
		#region OpenInvoiceAndCharge
		public abstract class openInvoiceAndCharge : PX.Data.IBqlField
		{
		}

		[PXBaseCury()]
		[PXUIField(DisplayName = "Outstanding Invoices and Memos", Enabled = false)]
		public virtual decimal? OpenInvoiceAndCharge { get; set; }
		#endregion
		#region CreditMemosandUnappliedPayment
		public abstract class creditMemosandUnappliedPayment : PX.Data.IBqlField
		{
		}

		[PXBaseCury()]
		[PXUIField(DisplayName = "Unapplied Payments", Enabled = false)]
		public virtual decimal? CreditMemosandUnappliedPayment { get; set; }
		#endregion
		#region NetBalance
		public abstract class netBalance : PX.Data.IBqlField
		{
		}

		[PXBaseCury()]
		[PXUIField(DisplayName = "Net Balance", Enabled = false)]
		public virtual decimal? NetBalance { get; set; }
		#endregion
		#region CreditLimit
		public abstract class creditLimit : PX.Data.IBqlField
		{
		}

		[PXBaseCury()]
		[PXUIField(DisplayName = "Credit Limit", Enabled = false)]
		public virtual decimal? CreditLimit { get; set; }
		#endregion
		#region AvailableCredit
		public abstract class availableCredit : PX.Data.IBqlField
		{
		}

		[PXBaseCury()]
		[PXUIField(DisplayName = "Available Credit", Enabled = false)]
		public virtual decimal? AvailableCredit { get; set; }
		#endregion
	}
}
