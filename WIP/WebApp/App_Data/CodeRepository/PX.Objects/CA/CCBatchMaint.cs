using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;

using PX.Common;
using PX.Data;
using PX.Data.Auth;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.AR.CCPaymentProcessing;
using PX.Objects.AR.CCPaymentProcessing.Helpers;
using PX.Objects.AR.CCPaymentProcessing.Interfaces;
using PX.Objects.AR.Repositories;
using PX.Objects.BQLConstants;
using PX.Objects.CA;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.Extensions.PaymentTransaction;

using ARStandalone = PX.Objects.AR.Standalone;

namespace PX.Objects.CA
{
	public class CCBatchMaint : PXGraph<CCBatchMaint>
	{
		#region Internal Types Definition
		internal enum StatusMatchingResult
		{
			NoMatch,
			Match,
			SuccessMatch,
			NoMatchSkipValidation,
			VoidPaymentWithoutVoidTransaction
		}
		#endregion

		#region Selects
		public PXSelect<CCBatch> BatchView;

		public PXSelect<CCBatchStatistics, Where<CCBatchStatistics.batchID, Equal<Current<CCBatch.batchID>>>> CardTypeSummary;

		public PXSelectJoin<CCBatchTransaction,
			LeftJoin<ARPayment, On<CCBatchTransaction.docType, Equal<ARPayment.docType>,
				And<CCBatchTransaction.refNbr, Equal<ARPayment.refNbr>>>>,
			Where<CCBatchTransaction.batchID, Equal<Current<CCBatch.batchID>>>> Transactions;

		public PXSelect<CCBatchTransactionAlias1, Where<CCBatchTransactionAlias1.batchID, Equal<Current<CCBatch.batchID>>,
			And<CCBatchTransactionAlias1.processingStatus, Equal<CCBatchTranProcessingStatusCode.missing>>>> MissingTransactions;

		public PXSelectJoin<CCBatchTransactionAlias2,
			InnerJoin<ARPayment, On<ARPayment.docType, Equal<CCBatchTransactionAlias2.docType>,
				And<ARPayment.refNbr, Equal<CCBatchTransactionAlias2.refNbr>>>,
			InnerJoin<CCProcessingCenter, On<CCProcessingCenter.processingCenterID, Equal<Current<CCBatch.processingCenterID>>>>>,
			Where<CCBatchTransactionAlias2.batchID, Equal<Current<CCBatch.batchID>>,
				And<Where<ARPayment.depositAsBatch, Equal<boolFalse>,
					Or2<Where<ARPayment.deposited, Equal<boolTrue>,
						And<Where<ARPayment.depositNbr, IsNull, Or<IsNull<ARPayment.depositNbr, EmptyString>, NotEqual<IsNull<Current<CCBatch.depositNbr>, EmptyString>>>>>>,
					Or<NotExists<Select<CashAccountDeposit,
						Where<CashAccountDeposit.accountID, Equal<CCProcessingCenter.depositAccountID>,
							And<CashAccountDeposit.depositAcctID, Equal<ARPayment.cashAccountID>,
							And<Where<CashAccountDeposit.paymentMethodID, Equal<ARPayment.paymentMethodID>,
									Or<CashAccountDeposit.paymentMethodID, Equal<BQLConstants.EmptyString>>>>>>>>>>>>>> ExcludedFromDepositTransactions;

		public PXSelect<ExternalTransaction> ExternalTransactions;
		public PXSelect<ARPayment> Payment;

		public PXSelect<CCProcessingCenter,
			Where<CCProcessingCenter.processingCenterID,
				Equal<Current<CCBatch.processingCenterID>>>> ProcessingCenter;
		#endregion

		#region Actions
		public PXSave<CCBatch> Save;
		public PXCancel<CCBatch> Cancel;
		public PXFirst<CCBatch> First;
		public PXPrevious<CCBatch> Previous;
		public PXNext<CCBatch> Next;
		public PXLast<CCBatch> Last;

		public PXAction<CCBatch> createDeposit;
		[PXUIField(DisplayName = "Create Deposit", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable CreateDeposit(PXAdapter adapter)
		{
			if (BatchView.Current != null)
			{
				CCProcessingCenter procCenter = ProcessingCenter.SelectSingle();

				if (procCenter.DepositAccountID == null)
				{
					throw new PXException(PXMessages.LocalizeFormatNoPrefix(Messages.SpecifyActiveDepositAccountInProcessingCenter, procCenter.ProcessingCenterID));
				}
				else
				{
					CashAccount depositAccount = CashAccount.PK.Find(this, procCenter.DepositAccountID);
					if (depositAccount?.Active != true)
					{
						throw new PXException(PXMessages.LocalizeFormatNoPrefix(Messages.SpecifyActiveDepositAccountInProcessingCenter, procCenter.ProcessingCenterID));
					}
				}

				CreateDepositProc(adapter.MassProcess);

				this.Save.Press();
			}
			return adapter.Get();
		}

		private List<CCBatchTransactionAlias1> SelectedMissingTransactions(int? batchID, out bool allSelected)
		{
			allSelected = true;
			var selectedLines = new List<CCBatchTransactionAlias1>();
			foreach (CCBatchTransactionAlias1 row in MissingTransactions.Cache.Cached)
			{
				if (row.BatchID != batchID) continue;
				if (row.SelectedToHide == true)
				{
					selectedLines.Add(row);
				}
				else
				{
					allSelected = false;
				}
			}
			return selectedLines;
		}

		public PXAction<CCBatch> record;
		[PXUIField(DisplayName = "Record", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton(PopupCommand = nameof(refreshGraph))]
		public virtual IEnumerable Record(PXAdapter adapter)
		{
			var batch = BatchView.Current;
			if (batch == null)
				return adapter.Get();

			var selectedLines = SelectedMissingTransactions(batch.BatchID, out var _);
			if (selectedLines.Count() > 1)
			{
				throw new PXException(Messages.SelectOnlyOneTransaction);
			}

			Save.Press();

			CCBatchTransaction tran = selectedLines.First();
			switch (tran.SettlementStatus)
			{
				case CCBatchTranSettlementStatusCode.SettledSuccessfully:
					{
						var documentGraph = PXGraph.CreateInstance<ARPaymentEntry>();
						CreatePayment(documentGraph, batch, tran, ARDocType.Payment);
						throw new PXPopupRedirectException(documentGraph, Messages.ViewDocument, true);
					}
				case CCBatchTranSettlementStatusCode.RefundSettledSuccessfully:
					{
						var documentGraph = PXGraph.CreateInstance<ARPaymentEntry>();
						CreatePayment(documentGraph, batch, tran, ARDocType.Refund);
						throw new PXPopupRedirectException(documentGraph, Messages.ViewDocument, true);
					}
				case CCBatchTranSettlementStatusCode.Voided:
					{
						string docType = tran.DocType, refNbr = tran.RefNbr;
						if (docType == null || refNbr == null)
						{
							var extTran = GetExternalTransaction(batch.ProcessingCenterID, tran.PCTranNumber);
							docType = extTran?.DocType;
							refNbr = extTran?.RefNbr;
						}

						if (docType == null || refNbr == null)
						{
							var blankPaymentGraph = PXGraph.CreateInstance<ARPaymentEntry>();
							CreatePayment(blankPaymentGraph, batch, tran, ARDocType.Payment);
							throw new PXPopupRedirectException(blankPaymentGraph, Messages.ViewDocument, true);
						}

						var documentGraph = GetDocumentGraph(docType, refNbr);
						if (documentGraph is ARPaymentEntry paymentGraph)
						{
							paymentGraph.VoidCheck(adapter);
						}
						else if (documentGraph is ARCashSaleEntry cashSaleGraph)
						{
							cashSaleGraph.VoidCheck(adapter);
						}
						throw new PXPopupRedirectException(documentGraph, Messages.ViewDocument, true);
					}
			}
			return adapter.Get();
		}

		public PXAction<CCBatch> refreshGraph;
		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXButton]
		protected virtual IEnumerable RefreshGraph(PXAdapter adapter)
		{
			return Cancel.Press(adapter);
		}

		private void CreatePayment(ARPaymentEntry te, CCBatch batch, CCBatchTransaction batchTran, string arDocType)
		{
			var extension = te.GetExtension<ARPaymentEntry.PaymentTransaction>();
			var inputInfo = extension?.InputPmtInfo?.Current;
			if (inputInfo != null)
				inputInfo.PCTranNumber = batchTran.PCTranNumber;

			var paymentCache = te.Caches[typeof(ARPayment)];

			var doc = new ARPayment();
			doc.DocType = arDocType;
			doc = te.Document.Insert(doc);

			var repo = new CustomerPaymentMethodRepository(this);
			var cpmData = repo.GetCustomerPaymentMethodWithProfileDetail(batch.ProcessingCenterID, batchTran.PCCustomerID, batchTran.PCPaymentProfileID);
			if (cpmData != null)
			{
				var (cpm, _) = cpmData;
				paymentCache.SetValueExt<ARPayment.customerID>(doc, cpm.BAccountID);
				paymentCache.SetValueExt<ARPayment.paymentMethodID>(doc, cpm.PaymentMethodID);
				paymentCache.SetValueExt<ARPayment.pMInstanceID>(doc, cpm.PMInstanceID);
			}
			else
			{
				doc.NewCard = true;
				var cpm = GetCPM(this, batch.ProcessingCenterID, batchTran.PCCustomerID);
				paymentCache.SetValueExt<ARPayment.customerID>(doc, cpm?.BAccountID);
			}

			paymentCache.SetValueExt<ARPayment.processingCenterID>(doc, batch.ProcessingCenterID);
			paymentCache.SetValueExt<ARPayment.curyOrigDocAmt>(doc, batchTran.Amount);
		}

		private CustomerPaymentMethod GetCPM(PXGraph graph, string processingCenterID, string pcCustomerID)
		{
			var query = new PXSelectReadonly<CustomerPaymentMethod,
				Where<CustomerPaymentMethod.cCProcessingCenterID, Equal<Required<CustomerPaymentMethod.cCProcessingCenterID>>,
					And<CustomerPaymentMethod.customerCCPID, Equal<Required<CustomerPaymentMethod.customerCCPID>>>>>(graph);

			return query.SelectSingle(processingCenterID, pcCustomerID);
		}

		public PXAction<CCBatch> hide;
		[PXUIField(DisplayName = "Hide", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual IEnumerable Hide(PXAdapter adapter)
		{
			var batch = BatchView.Current;
			if (batch == null)
				return adapter.Get();

			var selectedLines = SelectedMissingTransactions(batch.BatchID, out bool hideAll);
			if (selectedLines.Any())
			{
				WebDialogResult wdr =
					MissingTransactions.Ask(
						Messages.ThisTransactionWillBeExcludedProceed,
						MessageButtons.YesNo);

				if (wdr == WebDialogResult.Yes)
				{
					foreach (var tran in selectedLines)
					{
						tran.ProcessingStatus = CCBatchTranProcessingStatusCode.Hidden;
						tran.SelectedToHide = false;
						this.MissingTransactions.Update(tran);
					}

					if (hideAll && batch.Status == CCBatchStatusCode.PendingReview)
					{
						batch.Status = CCBatchStatusCode.Processed;
					}

					this.Actions.PressSave();
				}
			}

			Transactions.View.Cache.Clear();
			Transactions.View.Cache.ClearQueryCache();

			return adapter.Get();
		}

		public PXAction<CCBatch> unhide;
		[PXUIField(DisplayName = "Unhide", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual IEnumerable Unhide(PXAdapter adapter)
		{
			var batch = BatchView.Current;
			if (batch == null)
				return adapter.Get();

			var selectedLines = new List<CCBatchTransaction>();
			foreach (CCBatchTransaction row in Transactions.Cache.Cached)
			{
				if (row.BatchID != batch.BatchID) continue;
				if (row.SelectedToUnhide == true)
					selectedLines.Add(row);
			}

			if (selectedLines.Any())
			{
				foreach (var tran in selectedLines)
				{
					tran.ProcessingStatus = CCBatchTranProcessingStatusCode.Missing;
					tran.SelectedToUnhide = false;
					Transactions.Update(tran);
				}

				if (batch.Status == CCBatchStatusCode.Processed)
				{
					batch.Status = CCBatchStatusCode.PendingReview;
				}

				this.Actions.PressSave();
			}

			return adapter.Get();
		}

		public PXAction<CCBatch> repeatMatching;
		[PXUIField(DisplayName = "Match", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual IEnumerable RepeatMatching(PXAdapter adapter)
		{
			var batch = BatchView.Current;
			if (batch != null)
			{
				bool fullyProcessed = MatchTransactions();
				if (fullyProcessed && batch.Status != CCBatchStatusCode.Deposited)
				{
					batch.Status = CCBatchStatusCode.Processed;
					BatchView.UpdateCurrent();
				}
			}

			MissingTransactions.View.Cache.Clear();
			MissingTransactions.View.Cache.ClearQueryCache();
			return adapter.Get();
		}

		private void CreateDepositProc(bool isMassProcess)
		{
			CCBatch batch = BatchView.Current;
			CADepositEntry cadGraph = PXGraph.CreateInstance<CADepositEntry>();

			var paymentsToAdd = new List<PaymentInfo>();
			var chargesToAdd = new List<KeyValuePair<string, CCBatchTransaction>>();

			PXSelectBase<Light.ARPayment> paymentSelect = new PXSelectJoin<Light.ARPayment,
				LeftJoin<CADepositDetail, On<CADepositDetail.origDocType, Equal<Light.ARPayment.docType>,
					And<CADepositDetail.origRefNbr, Equal<Light.ARPayment.refNbr>,
					And<CADepositDetail.origModule, Equal<GL.BatchModule.moduleAR>,
					And<CADepositDetail.tranType, Equal<CATranType.cADeposit>>>>>,
				LeftJoin<CADeposit, On<CADeposit.tranType, Equal<CADepositDetail.tranType>,
					And<CADeposit.refNbr, Equal<CADepositDetail.refNbr>>>,
				InnerJoin<CashAccountDeposit, On<CashAccountDeposit.depositAcctID, Equal<Light.ARPayment.cashAccountID>,
					And<Where<CashAccountDeposit.paymentMethodID, Equal<Light.ARPayment.paymentMethodID>,
						Or<CashAccountDeposit.paymentMethodID, Equal<BQLConstants.EmptyString>>>>>,
				InnerJoin<PaymentMethod, On<PaymentMethod.paymentMethodID, Equal<Light.ARPayment.paymentMethodID>>,
				InnerJoin<CCBatchTransaction, On<CCBatchTransaction.docType, Equal<Light.ARPayment.docType>,
					And<CCBatchTransaction.refNbr, Equal<Light.ARPayment.refNbr>>>,
				InnerJoin<CCProcessingCenter, On<CCProcessingCenter.depositAccountID, Equal<CashAccountDeposit.accountID>>>>>>>>,
				Where<CCProcessingCenter.processingCenterID, Equal<Required<CCProcessingCenter.processingCenterID>>,
					And<CCBatchTransaction.batchID, Equal<Required<CCBatchTransaction.batchID>>,
					And<Light.ARPayment.depositAsBatch, Equal<boolTrue>,
					And<Light.ARPayment.deposited, NotEqual<boolTrue>,
					And<Light.ARPayment.depositNbr, IsNull,
					And<Where<CADepositDetail.refNbr, IsNull,
						Or<CADeposit.voided, Equal<boolTrue>>>>>>>>>>(this);

			foreach (PXResult<Light.ARPayment, CADepositDetail, CADeposit, CashAccountDeposit, PaymentMethod, CCBatchTransaction, CCProcessingCenter> res in paymentSelect.Select(batch.ProcessingCenterID, batch.BatchID))
			{
				Light.ARPayment payment = res;
				CCBatchTransaction batchTran = res;

				if (!DocumentRequiresDeposit(batchTran))
					continue;

				if (payment.Released == false)
					throw new PXException(PXMessages.LocalizeNoPrefix(Messages.UnreleasedDocumentsCannotBeIncludedInBankDeposit));

				if (!paymentsToAdd.Any(p => p.DocType == payment.DocType && p.RefNbr == payment.RefNbr))
				{
					PaymentInfo paymentInfo = cadGraph.Copy(payment, new PaymentInfo());
					paymentsToAdd.Add(paymentInfo);
				}
			}

			var chargeSelect = new PXSelectJoin<CCBatchTransaction,
				LeftJoin<CCProcessingCenterFeeType, On<CCProcessingCenterFeeType.processingCenterID, Equal<Required<CCProcessingCenter.processingCenterID>>,
					And<CCProcessingCenterFeeType.feeType, Equal<CCBatchTransaction.feeType>>>>,
				Where<CCBatchTransaction.batchID, Equal<Required<CCBatchTransaction.batchID>>,
					And<CCBatchTransaction.totalFee, NotEqual<decimal0>>>>(this);

			foreach (PXResult<CCBatchTransaction, CCProcessingCenterFeeType> res in chargeSelect.Select(batch.ProcessingCenterID, batch.BatchID))
			{
				CCBatchTransaction batchTran = res;
				CCProcessingCenterFeeType feeType = res;

				if (feeType.EntryTypeID == null)
					throw new PXException(PXMessages.LocalizeFormatNoPrefix(Messages.TheFeeTypeIsNotLinkedToEntryType, batchTran.FeeType));

				chargesToAdd.Add(new KeyValuePair<string, CCBatchTransaction>(feeType.EntryTypeID, batchTran));
			}

			if (!paymentsToAdd.Any() && !chargesToAdd.Any_())
			{
				if (isMassProcess || BatchView.Ask(PXMessages.Localize(Messages.NoPaymentsSetBatchStatusToDeposited), MessageButtons.YesNo) == WebDialogResult.Yes)
				{
					batch.Status = CCBatchStatusCode.Deposited;
					BatchView.Update(batch);
				}
				return;
			}

			CADeposit deposit = CreateCADepositHeader(cadGraph);
			if (deposit == null) return;

			if (paymentsToAdd.Any())
			{
				cadGraph.AddPaymentInfoBatch(paymentsToAdd);
			}

			if (chargesToAdd.Any_())
			{
				foreach (var chargeGroup in chargesToAdd.GroupBy(res => res.Key, res => res.Value, StringComparer.OrdinalIgnoreCase))
				{
					string entryTypeID = chargeGroup.Key;
					var charge = new CADepositCharge
					{
						EntryTypeID = entryTypeID,
						CuryChargeableAmt = 0,
						CuryChargeAmt = 0
					};

					foreach (var bt in chargeGroup)
					{
						charge.CuryChargeableAmt += bt.Amount;
						charge.CuryChargeAmt += bt.TotalFee;
					}

					charge = cadGraph.Charges.Insert(charge);
				}
			}

			cadGraph.Document.SetValueExt<CADeposit.curyControlAmt>(deposit, deposit.CuryTranAmt);
			cadGraph.Document.Update(deposit);
			cadGraph.Save.Press();

			batch.DepositType = cadGraph.Document.Current.TranType;
			batch.DepositNbr = cadGraph.Document.Current.RefNbr;
			BatchView.Update(batch);
		}

		private bool DocumentRequiresDeposit(CCBatchTransaction batchTran)
		{
			switch (batchTran.SettlementStatus)
			{
				case CCBatchTranSettlementStatusCode.SettledSuccessfully:
				case CCBatchTranSettlementStatusCode.RefundSettledSuccessfully:
					return true;
				default:
					return false;
			}
		}

		public static PXGraph CreateGraph()
		{
			return PXGraph.CreateInstance<CCBatchMaint>();
		}

		private CADeposit CreateCADepositHeader(CADepositEntry graph)
		{
			CCBatch batch = BatchView.Current;
			string extRefNbr = batch.ProcessingCenterID + "." + batch.SettlementTime.Value.Date.ToShortDateString();
			CCProcessingCenter procCenter = ProcessingCenter.SelectSingle();
			var deposit = new CADeposit()
			{
				TranType = CATranType.CADeposit,
				CashAccountID = procCenter.DepositAccountID,
				TranDate = batch.SettlementTime.Value.Date,
				ExtRefNbr = batch.ProcessingCenterID + "." + batch.SettlementTime.Value.Date.ToShortDateString(),
				TranDesc = PXMessages.LocalizeFormatNoPrefix(Messages.DepositOfSettlementBatch, extRefNbr)
			};

			return graph.Document.Insert(deposit);
		}

		public bool MatchTransactions()
		{
			return MatchTransactions(BatchView.Current);
		}

		// The content of the method should be merged in the parameterless overload
		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2021R2)]
		public bool MatchTransactions(CCBatch ccBatch)
		{
			if (ccBatch == null)
			{
				return false;
			}

			bool fullyProcessed = true;
			var exceptions = new Dictionary<string, Exception>(StringComparer.Ordinal);
			foreach (CCBatchTransaction batchTran in GetBatchTransactions(ccBatch.BatchID, CCBatchTranProcessingStatusCode.Missing, CCBatchTranProcessingStatusCode.PendingProcessing))
			{
				try
				{
					var paymentGraph = CreateInstance<ARPaymentEntry>();
					ARPayment payment = null;

					ExternalTransaction extTran = GetExternalTransaction(ccBatch.ProcessingCenterID, batchTran.PCTranNumber);
					if (extTran != null)
					{
						payment = FindARPayment(paymentGraph, extTran.DocType, extTran.RefNbr, batchTran.SettlementStatus);
					}
					else
					{
						var tranData = GetTranDetails(ccBatch.ProcessingCenterID, batchTran.PCTranNumber);
						extTran = GetExternalTransactionByNoteID(tranData.TranUID);
						if (extTran != null)
						{
							extTran.TranNumber = batchTran.PCTranNumber;
							ExternalTransactions.Update(extTran);
							this.Actions.PressSave();

							payment = FindARPayment(paymentGraph, extTran.DocType, extTran.RefNbr, batchTran.SettlementStatus);
							paymentGraph.Document.Current = payment;
							var extension = paymentGraph.GetExtension<ARPaymentEntry.PaymentTransaction>();
							var extTranDetail = extTran.GetExtension<ExternalTransactionDetail>();
							extension.CheckAndRecordTransaction(extTranDetail, tranData);

							extTran = extension.ExternalTransaction.Select().RowCast<ExternalTransactionDetail>().FirstOrDefault(tran => tran.TranNumber == batchTran.PCTranNumber)?.Base as ExternalTransaction;

							fullyProcessed &= HandleExternalUpdate(batchTran);
							
							continue;
						}
						else
						{
							PickValuablesFromTransactionData(tranData, batchTran);
						}
					}

					if (extTran == null || payment == null)
					{
						fullyProcessed &= ProcessTranWithMissingDocument(batchTran);
						continue;
					}

					var matchingResult = MatchStatuses(batchTran.SettlementStatus, extTran, payment, paymentGraph);
					if (matchingResult == StatusMatchingResult.NoMatch)
					{
						ValidateTransaction(extTran);
						fullyProcessed &= HandleExternalUpdate(batchTran);
						ccBatch = BatchView.Current;
						continue;
					}
					else if (matchingResult == StatusMatchingResult.VoidPaymentWithoutVoidTransaction)
					{
						try
						{
							VoidCardPayment(paymentGraph, payment);
						}
						catch
						{
							matchingResult = StatusMatchingResult.NoMatch;
						}
					}

					fullyProcessed &= UpdateRelatedRecords(ccBatch, batchTran, extTran, payment, matchingResult, paymentGraph);
				}
				catch (Exception e)
				{
					exceptions[batchTran.PCTranNumber] = e;
				}
			}

			if (exceptions.Any())
			{
				fullyProcessed = false;
				string Prefix(string id) => string.Format(Messages.ErrorWhileProcessingTransaction, id);
				string aggregatedMessage = string.Join(Environment.NewLine, exceptions.Select(e => Prefix(e.Key) + " " + e.Value.Message));
				throw new PXException(aggregatedMessage);
			}

			return fullyProcessed;
		}

		private bool HandleExternalUpdate(CCBatchTransaction batchTran)
		{
			var batchID = BatchView.Current.BatchID;
			BatchView.Cache.Clear();
			BatchView.Cache.ClearQueryCache();
			BatchView.Current = CCBatch.PK.Find(this, batchID);
			SelectTimeStamp();
			var updatedBatchTran = CCBatchTransaction.PK.Find(this, batchTran);
			if (updatedBatchTran?.ProcessingStatus != CCBatchTranProcessingStatusCode.Processed)
			{
				SetBatchTranStatus(batchTran, CCBatchTranProcessingStatusCode.Missing);
				return false;
			}
			return true;
		}

		internal static StatusMatchingResult MatchStatuses(string batchTranSettlementStatus, ExternalTransaction extTran, ARRegister payment, PXGraph graph)
		{
			string extTranProcStatus = extTran.ProcStatus;
			switch (batchTranSettlementStatus)
			{
				case CCBatchTranSettlementStatusCode.SettledSuccessfully:
					{
						if (extTranProcStatus == ExtTransactionProcStatusCode.CaptureSuccess)
							return StatusMatchingResult.SuccessMatch;

						if (extTranProcStatus.IsIn(
							ExtTransactionProcStatusCode.VoidFail,
							ExtTransactionProcStatusCode.VoidDecline))
						{
							var extTranStatus = ExternalTranHelper.GetTransactionState(graph, extTran);
							if (extTranStatus.IsCaptured || extTranStatus.IsRefunded)
							{
								return StatusMatchingResult.SuccessMatch;
							}
						}
					}
					break;
				case CCBatchTranSettlementStatusCode.Voided:
					{
						var extTranStatus = ExternalTranHelper.GetTransactionState(graph, extTran);
						if (payment.DocType.IsIn(ARDocType.VoidPayment, ARDocType.CashReturn))
						{
							return extTranStatus.IsCaptured ? StatusMatchingResult.VoidPaymentWithoutVoidTransaction : StatusMatchingResult.Match;
						}
						else
						{
							return (extTranStatus.IsCaptured && !extTranStatus.IsOpenForReview) ? StatusMatchingResult.NoMatchSkipValidation : StatusMatchingResult.Match;
						}
					}
				case CCBatchTranSettlementStatusCode.RefundSettledSuccessfully:
					{
						if (extTranProcStatus == ExtTransactionProcStatusCode.CreditSuccess)
							return StatusMatchingResult.SuccessMatch;
					}
					break;
				case CCBatchTranSettlementStatusCode.Declined:
					{
						if (extTranProcStatus.IsIn(
							ExtTransactionProcStatusCode.AuthorizeDecline,
							ExtTransactionProcStatusCode.CaptureDecline,
							ExtTransactionProcStatusCode.CreditDecline))
							return StatusMatchingResult.Match;
					}
					break;
				case CCBatchTranSettlementStatusCode.GeneralError:
					{
						if (extTranProcStatus.IsIn(
							ExtTransactionProcStatusCode.AuthorizeFail,
							ExtTransactionProcStatusCode.CaptureFail))
							return StatusMatchingResult.Match;
					}
					break;
				case CCBatchTranSettlementStatusCode.Expired:
					{
						if (extTranProcStatus.IsIn(
							ExtTransactionProcStatusCode.AuthorizeExpired,
							ExtTransactionProcStatusCode.CaptureExpired))
							return StatusMatchingResult.Match;
					}
					break;
			}
			return StatusMatchingResult.NoMatch;
		}

		private static ARPayment FindARPayment(PXGraph graph, string docType, string refNbr, string settlementStatus)
		{
			if (settlementStatus.IsIn(CCBatchTranSettlementStatusCode.Voided, CCBatchTranSettlementStatusCode.RefundSettledSuccessfully))
			{
				ARStandalone.ARRegister returnARRegister = PXSelect<ARStandalone.ARRegister,
					Where<ARStandalone.ARRegister.origDocType, Equal<Required<ARStandalone.ARRegister.origDocType>>,
					And<ARStandalone.ARRegister.origRefNbr, Equal<Required<ARStandalone.ARRegister.origRefNbr>>,
					And<ARStandalone.ARRegister.docType, In3<ARDocType.voidPayment, ARDocType.cashReturn>>>>>
					.Select(graph, docType, refNbr);
				if (returnARRegister != null)
					return ARPayment.PK.Find(graph, returnARRegister.DocType, returnARRegister.RefNbr);
			}

			return ARPayment.PK.Find(graph, docType, refNbr);
		}

		private void SetBatchTranStatus(CCBatchTransaction batchTran, string processingStatus)
		{
			using (var scope = new PXTransactionScope())
			{
				batchTran.ProcessingStatus = processingStatus;
				Transactions.Update(batchTran);
				Actions.PressSave();
				scope.Complete();
			}
		}

		private bool UpdateRelatedRecords(CCBatch ccBatch, CCBatchTransaction batchTran, ExternalTransaction extTran, ARPayment payment, StatusMatchingResult matchingResult, ARPaymentEntry paymentGraph)
		{
			bool success = true;
			using (var scope = new PXTransactionScope())
			{
				if (matchingResult != StatusMatchingResult.NoMatchSkipValidation)
				{
					batchTran.TransactionID = extTran.TransactionID;
					batchTran.OriginalStatus = extTran.ProcStatus;
					batchTran.DocType = payment.DocType;
					batchTran.RefNbr = payment.RefNbr;
				}

				if (matchingResult == StatusMatchingResult.NoMatch ||
					matchingResult == StatusMatchingResult.NoMatchSkipValidation)
				{
					batchTran.ProcessingStatus = CCBatchTranProcessingStatusCode.Missing;
					success = false;
				}
				else
				{
					batchTran.CurrentStatus = batchTran.OriginalStatus;
					batchTran.ProcessingStatus = CCBatchTranProcessingStatusCode.Processed;
					if (matchingResult == StatusMatchingResult.SuccessMatch)
					{
						extTran.Settled = true;
						ExternalTransactions.Update(extTran);
						MarkPaymentSettled(paymentGraph, payment, ccBatch);
					}
				}

				Transactions.Update(batchTran);
				BatchView.Update(ccBatch);
				Actions.PressSave();
				scope.Complete();
			}
			return success;
		}

		private void MarkPaymentSettled(ARPaymentEntry paymentGraph, ARPayment payment, CCBatch ccBatch)
		{
			if (new ARStandalone.ARCashSaleType.ListAttribute().ValueLabelDic.ContainsKey(payment.DocType))
			{
				var cashSaleGraph = PXGraph.CreateInstance<ARCashSaleEntry>();
				var arCashSale = ARStandalone.ARCashSale.PK.Find(cashSaleGraph, payment.DocType, payment.RefNbr);

				arCashSale.Settled = true;
				arCashSale.Cleared = true;
				arCashSale.ClearDate = ccBatch.SettlementTime;

				cashSaleGraph.Caches[typeof(ARStandalone.ARCashSale)].Update(arCashSale);
				cashSaleGraph.Save.Press();
			}
			else
			{
				payment = ARPayment.PK.Find(paymentGraph, payment);
				payment.Settled = true;
				payment.Cleared = true;
				payment.ClearDate = ccBatch.SettlementTime;

				paymentGraph.Caches[typeof(ARPayment)].Update(payment);
				paymentGraph.Save.Press();
			}
		}

		private bool ProcessTranWithMissingDocument(CCBatchTransaction batchTran)
		{
			if (batchTran.SettlementStatus.IsIn(
							CCBatchTranSettlementStatusCode.Declined,
							CCBatchTranSettlementStatusCode.Expired,
							CCBatchTranSettlementStatusCode.GeneralError))
			{
				SetBatchTranStatus(batchTran, CCBatchTranProcessingStatusCode.Processed);
				return true;
			}
			else
			{
				SetBatchTranStatus(batchTran, CCBatchTranProcessingStatusCode.Missing);
				return false;
			}
		}

		private void ValidateTransaction(ExternalTransaction transaction)
		{
			ExternalTransactionValidation.ValidateCCPayment(this, new List<IExternalTransaction> { transaction }, false);
		}

		private void VoidCardPayment(ARPaymentEntry graph, ARPayment payment)
		{
			graph.Document.Current = payment;
			var paymentTransactionExt = graph.GetExtension<ARPaymentEntry.PaymentTransaction>();
			paymentTransactionExt.voidCCPayment.Press();
		}

		protected CCProcessingBase.Interfaces.V2.TransactionData GetTranDetails(string procCenterId, string transactionId)
		{
			var paymentProcessing = new CCPaymentProcessing(this);
			var details = paymentProcessing.GetTransactionById(transactionId, procCenterId);
			return details;
		}

		private void PickValuablesFromTransactionData(CCProcessingBase.Interfaces.V2.TransactionData tranData, CCBatchTransaction batchTran)
		{
			if (batchTran.PCCustomerID == null)
			{
				batchTran.PCCustomerID = tranData.CustomerId;
				batchTran.PCPaymentProfileID = tranData.PaymentId;
				Transactions.Update(batchTran);
			}
		}
		#endregion

		#region Navigation
		public PXAction<CCBatchTransaction> ViewPaymentAll;
		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewPaymentAll(PXAdapter adapter)
		{
			return ViewPayment(adapter, Transactions.Current);
		}

		public PXAction<CCBatchTransaction> ViewPaymentExcl;
		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewPaymentExcl(PXAdapter adapter)
		{
			return ViewPayment(adapter, ExcludedFromDepositTransactions.Current);
		}

		private IEnumerable ViewPayment(PXAdapter adapter, CCBatchTransaction batchTransaction)
		{
			if (batchTransaction != null)
			{
				PXGraph documentGraph = GetDocumentGraph(batchTransaction.DocType, batchTransaction.RefNbr);
				if (documentGraph != null)
					throw new PXRedirectRequiredException(documentGraph, true, Messages.ViewDocument) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
			}
			return adapter.Get();
		}

		private PXGraph GetDocumentGraph(string docType, string refNbr)
		{
			if (new ARPaymentType.ListAttribute().ValueLabelDic.ContainsKey(docType))
				return GetARPaymentGraph(docType, refNbr);
			if (new ARStandalone.ARCashSaleType.ListAttribute().ValueLabelDic.ContainsKey(docType))
				return GetCashSaleGraph(docType, refNbr);
			throw new PXException(AR.Messages.UnknownDocumentType);
		}

		private ARCashSaleEntry GetCashSaleGraph(string docType, string refNbr)
		{
			var doc = ARStandalone.ARCashSale.PK.Find(this, docType, refNbr);
			if (doc == null)
				return null;
			var graph = PXGraph.CreateInstance<ARCashSaleEntry>();
			graph.Document.Current = doc;
			return graph;
		}

		private ARPaymentEntry GetARPaymentGraph(string docType, string refNbr)
		{
			var doc = ARPayment.PK.Find(this, docType, refNbr);
			if (doc == null)
				return null;
			var graph = PXGraph.CreateInstance<ARPaymentEntry>();
			graph.Document.Current = doc;
			return graph;
		}
		#endregion

		#region Events
		protected void _(Events.RowSelected<CCBatch> e)
		{
			CCBatch row = e.Row;
			if (row == null) return;

			bool batchProcessed = row.Status == CCBatchStatusCode.Processed && string.IsNullOrEmpty(row.DepositNbr);
			this.createDeposit.SetEnabled(batchProcessed);

			bool batchDeposited = row.Status == CCBatchStatusCode.Deposited;
			bool anythingSelectedToHide = MissingTransactions.Cache.Cached.RowCast<CCBatchTransactionAlias1>().Any(tran => tran?.BatchID == row.BatchID && tran?.SelectedToHide == true);
			this.hide.SetEnabled(!batchDeposited && anythingSelectedToHide);
			this.record.SetEnabled(!batchDeposited && anythingSelectedToHide);

			bool anythingSelectedToUnhide = Transactions.Cache.Cached.RowCast<CCBatchTransaction>().Any(tran => tran?.BatchID == row.BatchID && tran?.SelectedToUnhide == true);
			this.unhide.SetEnabled(!batchDeposited && anythingSelectedToUnhide);

			this.repeatMatching.SetEnabled(!batchDeposited);
		}

		protected virtual void _(Events.FieldSelecting<CCBatch.excludedCount> e)
		{
			e.ReturnValue = ExcludedFromDepositTransactions.Select().Count;
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Imported Count")]
		protected void _(Events.CacheAttached<CCBatch.importedTransactionCount> _) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Missing Count")]
		protected void _(Events.CacheAttached<CCBatch.missingCount> _) { }

		protected void _(Events.RowSelected<CCBatchTransaction> e)
		{
			CCBatchTransaction row = e.Row;
			if (row == null) return;
			PXCache cache = e.Cache;

			PXUIFieldAttribute.SetEnabled<CCBatchTransaction.selectedToUnhide>(cache, row, row.ProcessingStatus == CCBatchTranProcessingStatusCode.Hidden);

			ARPayment doc = Payment.Search<ARPayment.docType, ARPayment.refNbr>(row.DocType, row.RefNbr);
			UIState.RaiseOrHideError<CCBatchTransaction.pCTranNumber>(cache, row, doc?.Status == ARDocStatus.Balanced, Messages.UnreleasedDocsCannotBeDeposited, PXErrorLevel.RowWarning);
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Bank Deposit")]
		protected void _(Events.CacheAttached<ARPayment.depositNbr> _) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Doc. Status")]
		protected void _(Events.CacheAttached<ARPayment.status> _) { }
		#endregion

		#region Constructor and properties
		[Obsolete(InternalMessages.PropertyIsObsoleteAndWillBeRemoved2021R2)]
		public PXGraph Graph
		{
			get; private set;
		}

		CCBatchRepository _ccBatch;
		CCBatchTransactionRepository _ccBatchTransaction;
		CCBatchStatisticsRepository _ccBatchStatistics;
		ExternalTransactionRepository _externalTransactions;

		Lazy<ExternalTransactionRepository> _extTranRepo;
		ExternalTransactionRepository ExtTranRepo => _extTranRepo.Value;

		public CCBatchMaint()
		{
			this._extTranRepo = new Lazy<ExternalTransactionRepository>(() => new ExternalTransactionRepository(this));

			Save.SetVisible(false);
			Cancel.SetVisible(false);
		}
		#endregion

		#region Repository handling
		bool repositoryInitialized = false;
		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2021R2)]
		private void InitializeRepository(bool force = false)
		{
			if (repositoryInitialized & !force)
				return;
			var graph = PXGraph.CreateInstance<CCBatchHelperGraph>();
			Graph = graph;
			_ccBatch = new CCBatchRepository(graph);
			_ccBatchStatistics = new CCBatchStatisticsRepository(graph);
			_ccBatchTransaction = new CCBatchTransactionRepository(graph);
			_externalTransactions = new ExternalTransactionRepository(graph);
			repositoryInitialized = true;
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2021R2)]
		public CCBatch GetCCBatchByExtBatchId(string extBatchID)
		{
			InitializeRepository();
			return _ccBatch.GetCCBatchByExtBatchId(extBatchID);
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2021R2)]
		public CCBatch InsertBatch(CCBatch batch)
		{
			InitializeRepository();
			using (var scope = new PXTransactionScope())
			{
				var record = new CCBatch
				{
					ExtBatchID = batch.ExtBatchID,
					Status = batch.Status,
					ProcessingCenterID = batch.ProcessingCenterID,
					SettlementTimeUTC = batch.SettlementTimeUTC,
					SettlementState = batch.SettlementState,
					TransactionCount = batch.TransactionCount,
					ProcessedCount = batch.ProcessedCount,
					MissingCount = batch.MissingCount,
					SettledAmount = batch.SettledAmount,
					RefundAmount = batch.RefundAmount
				};

				record = _ccBatch.InsertCCBatch(record);
				SaveRepository();
				scope.Complete();
				return record;
			}
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2021R2)]
		public CCBatch UpdateBatch(CCBatch item)
		{
			InitializeRepository();
			using (var scope = new PXTransactionScope())
			{
				item = _ccBatch.UpdateCCBatch(item);
				SaveRepository();
				scope.Complete();
			}
			return item;
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2021R2)]
		public CCBatchStatistics InsertBatchStatistics(CCBatchStatistics statistics)
		{
			InitializeRepository();
			using (var scope = new PXTransactionScope())
			{
				var record = new CCBatchStatistics
				{
					BatchID = statistics.BatchID,
					CardType = statistics.CardType,
					DeclineCount = statistics.DeclineCount,
					ErrorCount = statistics.ErrorCount,
					RefundAmount = statistics.RefundAmount,
					RefundCount = statistics.RefundCount,
					SettledAmount = statistics.SettledAmount,
					SettledCount = statistics.SettledCount,
					VoidCount = statistics.VoidCount
				};

				record = _ccBatchStatistics.InsertCCBatchStatistics(record);
				SaveRepository();
				scope.Complete();
				return record;
			}
		}

		// The method should be made private
		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2021R2)]
		public PXResultset<CCBatchTransaction> GetBatchTransactions(int? batchID, params string[] procStatuses)
		{
			return PXSelect<CCBatchTransaction, Where<CCBatchTransaction.batchID, Equal<Required<CCBatch.batchID>>,
				And<CCBatchTransaction.processingStatus, In<Required<CCBatchTransaction.processingStatus>>>>>
				.Select(this, batchID, procStatuses);
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2021R2)]
		public CCBatchTransaction InsertBatchTransaction(CCBatchTransaction transaction)
		{
			InitializeRepository();

			var record = new CCBatchTransaction
			{
				BatchID = transaction.BatchID,
				Amount = transaction.Amount,
				AccountNumber = transaction.AccountNumber,
				CardType = transaction.CardType,
				CurrentStatus = transaction.CurrentStatus,
				OriginalStatus = transaction.OriginalStatus,
				PCTranNumber = transaction.PCTranNumber,
				PCCustomerID = transaction.PCCustomerID,
				PCPaymentProfileID = transaction.PCPaymentProfileID,
				SubmitTime = transaction.SubmitTime,
				ProcessingStatus = transaction.ProcessingStatus,
				SettlementStatus = transaction.SettlementStatus,
				InvoiceNbr = transaction.InvoiceNbr,
				FixedFee = transaction.FixedFee,
				PercentageFee = transaction.PercentageFee,
				FeeType = transaction.FeeType
			};

			record = _ccBatchTransaction.InsertCCBatchTransaction(record);
			return record;
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2021R2)]
		public void UpdateBatchTransaction(CCBatchTransaction item)
		{
			InitializeRepository();
			_ccBatchTransaction.UpdateCCBatchTransaction(item);
		}

		// The method should be made private
		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2021R2)]
		public ExternalTransaction GetExternalTransaction(string cCProcessingCenterID, string tranNumber)
		{
			ExternalTransactions.Cache.Clear();
			ExternalTransactions.Cache.ClearQueryCache();

			var records = ExtTranRepo.GetExternalTransaction(cCProcessingCenterID, tranNumber);
			foreach (var extTran in records)
			{
				if (extTran.NeedSync != true && extTran.SyncStatus != CCSyncStatusCode.Error)
				{
					return extTran;
				}
			}
			return null;
		}

		// The method should be made private
		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2021R2)]
		public ExternalTransaction GetExternalTransactionByNoteID(Guid? noteID)
		{
			ExternalTransactions.Cache.Clear();
			ExternalTransactions.Cache.ClearQueryCache();

			var extTran = ExtTranRepo.GetExternalTransactionByNoteID(noteID);
			if (extTran?.NeedSync != true && extTran?.SyncStatus != CCSyncStatusCode.Error)
			{
				return extTran;
			}
			return null;
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2021R2)]
		public void UpdateExternalTransaction(ExternalTransaction item)
		{
			InitializeRepository();
			_externalTransactions.UpdateExternalTransaction(item);
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2021R2)]
		public void SaveRepository()
		{
			Graph?.Actions.PressSave();
		}
		#endregion Repository handling
	}

	#region Repository Types
	[Obsolete(InternalMessages.ClassIsObsoleteAndWillBeRemoved2021R2)]
	public class CCBatchRepository
	{
		protected readonly PXGraph graph;
		public CCBatchRepository(PXGraph graph)
		{
			this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
		}

		public CCBatch GetCCBatch(int? batchID)
		{
			return PXSelect<CCBatch, Where<CCBatch.batchID, Equal<Required<CCBatch.batchID>>>>
				.Select(graph, batchID);
		}

		public CCBatch GetCCBatchByExtBatchId(string extBatchID)
		{
			return PXSelect<CCBatch, Where<CCBatch.extBatchID, Equal<Required<CCBatch.extBatchID>>>>
				.Select(graph, extBatchID);
		}

		public CCBatch InsertCCBatch(CCBatch ccBatch)
		{
			return graph.Caches[typeof(CCBatch)].Insert(ccBatch) as CCBatch;
		}

		public CCBatch UpdateCCBatch(CCBatch ccBatch)
		{
			return graph.Caches[typeof(CCBatch)].Update(ccBatch) as CCBatch;
		}
	}

	[Obsolete(InternalMessages.ClassIsObsoleteAndWillBeRemoved2021R2)]
	public class CCBatchStatisticsRepository
	{
		protected readonly PXGraph graph;
		public CCBatchStatisticsRepository(PXGraph graph)
		{
			this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
		}

		public CCBatchStatistics GetCCBatchStatistics(int? batchID, string cardType)
		{
			return PXSelect<CCBatchStatistics, Where<CCBatchStatistics.batchID, Equal<Required<CCBatchStatistics.batchID>>,
				And<CCBatchStatistics.cardType, Equal<Required<CCBatchStatistics.cardType>>>>>.Select(graph, batchID, cardType);
		}

		public CCBatchStatistics InsertCCBatchStatistics(CCBatchStatistics ccBatch)
		{
			return graph.Caches[typeof(CCBatchStatistics)].Insert(ccBatch) as CCBatchStatistics;
		}

		public CCBatchStatistics UpdateCCBatchStatistics(CCBatchStatistics ccBatch)
		{
			return graph.Caches[typeof(CCBatchStatistics)].Update(ccBatch) as CCBatchStatistics;
		}
	}

	[Obsolete(InternalMessages.ClassIsObsoleteAndWillBeRemoved2021R2)]
	public class CCBatchTransactionRepository
	{
		protected readonly PXGraph graph;
		public CCBatchTransactionRepository(PXGraph graph)
		{
			this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
		}

		public CCBatchTransaction GetCCBatchTransaction(int? batchID, int transactionID)
		{
			return PXSelect<CCBatchTransaction, Where<CCBatchTransaction.batchID, Equal<Required<CCBatch.batchID>>,
				And<CCBatchTransaction.transactionID, Equal<Required<CCBatchTransaction.transactionID>>>>>
				.Select(graph, batchID, transactionID);
		}

		public PXResultset<CCBatchTransaction> GetCCBatchTransactions(int? batchID, params string[] procStatuses)
		{
			return PXSelect<CCBatchTransaction, Where<CCBatchTransaction.batchID, Equal<Required<CCBatch.batchID>>,
				And<CCBatchTransaction.processingStatus, In<Required<CCBatchTransaction.processingStatus>>>>>
				  .Select(graph, batchID, procStatuses);
		}

		public CCBatchTransaction InsertCCBatchTransaction(CCBatchTransaction ccBatchTransaction)
		{
			return graph.Caches[typeof(CCBatchTransaction)].Insert(ccBatchTransaction) as CCBatchTransaction;
		}

		public CCBatchTransaction UpdateCCBatchTransaction(CCBatchTransaction ccBatchTransaction)
		{
			return graph.Caches[typeof(CCBatchTransaction)].Update(ccBatchTransaction) as CCBatchTransaction;
		}
	}

	[Obsolete(InternalMessages.ClassIsObsoleteAndWillBeRemoved2021R2)]
	public class CCBatchHelperGraph : PXGraph<CCBatchHelperGraph>
	{
		public PXSelect<CCBatch> CCBatches;
		public PXSelect<CCBatchStatistics> CCBatchStatistics;
		public PXSelect<CCBatchTransaction> CCBatchTransactions;
		public PXSelect<ExternalTransaction> ExternalTransactions;
	}
	#endregion Repository Types
}
