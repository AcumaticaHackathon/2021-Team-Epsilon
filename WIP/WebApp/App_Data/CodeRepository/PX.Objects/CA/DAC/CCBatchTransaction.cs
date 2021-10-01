using System;
using System.Collections.Generic;
using System.Linq;

using PX.CCProcessingBase.Interfaces.V2;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CS;

namespace PX.Objects.CA
{
	[Serializable]
	[PXCacheName("CCBatchTransaction")]
	public class CCBatchTransaction : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<CCBatchTransaction>.By<CCBatchTransaction.batchID, pCTranNumber>
		{
			public static CCBatchTransaction Find(PXGraph graph, int? batchID, string pCTranNumber) => FindBy(graph, batchID, pCTranNumber);
		}

		public static class FK
		{
			public class CCBatch : CA.CCBatch.PK.ForeignKeyOf<CCBatchTransaction>.By<batchID> { }
			public class ARPayment : AR.ARPayment.PK.ForeignKeyOf<CCBatchTransaction>.By<docType, refNbr> { }
			public class ExternalTransaction : AR.ExternalTransaction.PK.ForeignKeyOf<CCBatchTransaction>.By<transactionID> { }
		}
		#endregion

		#region SelectedToHide
		[PXBool]
		[PXUIField(DisplayName = "Selected")]
		public bool? SelectedToHide { get; set; }
		public abstract class selectedToHide : PX.Data.BQL.BqlBool.Field<selectedToHide> { }
		#endregion

		#region SelectedToUnhide
		[PXBool]
		[PXUIField(DisplayName = "Selected")]
		public bool? SelectedToUnhide { get; set; }
		public abstract class selectedToUnhide : PX.Data.BQL.BqlBool.Field<selectedToUnhide> { }
		#endregion

		#region BatchID
		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Batch ID")]
		[PXParent(typeof(Select<CCBatch, Where<CCBatch.batchID, Equal<Current<CCBatchTransaction.batchID>>>>))]
		[PXFormula(null, typeof(CountCalc<CCBatch.importedTransactionCount>))]
		[PXUnboundFormula(typeof(Switch<Case<Where<CCBatchTransaction.processingStatus, Equal<CCBatchTranProcessingStatusCode.pendingProcessing>>, CS.int1>, CS.int0>),
			typeof(SumCalc<CCBatch.unprocessedCount>))]
		[PXUnboundFormula(typeof(Switch<Case<Where<CCBatchTransaction.processingStatus, Equal<CCBatchTranProcessingStatusCode.missing>>, CS.int1>, CS.int0>),
			typeof(SumCalc<CCBatch.missingCount>))]
		[PXUnboundFormula(typeof(Switch<Case<Where<CCBatchTransaction.processingStatus, Equal<CCBatchTranProcessingStatusCode.hidden>>, CS.int1>, CS.int0>),
			typeof(SumCalc<CCBatch.hiddenCount>))]
		[PXUnboundFormula(typeof(Switch<Case<Where<CCBatchTransaction.processingStatus, Equal<CCBatchTranProcessingStatusCode.processed>>, CS.int1>, CS.int0>),
			typeof(SumCalc<CCBatch.processedCount>))]
		[PXDBDefault(typeof(CCBatch.batchID))]
		public virtual int? BatchID { get; set; }
		public abstract class batchID : PX.Data.BQL.BqlInt.Field<batchID> { }
		#endregion

		#region PCTranNumber
		[PXDBString(15, IsKey = true, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Proc. Center Tran. Nbr.")]
		public virtual string PCTranNumber { get; set; }
		public abstract class pCTranNumber : PX.Data.BQL.BqlString.Field<pCTranNumber> { }
		#endregion

		#region PCCustomerID
		[PXDBString(40, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Proc. Center Customer ID")]
		public virtual string PCCustomerID { get; set; }
		public abstract class pcCustomerID : PX.Data.BQL.BqlString.Field<pcCustomerID> { }
		#endregion

		#region PCPaymentProfileID
		[PXDBString(40, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Proc. Center Profile ID")]
		public virtual string PCPaymentProfileID { get; set; }
		public abstract class pcPaymentProfileID : PX.Data.BQL.BqlString.Field<pcPaymentProfileID> { }
		#endregion

		#region SettlementStatus
		[PXDBString(3, IsFixed = true)]
		[CCBatchTranSettlementStatusCode.List]
		[PXUIField(DisplayName = "Settlement Status")]
		public virtual string SettlementStatus { get; set; }
		public abstract class settlementStatus : PX.Data.BQL.BqlString.Field<settlementStatus> { }
		#endregion

		#region InvoiceNbr
		[PXDBString(40, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Invoice Nbr")]
		public virtual string InvoiceNbr { get; set; }
		public abstract class invoiceNbr : PX.Data.BQL.BqlString.Field<invoiceNbr> { }
		#endregion

		#region SubmitTime
		[PXDBDate(PreserveTime = true)]
		[PXUIField(DisplayName = "Submit Time")]
		public virtual DateTime? SubmitTime { get; set; }
		public abstract class submitTime : PX.Data.BQL.BqlDateTime.Field<submitTime> { }
		#endregion

		#region CardType
		[PXDBString(15, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Card Type")]
		public virtual string CardType { get; set; }
		public abstract class cardType : PX.Data.BQL.BqlString.Field<cardType> { }
		#endregion

		#region AccountNumber
		[PXDBString(10, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Card/Account Nbr.")]
		public virtual string AccountNumber { get; set; }
		public abstract class accountNumber : PX.Data.BQL.BqlString.Field<accountNumber> { }
		#endregion

		#region Amount
		[PXDBCury(typeof(CCBatch.curyID))]
		[PXUIField(DisplayName = "Amount")]
		public virtual decimal? Amount { get; set; }
		public abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }
		#endregion

		#region FixedFee
		[PXDBCury(typeof(CCBatch.curyID))]
		[PXUIField(DisplayName = "Fixed Fee")]
		public virtual decimal? FixedFee { get; set; }
		public abstract class fixedFee : PX.Data.BQL.BqlDecimal.Field<fixedFee> { }
		#endregion

		#region PercentageFee
		[PXDBCury(typeof(CCBatch.curyID))]
		[PXUIField(DisplayName = "Percentage Fee")]
		public virtual decimal? PercentageFee { get; set; }
		public abstract class percentageFee : PX.Data.BQL.BqlDecimal.Field<percentageFee> { }
		#endregion

		#region TotalFee
		[PXCury(typeof(CCBatch.curyID))]
		[PXDBCalced(typeof(Add<IsNull<fixedFee, decimal0>, IsNull<percentageFee, decimal0>>), typeof(decimal))]
		[PXUIField(DisplayName = "Total Fee")]
		public virtual decimal? TotalFee { get; set; }
		public abstract class totalFee : PX.Data.BQL.BqlDecimal.Field<totalFee> { }
		#endregion

		#region FeeType
		[PXDBString]
		[PXUIField(DisplayName = "Fee Type")]
		public virtual string FeeType { get; set; }
		public abstract class feeType : PX.Data.BQL.BqlString.Field<feeType> { }
		#endregion

		#region TransactionID
		[PXDBInt]
		[PXUIField(DisplayName = "Transaction ID")]
		public virtual int? TransactionID { get; set; }
		public abstract class transactionID : PX.Data.BQL.BqlInt.Field<transactionID> { }
		#endregion

		#region OriginalStatus
		[PXDBString(3, IsFixed = true)]
		[ExtTransactionProcStatusCode.List]
		[PXUIField(DisplayName = "Original Status")]
		public virtual string OriginalStatus { get; set; }
		public abstract class originalStatus : PX.Data.BQL.BqlString.Field<originalStatus> { }
		#endregion

		#region CurrentStatus
		[PXDBString(3, IsFixed = true, InputMask = "")]
		[PXUIField(DisplayName = "Current Status")]
		public virtual string CurrentStatus { get; set; }
		public abstract class currentStatus : PX.Data.BQL.BqlString.Field<currentStatus> { }
		#endregion

		#region ProcessingStatus
		[PXDBString(3, IsFixed = true)]
		[CCBatchTranProcessingStatusCode.List]
		[PXUIField(DisplayName = "Processing Status")]
		public virtual string ProcessingStatus { get; set; }
		public abstract class processingStatus : PX.Data.BQL.BqlString.Field<processingStatus> { }
		#endregion

		#region DocType
		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Doc. Type", Visibility = PXUIVisibility.SelectorVisible)]
		[ARDocType.List]
		public virtual string DocType { get; set; }
		public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
		#endregion

		#region RefNbr
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(Search<ARRegister.refNbr, Where<ARRegister.docType, Equal<Optional<CCBatchTransaction.docType>>>>))]
		public virtual string RefNbr { get; set; }
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		#endregion

		#region CreatedDateTime
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		#endregion

		#region CreatedByID
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		#endregion

		#region CreatedByScreenID
		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		#endregion

		#region LastModifiedDateTime
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		#endregion

		#region LastModifiedByID
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		#endregion

		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		#endregion

		#region Tstamp
		[PXDBTimestamp]
		[PXUIField(DisplayName = "Tstamp")]
		public virtual byte[] Tstamp { get; set; }
		public abstract class tstamp : PX.Data.BQL.BqlByteArray.Field<tstamp> { }
		#endregion

		#region Noteid
		[PXNote]
		public virtual Guid? Noteid { get; set; }
		public abstract class noteid : PX.Data.BQL.BqlGuid.Field<noteid> { }
		#endregion
	}

	public static class CCBatchTranSettlementStatusCode
	{
		public const string SettledSuccessfully = "SSC";
		public const string SettlementError = "SER";
		public const string Voided = "VOI";
		public const string Declined = "DEC";
		public const string RefundSettledSuccessfully = "RSS";
		public const string GeneralError = "ERR";
		public const string Expired = "EXP";
		public const string Unknown = "UNK";

		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { SettledSuccessfully, SettlementError, Voided, Declined, RefundSettledSuccessfully, GeneralError, Expired, Unknown },
				new string[] { "Settled Successfully", "Settlement Error", "Voided", "Declined", "Refund Settled Successfully", "General Error", "Expired", "Unknown" })
			{ }
		}
		public class settledSuccessfully : PX.Data.BQL.BqlString.Constant<settledSuccessfully>
		{
			public settledSuccessfully() : base(SettledSuccessfully) { }
		}

		public class voided : PX.Data.BQL.BqlString.Constant<voided>
		{
			public voided() : base(Voided) { }
		}

		public class declined : PX.Data.BQL.BqlString.Constant<declined>
		{
			public declined() : base(Declined) { }
		}

		public class refundSettledSuccessfully : PX.Data.BQL.BqlString.Constant<refundSettledSuccessfully>
		{
			public refundSettledSuccessfully() : base(RefundSettledSuccessfully) { }
		}

		public class generalError : PX.Data.BQL.BqlString.Constant<generalError>
		{
			public generalError() : base(GeneralError) { }
		}

		public class expired : PX.Data.BQL.BqlString.Constant<expired>
		{
			public expired() : base(Expired) { }
		}

		public class unknown : PX.Data.BQL.BqlString.Constant<unknown>
		{
			public unknown() : base(Unknown) { }
		}

		public static string GetCode(CCTranStatus tranType)
		{
			if (codes.TryGetValue(tranType, out var code))
				return code;
			else
				throw new ArgumentException(nameof(tranType));
		}

		private static readonly Dictionary<CCTranStatus, string> codes =
			new Dictionary<CCTranStatus, string>
			{
				{ CCTranStatus.SettledSuccessfully, SettledSuccessfully },
				{CCTranStatus.SettlementError, SettlementError },
				{ CCTranStatus.Voided, Voided },
				{ CCTranStatus.Declined, Declined },
				{ CCTranStatus.RefundSettledSuccessfully, RefundSettledSuccessfully },
				{ CCTranStatus.GeneralError, GeneralError },
				{ CCTranStatus.Expired, Expired },
				{ CCTranStatus.Unknown, Unknown }
			};
	}

	public static class CCBatchTranProcessingStatusCode
	{
		public const string PendingProcessing = "PPR";
		public const string Processed = "PRD";
		public const string Missing = "MIS";
		public const string Hidden = "HID";

		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { PendingProcessing, Processed, Missing, Hidden },
				new string[] { "Pending Processing", "Processed", "Missing", "Hidden" })
			{ }
		}

		public class pendingProcessing : PX.Data.BQL.BqlString.Constant<pendingProcessing>
		{
			public pendingProcessing() : base(PendingProcessing) { }
		}

		public class processed : PX.Data.BQL.BqlString.Constant<processed>
		{
			public processed() : base(Processed) { }
		}

		public class missing : PX.Data.BQL.BqlString.Constant<missing>
		{
			public missing() : base(Missing) { }
		}

		public class hidden : PX.Data.BQL.BqlString.Constant<hidden>
		{
			public hidden() : base(Hidden) { }
		}
	}

	#region Aliases
	[PXHidden]
	public class CCBatchTransactionAlias1 : CCBatchTransaction
	{
		public new abstract class selectedToHide : PX.Data.BQL.BqlBool.Field<selectedToHide> { }
		public new abstract class selectedToUnhide : PX.Data.BQL.BqlBool.Field<selectedToUnhide> { }
		public new abstract class batchID : PX.Data.BQL.BqlInt.Field<batchID> { }
		public new abstract class pCTranNumber : PX.Data.BQL.BqlString.Field<pCTranNumber> { }
		public new abstract class settlementStatus : PX.Data.BQL.BqlString.Field<settlementStatus> { }
		public new abstract class invoiceNbr : PX.Data.BQL.BqlString.Field<invoiceNbr> { }
		public new abstract class submitTime : PX.Data.BQL.BqlDateTime.Field<submitTime> { }
		public new abstract class cardType : PX.Data.BQL.BqlString.Field<cardType> { }
		public new abstract class accountNumber : PX.Data.BQL.BqlString.Field<accountNumber> { }
		public new abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }
		public new abstract class transactionID : PX.Data.BQL.BqlInt.Field<transactionID> { }
		public new abstract class originalStatus : PX.Data.BQL.BqlString.Field<originalStatus> { }
		public new abstract class currentStatus : PX.Data.BQL.BqlString.Field<currentStatus> { }
		public new abstract class processingStatus : PX.Data.BQL.BqlString.Field<processingStatus> { }
		public new abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
		public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		public new abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		public new abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		public new abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		public new abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		public new abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		public new abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		public new abstract class tstamp : PX.Data.BQL.BqlByteArray.Field<tstamp> { }
		public new abstract class noteid : PX.Data.BQL.BqlGuid.Field<noteid> { }
	}

	[PXHidden]
	public class CCBatchTransactionAlias2 : CCBatchTransaction
	{
		public new abstract class selectedToHide : PX.Data.BQL.BqlBool.Field<selectedToHide> { }
		public new abstract class selectedToUnhide : PX.Data.BQL.BqlBool.Field<selectedToUnhide> { }
		public new abstract class batchID : PX.Data.BQL.BqlInt.Field<batchID> { }
		public new abstract class pCTranNumber : PX.Data.BQL.BqlString.Field<pCTranNumber> { }
		public new abstract class settlementStatus : PX.Data.BQL.BqlString.Field<settlementStatus> { }
		public new abstract class invoiceNbr : PX.Data.BQL.BqlString.Field<invoiceNbr> { }
		public new abstract class submitTime : PX.Data.BQL.BqlDateTime.Field<submitTime> { }
		public new abstract class cardType : PX.Data.BQL.BqlString.Field<cardType> { }
		public new abstract class accountNumber : PX.Data.BQL.BqlString.Field<accountNumber> { }
		public new abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }
		public new abstract class transactionID : PX.Data.BQL.BqlInt.Field<transactionID> { }
		public new abstract class originalStatus : PX.Data.BQL.BqlString.Field<originalStatus> { }
		public new abstract class currentStatus : PX.Data.BQL.BqlString.Field<currentStatus> { }
		public new abstract class processingStatus : PX.Data.BQL.BqlString.Field<processingStatus> { }
		public new abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
		public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		public new abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		public new abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		public new abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		public new abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		public new abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		public new abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		public new abstract class tstamp : PX.Data.BQL.BqlByteArray.Field<tstamp> { }
		public new abstract class noteid : PX.Data.BQL.BqlGuid.Field<noteid> { }
	}
	#endregion Aliases
}
