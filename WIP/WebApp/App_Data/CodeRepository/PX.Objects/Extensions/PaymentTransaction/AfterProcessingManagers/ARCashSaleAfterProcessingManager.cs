using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects;
using PX.Objects.AR;
using PX.Objects.AR.CCPaymentProcessing.Common;
using PX.Objects.AR.CCPaymentProcessing.Helpers;
using PX.Objects.AR.CCPaymentProcessing.Repositories;
using PX.Objects.AR.CCPaymentProcessing.Interfaces;
using PX.Objects.AR.CCPaymentProcessing;
using PX.Objects.AR.Standalone;
using PX.Objects.CA;

namespace PX.Objects.Extensions.PaymentTransaction
{
	public class ARCashSaleAfterProcessingManager : AfterProcessingManager
	{
		public bool ReleaseDoc { get; set; }

		public ARCashSaleEntry Graph { get; set; }

		private ARCashSaleEntry graphWithOriginDoc;

		private IBqlTable inputTable;

		public override void RunAuthorizeActions(IBqlTable table, bool success)
		{
			inputTable = table;
			ARCashSaleEntry graph = CreateGraphIfNeeded(table);
			ChangeDocProcessingStatus(graph, CCTranType.AuthorizeOnly, success);
			if (success)
			{
				ARCashSale cashSale = Graph.Document.Current;
				UpdateCCBatch(graph, cashSale);
			}

			RestoreCopy();
		}

		public override void RunCaptureActions(IBqlTable table, bool success)
		{
			RunCaptureActions(table, CCTranType.AuthorizeAndCapture, success);
		}

		public override void RunPriorAuthorizedCaptureActions(IBqlTable table, bool success)
		{
			RunCaptureActions(table, CCTranType.PriorAuthorizedCapture, success);
		}

		public override void RunVoidActions(IBqlTable table, bool success)
		{
			inputTable = table;
			CCTranType tranType = CCTranType.VoidOrCredit;
			ARCashSaleEntry graph = CreateGraphIfNeeded(table);
			ChangeDocProcessingStatus(graph, tranType, success);
			UpdateCashSale(graph, tranType, success);
			ARCashSale cashSale = Graph.Document.Current;
			if (ReleaseDoc = cashSale.VoidAppl == true && NeedRelease(cashSale))
			{
				ReleaseDocument(graph, tranType, success);
			}
			if (success)
			{
				UpdateCCBatch(graph, cashSale);
			}

			RestoreCopy();
		}

		public override void RunCreditActions(IBqlTable table,  bool success)
		{
			inputTable = table;
			CCTranType type = CCTranType.VoidOrCredit;
			ARCashSaleEntry graph = CreateGraphIfNeeded(table);
			ChangeDocProcessingStatus(graph, type, success);
			UpdateCashSale(graph, type, success);
			ARCashSale cashSale = Graph.Document.Current;
			if (NeedRelease(cashSale))
			{
				ReleaseDocument(graph, type, success);
			}
			if (success)
			{
				UpdateCCBatch(graph, cashSale);
			}

			RestoreCopy();
		}

		private void RunCaptureActions(IBqlTable table, CCTranType tranType, bool success)
		{
			inputTable = table;
			ARCashSaleEntry graph = CreateGraphIfNeeded(table);
			ChangeDocProcessingStatus(graph, tranType, success);
			UpdateCashSale(graph, tranType, success);
			if (success)
			{
				UpdateDocCleared(graph);
			}

			ARCashSale cashSale = Graph.Document.Current;
			if (NeedRelease(cashSale))
			{
				ReleaseDocument(graph, tranType, success);
			}
			if (success)
			{
				UpdateCCBatch(graph, cashSale);
			}

			RestoreCopy();
		}

		private void UpdateCCBatch(ARCashSaleEntry arGraph, ARCashSale doc)
		{
			ExternalTransaction currTran = arGraph.ExternalTran.SelectSingle();
			ExternalTransactionState state = ExternalTranHelper.GetTransactionState(arGraph, currTran);
			if (state.NeedSync || state.SyncFailed) return;

			foreach (PXResult<CCBatch, CCBatchTransaction> result in PXSelectJoin<CCBatch,
									InnerJoin<CCBatchTransaction, On<CCBatch.batchID, Equal<CCBatchTransaction.batchID>>>,
									Where<CCBatch.processingCenterID, Equal<Required<CCBatch.processingCenterID>>,
										And<CCBatchTransaction.pCTranNumber, Equal<Required<CCBatchTransaction.pCTranNumber>>,
										And<CCBatchTransaction.processingStatus, In3<CCBatchTranProcessingStatusCode.missing, CCBatchTranProcessingStatusCode.pendingProcessing>>>>>
									.SelectSingleBound(arGraph, null, currTran.ProcessingCenterID, currTran.TranNumber))
			{
				CCBatch batch = (CCBatch)result;
				CCBatchTransaction batchTran = (CCBatchTransaction)result;
				if (batch != null && batchTran != null)
				{
					var batchGraph = PXGraph.CreateInstance<CCBatchMaint>();
					batchGraph.BatchView.Current = batch;
					batchGraph.Transactions.Select();

					batchTran.TransactionID = currTran.TransactionID;
					batchTran.OriginalStatus = currTran.ProcStatus;
					batchTran.DocType = doc.DocType;
					batchTran.RefNbr = doc.RefNbr;
					batchTran.CurrentStatus = batchTran.OriginalStatus;
					batchTran.ProcessingStatus = CCBatchTranProcessingStatusCode.Processed;
					batchTran = batchGraph.Transactions.Update(batchTran);

					bool batchIsProcessed = true;
					foreach (CCBatchTransaction tran in batchGraph.Transactions.Select())
					{
						if (tran.ProcessingStatus != CCBatchTranProcessingStatusCode.Processed && tran.ProcessingStatus != CCBatchTranProcessingStatusCode.Hidden)
						{
							batchIsProcessed = false;
							break;
						}
					}

					if (batchIsProcessed)
					{
						batch.Status = CCBatchStatusCode.Processed;
					}
					batchGraph.BatchView.UpdateCurrent();
					batchGraph.Save.Press();

					CCBatchMaint.StatusMatchingResult matchingResult = CCBatchMaint.MatchStatuses(batchTran.SettlementStatus, currTran, doc, arGraph);
					if (matchingResult == CCBatchMaint.StatusMatchingResult.SuccessMatch)
					{
						currTran.Settled = true;
						arGraph.ExternalTran.Update(currTran);

						doc.Settled = true;
						doc.Cleared = true;
						doc.ClearDate = batch.SettlementTime;
						arGraph.Caches[typeof(ARCashSale)].Update(doc);
						arGraph.Save.Press();
					}

					arGraph.Actions.PressSave();
				}
			}
		}

		private bool NeedRelease(ARCashSale cashSale)
		{
			return ReleaseDoc && cashSale.Released == false && Graph.arsetup.Current.IntegratedCCProcessing == true;
		}

		public void UpdateCashSale(ARCashSaleEntry graph, CCTranType tranType, bool success)
		{
			if (!success)
				return;
			ARCashSaleEntry cashSaleGraph = graph as ARCashSaleEntry;
			ARCashSale toProc = (ARCashSale)cashSaleGraph.Document.Current;
			IExternalTransaction currTran = cashSaleGraph.ExternalTran.SelectSingle();
			if (currTran != null)
			{
				toProc.DocDate = currTran.LastActivityDate.Value.Date;
				toProc.AdjDate = currTran.LastActivityDate.Value.Date;

				ExternalTransactionState tranState = ExternalTranHelper.GetTransactionState(cashSaleGraph, currTran);
				if (tranState.IsActive)
				{
					toProc.ExtRefNbr = currTran.TranNumber;
				}
				else if (toProc.DocType != ARDocType.CashReturn && (tranState.IsVoided || tranState.IsDeclined))
				{
					toProc.ExtRefNbr = null;
				}

				cashSaleGraph.Document.Update(toProc);
			}
		}

		private void UpdateDocCleared(ARCashSaleEntry graph)
		{
			IExternalTransaction currTran = graph.ExternalTran.SelectSingle();
			ExternalTransactionState state = ExternalTranHelper.GetTransactionState(graph, currTran);

			if (state?.IsCaptured == true)
			{
				ARCashSale doc = graph.Document.Current;

				var processingCenter = CCProcessingCenter.PK.Find(graph, doc.ProcessingCenterID);
				if (processingCenter.ImportSettlementBatches == false)
				{
					doc.Cleared = true;
					doc.ClearDate = currTran.LastActivityDate.Value.Date;
				}
			}
		}

		public void ReleaseDocument(ARCashSaleEntry cashSaleGraph, CCTranType procTran, bool success) 
		{
			var doc = cashSaleGraph.Document.Current;
			if (doc != null && success)
			{
				var tran = cashSaleGraph.ExternalTran.SelectSingle(doc.DocType, doc.RefNbr);
				if (tran != null)
				{
					ExternalTransactionState state = ExternalTranHelper.GetTransactionState(cashSaleGraph, tran);
					if (!state.IsDeclined && !state.IsOpenForReview)
					{
						PersistData();
						doc = cashSaleGraph.Document.Current;
						PaymentTransactionGraph<ARCashSaleEntry, ARCashSale>.ReleaseARDocument(doc);
					}
				}
			}
		}

		private void ChangeDocProcessingStatus(ARCashSaleEntry cashSaleGraph, CCTranType tranType, bool success)
		{
			ARCashSale cashSale = cashSaleGraph.Document.Current; 
			var extTran = cashSaleGraph.ExternalTran.SelectSingle();
			if (extTran == null) return;

			DeactivateExpiredTrans(cashSaleGraph);
			ExternalTransactionState state = ExternalTranHelper.GetTransactionState(cashSaleGraph, extTran);
			ChangeCaptureFailedFlag(state, cashSale);
			ChangeUserAttentionFlag(state, cashSaleGraph);
			if (success)
			{
				bool pendingProcessing = true;
				if (extTran != null)
				{
					if (state.IsCaptured && !state.IsOpenForReview)
					{
						pendingProcessing = false;
					}
					if (cashSale.DocType == ARDocType.CashReturn && (state.IsVoided || state.IsRefunded)
						&& !state.IsOpenForReview)
					{
						pendingProcessing = false;
					}
					ChangeDocProcessingFlags(state, cashSale, tranType);
				}
				cashSale.PendingProcessing = pendingProcessing;
			}
			ChangeOriginDocProcessingStatus(cashSaleGraph, tranType, success);
			cashSale = SyncActualExternalTransation(cashSaleGraph, cashSale);
			cashSaleGraph.Document.Update(cashSale);
		}

		private void DeactivateExpiredTrans(ARCashSaleEntry graph)
		{
			var cache = graph.Document.Cache;
			var res = graph.ExternalTran.Select().RowCast<ExternalTransaction>()
				.Where(i => ExternalTranHelper.IsExpired(i) && i.Active == true);
			foreach (var item in res)
			{
				item.Active = false;
				item.ProcStatus = ExtTransactionProcStatusCode.AuthorizeExpired;
				graph.ExternalTran.Update(item);
			}
		}

		private void ChangeUserAttentionFlag(ExternalTransactionState state, ARCashSaleEntry graph)
		{
			ARCashSale doc = graph.Document.Current;
			bool newVal = false;
			if (state.IsOpenForReview || doc.IsCCCaptureFailed == true 
				|| state.ProcessingStatus == ProcessingStatus.VoidFail || state.ProcessingStatus == ProcessingStatus.VoidDecline)
			{
				newVal = true;
			}
			doc.IsCCUserAttention = newVal;
		}

		private void ChangeOriginDocProcessingStatus(ARCashSaleEntry cashSaleGraph, CCTranType tranType, bool success)
		{
			IExternalTransaction tran = cashSaleGraph.ExternalTran.SelectSingle();
			ARCashSale cashSale = cashSaleGraph.Document.Current;
			ExternalTransactionState tranState = ExternalTranHelper.GetTransactionState(cashSaleGraph, tran);
			var oCashSaleGraph = GetGraphWithOriginDoc(cashSaleGraph, tranType);
			if (oCashSaleGraph != null)
			{
				var oExtTran = oCashSaleGraph.ExternalTran.SelectSingle();
				ARCashSale oCashSale = oCashSaleGraph.Document.Current;
				if (oExtTran.TransactionID == tran.TransactionID)
				{
					ChangeCaptureFailedFlag(tranState, oCashSale);
					if (success)
					{
						ChangeDocProcessingFlags(tranState, oCashSale, tranType);
					}
				}
				cashSaleGraph.Caches[typeof(ARCashSale)].Update(oCashSale);
			}
		}

		private void ChangeDocProcessingFlags(ExternalTransactionState tranState, ARCashSale doc, CCTranType tranType)
		{
			if (tranState.HasErrors) return;
			doc.IsCCAuthorized = doc.IsCCCaptured = doc.IsCCRefunded = false;
			if (!tranState.IsDeclined && !tranState.IsOpenForReview && !ExternalTranHelper.IsExpired(tranState.ExternalTransaction))
			{
				switch (tranType)
				{
					case CCTranType.AuthorizeAndCapture: doc.IsCCCaptured = true; break;
					case CCTranType.CaptureOnly: doc.IsCCCaptured = true; break;
					case CCTranType.PriorAuthorizedCapture: doc.IsCCCaptured = true; break;
					case CCTranType.AuthorizeOnly: doc.IsCCAuthorized = true; break;
					case CCTranType.Credit: doc.IsCCRefunded = true; break;
				}
				if (tranType == CCTranType.VoidOrCredit && tranState.IsRefunded)
				{
					doc.IsCCRefunded = true;
				}
			}
		}

		private void ChangeCaptureFailedFlag(ExternalTransactionState state, ARCashSale doc)
		{
			if (doc.IsCCCaptureFailed == false
				&& (state.ProcessingStatus == ProcessingStatus.CaptureFail || state.ProcessingStatus == ProcessingStatus.CaptureDecline))
			{
				doc.IsCCCaptureFailed = true;
			}
			else if (doc.IsCCCaptureFailed == true && (state.IsCaptured || state.IsVoided 
				|| (state.IsPreAuthorized && !state.HasErrors && !CheckCaptureFailedExists(state))))
			{
				doc.IsCCCaptureFailed = false;
			}
		}

		private bool CheckCaptureFailedExists(ExternalTransactionState state)
		{
			bool ret = false;
			var repo = GetPaymentProcessingRepository();
			IEnumerable<CCProcTran> procTrans = repo.GetCCProcTranByTranID(state.ExternalTransaction.TransactionID);
			if (CCProcTranHelper.HasCaptureFailed(procTrans))
			{
				ret = true;
			}
			return ret;
		}

		private ARCashSaleEntry GetGraphByDocTypeRefNbr(string docType, string refNbr)
		{
			var cashSaleGraph = PXGraph.CreateInstance<ARCashSaleEntry>();
			cashSaleGraph.Document.Current = PXSelect<ARCashSale, Where<ARCashSale.docType, Equal<Required<ARCashSale.docType>>,
				And<ARCashSale.refNbr, Equal<Required<ARCashSale.refNbr>>>>>
					.SelectWindowed(cashSaleGraph, 0, 1, docType, refNbr);
			return cashSaleGraph;
		}

		public override void PersistData()
		{
			ARCashSale doc = Graph?.Document.Current;
			if (doc != null)
			{
				PXEntryStatus status = Graph.Document.Cache.GetStatus(doc);
				if (status != PXEntryStatus.Notchanged)
				{
					Graph.Save.Press();
				}
			}
			RestoreCopy();
		}

		private ARCashSaleEntry GetGraphWithOriginDoc(ARCashSaleEntry graph, CCTranType tranType)
		{
			if (graphWithOriginDoc != null)
			{
				return graphWithOriginDoc;
			}

			IExternalTransaction tran = graph.ExternalTran.SelectSingle();
			ARCashSale cashSale = graph.Document.Current;
			ExternalTransactionState tranState = ExternalTranHelper.GetTransactionState(graph, tran);
			if (tranType == CCTranType.VoidOrCredit
				&& tran.DocType == cashSale.OrigDocType && tran.RefNbr == cashSale.OrigRefNbr
				&& tran.DocType == ARDocType.CashSale)
			{
				graphWithOriginDoc = GetGraphByDocTypeRefNbr(tran.DocType, tran.RefNbr);
			}
			return graphWithOriginDoc;
		}

		protected virtual ARCashSaleEntry CreateGraphIfNeeded(IBqlTable table)
		{
			if (Graph == null)
			{
				ARCashSale cashSale = table as ARCashSale;
				Graph = GetGraphByDocTypeRefNbr(cashSale.DocType, cashSale.RefNbr);
				Graph.Document.Update(cashSale);
			}
			return Graph;
		}

		protected virtual ICCPaymentProcessingRepository GetPaymentProcessingRepository()
		{
			ICCPaymentProcessingRepository repository = new CCPaymentProcessingRepository(Graph);
			return repository;
		}

		public override PXGraph GetGraph()
		{
			return Graph;
		}

		private void RestoreCopy()
		{
			ARCashSale doc = Graph?.Document.Current;
			if (doc != null && inputTable != null)
			{
				Graph.Document.Cache.RestoreCopy(inputTable, doc);
			}
		}
		
		private ARCashSale SyncActualExternalTransation(ARCashSaleEntry cashSaleGraph, ARCashSale cashSale)
		{
			ExternalTransaction lastExtTran = cashSaleGraph.ExternalTran.SelectSingle();

			if (cashSale.CCActualExternalTransactionID == null)
			{
				cashSale.CCActualExternalTransactionID = lastExtTran.TransactionID;
				return cashSale;
			}

			if (lastExtTran.TransactionID > cashSale.CCActualExternalTransactionID)
			{
				ExternalTransaction currentActualExtTran = cashSaleGraph.ExternalTran
					.Select()
					.SingleOrDefault(t =>
						((ExternalTransaction)t).TransactionID == cashSale.CCActualExternalTransactionID);

				if(lastExtTran == null || currentActualExtTran == null) return cashSale;

				ExternalTransactionState stateOfLastExtTran = ExternalTranHelper.GetTransactionState(cashSaleGraph, lastExtTran);
				ExternalTransactionState stateOfActualExtTran = ExternalTranHelper.GetTransactionState(cashSaleGraph, currentActualExtTran);

				if (!stateOfActualExtTran.IsActive || stateOfLastExtTran.IsActive)
				{
					cashSale.CCActualExternalTransactionID = lastExtTran.TransactionID;
				}
			}

			return cashSale;
		}
	}
}
