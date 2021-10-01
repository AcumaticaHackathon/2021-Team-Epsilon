using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.CCProcessingBase.Interfaces.V2;
using PX.Common;
using PX.Data;
using PX.Data.Update.ExchangeService;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.AR.CCPaymentProcessing;
using PX.Objects.AR.CCPaymentProcessing.Wrappers;
using PX.Objects.BQLConstants;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.GL.FinPeriods;
using PX.Objects.TX;
using PX.SM;

namespace PX.Objects.CA
{
	public class CCBatchEnq : PXGraph
	{
		// ReSharper disable InconsistentNaming
		[InjectDependency]
		private ILegacyCompanyService _legacyCompanyService { get; set; }
		// ReSharper restore InconsistentNaming

		#region Internal Types Definition
		[PXHidden]
		[Serializable]
		public partial class BatchFilter : IBqlTable
		{
			#region ProcessingCenterID
			[PXDefault]
			[PXDBString(10, IsUnicode = true)]
			[PXSelector(typeof(Search<CCProcessingCenter.processingCenterID>))]
			[PXUIField(DisplayName = "Proc. Center ID")]
			public virtual string ProcessingCenterID { get; set; }
			public abstract class processingCenterID : PX.Data.BQL.BqlString.Field<processingCenterID> { }
			#endregion

			#region LastSettlementDateUTC
			[PXDate(UseTimeZone = true)]
			[PXUIField(DisplayName = "Last Settlement Date UTC")]
			public virtual DateTime? LastSettlementDateUTC { get; set; }
			public abstract class lastSettlementDateUTC : PX.Data.BQL.BqlDateTime.Field<lastSettlementDateUTC> { }
			#endregion

			#region LastSettlementDate			
			[PXDate(UseTimeZone = true)]
			[PXUIField(DisplayName = "Last Settlement Date")]
			public virtual DateTime? LastSettlementDate
			{
				get
				{
					return LastSettlementDateUTC.HasValue
						? PXTimeZoneInfo.ConvertTimeFromUtc(LastSettlementDateUTC.Value, LocaleInfo.GetTimeZone())
						: (DateTime?)null;
				}
			}
			public abstract class lastSettlementDate : PX.Data.BQL.BqlDateTime.Field<lastSettlementDate> { }
			#endregion

			#region FirstImportDateUTC
			[PXDate(UseTimeZone = true)]
			[PXUIField(DisplayName = "First Import Date")]
			public virtual DateTime? FirstImportDateUTC { get; set; }
			public abstract class firstImportDateUTC : PX.Data.BQL.BqlDateTime.Field<firstImportDateUTC> { }
			#endregion

			#region ImportThroughDateUTC
			[PXDate(UseTimeZone = true)]
			[PXUIField(DisplayName = "Import Batches Through UTC")]
			public virtual DateTime? ImportThroughDateUTC { get; set; }
			public abstract class importThroughDateUTC : PX.Data.BQL.BqlDateTime.Field<importThroughDateUTC> { }
			#endregion

			#region ImportThroughDate
			[PXDate(UseTimeZone = true)]
			[PXUIField(DisplayName = "Import Batches Through")]
			public virtual DateTime? ImportThroughDate
			{
				get
				{
					return ImportThroughDateUTC.HasValue
						? PXTimeZoneInfo.ConvertTimeFromUtc(ImportThroughDateUTC.Value, LocaleInfo.GetTimeZone())
						: (DateTime?)null;
				}

				set
				{
					ImportThroughDateUTC = value.HasValue
						? PXTimeZoneInfo.ConvertTimeToUtc(value.Value, LocaleInfo.GetTimeZone())
						: (DateTime?)null;
				}
			}
			public abstract class importThroughDate : PX.Data.BQL.BqlDateTime.Field<importThroughDate> { }
			#endregion
		}

		public struct DateRange
		{
			public DateTime StartDate { get; set; }
			public DateTime EndDate { get; set; }

			public DateRange(DateTime startDate, DateTime endDate)
			{
				StartDate = startDate;
				EndDate = endDate;
			}

			public IEnumerable<DateRange> Split(int intervalDays)
			{
				if (StartDate > EndDate || intervalDays <= 0)
					yield break;

				DateTime partStart = StartDate;
				while (true)
				{
					DateTime partEnd = partStart.AddDays(intervalDays);
					if (partEnd >= EndDate)
					{
						if (partStart <= EndDate)
						{
							yield return new DateRange(partStart, EndDate);
						}
						yield break;
					}
					else
					{
						yield return new DateRange(partStart, partEnd);
						partStart = partEnd.AddDays(1).Date;
					}
				}
			}
		}
		#endregion

		#region Actions
		private const string ProcessingCenterToken = "ProcessingCenterID";
		private const string NeedToWaitOnBatchToken = "NeedToWaitOnBatch";
		private const int LongRunUpdateSpan = 5000;

		public PXCancel<BatchFilter> Cancel;

		public PXAction<BatchFilter> importAndProcessBatches;
		[PXUIField(DisplayName = "Import Batches", MapEnableRights = PXCacheRights.Select, Visible = true)]
		[PXProcessButton]
		protected IEnumerable ImportAndProcessBatches(PXAdapter a)
		{
			Guid? uid = UID as Guid?;
			var filter = Filter.Current;

			CheckProcessRunning(filter.ProcessingCenterID);
			PXLongOperation.StartOperation(this, () =>
			{
				PXLongOperation.SetCustomInfo(filter.ProcessingCenterID, ProcessingCenterToken);

				var enqGraph = PXGraph.CreateInstance<CCBatchEnq>();
				enqGraph.DoImportBatches(new PXAdapter(enqGraph.Batches), filter);
				SetDefaultImportDates(filter);

				var enqGraph2 = PXGraph.CreateInstance<CCBatchEnq>();
				enqGraph2.UID = uid;
				enqGraph2.Batches.DoNotCheckPrevOperation = true;
				enqGraph2.Filter.Current = filter;

				int? lastBatchToProcess = null;
				foreach (CCBatch b in enqGraph2.Batches.Select())
				{
					if (b.Status.IsIn(CCBatchStatusCode.PendingImport, CCBatchStatusCode.PendingProcessing, CCBatchStatusCode.Processing))
					{
						b.Selected = true;
						enqGraph2.Batches.Update(b);
						lastBatchToProcess = b.BatchID;
					}
				}

				if (lastBatchToProcess != null)
				{
					PX.Common.PXContext.SetSlot(NeedToWaitOnBatchToken, lastBatchToProcess);
				}

				enqGraph2.Actions["Process"].Press();
			});

			Batches.View.Cache.Clear();
			Batches.View.Cache.ClearQueryCache();
			return a.Get();
		}

		public PXAction<BatchFilter> ViewDocument;
		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewDocument(PXAdapter adapter)
		{
			CCBatch batch = this.Batches.Current;
			if (batch != null)
			{
				var graph = PXGraph.CreateInstance<CCBatchMaint>();
				graph.BatchView.Current = graph.BatchView.Search<CCBatch.batchID>(batch.BatchID);
				if (graph.BatchView.Current != null)
				{
					PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.InlineWindow);
				}
			}
			return adapter.Get();
		}

		internal void CheckProcessRunning(string filterValue)
		{
			string screenID = this.Accessinfo.ScreenID.Replace(".", "");
			string companyName = PXAccess.GetCompanyName();
			foreach (RowTaskInfo info in PXLongOperation.GetTaskList()
				.Where(i => PXLongOperation.GetStatus(i.NativeKey) == PXLongRunStatus.InProcess))
			{
				if (screenID == info.Screen?.Replace(".", "") &&
					companyName == _legacyCompanyService.ExtractCompany(info.User) &&
					filterValue == PXLongOperation.GetCustomInfo(info.NativeKey, ProcessingCenterToken) as string)
				{
					throw new PXException(Messages.ImportOfSettlementBatchesInProgress);
				}
			}
		}
		#endregion

		#region Selects
		public PXFilter<BatchFilter> Filter;
		[PXFilterable]
		public PXFilteredProcessing<CCBatch, BatchFilter, Where<CCBatch.processingCenterID, Equal<Current<BatchFilter.processingCenterID>>>, OrderBy<Desc<CCBatch.settlementTimeUTC>>> Batches;
		#endregion

		#region Execute Select
		protected virtual IEnumerable filter()
		{
			var cache = Filter.Cache;
			if (cache != null)
			{
				var filter = cache.Current as BatchFilter;
				if (filter != null && filter.ProcessingCenterID == null)
				{
					var activeProcCenters = new PXSelect<CCProcessingCenter,
						Where<CCProcessingCenter.isActive, Equal<boolTrue>>>(this);
					if (activeProcCenters.Select().Count == 1)
					{
						string procCenterID = activeProcCenters.SelectSingle().ProcessingCenterID;
						cache.SetValueExt<BatchFilter.processingCenterID>(filter, procCenterID);
					}
				}
			}

			yield return Filter.Cache.Current;
			Filter.Cache.IsDirty = false;
		}
		#endregion

		#region Events
		protected void _(Events.FieldUpdated<BatchFilter.processingCenterID> e)
		{
			var filter = (BatchFilter)e.Row;
			if (filter == null) return;

			SetDefaultImportDates(filter);
		}

		private void SetDefaultImportDates(BatchFilter filter)
		{
			var ccProcessingCenter = CCProcessingCenter.PK.Find(this, filter.ProcessingCenterID);
			filter.LastSettlementDateUTC = ccProcessingCenter?.LastSettlementDateUTC;
			filter.FirstImportDateUTC = MaxDate(filter.LastSettlementDateUTC, ccProcessingCenter?.ImportStartDate);
			var defaultImportThrough = ShiftDateByDefaultImportPeriod(filter.FirstImportDateUTC);
			filter.ImportThroughDateUTC = MinDate(defaultImportThrough, DateTime.UtcNow);
		}
		#endregion

		#region Constructor
		public CCBatchEnq()
		{
			this.Batches.SetSelected<CCBatch.selected>();

			BatchFilter filter = Filter.Current;
			Batches.SetProcessDelegate(batches => ImportTransactions(batches, filter));

			Batches.SetProcessCaption(Messages.ImportTransactions);
			Batches.SetProcessAllCaption(Messages.ImportAllTransactions);

			Batches.SetProcessAllVisible(false);
			Batches.SetProcessVisible(false);
		}
		#endregion

		#region Import Implementation
		private static CCPaymentProcessing CreatePaymentProcessing(PXGraph contextGraph)
		{
			return new CCPaymentProcessing(contextGraph);
		}

		#region Import Batches
		private IEnumerable DoImportBatches(PXAdapter adapter, BatchFilter filter)
		{
			var processingCenterID = filter.ProcessingCenterID;
			var processingCenter = CCProcessingCenter.PK.Find(this, processingCenterID);
			if (processingCenter?.IsActive != true || processingCenter?.ImportSettlementBatches != true)
				throw new PXException(Messages.MakeSureProcessingCenterIsActiveAndImportSettlementBatches, processingCenterID);

			DateTime utcNow = DateTime.UtcNow;
			DateTime? firstImportDate = filter.FirstImportDateUTC;
			if (firstImportDate == null)
				throw new PXException(Messages.SetImportStartDateForTheProcessingCenter, processingCenterID);
			firstImportDate = DateTime.SpecifyKind(firstImportDate.Value, DateTimeKind.Utc);

			DateTime lastImportDate = DateTime.SpecifyKind(filter.ImportThroughDateUTC ?? utcNow, DateTimeKind.Utc);

			ImportBatchesForPeriod(processingCenterID, firstImportDate.Value, lastImportDate);

			Batches.View.Cache.Clear();
			Batches.View.Cache.ClearQueryCache();

			return adapter.Get();
		}

		private bool ImportBatchesForPeriod(string processingCenterID, DateTime firstImportDate, DateTime lastImportDate)
		{
			bool batchImported = false;
			var graph = PXGraph.CreateInstance<CCBatchMaint>();
			foreach (var batchData in GetBatches(processingCenterID, firstImportDate, lastImportDate))
			{
				using (var scope = new PXTransactionScope())
				{
					if (GetCCBatchByExtBatchId(graph, batchData.BatchId) != null)
						continue;

					var ccBatch = TransformSettledBatch(processingCenterID, batchData);
					ccBatch = graph.BatchView.Insert(ccBatch);

					foreach (var statisticsData in batchData.Statistics)
					{
						var ccBatchStatistics = TransformBatchStatistics(ccBatch.BatchID, statisticsData);
						graph.CardTypeSummary.Insert(ccBatchStatistics);
					}
					batchImported = true;
					graph.Actions.PressSave();
					graph.Clear();
					scope.Complete();
				}
			}

			return batchImported;
		}

		private IEnumerable<BatchData> GetBatches(string processingCenterID, DateTime firstSettlementDate, DateTime lastSettlementDate)
		{
			// Authorize.Net does not support requests for periods longer than 31 days.
			const int maxDateRangeLength = 31;
			return new DateRange(firstSettlementDate, lastSettlementDate)
				.Split(maxDateRangeLength)
				.SelectMany(dateRange => GetBatchesForSinglePeriod(processingCenterID, dateRange.StartDate, dateRange.EndDate));
		}

		private IEnumerable<BatchData> GetBatchesForSinglePeriod(string processingCenterID, DateTime firstSettlementDate, DateTime lastSettlementDate)
		{
			return CreatePaymentProcessing(this).GetSettledBatches(processingCenterID, new BatchSearchParams
			{
				FirstSettlementDate = firstSettlementDate,
				LastSettlementDate = lastSettlementDate,
				IncludeStatistics = true
			});
		}

		private static DateTime? ShiftDateByDefaultImportPeriod(DateTime? firstDate)
		{
			const int importThroughOffset = 7;
			return firstDate?.AddDays(importThroughOffset);
		}

		private static DateTime? MinDate(DateTime? dateA, DateTime? dateB)
		{
			return dateB == null || dateA < dateB
				? dateA
				: dateB;
		}

		private static DateTime? MaxDate(DateTime? dateA, DateTime? dateB)
		{
			return dateB == null || dateA > dateB
				? dateA
				: dateB;
		}
		#endregion Import Batches

		#region Import Transactions
		private static void ImportTransactions(IEnumerable<CCBatch> batches, BatchFilter filter)
		{
			if (PXContext.GetSlot<bool>(PXProcessing.ScheduleIsRunning))
			{
				var enqGraph = PXGraph.CreateInstance<CCBatchEnq>();
				PXLongOperation.StartOperation(enqGraph, delegate ()
				{
					string processingCenterID = filter.ProcessingCenterID;
					enqGraph.CheckProcessRunning(processingCenterID);
					PXLongOperation.SetCustomInfo(processingCenterID, ProcessingCenterToken);

					enqGraph.DoImportBatches(new PXAdapter(enqGraph.Batches), filter);
					batches = SelectBatches(enqGraph, processingCenterID, CCBatchStatusCode.PendingImport, CCBatchStatusCode.PendingProcessing, CCBatchStatusCode.Processing).RowCast<CCBatch>();

					DoImportTransactions(batches);
				});
			}
			else
			{
				DoImportTransactions(batches);
			}
		}

		private static void DoImportTransactions(IEnumerable<CCBatch> batches)
		{
			bool failed = false;
			var graph = PXGraph.CreateInstance<CCBatchMaint>();
			foreach (CCBatch batch in batches.OrderBy(batch => batch.SettlementTimeUTC))
			{
				PXProcessing.SetCurrentItem(batch);
				graph.BatchView.Current = batch;

				if (batch.Status == CCBatchStatusCode.PendingImport)
				{
					bool importSucceeded = TryImportTransactionsFromProcCenter(graph);
					if (!importSucceeded)
					{
						failed = true;
						continue;
					}
				}

				bool processingSucceeded = TryProcessImportedTransactions(graph);
				if (processingSucceeded)
				{
					PXProcessing.SetProcessed();
				}
				else
				{
					failed = true;
				}

				// The process has to wait on the last record to make sure the processing screen appears even for short processes
				if (PX.Common.PXContext.GetSlot<int>(NeedToWaitOnBatchToken) == batch.BatchID)
				{
					System.Threading.Thread.Sleep(LongRunUpdateSpan);
				}
			}
			if (failed)
			{
				throw new PXOperationCompletedWithErrorException(ErrorMessages.SeveralItemsFailed);
			}
		}

		private static bool TryImportTransactionsFromProcCenter(CCBatchMaint graph)
		{
			try
			{
				SetBatchStatus(graph, CCBatchStatusCode.Processing);

				CCBatch batch = graph.BatchView.Current;
				var transactions = CreatePaymentProcessing(graph).GetTransactionsByBatch(batch.ExtBatchID, batch.ProcessingCenterID);
				using (var scope = new PXTransactionScope())
				{
					foreach (var transaction in transactions)
					{
						if (CCBatchTransaction.PK.Find(graph, batch.BatchID, transaction.TranID) != null)
							continue;
						var ccBatchTran = TransformBatchTransaction(batch.BatchID, transaction);
						graph.Transactions.Insert(ccBatchTran);
					}

					graph.Actions.PressSave();
					scope.Complete();
				}
				return true;
			}
			catch (Exception e)
			{
				SetBatchStatus(graph, CCBatchStatusCode.PendingImport);
				PXProcessing.SetError(e);
				return false;
			}
		}

		private static bool TryProcessImportedTransactions(CCBatchMaint graph)
		{
			try
			{
				SetBatchStatus(graph, CCBatchStatusCode.Processing);
				bool fullyProcessed = graph.MatchTransactions();

				if (fullyProcessed)
				{
					SetBatchStatus(graph, CCBatchStatusCode.Processed);

					if (graph.ProcessingCenter.SelectSingle()?.AutoCreateBankDeposit == true)
					{
						return TryCreateDeposit(graph);
					}
				}
				else
				{
					SetBatchStatus(graph, CCBatchStatusCode.PendingReview);
				}
				return true;
			}
			catch (Exception e)
			{
				SetBatchStatus(graph, CCBatchStatusCode.PendingProcessing);
				PXProcessing.SetError(e);
				return false;
			}
		}

		private static bool TryCreateDeposit(CCBatchMaint graph)
		{
			try
			{
				var adapter = new PXAdapter(graph.BatchView)
				{
					MassProcess = true
				};
				graph.createDeposit.PressButton(adapter);
				return true;
			}
			catch (Exception e)
			{
				SetBatchStatus(graph, CCBatchStatusCode.Processed);
				PXProcessing.SetError(e);
				return false;
			}
		}

		private CCBatch GetCCBatchByExtBatchId(PXGraph graph, string extBatchID)
		{
			return PXSelect<CCBatch, Where<CCBatch.extBatchID, Equal<Required<CCBatch.extBatchID>>>>
				.Select(graph, extBatchID);
		}

		private static PXResultset<CCBatch> SelectBatches(PXGraph graph, string processingCenterID, params string[] statuses)
		{
			return PXSelect<CCBatch, Where<CCBatch.processingCenterID, Equal<Required<CCBatch.processingCenterID>>,
				And<CCBatch.status, In<Required<CCBatch.status>>>>, OrderBy<Asc<CCBatch.settlementTimeUTC>>>.Select(graph, processingCenterID, statuses);
		}

		private static void SetBatchStatus(CCBatchMaint graph, string newStatus)
		{
			using (var scope = new PXTransactionScope())
			{
				CCBatch batch = graph.BatchView.Current;
				if (batch == null)
					return;

				batch.Status = newStatus;
				graph.BatchView.Update(batch);
				graph.Actions.PressSave();
				scope.Complete();
			}
		}
		#endregion Import Transactions

		#region Data Transformation
		private static CCBatch TransformSettledBatch(string processingCenterID, BatchData batchData)
		{
			return new CCBatch
			{
				ExtBatchID = batchData.BatchId,
				Status = CCBatchStatusCode.PendingImport,
				ProcessingCenterID = processingCenterID,
				SettlementTimeUTC = batchData.SettlementTimeUTC,
				SettlementState = CCBatchSettlementState.GetCode(batchData.SettlementState)
			};
		}

		private static CCBatchStatistics TransformBatchStatistics(int? batchID, BatchStatisticsData statData)
		{
			return new CCBatchStatistics
			{
				BatchID = batchID,
				CardType = statData.CardType,
				DeclineCount = statData.DeclineCount,
				ErrorCount = statData.ErrorCount,
				RefundAmount = statData.RefundAmount,
				RefundCount = statData.RefundCount,
				SettledAmount = statData.SettledAmount,
				SettledCount = statData.SettledCount,
				VoidCount = statData.VoidCount
			};
		}

		private static CCBatchTransaction TransformBatchTransaction(int? batchID, TransactionData tranData)
		{
			return new CCBatchTransaction
			{
				BatchID = batchID,
				PCTranNumber = tranData.TranID,
				PCCustomerID = tranData.CustomerId,
				PCPaymentProfileID = tranData.PaymentId,
				SettlementStatus = CCBatchTranSettlementStatusCode.GetCode(tranData.TranStatus),
				InvoiceNbr = tranData.DocNum,
				SubmitTime = tranData.SubmitTime,
				CardType = tranData.CardType,
				AccountNumber = tranData.AccountNumber,
				Amount = tranData.Amount,
				FixedFee = tranData.FixedFee,
				PercentageFee = tranData.PercentageFee,
				FeeType = tranData.FeeType,
				ProcessingStatus = CCBatchTranProcessingStatusCode.PendingProcessing
			};
		}
		#endregion Data Transformation
		#endregion Import Implementation
	}
}
