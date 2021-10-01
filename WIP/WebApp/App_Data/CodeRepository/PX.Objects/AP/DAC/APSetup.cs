using System;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.Common;
using PX.Objects.AR;
using PX.Objects.EP.Standalone;

namespace PX.Objects.AP
{
    /// <summary>
    /// Allows to configure the Accounts Payable module. 
    /// Some of the settings, such as numbering sequences, cannot be changed once the module is in use.
    /// </summary>
	[System.SerializableAttribute()]
    [PXPrimaryGraph(typeof(APSetupMaint))]
    [PXCacheName(Messages.APSetup)]
	public partial class APSetup : IBqlTable
	{
		#region Keys
		public static class FK
		{
			public class DefaultVendorClass : AP.VendorClass.PK.ForeignKeyOf<APSetup>.By<dfltVendorClassID> { }
			public class BatchNumberingSequence : CS.Numbering.PK.ForeignKeyOf<APSetup>.By<batchNumberingID> { }
			public class InvoiceNumberingSequence : CS.Numbering.PK.ForeignKeyOf<APSetup>.By<invoiceNumberingID> { }
			public class DebitAdjustmentNumberingSequence : CS.Numbering.PK.ForeignKeyOf<APSetup>.By<debitAdjNumberingID> { }
			public class CreditAdjustmentNumberingSequence : CS.Numbering.PK.ForeignKeyOf<APSetup>.By<creditAdjNumberingID> { }
			public class PaymentNumberingSequence : CS.Numbering.PK.ForeignKeyOf<APSetup>.By<checkNumberingID> { }
			public class PriceWorksheetNumberingSequence : CS.Numbering.PK.ForeignKeyOf<APSetup>.By<priceWSNumberingID> { }
		}
		#endregion

		#region BatchNumberingID
		public abstract class batchNumberingID : PX.Data.BQL.BqlString.Field<batchNumberingID> { }
		protected String _BatchNumberingID;

        /// <summary>
        /// The numbering sequence used for batches generated by the Accounts Payable module.
        /// </summary>
        /// <value>
        /// This field is a link to a <see cref="Numbering"/> record.
        /// </value>
		[PXDBString(10, IsUnicode = true)]
		[PXDefault("BATCH")]
		[PXSelector(typeof(Numbering.numberingID), DescriptionField = typeof(Numbering.descr))]
		[PXUIField(DisplayName = "Batch Numbering Sequence", Visibility = PXUIVisibility.Visible)]
		public virtual String BatchNumberingID
		{
			get
			{
				return this._BatchNumberingID;
			}
			set
			{
				this._BatchNumberingID = value;
			}
		}
		#endregion
		#region DfltVendorClassID
		public abstract class dfltVendorClassID : PX.Data.BQL.BqlString.Field<dfltVendorClassID> { }
		protected String _DfltVendorClassID;

        /// <summary>
        /// The vendor class to be used as the default vendor class. 
        /// When a new vendor class is created using the Vendor Classes (AP.20.10.00) form,
        /// the settings defined for the class specified here will be inserted into the appropriate boxes.
        /// </summary>
        /// <value>
        /// Refers the <see cref="VendorClass"/> DAC.
        /// </value>
		[PXDBString(10, IsUnicode = true)]
		[PXSelector(typeof(Search2<
			VendorClass.vendorClassID, 
				LeftJoin<EPEmployeeClass, 
					On<EPEmployeeClass.vendorClassID, Equal<VendorClass.vendorClassID>>>, 
			Where<
				EPEmployeeClass.vendorClassID, IsNull>>))]
		[PXUIField(DisplayName = "Default Vendor Class ID", Visibility=PXUIVisibility.Visible)]
		public virtual String DfltVendorClassID
		{
			get
			{
				return this._DfltVendorClassID;
			}
			set
			{
				this._DfltVendorClassID = value;
			}
		}
		#endregion
		#region PerRetainTran
		public abstract class perRetainTran : PX.Data.BQL.BqlShort.Field<perRetainTran> { }
		protected Int16? _PerRetainTran;

        /// <summary>
        /// Defines the number of periods, during which the batches generated by the Accounts Payable module will be stored in the database.
        /// Increasing this value later, after some period of use, is not recommended, 
        /// because the history deleted from the database cannot be restored and some reports will be incomplete.
        /// </summary>
		[PXDBShort()]
		[PXDefault((short)99)]
		[PXUIField(DisplayName = "Keep Transactions for", Visibility = PXUIVisibility.Visible)]
		public virtual Int16? PerRetainTran
		{
			get
			{
				return this._PerRetainTran;
			}
			set
			{
				this._PerRetainTran = value;
			}
		}
		#endregion
		#region PerRetainHist
		public abstract class perRetainHist : PX.Data.BQL.BqlShort.Field<perRetainHist> { }
		protected Int16? _PerRetainHist;

        /// <summary>
        /// Defines the number of periods, during which the Accounts Payable module history will be stored in the database.
        /// Increasing this value later, after some period of use, is not recommended, 
        /// because the history deleted from the database cannot be restored and some reports will be incomplete.
        /// </summary>
		[PXDBShort()]
		[PXDefault((short)0)]
		[PXUIField(DisplayName = "Periods to Retain History", Visibility = PXUIVisibility.Invisible)]
		public virtual Int16? PerRetainHist
		{
			get
			{
				return this._PerRetainHist;
			}
			set
			{
				this._PerRetainHist = value;
			}
		}
		#endregion
		#region InvoiceNumberingID
		public abstract class invoiceNumberingID : PX.Data.BQL.BqlString.Field<invoiceNumberingID> { }
		protected String _InvoiceNumberingID;

        /// <summary>
        /// The numbering sequence used for Accounts Payable bills.
        /// </summary>
        /// <value>
        /// This field is a link to a <see cref="Numbering"/> record.
        /// </value>
		[PXDBString(10, IsUnicode = true)]
		[PXDefault("APBILL")]
		[PXSelector(typeof(Numbering.numberingID), DescriptionField = typeof(Numbering.descr))]
		[PXUIField(DisplayName = "Bill Numbering Sequence", Visibility = PXUIVisibility.Visible)]
		public virtual String InvoiceNumberingID
		{
			get
			{
				return this._InvoiceNumberingID;
			}
			set
			{
				this._InvoiceNumberingID = value;
			}
		}
		#endregion
		#region PastDue00
		public abstract class pastDue00 : PX.Data.BQL.BqlShort.Field<pastDue00> { }
		protected Int16? _PastDue00;

        /// <summary>
        /// The maximum number of days outstanding or past due for the document to be included in the first category.
        /// </summary>
		[PXDBShort()]
		[PXDefault((short)0)]
		[PXUIField(DisplayName = "Aging Period 1", Visibility = PXUIVisibility.Visible)]
		public virtual Int16? PastDue00
		{
			get
			{
				return this._PastDue00;
			}
			set
			{
				this._PastDue00 = value;
			}
		}
		#endregion
		#region PastDue01
		public abstract class pastDue01 : PX.Data.BQL.BqlShort.Field<pastDue01> { }
		protected Int16? _PastDue01;

        /// <summary>
        /// The maximum number of days outstanding or past due for the document to be included in the second category. 
        /// If the value here is greater than the PastDue00 value, documents from the first category are not included in the second category.
        /// </summary>
		[PXDBShort()]
		[PXDefault((short)0)]
		[PXUIField(DisplayName = "Aging Period 2", Visibility = PXUIVisibility.Visible)]
		public virtual Int16? PastDue01
		{
			get
			{
				return this._PastDue01;
			}
			set
			{
				this._PastDue01 = value;
			}
		}
		#endregion
		#region PastDue02
		public abstract class pastDue02 : PX.Data.BQL.BqlShort.Field<pastDue02> { }
		protected Int16? _PastDue02;

        /// <summary>
        /// The maximum number of days outstanding or past due for the document to be included in the third category. 
        /// If the value here is greater than the PastDue01 value, documents from the second category are not included in the third category.
        /// </summary>
		[PXDBShort()]
		[PXDefault((short)0)]
		[PXUIField(DisplayName = "Aging Period 3", Visibility = PXUIVisibility.Visible)]
		public virtual Int16? PastDue02
		{
			get
			{
				return this._PastDue02;
			}
			set
			{
				this._PastDue02 = value;
			}
		}
		#endregion
		#region CheckNumberingID
		public abstract class checkNumberingID : PX.Data.BQL.BqlString.Field<checkNumberingID> { }
		protected String _CheckNumberingID;

        /// <summary>
        /// The numbering sequence used for Accounts Payable payments.
        /// </summary>
        /// <value>
        /// This field is a link to a <see cref="Numbering"/> record.
        /// </value>
		[PXDBString(10, IsUnicode = true)]
		[PXDefault("APPAYMENT")]
		[PXSelector(typeof(Numbering.numberingID), DescriptionField = typeof(Numbering.descr))]
		[PXUIField(DisplayName = "Payment Numbering Sequence", Visibility = PXUIVisibility.Visible)]
		public virtual String CheckNumberingID
		{
			get
			{
				return this._CheckNumberingID;
			}
			set
			{
				this._CheckNumberingID = value;
			}
		}
		#endregion
        #region CreditAdjNumberingID
        public abstract class creditAdjNumberingID : PX.Data.BQL.BqlString.Field<creditAdjNumberingID> { }
        protected String _CreditAdjNumberingID;

        /// <summary>
        /// The numbering sequence used for credit adjustments.
        /// </summary>
        /// <value>
        /// This field is a link to a <see cref="Numbering"/> record.
        /// </value>
        [PXDBString(10, IsUnicode = true)]
        [PXDefault("APBILL")]
        [PXSelector(typeof(Numbering.numberingID), DescriptionField = typeof(Numbering.descr))]
        [PXUIField(DisplayName = "Credit Adjustment Numbering Sequence", Visibility = PXUIVisibility.Visible)]
        public virtual String CreditAdjNumberingID
        {
            get
            {
                return this._CreditAdjNumberingID;
            }
            set
            {
                this._CreditAdjNumberingID = value;
            }
        }
        #endregion
        #region DebitAdjNumberingID
        public abstract class debitAdjNumberingID : PX.Data.BQL.BqlString.Field<debitAdjNumberingID> { }
        protected String _DebitAdjNumberingID;

        /// <summary>
        /// The numbering sequence used for debit adjustments.
        /// </summary>
        /// <value>
        /// This field is a link to a <see cref="Numbering"/> record.
        /// </value>
        [PXDBString(10, IsUnicode = true)]
        [PXDefault("APBILL")]
        [PXSelector(typeof(Numbering.numberingID), DescriptionField = typeof(Numbering.descr))]
        [PXUIField(DisplayName = "Debit Adjustment Numbering Sequence", Visibility = PXUIVisibility.Visible)]
        public virtual String DebitAdjNumberingID
        {
            get
            {
                return this._DebitAdjNumberingID;
            }
            set
            {
                this._DebitAdjNumberingID = value;
            }
        }
        #endregion
        #region PriceWSNumberingID
        public abstract class priceWSNumberingID : PX.Data.BQL.BqlString.Field<priceWSNumberingID> { }
        protected String _PriceWSNumberingID;
        [PXDBString(10, IsUnicode = true)]
        [PXDefault("APPRICEWS")]
        [PXSelector(typeof(Numbering.numberingID), DescriptionField = typeof(Numbering.descr))]
        [PXUIField(DisplayName = "Price Worksheet Numbering Sequence", Visibility = PXUIVisibility.Visible)]
        public virtual String PriceWSNumberingID
        {
            get
            {
                return this._PriceWSNumberingID;
            }
            set
            {
                this._PriceWSNumberingID = value;
            }
        }
        #endregion
		#region DefaultTranDesc
		public abstract class defaultTranDesc : PX.Data.BQL.BqlString.Field<defaultTranDesc> { }
		protected String _DefaultTranDesc;

        /// <summary>
        /// Default way to populate description for new transactions.
        /// </summary>
        /// <value>
        /// "C" - Combination of Vendor ID and Name,
        /// "I" - Vendor ID,
        /// "N" - Vendor Name,
        /// "U" - description entered by user.
        /// </value>
		[PXDBString(1, IsFixed = true)]
		[PXDefault("C")]
		[PXStringList(new string[] {"C", "I", "N", "U"}, new string[] {"Combination ID and Name", "Vendor ID", "Vendor Name", "User Entered Description"})]
		[PXUIField(DisplayName = "Default Transaction Description", Visibility = PXUIVisibility.Invisible)]
		public virtual String DefaultTranDesc
		{
			get
			{
				return this._DefaultTranDesc;
			}
			set
			{
				this._DefaultTranDesc = value;
			}
		}
		#endregion
		#region ExpenseSubMask
		public abstract class expenseSubMask : PX.Data.BQL.BqlString.Field<expenseSubMask> { }
		protected String _ExpenseSubMask;

        /// <summary>
        /// The subaccount mask that defines the rule of choosing segment values for the expense subaccount 
        /// to be used for non-stock items on data entry forms in the Accounts Payable module.
        /// To set up the rule, select a segment, press F3, and choose a source of the segment value, which is one of the following options:
        /// </summary>
        /// <value>
        /// The mask may include the following characters:
        /// C: Expense subaccount associated with branch
        /// E: Subaccount associated with employee
        /// I: Subaccount associated with non-stock item
        /// L: Subaccount associated with vendor location
        /// For a segment, the characters designating each option are repeated as many times as there are characters in the segment.
        /// </value>
		[PXDefault()]
		[SubAccountMask(DisplayName = "Combine Expense Sub. From")]
		public virtual String ExpenseSubMask
		{
			get
			{
				return this._ExpenseSubMask;
			}
			set
			{
				this._ExpenseSubMask = value;
			}
		}
		#endregion
		#region AutoPost
		public abstract class autoPost : PX.Data.BQL.BqlBool.Field<autoPost> { }
		protected bool? _AutoPost;

        /// <summary>
        /// If set to <c>true</c>, indicates that transactions will be automatically posted to the General Ledger once they are released in the Accounts Payable module.
        /// </summary>
		[PXDBBool()]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Automatically Post on Release", Visibility = PXUIVisibility.Visible)]
		public virtual bool? AutoPost
		{
			get
			{
				return this._AutoPost;
			}
			set
			{
				this._AutoPost = value;
			}
		}
		#endregion
		#region TransactionPosting
		public abstract class transactionPosting : PX.Data.BQL.BqlString.Field<transactionPosting> { }
		protected String _TransactionPosting;

        /// <summary>
        /// Indicates whether to post transactions to GL as summary or in detailed mode.
        /// Is set by the <see cref="SummaryPost"/> field.
        /// </summary>
        /// <value>
        /// <c>"S"</c> - Summary, <c>"D"</c> - Detail. Defaults to Detail.
        /// </value>
		[PXDBString(1, IsFixed = true)]
		[PXDefault("D")]
		[PXUIField(DisplayName = "Transaction Posting", Visibility = PXUIVisibility.Invisible)]
		[PXStringList(new string[] {"S","D"}, new string[] {"Summary", "Detail"})]
		public virtual String TransactionPosting
		{
			get
			{
				return this._TransactionPosting;
			}
			set
			{
				this._TransactionPosting = value;
			}
		}
		#endregion
		#region ValidateDataConsistencyOnRelease
		[Obsolete("This field is not used anymore and will be removed in 2018R2. Use " + nameof(DataInconsistencyHandlingMode) + "instead.")]
		public abstract class validateDataConsistencyOnRelease : PX.Data.BQL.BqlBool.Field<validateDataConsistencyOnRelease> { }
		[Obsolete("This field is not used anymore and will be removed in 2018R2. Use " + nameof(DataInconsistencyHandlingMode) + "instead.")]
		public virtual bool? ValidateDataConsistencyOnRelease
		{
			get;
			set;
		}
		#endregion
		#region DataInconsistencyHandlingMode
		public abstract class dataInconsistencyHandlingMode : PX.Data.BQL.BqlString.Field<dataInconsistencyHandlingMode> { }
		[PXDBString(1)]
		[PXDefault(Common.DataIntegrity.InconsistencyHandlingMode.Log)]
		[PXUIField(DisplayName = Common.Messages.ExtraDataIntegrityValidation)]
		[LabelList(typeof(Common.DataIntegrity.InconsistencyHandlingMode))]
		public virtual string DataInconsistencyHandlingMode
		{
			get;
			set;
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		protected Byte[] _tstamp;
		[PXDBTimestamp()]
		public virtual Byte[] tstamp
		{
			get
			{
				return this._tstamp;
			}
			set
			{
				this._tstamp = value;
			}
		}
		#endregion
		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		protected Guid? _CreatedByID;
		[PXDBCreatedByID()]
		public virtual Guid? CreatedByID
		{
			get
			{
				return this._CreatedByID;
			}
			set
			{
				this._CreatedByID = value;
			}
		}
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		protected String _CreatedByScreenID;
		[PXDBCreatedByScreenID()]
		public virtual String CreatedByScreenID
		{
			get
			{
				return this._CreatedByScreenID;
			}
			set
			{
				this._CreatedByScreenID = value;
			}
		}
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		protected DateTime? _CreatedDateTime;
		[PXDBCreatedDateTime()]
		public virtual DateTime? CreatedDateTime
		{
			get
			{
				return this._CreatedDateTime;
			}
			set
			{
				this._CreatedDateTime = value;
			}
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		protected Guid? _LastModifiedByID;
		[PXDBLastModifiedByID()]
		public virtual Guid? LastModifiedByID
		{
			get
			{
				return this._LastModifiedByID;
			}
			set
			{
				this._LastModifiedByID = value;
			}
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		protected String _LastModifiedByScreenID;
		[PXDBLastModifiedByScreenID()]
		public virtual String LastModifiedByScreenID
		{
			get
			{
				return this._LastModifiedByScreenID;
			}
			set
			{
				this._LastModifiedByScreenID = value;
			}
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		protected DateTime? _LastModifiedDateTime;
		[PXDBLastModifiedDateTime()]
		public virtual DateTime? LastModifiedDateTime
		{
			get
			{
				return this._LastModifiedDateTime;
			}
			set
			{
				this._LastModifiedDateTime = value;
			}
		}
		#endregion
		#region SummaryPost
		public abstract class summaryPost : PX.Data.BQL.BqlBool.Field<summaryPost> { }

        /// <summary>
        /// Indicates whether to post transactions to GL as summary or in detailed mode.
        /// This field depends on and changes <see cref="TransactionPosting"/> field when set.
        /// </summary>
        /// <value>
        /// <c>true</c> indicates that summary should be posted
        /// (corresponds to <c>"S"</c> value of the <see cref="TransactionPosting"/> field.)
        /// </value>
		[PXBool()]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Post Summary on Updating GL", Visibility = PXUIVisibility.Visible)]
		public virtual bool? SummaryPost
		{
			get
			{
				return (this._TransactionPosting == "S");
			}
			set
			{
				this._TransactionPosting = (value == true) ? "S" : "D";
			}
		}
		#endregion
		#region RequireApprovePayments
		public abstract class requireApprovePayments : PX.Data.BQL.BqlBool.Field<requireApprovePayments> { }
		protected bool? _RequireApprovePayments;

        /// <summary>
        /// If set to <c>true</c>, indicates that approval of bills is required before bills may be paid. 
        /// Bills can be approved using the Approve Bills for Payment (AP.50.20.00) form.
        /// </summary>
		[PXDBBool()]
		[PXDefault(true)]
        [PXUIField(DisplayName = "Require Approval of Bills Prior to Payment", Visibility = PXUIVisibility.Visible)]
		public virtual bool? RequireApprovePayments
		{
			get
			{
				return this._RequireApprovePayments;
			}
			set
			{
				this._RequireApprovePayments = value;
			}
		}
		#endregion
		#region RequireControlTotal
		public abstract class requireControlTotal : PX.Data.BQL.BqlBool.Field<requireControlTotal> { }
		protected Boolean? _RequireControlTotal;

        /// <summary>
        /// If set to <c>true</c>, adds the Amount box to the Summary area of the Bills and Adjustments (AP.30.10.00) form.
        /// To save a document in the Balanced status, the user must enter the document total in this box after reviewing the document.
        /// </summary>
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Validate Document Totals on Entry")]
		public virtual Boolean? RequireControlTotal
		{
			get
			{
				return this._RequireControlTotal;
			}
			set
			{
				this._RequireControlTotal = value;
			}
		}
		#endregion
		#region RequireControlTaxTotal
		public abstract class requireControlTaxTotal : PX.Data.BQL.BqlBool.Field<requireControlTaxTotal> { }
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Validate Tax Totals on Entry")]
		public virtual bool? RequireControlTaxTotal { get; set; }
		#endregion
		#region HoldEntry
		public abstract class holdEntry : PX.Data.BQL.BqlBool.Field<holdEntry> { }
		protected Boolean? _HoldEntry;

        /// <summary>
        /// If set to <c>true</c>, new documents will have the On Hold status by default, which prevents them from being released.
        /// </summary>
		[PXDBBool()]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Hold Documents on Entry")]
		public virtual Boolean? HoldEntry
		{
			get
			{
				return this._HoldEntry;
			}
			set
			{
				this._HoldEntry = value;
			}
		}
		#endregion
		#region EarlyChecks
		public abstract class earlyChecks : PX.Data.BQL.BqlBool.Field<earlyChecks> { }
		protected Boolean? _EarlyChecks;

        /// <summary>
        /// If set to <c>true</c>, allows to enter and print checks for bills, belonging to future periods.
        /// </summary>
        /// <value>
        /// Defaults to <c>true</c>.
        /// </value>
		[PXDBBool()]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Enable Early Checks")]
		public virtual Boolean? EarlyChecks
		{
			get
			{
				return this._EarlyChecks;
			}
			set
			{
				this._EarlyChecks = value;
			}
		}
		#endregion
		#region RequireVendorRef
		public abstract class requireVendorRef : PX.Data.BQL.BqlBool.Field<requireVendorRef> { }
		protected Boolean? _RequireVendorRef;

        /// <summary>
        /// When set to <c>true</c>, indicates that users must fill in Vendor Ref. (<see cref="APInvoice.InvoiceNbr"/>) on data entry forms in the Accounts Payable, Taxes and Purchase Orders modules.
        /// Also this check box controls the Ext. Ref. Number box (<see cref="PX.Objects.GL.GLTranDoc.ExtRefNbr">GLTranDoc.ExtRefNbr</see>) on the Journal Vouchers (GL.30.40.00) form of the General Ledger module.
        /// </summary>
		[PXDBBool()]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Require Vendor Reference")]
		public virtual Boolean? RequireVendorRef
		{
			get
			{
				return this._RequireVendorRef;
			}
			set
			{
				this._RequireVendorRef = value;
			}
		}
		#endregion
		#region PaymentLeadTime
		public abstract class paymentLeadTime : PX.Data.BQL.BqlShort.Field<paymentLeadTime> { }
		protected Int16? _PaymentLeadTime;

        /// <summary>
        /// The number of days on average required for a payment to reach a vendor location. 
        /// This value is used as a default value for multiple boxes on the Approve Bills for Payment (AP.50.20.00) and Prepare Payments (AP.50.30.00) forms.
        /// </summary>
		[PXDBShort()]
		[PXDefault((short)7)]
		[PXUIField(DisplayName = "Payment Lead Time")]
		public virtual Int16? PaymentLeadTime
		{
			get
			{
				return this._PaymentLeadTime;
			}
			set
			{
				this._PaymentLeadTime = value;
			}
		}
		#endregion
        #region InvoicePrecision
        public abstract class invoicePrecision : PX.Data.BQL.BqlDecimal.Field<invoicePrecision> { }
        protected decimal? _InvoicePrecision;

        /// <summary>
        /// Determines the smallest value to round bills amount to. 
        /// Doesn't have any effect unless Invoice Rounding feature is enabled.
        /// If Rounding Rule (<see cref="APSetup.InvoiceRounding"/>) is set to Use Currency Precision (<c>"N"</c>) the value of this field similarly does not affect rounding.
        /// </summary>
        /// <value>
        /// Allowed values are: <c>"0.05"</c>, <c>"0.1"</c>, <c>"0.5"</c>, <c>"1.0"</c>, <c>"10"</c>, <c>"100"</c>. Defaults to <c>"0.1"</c>
        /// </value>
        [PXDBDecimalString(2)]
        [InvoicePrecision.List]
        [PXDefault(TypeCode.Decimal, CS.InvoicePrecision.m01)]
        [PXUIField(DisplayName = "Rounding Precision")]
        public virtual decimal? InvoicePrecision
        {
            get
            {
                return this._InvoicePrecision;
            }
            set
            {
                this._InvoicePrecision = value;
            }
        }
        #endregion
        #region InvoiceRounding
        public abstract class invoiceRounding : PX.Data.BQL.BqlString.Field<invoiceRounding> { }
        protected String _InvoiceRounding;

        /// <summary>
        /// Determines the rule to be used to round bills total amounts.
        /// Doesn't have any effect unless Invoice Rounding feature is enabled.
        /// Smallest unit to round to is determined by the precision of the <see cref="PX.Objects.CM.Currency">Currency</see> if Use Currency Precision is selected,
        /// or by the value of the <see cref="APSetup.InvoicePrecision"/> field otherwise.
        /// </summary>
        /// <value>
        /// Allowed values are:
        /// <c>"N"</c> - Use Currency Precision: To round the totals to the decimal precision supported by the currency of the document.
        /// (See <see cref="PX.Objects.CM.Currency.DecimalPlaces">Currency.DecimalPlaces</see> field.)
        /// <c>"R"</c> - Nearest: To round each bill total to the nearest multiple of the smallest unit (specified in the Rounding Precision box). (Mathematical rounding)
        /// <c>"C"</c> - Up: To round up each bill total to the next multiple of the smallest unit.
        /// <c>"F"</c> - Down: To round down each bill total to the previous multiple of the smallest unit.
        /// </value>
        [PXDBString(1, IsFixed = true)]
        [PXDefault(RoundingType.Currency)]
        [PXUIField(DisplayName = "Rounding Rule for Bills")]
        [InvoiceRounding.List]
        public virtual String InvoiceRounding
        {
            get
            {
                return this._InvoiceRounding;
            }
            set
            {
                this._InvoiceRounding = value;
            }
        }
        #endregion
        #region RaiseErrorOnDoubleInvoiceNbr
        public abstract class raiseErrorOnDoubleInvoiceNbr : PX.Data.BQL.BqlBool.Field<raiseErrorOnDoubleInvoiceNbr> { }

        /// <summary>
        /// When set to <c>true</c>, makes the system generate errors when a new document is created with a value in the
        /// <see cref="APInvoice.InvoiceNbr">Vendor Ref</see>. box that has already been used in the system.
        /// </summary>
        [PXDBBool]
        [PXDefault(false)]
		[PXUIField(DisplayName = "Raise an Error on Duplicate Vendor Reference Number")]
        public virtual Boolean? RaiseErrorOnDoubleInvoiceNbr { get; set; }
        #endregion
		#region LoadVendorsPricesUsingAlternateID
		public abstract class loadVendorsPricesUsingAlternateID : PX.Data.BQL.BqlBool.Field<loadVendorsPricesUsingAlternateID> { }

		/// <summary>
		/// When set to <c>true</c>, makes it possible to load 
		/// <see cref="APVendorPrice">Vendor Prices</see> by
		/// alternate ID
		/// </summary>
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Load Vendor Prices by Alternate ID")]
		public virtual Boolean? LoadVendorsPricesUsingAlternateID { get; set; }
		#endregion
		#region VendorPriceUpdate
		public abstract class vendorPriceUpdate : PX.Data.BQL.BqlString.Field<vendorPriceUpdate> { }
		protected String _VendorPriceUpdate;
		[PXDBString(1, IsFixed = true)]
		[PXDefault(APVendorPriceUpdateType.Purchase)]
		[PXUIField(DisplayName = "Vendor Price Update", Visibility = PXUIVisibility.Visible)]
		[APVendorPriceUpdateType.List]
		public virtual String VendorPriceUpdate
		{
			get
			{
				return _VendorPriceUpdate;
			}
			set
			{
				this._VendorPriceUpdate = value;
			}
		}
		#endregion
		#region ApplyQuantityDiscountBy
		public abstract class applyQuantityDiscountBy : PX.Data.BQL.BqlString.Field<applyQuantityDiscountBy> { }
		protected String _ApplyQuantityDiscountBy;
		[PXDBString(1, IsFixed = true)]
		[PXDefault(ApplyQuantityDiscountType.DocumentLineUOM, PersistingCheck = PXPersistingCheck.Null)]
		[ApplyQuantityDiscountType.List]
		[PXUIField(DisplayName = "Apply Quantity Discounts To", Visibility = PXUIVisibility.Visible)]
		public virtual String ApplyQuantityDiscountBy
		{
			get
			{
				return this._ApplyQuantityDiscountBy;
			}
			set
			{
				this._ApplyQuantityDiscountBy = value;
			}
		}
		#endregion
        #region RetentionType
        public abstract class retentionType : PX.Data.BQL.BqlString.Field<retentionType> { }
        protected String _RetentionType;

        /// <summary>
        /// The way the history of prices will be retained.
        /// </summary>
        /// <value>
        /// Allowed values are:
        /// <code>"L"</code> - Last Price Only: the last defined price is kept;
        /// <code>"F"</code> - Fixed Number of Months: the history of price changes is kept for the number of months specified in the <see cref="NumberOfMonths"/> field. The period is calculated back from the current system date.
        /// </value>
        [PXDBString(1, IsFixed = true)]
        [PXDefault(AR.RetentionTypeList.LastPrice)]
        [AR.RetentionTypeList.List()]
        [PXUIField(DisplayName = "Retention Type", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual String RetentionType
        {
            get
            {
                return this._RetentionType;
            }
            set
            {
                this._RetentionType = value;
            }
        }
        #endregion
        #region NumberOfMonths
        public abstract class numberOfMonths : PX.Data.BQL.BqlInt.Field<numberOfMonths> { }
        protected Int32? _NumberOfMonths;

        /// <summary>
        /// The number of months the history of price changes should be kept.
        /// This field is relevant if the Fixed Number of Months (<code>"F"</code>) option is selected in the <see cref="RetentionType"/>.
        /// </summary>
        [PXDBInt()]
        [PXDefault(12)]
        [PXUIField(DisplayName = "Number of Months", Visibility = PXUIVisibility.Visible)]
        public virtual Int32? NumberOfMonths
        {
            get
            {
                return this._NumberOfMonths;
            }
            set
            {
                this._NumberOfMonths = value;
            }
        }
        #endregion
        #region SuggestPaymentAmount
        public abstract class suggestPaymentAmount : PX.Data.BQL.BqlBool.Field<suggestPaymentAmount> { }

        [PXDBBool]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Set Zero Payment Amount to Application Amount")]
        public bool? SuggestPaymentAmount
        {
            get;
            set;
        }
        #endregion
		#region MigrationMode
		public abstract class migrationMode : PX.Data.BQL.BqlBool.Field<migrationMode> { }
		/// <summary>
		/// Specifies (if set to <c>true</c>) that migration mode is activated for the AP module.
		/// In other words, this gives an ability to create the document with starting balance without any applications.
		/// </summary>
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = Common.Messages.ActivateMigrationMode)]
		public virtual bool? MigrationMode
		{
			get;
			set;
	    }
		#endregion
		#region RetainTaxes
		public abstract class retainTaxes : PX.Data.BQL.BqlBool.Field<retainTaxes> { }
		
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Retain Taxes", Visibility = PXUIVisibility.Visible, FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual bool? RetainTaxes
		{
			get;
			set;
		}
		#endregion
		#region RetainageBillsAutoRelease
		public abstract class retainageBillsAutoRelease : PX.Data.BQL.BqlBool.Field<retainageBillsAutoRelease> { }
		
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Automatically Release Retainage Bills", Visibility = PXUIVisibility.Visible, FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual bool? RetainageBillsAutoRelease
		{
			get;
			set;
		}
		#endregion
		#region RequireSingleProjectPerDocument
		public abstract class requireSingleProjectPerDocument : PX.Data.BQL.BqlBool.Field<requireSingleProjectPerDocument> { }

		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Require Single Project per Document", Visibility = PXUIVisibility.Visible, FieldClass = nameof(FeaturesSet.Retainage))]
		public virtual bool? RequireSingleProjectPerDocument
		{
			get;
			set;
		}
		#endregion
		#region PPDDebitAdjustmentDescr
		public abstract class pPDDebitAdjustmentDescr : PX.Data.BQL.BqlString.Field<pPDDebitAdjustmentDescr> { }
		[PXDBLocalizableString(150, IsUnicode = true)]
		[PXUIField(DisplayName = Messages.PPDDebitAdjustmentDescr)]
		public virtual string PPDDebitAdjustmentDescr
		{
			get;
			set;
		}
		#endregion
		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		[PXNote]
		public virtual Guid? NoteID
		{
			get;
			set;
		}
		#endregion

		#region IntercompanyExpenseAccountDefault
		public abstract class intercompanyExpenseAccountDefault : PX.Data.BQL.BqlShort.Field<intercompanyExpenseAccountDefault>
		{
			public class ListAttribute : PXStringListAttribute
			{
				public ListAttribute() : base(
					new string[] { APAcctSubDefault.MaskLocation, APAcctSubDefault.MaskItem },
					new string[] { MaskVendorLocationLabel, Messages.MaskInventoryItem }
					)
				{ }

				public static string MaskVendorLocationLabel =>
					PXAccess.FeatureInstalled<FeaturesSet.accountLocations>()
						? Messages.MaskLocation
						: Messages.MaskVendor;
			}
		}

		[PXDBString(1, IsFixed = true)]
		[PXDefault(APAcctSubDefault.MaskLocation)]
		[PXUIField(DisplayName = "Use Intercompany Expense Account From", FieldClass = nameof(FeaturesSet.InterBranch))]
		[intercompanyExpenseAccountDefault.List]
		public virtual string IntercompanyExpenseAccountDefault { get; set; }
		#endregion

	}

	public class APVendorPriceUpdateType
	{
		public class List : PXStringListAttribute
		{
			public List() : base(
				new[]
				{
					Pair(None, Messages.VendorUpdateNone),
					Pair(Purchase, Messages.VendorUpdatePurchase),
					Pair(ReleaseAPBill, Messages.VendorUpdateAPBillRelease),
				}) {}
		}

		public const string None = "N";
		public const string Purchase = "P";
		public const string ReleaseAPBill = "B";
    }
}
