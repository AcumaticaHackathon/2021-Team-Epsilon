using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects;
using PX.Objects.AR;
using PX.Objects.AR.CCPaymentProcessing.Common;
using PX.Objects.AR.CCPaymentProcessing.Helpers;
using PX.Objects.AR.CCPaymentProcessing.Interfaces;
using PX.Objects.AR.CCPaymentProcessing;
using PX.Objects.AR.CCPaymentProcessing.Repositories;
using PX.Objects.CA;
using PX.Objects.SO;

namespace PX.Objects.Extensions.PaymentTransaction
{
	public class ARPaymentAfterProcessingManager : AfterProcessingManager
	{
		public bool ReleaseDoc { get; set; }
		public bool RaisedVoidForReAuthorization { get; set; }
		public bool NeedSyncContext { get; set; }
		public ARPaymentEntry Graph { get; set; }

		private ARPaymentEntry graphWithOriginDoc;

		private IBqlTable inputTable;

		private bool importDeactivatedTran;

		public override void RunAuthorizeActions(IBqlTable table, bool success)
		{
			inputTable = table;
			CCTranType tranType = CCTranType.AuthorizeOnly;
			ARPaymentEntry graph = CreateGraphIfNeeded(table);
			ChangeDocProcessingStatus(graph, tranType, success);
			if (this.RaisedVoidForReAuthorization)
			{
				UpdateDocReAuthFieldsAfterValidationByVoidForReAuth(graph);
			}
			else
			{
				UpdateDocReAuthFieldsAfterAuthorize(graph);
			}

			UpdateARPayment(graph, tranType, success);
			if (success)
			{
				ARPayment doc = Graph.Document.Current;
				UpdateCCBatch(graph, doc);
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
			ARPaymentEntry graph = CreateGraphIfNeeded(table);
			if (CheckImportDeactivatedTran(graph)) return;
			ARPayment doc = graph.Document.Current;
			if (!CreateVoidDocument(graph, success))
			{
				ChangeDocProcessingStatus(graph, tranType, success);
				if (this.RaisedVoidForReAuthorization)
				{
					UpdateDocReAuthFieldsAfterVoidForReAuth(graph);
				}
				else
				{
					UpdateDocReAuthFieldsAfterVoid(graph);
				}

				UpdateARPayment(graph, tranType, success);
				if (ReleaseDoc && NeedRelease(doc) && (ARPaymentType.VoidAppl(doc.DocType) == true
					|| doc.DocType == ARDocType.Refund))
				{
					ReleaseDocument(graph, tranType, success);
				}
				else
				{
					VoidOriginalPayment(graph, success);
				}
			}
			if (success)
			{
				UpdateCCBatch(graph, doc);
			}

			RestoreCopy();
		}

		public override void RunCreditActions(IBqlTable table, bool success)
		{
			inputTable = table;
			CCTranType type = CCTranType.VoidOrCredit;
			ARPaymentEntry graph = CreateGraphIfNeeded(table);
			if (CheckImportDeactivatedTran(graph)) return;
			bool createVoidDoc = CreateVoidDocument(graph, success);
			ARPayment doc = Graph.Document.Current;
			if (!createVoidDoc)
			{
				ChangeDocProcessingStatus(graph, type, success);
				UpdateARPaymentAndSetWarning(graph, type, success);
				if (ReleaseDoc && NeedRelease(doc))
				{
					ReleaseDocument(graph, type, success);
				}
			}

			if (success)
			{
				UpdateCCBatch(graph, doc);
			}
			RestoreCopy();
		}

		public override void RunCaptureOnlyActions(IBqlTable table, bool success)
		{
			inputTable = table;
			CCTranType tranType = CCTranType.CaptureOnly;
			ARPaymentEntry graph = CreateGraphIfNeeded(table);
			ChangeDocProcessingStatus(graph, tranType, success);
			UpdateDocReAuthFieldsAfterCapture(graph);
			UpdateExtRefNbrARPayment(graph, tranType, success);
			ARPayment doc = Graph.Document.Current;
			if (ReleaseDoc && NeedRelease(doc) && doc.DocType != ARDocType.Refund)
			{
				ReleaseDocument(graph, tranType, success);
			}
			if (success)
			{
				UpdateCCBatch(graph, doc);
			}

			RestoreCopy();
		}

		public override void RunUnknownActions(IBqlTable table, bool success)
		{
			inputTable = table;
			ARPaymentEntry graph = CreateGraphIfNeeded(table);
			if (CheckImportDeactivatedTran(graph)) return;
			SyncActualExternalTransation(graph, graph.Document.Current);
			ChangeUserAttentionFlag(graph);
			UpdateARPayment(graph, CCTranType.Unknown, success);
			ARPayment payment = graph.Document.Current;
			graph.Document.Update(payment);
			RestoreCopy();
		}

		public bool NeedReleaseForCapture(ARPayment doc)
		{
			bool ret = false;
			try
			{
				ARPaymentEntry.CheckValidPeriodForCCTran(Graph, doc);
				ret = NeedRelease(doc) && doc.DocType != ARDocType.Refund;
			}
			catch { }
			return ret;
		}

		private void RunCaptureActions(IBqlTable table, CCTranType tranType, bool success)
		{
			inputTable = table;
			ARPaymentEntry graph = CreateGraphIfNeeded(table);
			ChangeDocProcessingStatus(graph, tranType, success);
			UpdateDocReAuthFieldsAfterCapture(graph);
			UpdateARPaymentAndSetWarning(graph, tranType, success);
			UpdatePaymentAmountIfNeeded(graph);
			if (success)
			{
				UpdateDocCleared(graph);
			}

			ARPayment doc = graph.Document.Current;
			if (ReleaseDoc && NeedReleaseForCapture(doc))
			{
				ReleaseDocument(graph, tranType, success);
			}
			if (IsMassProcess)
			{
				CheckForHeldForReviewStatusAfterProc(graph, tranType, success);
			}
			if (success)
			{
				UpdateCCBatch(graph, doc);
			}

			RestoreCopy();
		}

		private void UpdateCCBatch(ARPaymentEntry arGraph, ARPayment doc)
		{
			ExternalTransaction currTran = GetLastProcessedExternalTran(arGraph);
			ExternalTransactionState state = ExternalTranHelper.GetTransactionState(arGraph, currTran);
			if (state.NeedSync || state.SyncFailed) return;

			foreach (PXResult<CCBatch, CCBatchTransaction> result in PXSelectJoin<CCBatch,
									InnerJoin<CCBatchTransaction, On<CCBatch.batchID, Equal<CCBatchTransaction.batchID>>>,
									Where<CCBatch.processingCenterID, Equal<Required<CCBatch.processingCenterID>>,
										And<CCBatchTransaction.pCTranNumber, Equal<Required<CCBatchTransaction.pCTranNumber>>,
										And<CCBatchTransaction.processingStatus, In3<CCBatchTranProcessingStatusCode.missing, CCBatchTranProcessingStatusCode.pendingProcessing>>>>>
									.SelectSingleBound(arGraph, null, currTran.ProcessingCenterID, currTran.TranNumber))
			{
				CCBatch batch = result;
				CCBatchTransaction batchTran = result;
				if (batch != null && batchTran != null)
				{
					batchTran.TransactionID = currTran.TransactionID;
					batchTran.OriginalStatus = currTran.ProcStatus;
					batchTran.DocType = doc.DocType;
					batchTran.RefNbr = doc.RefNbr;
					batchTran.CurrentStatus = batchTran.OriginalStatus;
					batchTran.ProcessingStatus = CCBatchTranProcessingStatusCode.Processed;
					batchTran = arGraph.BatchTran.Update(batchTran);

					var batchGraph = PXGraph.CreateInstance<CCBatchMaint>();
					batchGraph.BatchView.Current = batch;
					bool batchIsProcessed = true;
					foreach (CCBatchTransaction tran in batchGraph.Transactions.Select())
					{
						if (tran.PCTranNumber != batchTran.PCTranNumber
							&& tran.ProcessingStatus.IsNotIn(CCBatchTranProcessingStatusCode.Processed, CCBatchTranProcessingStatusCode.Hidden))
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
						arGraph.Caches[typeof(ARPayment)].Update(doc);
					}

					arGraph.Actions.PressSave();
				}
			}
		}

		private void UpdatePaymentAmountIfNeeded(ARPaymentEntry graph)
		{
			ARPayment doc = graph.Document.Current;
			ExternalTransaction extTran = GetLastProcessedExternalTran(graph);
			if (extTran == null) return;

			ExternalTransactionState state = ExternalTranHelper.GetTransactionState(graph, extTran);
			if (state.IsCaptured && extTran.Amount < doc.CuryOrigDocAmt && doc.Released == false)
			{
				bool res = ProcessAdjustments(graph, extTran);
				doc.CuryOrigDocAmt = extTran.Amount;
				if (!res)
				{
					doc.Hold = true;
				}
				graph.Document.Update(doc);
			}
		}

		private bool ProcessAdjustments(ARPaymentEntry graph, ExternalTransaction extTran)
		{
			PXResultset<SOAdjust> soAdjusts = graph.SOAdjustments.Select();
			PXResultset<ARAdjust> arAdjusts = graph.Adjustments.Select();
			bool soAdjUpdated = false;
			decimal? totalSum = 0;
			if (soAdjusts.Count > 1 || arAdjusts.Count > 1)
			{
				foreach (SOAdjust item in soAdjusts)
				{
					totalSum += item.CuryAdjgAmt;
				}
				foreach (ARAdjust item in arAdjusts)
				{
					totalSum += item.CuryAdjgAmt;
				}
				return extTran.Amount >= totalSum ? true : false;
			}

			SOAdjust soAdjust = soAdjusts; 
			ARAdjust arAdjust = arAdjusts;
			if (soAdjust != null && arAdjust != null && arAdjust.AdjdOrderType != soAdjust.AdjdOrderType
				&& arAdjust.AdjdOrderNbr != soAdjust.AdjdOrderNbr)
			{
				totalSum = soAdjust.CuryAdjgAmt + arAdjust.CuryAdjgAmt;
				return extTran.Amount >= totalSum ? true : false;
			}

			if (soAdjust != null)
			{
				if (soAdjust.CuryAdjgBilledAmt > 0 && soAdjust.CuryOrigAdjgAmt != soAdjust.CuryAdjgBilledAmt)
				{
					return false;
				}
				if(soAdjust.CuryAdjgAmt > extTran.Amount)
				{
					soAdjust.CuryAdjgAmt = extTran.Amount;
					graph.SOAdjustments.Update(soAdjust);
					soAdjUpdated = true;
				}
			}

			if (arAdjust != null && !soAdjUpdated)
			{
				if (arAdjust.CuryAdjgAmt > extTran.Amount)
				{
					arAdjust.CuryAdjgAmt = extTran.Amount;
					graph.Adjustments.Update(arAdjust);
				}
			}
			return true;
		}

		private bool NeedRelease(ARPayment doc)
		{
			return doc.Released == false && doc.Hold == false && Graph.arsetup.Current.IntegratedCCProcessing == true;
		}

		public void CheckForHeldForReviewStatusAfterProc(ARPaymentEntry paymentEntry, CCTranType procTran, bool success)
		{
			if (success)
			{
				var doc = paymentEntry.Document.Current;
				var query = new PXSelect<ExternalTransaction, Where<ExternalTransaction.docType, Equal<Required<ExternalTransaction.docType>>,
					And<ExternalTransaction.refNbr, Equal<Required<ExternalTransaction.refNbr>>>>, OrderBy<Desc<ExternalTransaction.transactionID>>>(paymentEntry);
				var result = query.Select(doc.DocType, doc.RefNbr);
				ExternalTransactionState state = ExternalTranHelper.GetActiveTransactionState(paymentEntry, result.RowCast<ExternalTransaction>());
				if (state.IsOpenForReview)
				{
					throw new PXSetPropertyException(AR.Messages.CCProcessingTranHeldWarning, PXErrorLevel.RowWarning);
				}
			}
		}

		public void ReleaseDocument(ARPaymentEntry paymentGraph, CCTranType tranType, bool success)
		{
			var doc = paymentGraph.Document.Current;
			if (doc != null && success)
			{
				var tran = GetLastProcessedExternalTran(paymentGraph);
				if (tran != null)
				{
					ExternalTransactionState state = ExternalTranHelper.GetTransactionState(paymentGraph, tran);

					if (!state.IsDeclined && !state.IsOpenForReview && !state.IsExpired && !state.SyncFailed && !state.NeedSync)
					{
						PersistData();
						doc = paymentGraph.Document.Current;
						PaymentTransactionGraph<ARPaymentEntry, ARPayment>.ReleaseARDocument(doc);
					}
				}
			}
		}

		public void UpdateARPaymentAndSetWarning(ARPaymentEntry paymentGraph, CCTranType tranType, bool success)
		{
			var toProc = paymentGraph.Document.Current;
			if (success && toProc.Released == false)
			{
				IExternalTransaction currTran = GetLastProcessedExternalTran(paymentGraph);
				ExternalTransactionState state = ExternalTranHelper.GetTransactionState(paymentGraph, currTran);

				if (currTran != null && currTran.TransactionID == toProc.CCActualExternalTransactionID)
				{
					if (state.IsActive)
					{
						if (toProc.AdjDate != null && !(PXLongOperation.GetCustomInfo() is PXProcessingInfo))
						{
							PXLongOperation.SetCustomInfo(new DocDateWarningDisplay(toProc.AdjDate.Value));
						}
						toProc.DocDate = currTran.LastActivityDate.Value.Date;
						toProc.AdjDate = currTran.LastActivityDate.Value.Date;
					}

					SetExtRefNbrValue(paymentGraph, toProc, currTran, state);

					paymentGraph.Document.Update(toProc);
				}
			}
		}

		public void UpdateARPayment(ARPaymentEntry paymentGraph, CCTranType tranType, bool success)
		{
			ARPayment toProc = paymentGraph.Document.Current;
			if (success && toProc.Released == false)
			{
				IExternalTransaction currTran = GetLastProcessedExternalTran(paymentGraph);
				if (currTran != null && currTran.TransactionID == toProc.CCActualExternalTransactionID)
				{
					ExternalTransactionState state = ExternalTranHelper.GetTransactionState(paymentGraph, currTran);
					toProc.DocDate = currTran.LastActivityDate.Value.Date;
					toProc.AdjDate = currTran.LastActivityDate.Value.Date;

					SetExtRefNbrValue(paymentGraph, toProc, currTran, state);

					paymentGraph.Document.Update(toProc);
				}
			}
		}

		public void ChangeDocProcessingStatus(ARPaymentEntry paymentGraph, CCTranType tranType, bool success)
		{
			var extTran = GetLastProcessedExternalTran(paymentGraph);
			ARPayment payment = paymentGraph.Document.Current;
			if (extTran == null) return;

			DeactivateExpiredTrans(paymentGraph);
			ExternalTransactionState state = ExternalTranHelper.GetTransactionState(paymentGraph, extTran);
			ChangeCaptureFailedFlag(state, payment);
			ChangeUserAttentionFlag(state, paymentGraph);
			payment = SyncActualExternalTransation(paymentGraph, payment);

			if (success)
			{
				bool pendingProcessing = true;
				bool syncNotNeeded = !state.IsOpenForReview && !state.NeedSync && !state.CreateProfile;
				if (PaymentDocType(payment)
					&& state.IsCaptured && syncNotNeeded)
				{
					pendingProcessing = false;
				}
				if ((payment.DocType == ARDocType.VoidPayment || payment.DocType == ARDocType.Refund)
					&& (state.IsVoided || state.IsRefunded) && syncNotNeeded)
				{
					pendingProcessing = false;
				}
				if (payment.Released == true && state.NeedSync)
				{
					pendingProcessing = false;
				}
				ChangeDocProcessingFlags(state, payment, tranType);
				payment.PendingProcessing = pendingProcessing;
			}
			ChangeOriginDocProcessingStatus(paymentGraph, tranType, success);
			paymentGraph.Document.Update(payment);
		}

		private bool CreateVoidDocument(ARPaymentEntry graph, bool success)
		{
			CCTranType type = CCTranType.VoidOrCredit;
			ARPayment doc = Graph.Document.Current;
			var extTran = GetLastProcessedExternalTran(graph);

			if (extTran == null || !NeedSyncContext || !success
				|| doc.Released == false || !PaymentDocType(doc)) return false;

			var state = ExternalTranHelper.GetTransactionState(graph, extTran);
			if (!state.IsRefunded && !state.IsVoided) return false;

			var newGraph = GetGraphByDocTypeRefNbr(doc.DocType, doc.RefNbr);
			var adapter = CreateAdapter(newGraph, doc);
			try
			{
				newGraph.VoidCheck(adapter);
			}
			catch (PXRedirectRequiredException)
			{
			}
			newGraph.Save.Press();
			var voidedDoc = newGraph.Document.Current;

			if (state.IsRefunded)
			{
				MoveTranToAnotherDoc(newGraph, extTran);
			}

			ChangeDocProcessingStatus(newGraph, type, true);
			UpdateARPaymentAndSetWarning(newGraph, type, true);
			if (ReleaseDoc && NeedRelease(voidedDoc) && ARPaymentType.VoidAppl(voidedDoc.DocType) == true)
			{
				ReleaseDocument(newGraph, type, true);
				graph.Clear();
				graph.Document.Current = PXSelect<ARPayment, Where<ARPayment.docType, Equal<Required<ARPayment.docType>>,
				And<ARPayment.refNbr, Equal<Required<ARPayment.refNbr>>>>>
					.Select(graph, doc.DocType, doc.RefNbr);
			}
			else
			{
				newGraph.Save.Press();
			}
			return true;
		}

		private bool VoidOriginalPayment(ARPaymentEntry graph, bool success)
		{
			ARPayment doc = graph.Document.Current;
			var extTran = GetLastProcessedExternalTran(graph);
			if (extTran == null || !NeedSyncContext || !success
				|| doc.Released == true || doc.Voided == true || !PaymentDocType(doc))
			{
				return false;
			}
			var state = ExternalTranHelper.GetTransactionState(graph, extTran);
			if (!state.IsVoided) return false;


			var adapter = CreateAdapter(graph, doc);
			graph.VoidCheck(adapter);
			return true;
		}

		private void MoveTranToAnotherDoc(ARPaymentEntry graph, ExternalTransaction extTran)
		{
			var doc = graph.Document.Current;
			PXDatabase.Update<ExternalTransaction>(
				new PXDataFieldAssign("DocType", doc.DocType),
				new PXDataFieldAssign("RefNbr", doc.RefNbr),
				new PXDataFieldRestrict("TransactionID", PXDbType.Int, 4, extTran.TransactionID, PXComp.EQ)
			);
			PXDatabase.Update<CCProcTran>(
				new PXDataFieldAssign("DocType", doc.DocType),
				new PXDataFieldAssign("RefNbr", doc.RefNbr),
				new PXDataFieldRestrict("TransactionID", PXDbType.Int, 4, extTran.TransactionID, PXComp.EQ)
			);
			graph.ExternalTran.Cache.Clear();
			graph.ExternalTran.Cache.ClearQueryCache();
		}

		private void UpdateDocCleared(ARPaymentEntry graph)
		{
			IExternalTransaction currTran = GetLastProcessedExternalTran(graph);
			ExternalTransactionState state = ExternalTranHelper.GetTransactionState(graph, currTran);

			if (state?.IsCaptured == true)
			{
				ARPayment doc = graph.Document.Current;

				var processingCenter = CCProcessingCenter.PK.Find(graph, doc.ProcessingCenterID);
				if (processingCenter.ImportSettlementBatches == false)
				{
					doc.Cleared = true;
					doc.ClearDate = currTran.LastActivityDate.Value.Date;
				}
			}
		}

		private void ChangeOriginDocProcessingStatus(ARPaymentEntry paymentGraph, CCTranType tranType, bool success)
		{
			IExternalTransaction tran = GetLastProcessedExternalTran(paymentGraph);
			ARPayment payment = paymentGraph.Document.Current;
			ExternalTransactionState tranState = ExternalTranHelper.GetTransactionState(paymentGraph, tran);
			var oPaymentGraph = GetGraphWithOriginDoc(paymentGraph, tranType);
			if (oPaymentGraph != null)
			{
				var oExtTran = GetLastProcessedExternalTran(oPaymentGraph);
				ARPayment oPayment = oPaymentGraph.Document.Current;
				if (oExtTran.TransactionID == tran.TransactionID)
				{
					ChangeCaptureFailedFlag(tranState, oPayment);
					if (success)
					{
						ChangeDocProcessingFlags(tranState, oPayment, tranType);
					}
				}
				paymentGraph.Caches[typeof(ARPayment)].Update(oPayment);
			}
		}

		private void UpdateExtRefNbrARPayment(ARPaymentEntry graph, CCTranType tranType, bool success)
		{
			var paymentGraph = graph as ARPaymentEntry;
			ARPayment doc = (ARPayment)paymentGraph.Document.Current;
			if (success && doc.Released == false)
			{
				IExternalTransaction currTran = GetLastProcessedExternalTran(graph);
				ExternalTransactionState state = ExternalTranHelper.GetTransactionState(paymentGraph, currTran);
				if (currTran != null)
				{
					SetExtRefNbrValue(paymentGraph, doc, currTran, state);
				}
				paymentGraph.Document.Update(doc);
			}
		}

		private void SetExtRefNbrValue(ARPaymentEntry graph, ARPayment doc, IExternalTransaction currTran, ExternalTransactionState state)
		{
			if (state.IsActive || (doc.DocType == ARDocType.Refund && state.IsVoided))
			{
				graph.Document.Cache.SetValue<ARPayment.extRefNbr>(doc, currTran.TranNumber);
			}
		}

		private void DeactivateExpiredTrans(ARPaymentEntry graph)
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

		private void ChangeCaptureFailedFlag(ExternalTransactionState state, ARPayment doc)
		{
			if (doc.IsCCCaptureFailed == false
				&& (state.ProcessingStatus == ProcessingStatus.CaptureFail || state.ProcessingStatus == ProcessingStatus.CaptureDecline))
			{
				doc.IsCCCaptureFailed = true;
			}
			else if (doc.IsCCCaptureFailed == true && (state.IsCaptured || state.IsVoided || 
				(state.IsPreAuthorized && !state.HasErrors && !CheckCaptureFailedExists(state))))
			{
				doc.IsCCCaptureFailed = false;
			}
		}

		private void ChangeUserAttentionFlag(ARPaymentEntry graph)
		{
			IExternalTransaction currTran = GetLastProcessedExternalTran(graph);
			if (currTran != null)
			{
				ExternalTransactionState state = ExternalTranHelper.GetTransactionState(graph, currTran);
				ChangeUserAttentionFlag(state, graph);
			}
		}

		private void ChangeUserAttentionFlag(ExternalTransactionState state, ARPaymentEntry graph)
		{
			ARPayment doc = graph.Document.Current;
			bool newVal = false;
			bool authFailedWithFlag = (state.ProcessingStatus == ProcessingStatus.AuthorizeDecline 
				|| state.ProcessingStatus == ProcessingStatus.AuthorizeFail) && doc.IsCCUserAttention == true; 
			if (authFailedWithFlag || state.IsOpenForReview || state.SyncFailed || doc.IsCCCaptureFailed == true
				|| state.ProcessingStatus == ProcessingStatus.VoidFail || state.ProcessingStatus == ProcessingStatus.VoidDecline)
			{
				newVal = true;
			}

			int activeTransCnt = graph.ExternalTran.Select()
					.RowCast<ExternalTransaction>().Where(i => i.Active == true 
						&& doc.DocType == i.DocType && doc.RefNbr == i.RefNbr).Count();
			if (activeTransCnt > 1)
			{
				newVal = true;
			}

			if (doc.PMInstanceID == PaymentTranExtConstants.NewPaymentProfile && activeTransCnt == 0
				&& PaymentDocType(doc))
			{
				newVal = true;
			}
			doc.IsCCUserAttention = newVal;
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

		private void ChangeDocProcessingFlags(ExternalTransactionState tranState, ARPayment doc, CCTranType tranType)
		{
			var extTran = tranState.ExternalTransaction;
			if (extTran?.TransactionID != doc.CCActualExternalTransactionID) return;
			
			doc.IsCCAuthorized = doc.IsCCCaptured = doc.IsCCRefunded = false;
			if (!tranState.IsDeclined && !tranState.IsOpenForReview && !tranState.SyncFailed
				&& !ExternalTranHelper.IsExpired(tranState.ExternalTransaction))
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
				if (PaymentDocType(doc) && tranState.IsCaptured)
				{
					doc.IsCCCaptured = true;
				}
			}
		}

		private PXAdapter CreateAdapter(ARPaymentEntry graph, ARPayment doc)
		{
			var dummyView = new PXView.Dummy(graph, graph.Document.View.BqlSelect, new List<object>() { doc });
			return new PXAdapter(dummyView);
		}

		private ARPaymentEntry GetGraphWithOriginDoc(ARPaymentEntry graph, CCTranType tranType)
		{
			if (graphWithOriginDoc != null)
			{
				return graphWithOriginDoc; 
			}
			IExternalTransaction tran = GetLastProcessedExternalTran(graph);
			ARPayment payment = graph.Document.Current;
			if (tranType == CCTranType.VoidOrCredit
				&& ((tran.DocType == payment.OrigDocType && tran.RefNbr == payment.OrigRefNbr)
					|| (tran.VoidDocType == payment.DocType && tran.VoidRefNbr == payment.RefNbr))
				&& (tran.DocType == ARDocType.Payment || tran.DocType == ARDocType.Prepayment))
			{
				graphWithOriginDoc = GetGraphByDocTypeRefNbr(tran.DocType, tran.RefNbr);
			}
			return graphWithOriginDoc;
		}

		private ARPaymentEntry GetGraphByDocTypeRefNbr(string docType, string refNbr)
		{
			var paymentGraph = PXGraph.CreateInstance<ARPaymentEntry>();

			paymentGraph.RowSelecting.RemoveHandler<ARPayment>(paymentGraph.ARPayment_RowSelecting);
			paymentGraph.FieldUpdating.RemoveHandler<SOAdjust.curyDocBal>(paymentGraph.SOAdjust_CuryDocBal_FieldUpdating);

			paymentGraph.Document.Current = PXSelect<ARPayment, Where<ARPayment.docType, Equal<Required<ARPayment.docType>>,
				And<ARPayment.refNbr, Equal<Required<ARPayment.refNbr>>>>>
					.Select(paymentGraph, docType, refNbr);

			return paymentGraph;
		}

		private ExternalTransaction GetLastProcessedExternalTran(ARPaymentEntry graph)
		{
			var procTrans = graph.ccProcTran.Select().RowCast<CCProcTran>();
			var extTrans = graph.ExternalTran.Select().RowCast<ExternalTransaction>();
			ExternalTransaction extTran = ExternalTranHelper.GetLastProcessedExtTran(extTrans, procTrans);
			return extTran;
		}

		public override void PersistData()
		{
			ARPayment doc = Graph?.Document.Current;
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

		public override PXGraph GetGraph()
		{
			return Graph;
		}

		protected virtual ARPaymentEntry CreateGraphIfNeeded(IBqlTable table)
		{
			if (Graph == null)
			{
				ARPayment doc = table as ARPayment;
				Graph = GetGraphByDocTypeRefNbr(doc.DocType, doc.RefNbr);
				Graph.Document.Update(doc);
			}
			return Graph;
		}

		protected virtual ICCPaymentProcessingRepository GetPaymentProcessingRepository()
		{
			ICCPaymentProcessingRepository repository = new CCPaymentProcessingRepository(Graph);
			return repository;
		}

		private bool CheckImportDeactivatedTran(ARPaymentEntry paymentGraph)
		{
			ExternalTransaction lastExttran = paymentGraph.ExternalTran.SelectSingle();
			if (lastExttran != null && lastExttran.NeedSync == true && lastExttran.Active == false)
			{
				importDeactivatedTran = true;
			}
			else
			{
				importDeactivatedTran = false;
			}
			return importDeactivatedTran;
		}

		private ARPayment SyncActualExternalTransation(ARPaymentEntry paymentGraph, ARPayment payment)
		{
			ExternalTransaction lastExtTran = GetLastProcessedExternalTran(paymentGraph);
			if (lastExtTran == null) return payment;

			ExternalTransaction currentActualExtTran = paymentGraph.ExternalTran
				.Select().SingleOrDefault(t => ((ExternalTransaction)t).TransactionID == payment.CCActualExternalTransactionID);

			if (payment.CCActualExternalTransactionID == null || currentActualExtTran == null)
			{
				payment.CCActualExternalTransactionID = lastExtTran.TransactionID;
				return payment;
			}

			if (lastExtTran.TransactionID > payment.CCActualExternalTransactionID)
			{
				ExternalTransactionState stateOfActualExtTran = ExternalTranHelper.GetTransactionState(paymentGraph, currentActualExtTran);
				ExternalTransactionState stateOfLastExtTran = ExternalTranHelper.GetTransactionState(paymentGraph, lastExtTran);

				if (!stateOfActualExtTran.IsActive || stateOfLastExtTran.IsActive)
				{
					payment.CCActualExternalTransactionID = lastExtTran.TransactionID;
				}
			}
			return payment;
		}

		private void RestoreCopy()
		{
			ARPayment doc = Graph?.Document.Current;
			if (doc != null && inputTable != null)
			{
				Graph.Document.Cache.RestoreCopy(inputTable, doc);
			}
		}

		private bool PaymentDocType(ARPayment payment)
		{
			bool ret = false;
			string docType = payment.DocType;
			if (docType == ARDocType.Payment || docType == ARDocType.Prepayment)
			{
				ret = true;
			}
			return ret;
		}

		#region Re-authorization of expired transactions 
		private void UpdateDocReAuthFieldsAfterAuthorize(ARPaymentEntry paymentGraph)
		{
			ARPayment payment = paymentGraph.Document.Current;
			ExternalTransactionState tranState = GetStateOfLastExternalTransaction(paymentGraph);

			if (tranState?.IsPreAuthorized == true || payment.CCReauthDate == null)
			{
				ExcludeFromReAuthProcess(paymentGraph, payment);
			}
			else
			{
				HandleUnsuccessfulAttemptOfReauth(paymentGraph, payment);
			}
		}

		private void UpdateDocReAuthFieldsAfterCapture(ARPaymentEntry graph)
		{
			ExternalTransactionState tranState = GetStateOfLastExternalTransaction(graph);

			if (tranState?.IsCaptured == true)
			{
				ARPayment payment = graph.Document.Current;
				ExcludeFromReAuthProcess(graph, payment);
			}
		}

		private void UpdateDocReAuthFieldsAfterVoid(ARPaymentEntry graph)
		{
			ExternalTransactionState tranState = GetStateOfLastExternalTransaction(graph);

			if (tranState?.IsVoided == true)
			{
				ARPayment payment = graph.Document.Current;
				ExcludeFromReAuthProcess(graph, payment);
			}
		}

		private void UpdateDocReAuthFieldsAfterValidationByVoidForReAuth(ARPaymentEntry paymentGraph)
		{
			ExternalTransactionState tranState = GetStateOfLastExternalTransaction(paymentGraph);

			if (tranState?.IsActive == false && tranState?.SyncFailed == false)
			{
				ARPayment payment = paymentGraph.Document.Current;
				IncludeToReAuthProcess(paymentGraph, payment);
			}
		}

		private void UpdateDocReAuthFieldsAfterVoidForReAuth(ARPaymentEntry paymentGraph)
		{
			ExternalTransactionState tranState = GetStateOfLastExternalTransaction(paymentGraph);

			if (tranState?.IsVoided == true)
			{
				ARPayment payment = paymentGraph.Document.Current;
				IncludeToReAuthProcess(paymentGraph, payment);
			}
		}

		private ExternalTransactionState GetStateOfLastExternalTransaction(ARPaymentEntry payment)
		{
			IExternalTransaction tran = GetLastProcessedExternalTran(payment);
			if (tran == null) return null;
			
			ExternalTransactionState tranState = ExternalTranHelper.GetTransactionState(payment, tran);
			return tranState;
		}
		
		private void HandleUnsuccessfulAttemptOfReauth(ARPaymentEntry paymentGraph, ARPayment payment)
		{
			ICCPaymentProcessingRepository repository = CCPaymentProcessingRepository.GetCCPaymentProcessingRepository();
			var processingCenter = repository.GetCCProcessingCenter(payment.ProcessingCenterID);
			
			payment.CCReauthTriesLeft -= 1;
			
			if (payment.CCReauthTriesLeft > 0)
			{
				payment.CCReauthDate = PXTimeZoneInfo.Now.AddHours(processingCenter.ReauthRetryDelay.Value);
				paymentGraph.Document.Update(payment);
			}
			else
			{
				payment.IsCCUserAttention = true;
				ExcludeFromReAuthProcess(paymentGraph, payment);
			}
		}
		
		private void ExcludeFromReAuthProcess(ARPaymentEntry paymentGraph, ARPayment payment)
		{
			payment.CCReauthDate = null;
			payment.CCReauthTriesLeft = 0;
			paymentGraph.Document.Update(payment);
		}
		
		private void IncludeToReAuthProcess(ARPaymentEntry paymentGraph, ARPayment payment)
		{
			ICCPaymentProcessingRepository repository = CCPaymentProcessingRepository.GetCCPaymentProcessingRepository();
			var processingCenter = repository.GetCCProcessingCenter(payment.ProcessingCenterID);
			var processingCenterPmntMethod = GetProcessingCenterPmntMethod(paymentGraph, payment);

			payment.CCReauthDate = PXTimeZoneInfo.Now.AddHours(processingCenterPmntMethod.ReauthDelay.Value);
			payment.CCReauthTriesLeft = processingCenter.ReauthRetryNbr;
			paymentGraph.Document.Update(payment);
		}
		
		private CCProcessingCenterPmntMethod GetProcessingCenterPmntMethod(ARPaymentEntry paymentGraph, ARPayment payment)
		{
			var query = new SelectFrom<CCProcessingCenterPmntMethod>
							.Where<CCProcessingCenterPmntMethod.paymentMethodID.IsEqual<@P.AsString>
								.And<CCProcessingCenterPmntMethod.processingCenterID.IsEqual<@P.AsString>>>
							.View(paymentGraph);
			
			var result = query.Select(payment.PaymentMethodID, payment.ProcessingCenterID);
			
			foreach (PXResult<CCProcessingCenterPmntMethod> processingCenterPmntMethod in result)
			{
				return processingCenterPmntMethod;
			}
			return null;
		}
		#endregion
	}
}
