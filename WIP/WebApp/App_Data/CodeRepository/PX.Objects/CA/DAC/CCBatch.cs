using System;
using System.Collections.Generic;

using PX.CCProcessingBase.Interfaces.V2;
using PX.Common;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM;
using PX.Objects.GL;

namespace PX.Objects.CA
{
	[Serializable]
	[PXCacheName("CCBatch")]
	public class CCBatch : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<CCBatch>.By<CCBatch.batchID>
		{
			public static CCBatch Find(PXGraph graph, int? batchID) => FindBy(graph, batchID);
		}

		public static class FK
		{
			public class Currency : CM.Currency.PK.ForeignKeyOf<CCBatch>.By<curyID> { }
			public class CADeposit : CA.CADeposit.PK.ForeignKeyOf<CCBatch>.By<depositType, depositNbr> { }
			public class CCProcessingCenter : CA.CCProcessingCenter.PK.ForeignKeyOf<CCBatch>.By<processingCenterID> { }
		}
		#endregion

		#region Selected
		[PXBool]
		[PXUIField(DisplayName = "Selected")]
		public bool? Selected { get; set; }
		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }
		#endregion

		#region BatchID
		[PXDBIdentity(IsKey = true)]
		[PXDefault]
		[PXSelector(typeof(Search<CCBatch.batchID>))]
		[PXUIField(DisplayName = "Reference Number", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual int? BatchID { get; set; }
		public abstract class batchID : PX.Data.BQL.BqlInt.Field<batchID> { }
		#endregion

		#region Status
		[PXDBString(3, IsFixed = true)]
		[CCBatchStatusCode.List]
		[PXUIField(DisplayName = "Status")]
		public virtual string Status { get; set; }
		public abstract class status : PX.Data.BQL.BqlString.Field<status> { }
		#endregion

		#region ProcessingCenterID
		[PXDBString(10, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Proc. Center ID")]
		public virtual string ProcessingCenterID { get; set; }
		public abstract class processingCenterID : PX.Data.BQL.BqlString.Field<processingCenterID> { }
		#endregion

		#region CuryID
		[PXDBScalar(typeof(Search2<CashAccount.curyID, InnerJoin<CCProcessingCenter, On<CCProcessingCenter.cashAccountID, Equal<CashAccount.cashAccountID>>>,
			Where<CCProcessingCenter.processingCenterID, Equal<CCBatch.processingCenterID>>>))]
		[PXString(IsUnicode = true)]
		[PXUIField(DisplayName = "Currency", Enabled = false)]
		[PXSelector(typeof(Currency.curyID))]
		public virtual string CuryID { get; set; }
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
		#endregion

		#region ExtBatchID
		[PXDBString(15, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Ext. Batch ID")]
		public virtual string ExtBatchID { get; set; }
		public abstract class extBatchID : PX.Data.BQL.BqlString.Field<extBatchID> { }
		#endregion

		#region SettlementTimeUTC
		[PXDBDate(PreserveTime = true, UseTimeZone = false, DisplayMask = "G", InputMask = "G")]
		[PXUIField(DisplayName = "Settlement Time UTC")]
		public virtual DateTime? SettlementTimeUTC { get; set; }
		public abstract class settlementTimeUTC : PX.Data.BQL.BqlDateTime.Field<settlementTimeUTC> { }
		#endregion

		#region SettlementTime
		[PXDate(InputMask = "G", DisplayMask = "G")]
		[PXUIField(DisplayName = "Settlement Time")]
		public virtual DateTime? SettlementTime
		{
			[PXDependsOnFields(typeof(settlementTimeUTC))]
			get
			{
				return SettlementTimeUTC.HasValue
					? PXTimeZoneInfo.ConvertTimeFromUtc(SettlementTimeUTC.Value, LocaleInfo.GetTimeZone())
					: (DateTime?)null;
			}
		}
		public abstract class settlementTime : PX.Data.BQL.BqlDateTime.Field<settlementTime> { }
		#endregion

		#region SettlementState
		[PXDBString(3, IsFixed = true)]
		[CCBatchSettlementState.List]
		[PXUIField(DisplayName = "Settlement State")]
		public virtual string SettlementState { get; set; }
		public abstract class settlementState : PX.Data.BQL.BqlString.Field<settlementState> { }
		#endregion

		#region ProcessedCount
		[PXDBInt]
		[PXDefault(0)]
		[PXUIField(DisplayName = "Processed Count")]
		public virtual int? ProcessedCount { get; set; }
		public abstract class processedCount : PX.Data.BQL.BqlInt.Field<processedCount> { }
		#endregion

		#region MissingCount
		[PXDBInt]
		[PXDefault(0)]
		[PXUIField(DisplayName = "Missing Transaction Count")]
		public virtual int? MissingCount { get; set; }
		public abstract class missingCount : PX.Data.BQL.BqlInt.Field<missingCount> { }
		#endregion

		#region HiddenCount
		[PXDBInt]
		[PXDefault(0)]
		[PXUIField(DisplayName = "Hidden Count")]
		public virtual int? HiddenCount { get; set; }
		public abstract class hiddenCount : PX.Data.BQL.BqlInt.Field<hiddenCount> { }
		#endregion

		#region ExcludedCount
		[PXInt]
		[PXUIField(DisplayName = "Excluded from Deposit Count")]
		public virtual int? ExcludedCount { get; set; }
		public abstract class excludedCount : PX.Data.BQL.BqlInt.Field<excludedCount> { }
		#endregion

		#region TransactionCount
		[PXDBInt]
		[PXDefault(0)]
		[PXUIField(DisplayName = "Transaction Count")]
		public virtual int? TransactionCount { get; set; }
		public abstract class transactionCount : PX.Data.BQL.BqlInt.Field<transactionCount> { }
		#endregion

		#region ImportedTransactionCount
		[PXDBInt]
		[PXDefault(0)]
		[PXUIField(DisplayName = "Imported Transaction Count")]
		public virtual int? ImportedTransactionCount { get; set; }
		public abstract class importedTransactionCount : PX.Data.BQL.BqlInt.Field<importedTransactionCount> { }
		#endregion

		#region UnprocessedCount
		[PXDBInt]
		[PXDefault(0)]
		[PXUIField(DisplayName = "Unprocessed Transaction Count")]
		public virtual int? UnprocessedCount { get; set; }
		public abstract class unprocessedCount : PX.Data.BQL.BqlInt.Field<unprocessedCount> { }
		#endregion

		#region SettledAmount
		[PXDBCury(typeof(CCBatch.curyID))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Settled Amount")]
		public virtual decimal? SettledAmount { get; set; }
		public abstract class settledAmount : PX.Data.BQL.BqlDecimal.Field<settledAmount> { }
		#endregion

		#region SettledCount
		[PXDBInt]
		[PXDefault(0)]
		[PXUIField(DisplayName = "Settled Count")]
		public virtual int? SettledCount { get; set; }
		public abstract class settledCount : PX.Data.BQL.BqlInt.Field<settledCount> { }
		#endregion

		#region RefundAmount
		[PXDBCury(typeof(CCBatch.curyID))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Refund Amount")]
		public virtual decimal? RefundAmount { get; set; }
		public abstract class refundAmount : PX.Data.BQL.BqlDecimal.Field<refundAmount> { }
		#endregion

		#region RefundCount
		[PXDBInt]
		[PXDefault(0)]
		[PXUIField(DisplayName = "Refund Count")]
		public virtual int? RefundCount { get; set; }
		public abstract class refundCount : PX.Data.BQL.BqlInt.Field<refundCount> { }
		#endregion

		#region VoidCount
		[PXDBInt]
		[PXDefault(0)]
		[PXUIField(DisplayName = "Void Count")]
		public virtual int? VoidCount { get; set; }
		public abstract class voidCount : PX.Data.BQL.BqlInt.Field<voidCount> { }
		#endregion

		#region DeclineCount
		[PXDBInt]
		[PXDefault(0)]
		[PXUIField(DisplayName = "Decline Count")]
		public virtual int? DeclineCount { get; set; }
		public abstract class declineCount : PX.Data.BQL.BqlInt.Field<declineCount> { }
		#endregion

		#region ErrorCount
		[PXDBInt]
		[PXDefault(0)]
		[PXUIField(DisplayName = "Error Count")]
		public virtual int? ErrorCount { get; set; }
		public abstract class errorCount : PX.Data.BQL.BqlInt.Field<errorCount> { }
		#endregion

		#region DepositType
		[PXDBString(3, IsFixed = true)]
		public virtual string DepositType { get; set; }
		public abstract class depositType : PX.Data.BQL.BqlString.Field<depositType> { }
		#endregion

		#region DepositNbr
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Bank Deposit", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXSelector(typeof(Search<CADeposit.refNbr, Where<CADeposit.tranType, Equal<Current<CCBatch.depositType>>>>))]
		public virtual string DepositNbr { get; set; }
		public abstract class depositNbr : PX.Data.BQL.BqlString.Field<depositNbr> { }
		#endregion

		#region Description
		[PXString]
		public virtual string Description
		{
			[PXDependsOnFields(typeof(processingCenterID), typeof(settlementTimeUTC))]
			get { return ProcessingCenterID + ": " + SettlementTime; }
			set { }
		}
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

	public static class CCBatchStatusCode
	{
		public const string PendingImport = "PIM";
		public const string PendingProcessing = "PPR";
		public const string Processing = "PRG";
		public const string PendingReview = "PRV";
		public const string Processed = "PRD";
		public const string Deposited = "DPD";
		public const string Error = "ERR";

		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { PendingImport, PendingProcessing, Processing, PendingReview, Processed, Deposited, Error },
				new string[] { "Pending Import", "Pending Processing", "Processing", "Pending Review", "Processed", "Deposited", "Error" })
			{ }
		}

		public class pendingImport : PX.Data.BQL.BqlString.Constant<pendingImport>
		{
			public pendingImport() : base(PendingImport) { }
		}

		public class pendingProcessing : PX.Data.BQL.BqlString.Constant<pendingProcessing>
		{
			public pendingProcessing() : base(PendingProcessing) { }
		}

		public class processing : PX.Data.BQL.BqlString.Constant<processing>
		{
			public processing() : base(Processing) { }
		}

		public class pendingReview : PX.Data.BQL.BqlString.Constant<pendingReview>
		{
			public pendingReview() : base(PendingReview) { }
		}

		public class processed : PX.Data.BQL.BqlString.Constant<processed>
		{
			public processed() : base(Processed) { }
		}

		public class deposited : PX.Data.BQL.BqlString.Constant<deposited>
		{
			public deposited() : base(Deposited) { }
		}

		public class error : PX.Data.BQL.BqlString.Constant<error>
		{
			public error() : base(Error) { }
		}
	}

	public static class CCBatchSettlementState
	{
		public const string SettledSuccessfully = "SSC";
		public const string SettlementError = "SER";
		public const string PendingSettlement = "SPN";
		public const string Unknown = "UNK";

		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { SettledSuccessfully, SettlementError, PendingSettlement, Unknown },
				new string[] { "Settled Successfully", "Settlement Error", "Pending Settlement", "Unknown" })
			{ }
		}
		public class settledSuccessfully : PX.Data.BQL.BqlString.Constant<settledSuccessfully>
		{
			public settledSuccessfully() : base(SettledSuccessfully) { }
		}

		public class settlementError : PX.Data.BQL.BqlString.Constant<settlementError>
		{
			public settlementError() : base(SettlementError) { }
		}

		public class pendingSettlement : PX.Data.BQL.BqlString.Constant<pendingSettlement>
		{
			public pendingSettlement() : base(PendingSettlement) { }
		}

		public class unknown : PX.Data.BQL.BqlString.Constant<unknown>
		{
			public unknown() : base(Unknown) { }
		}

		public static string GetCode(CCBatchState batchState)
		{
			if (codes.TryGetValue(batchState, out var code))
				return code;
			else
				throw new ArgumentException(nameof(batchState));
		}

		private static readonly Dictionary<CCBatchState, string> codes =
			new Dictionary<CCBatchState, string>
			{
				{ CCBatchState.SettledSuccessfully, SettledSuccessfully },
				{ CCBatchState.SettlementError, SettlementError},
				{ CCBatchState.PendingSettlement, PendingSettlement },
				{ CCBatchState.Unknown, Unknown }
			};
	}
}
