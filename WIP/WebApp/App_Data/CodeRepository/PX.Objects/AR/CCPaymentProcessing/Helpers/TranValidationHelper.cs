using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using PX.Data;
using PX.Objects;
using PX.Objects.Common;
using PX.Objects.AR;
using PX.Objects.Extensions.PaymentTransaction;
using PX.CCProcessingBase.Interfaces.V2;
using PX.Objects.AR.CCPaymentProcessing;
using PX.Objects.AR.CCPaymentProcessing.Repositories;
using PX.Objects.AR.CCPaymentProcessing.Interfaces;
using PX.Objects.AR.CCPaymentProcessing.Wrappers;
using ExternalTransactionState = PX.Objects.AR.CCPaymentProcessing.Common.ExternalTransactionState;

namespace PX.Objects.AR.CCPaymentProcessing.Helpers
{
	public static class TranValidationHelper
	{
		public class TranValidationException : PXException
		{
			public TranValidationException(SerializationInfo info, StreamingContext context) : base(info, context) { }

			public TranValidationException(string format, params object[] args) : base(format, args)
			{
				_MessagePrefix = null;
			}
		}

		public class AdditionalParams
		{
			public int? PMInstanceId { get; set; }
			public int? CustomerID { get; set; }
			public string ProcessingCenter { get; set; }
			public ICCPaymentProcessingRepository Repo { get; set; }
		}

		public static void CheckRecordedTranStatus(TransactionData tranData)
		{
			if (tranData.TranStatus == CCTranStatus.Declined)
			{
				throw new TranValidationException(Messages.ERR_IncorrectDeclinedTranStatus, tranData.TranID);
			}

			if (tranData.TranStatus == CCTranStatus.Expired)
			{
				throw new TranValidationException(Messages.ERR_IncorrectExpiredTranStatus, tranData.TranID);
			}

			if (tranData.TranStatus == CCTranStatus.Error || tranData.TranStatus == CCTranStatus.Unknown)
			{
				throw new TranValidationException(Messages.ERR_IncorrectTranStatus, tranData.TranID);
			}
			
		}

		public static void CheckTranTypeForRefund(TransactionData tranData)
		{
			if (tranData.TranType != null && (tranData.TranType == CCTranType.AuthorizeOnly
				|| CCTranTypeCode.IsCaptured(V2Converter.ConvertTranType(tranData.TranType.Value))))
			{
				var tranType = V2Converter.ConvertTranType(tranData.TranType.Value);
				var typeLabel = CCTranTypeCode.GetTypeLabel(tranType);
				throw new TranValidationException(AR.Messages.ERR_InvalidTransactionTypeForCustomerRefund);
			}
		}

		public static void CheckTranAmount(TransactionData tranData, IExternalTransaction storedTran)
		{
			var procStatus = CCProcessingHelper.GetProcessingStatusByTranData(tranData);
			string procStatusStr = ExtTransactionProcStatusCode.GetProcStatusStrByProcessingStatus(procStatus);
			if ((storedTran.ProcStatus == ExtTransactionProcStatusCode.AuthorizeSuccess
					|| storedTran.ProcStatus == ExtTransactionProcStatusCode.AuthorizeHeldForReview) 
				&& procStatusStr == ExtTransactionProcStatusCode.CaptureSuccess)
			{
				if (tranData.Amount > storedTran.Amount)
				{
					throw new TranValidationException(Messages.ERR_IncorrectTranAmount, tranData.TranID);
				}
			}
			else if (tranData.Amount != storedTran.Amount)
			{
				throw new TranValidationException(Messages.ERR_IncorrectTranAmount, tranData.TranID);
			}
		}

		public static void CheckCustomer(TransactionData tranData, int? customer, CustomerPaymentMethod cpm)
		{
			if (cpm != null && customer != null && cpm.BAccountID != customer)
			{
				throw new TranValidationException(Messages.ERR_TranIsUsedForAnotherCustomer, tranData.TranID);
			}
		}

		public static void CheckPmInstance(TransactionData tranData, Tuple<CustomerPaymentMethod, CustomerPaymentMethodDetail> cpmData)
		{
			if (tranData != null && cpmData != null)
			{
				CustomerPaymentMethod cpm = cpmData.Item1;
				CustomerPaymentMethodDetail cpmd = cpmData.Item2;
				if (tranData.CustomerId != null && (tranData.CustomerId != cpm.CustomerCCPID || tranData.PaymentId != cpmd.Value))
				{
					throw new TranValidationException(Messages.ERR_IncorrectPmInstanceId, tranData.TranID);
				}
			}
		}

		public static void CheckActiveTransactionStateForPayment(Common.ICCPayment checkedPayment, TransactionData tranData, ExternalTransactionState activeState)
		{
			var activeTran = activeState?.ExternalTransaction;

			if ((activeTran == null || activeState.IsPreAuthorized) && tranData.TranType == CCTranType.Credit)
			{
				throw new TranValidationException(Messages.ERR_CCNoTransactionToRefund, tranData.TranID, 
					checkedPayment.RefNbr, GetDocumentName(checkedPayment.DocType));
			}
			
			if (activeTran != null)
			{
				if (tranData.TranType == CCTranType.Credit && tranData.RefTranID != null
					&& activeTran.TranNumber != tranData.RefTranID)
				{
					throw new TranValidationException(Messages.ERR_RefundTranNotLinkedOrigTran, tranData.TranID, activeTran.TranNumber);
				}

				if (tranData.TranID != activeTran.TranNumber && tranData.RefTranID != activeTran.TranNumber)
				{
					if ((activeState.IsPreAuthorized || activeState.IsCaptured) && tranData.TranType == CCTranType.Void)
					{
						var voidLabel = CCProcessingHelper.GetTransactionTypeName(Common.CCTranType.Void);
						var activeTranLabel = activeState.IsPreAuthorized
							? CCProcessingHelper.GetTransactionTypeName(Common.CCTranType.AuthorizeOnly)
							: CCProcessingHelper.GetTransactionTypeName(Common.CCTranType.AuthorizeAndCapture);
						throw new TranValidationException(Messages.ERR_TranNotLinked, tranData.TranID, voidLabel, activeTran.TranNumber, activeTranLabel);
					}
					if (activeState.IsPreAuthorized && tranData.TranType == CCTranType.PriorAuthorizedCapture)
					{
						string capLabel = CCProcessingHelper.GetTransactionTypeName(Common.CCTranType.PriorAuthorizedCapture);
						string authLabel = CCProcessingHelper.GetTransactionTypeName(Common.CCTranType.AuthorizeOnly);
						throw new TranValidationException(Messages.ERR_TranNotLinked, tranData.TranID, capLabel,
							activeTran.TranNumber, authLabel);
					}
				}

				if (tranData.TranID != activeTran.TranNumber)
				{
					if (activeState.IsRefunded && tranData.TranType == CCTranType.Credit)
					{
						throw new TranValidationException(Messages.ERR_CCPaymentIsAlreadyRefunded);
					}
					if (activeState.IsCaptured && tranData.TranType != null 
						&& CCTranTypeCode.IsCaptured(V2Converter.ConvertTranType(tranData.TranType.Value)))
					{
						throw new TranValidationException(Messages.ERR_CCAuthorizedPaymentAlreadyCaptured);
					}
					if (activeState.IsPreAuthorized && tranData.TranType == CCTranType.AuthorizeOnly)
					{
						throw new TranValidationException(Messages.ERR_CCPaymentAlreadyAuthorized);
					}
				}
			}
		}

		public static void CheckSharedTranIsSuitableForRefund(Common.ICCPayment checkedPayment, TransactionData tranData, AdditionalParams prms)
		{
			if (tranData.TranType != CCTranType.Void) return;
			PXGraph graph = prms.Repo.Graph;
			var query = new PXSelect<ARPayment, Where<ARPayment.docType, Equal<Required<ARPayment.docType>>, 
				And<ARPayment.refNbr, Equal<Required<ARPayment.refNbr>>>>>(graph);
			var checkedPmt = query.SelectSingle(checkedPayment.DocType, checkedPayment.RefNbr);
			string tranNumber = tranData.RefTranID;
			if (tranNumber == null)
			{
				throw new TranValidationException(Messages.ERR_CCNoTransactionToVoid);
			}
			var dataFromOtherDoc = prms.Repo.GetExternalTransactionWithPayment(tranNumber, checkedPmt.ProcessingCenterID);
			if (dataFromOtherDoc == null)
			{
				throw new TranValidationException(Messages.ERR_CCNoTransactionToVoid);
			}
	
			var storedPmt = dataFromOtherDoc.Item2;
			var storedExtTran = dataFromOtherDoc.Item1;
			CheckNewAndStoredPayment(checkedPmt, storedPmt, storedExtTran);
			if (checkedPmt.CuryDocBal != storedExtTran.Amount)
			{
				throw new TranValidationException(Messages.ERR_IncorrectTranAmount, storedExtTran.TransactionID);
			}
			var storedState = ExternalTranHelper.GetTransactionState(graph, storedExtTran);
			if (storedState.IsRefunded)
			{
				throw new TranValidationException(Messages.ERR_IncorrectRefundTranType, storedExtTran.TranNumber);
			}
			if (storedState.IsVoided)
			{
				throw new TranValidationException(Messages.ERR_IncorrectVoidTranType, storedExtTran.TranNumber);
			}
		}

		public static void CheckNewAndStoredPayment(ARPayment checkedPmt, ARPayment storedPmt, IExternalTransaction storedExtTran)
		{
			if (checkedPmt != null && storedPmt != null && storedExtTran != null)
			{
				string tranNbr = storedExtTran.TranNumber;
				if (checkedPmt.CustomerID != storedPmt.CustomerID)
				{
					throw new TranValidationException(Messages.ERR_TranIsUsedForAnotherCustomer, tranNbr);
				}
				if (checkedPmt.PMInstanceID != PaymentTranExtConstants.NewPaymentProfile)
				{
					if (storedPmt.PMInstanceID != checkedPmt.PMInstanceID)
					{
						throw new TranValidationException(Messages.ERR_IncorrectPmInstanceId, tranNbr);
					}
				}
				else
				{
					if (storedPmt.CustomerID != checkedPmt.CustomerID)
					{
						throw new TranValidationException(Messages.ERR_TranIsUsedForAnotherCustomer, tranNbr);
					}
					if (storedPmt.PaymentMethodID != checkedPmt.PaymentMethodID)
					{
						throw new TranValidationException(Messages.ERR_IncorrectPaymentMethod, tranNbr);
					}
					if (storedPmt.ProcessingCenterID != checkedPmt.ProcessingCenterID)
					{
						throw new TranValidationException(Messages.ERR_IncorrectProcCenterID, tranNbr);
					}
				}
				if (storedPmt.CashAccountID != checkedPmt.CashAccountID)
				{
					throw new TranValidationException(Messages.ERR_IncorrectCashAccount, tranNbr);
				}
				if (storedExtTran.NeedSync == true)
				{
					throw new TranValidationException(Messages.ERR_TransactionIsNotValidated, tranNbr);
				}
				if (checkedPmt.CuryDocBal > storedExtTran.Amount)
				{
					throw new TranValidationException(Messages.ERR_IncorrectTranAmount, storedExtTran.TransactionID);
				}
			}
		}

		public static void CheckPaymentProfile(TransactionData tranData, AdditionalParams prms)
		{
			CustomerPaymentMethod cpm = null;
			Tuple<CustomerPaymentMethod, CustomerPaymentMethodDetail> res = null;
			ICCPaymentProcessingRepository repo = prms.Repo;
			if (prms.PMInstanceId > 0)
			{
				res = repo.GetCustomerPaymentMethodWithProfileDetail(prms.PMInstanceId);
				if (res == null)
				{
					throw new TranValidationException(AR.Messages.CreditCardWithID_0_IsNotDefined, prms.PMInstanceId);
				}
				else
				{
					CheckPmInstance(tranData, res);
				}
			}
			else
			{
				res = repo.GetCustomerPaymentMethodWithProfileDetail(prms.ProcessingCenter, tranData.CustomerId, tranData.PaymentId);
				if (res != null)
				{
					cpm = res.Item1;
					CheckCustomer(tranData, prms.CustomerID, cpm);
				}
				else
				{
					if (cpm == null && tranData.CustomerId != null)
					{	
						CustomerData ret = CCCustomerInformationManager.GetCustomerProfile(repo.Graph, prms.ProcessingCenter, tranData.CustomerId);
						if (!string.IsNullOrEmpty(ret?.CustomerCD))
						{
							string ccCustCD = CCProcessingHelper.DeleteCustomerPrefix(ret.CustomerCD.Trim());
							Customer customer = Customer.PK.Find(prms.Repo.Graph, prms.CustomerID);
							string customerCD = customer.AcctCD.Trim();
							
							if (!customerCD.Equals(ccCustCD))
							{
								Customer customerTran = CCProcessingHelper.GetCustomer(prms.Repo.Graph, ccCustCD);
								
								if (customerTran != null)
									throw new TranValidationException(Messages.ERR_TranIsUsedForAnotherCustomer, tranData.TranID);
							}
						}
					}
				}
			}
		}

		public static void CheckTranAlreadyRecorded(TransactionData tranData, AdditionalParams inputParams)
		{
			var query = new PXSelect<ExternalTransaction, Where<ExternalTransaction.tranNumber, Equal<Required<ExternalTransaction.tranNumber>>,
				And<ExternalTransaction.processingCenterID, Equal<Required<ExternalTransaction.processingCenterID>>,
				And<Not<ExternalTransaction.syncStatus, Equal<CCSyncStatusCode.error>,
					And<ExternalTransaction.active, Equal<False>>>>>>>(inputParams.Repo.Graph);
			ExternalTransaction storedTran = query.SelectSingle(tranData.TranID, inputParams.ProcessingCenter);
			if (storedTran != null)
			{
				throw new TranValidationException(GenerateTranAlreadyRecordedErrMsg(tranData.TranID, storedTran, inputParams));
			}
		}

		public static string GenerateTranAlreadyRecordedErrMsg(string tranId, ExternalTransaction extTran, AdditionalParams inputParams)
		{
			return GenerateTranAlreadyRecordedErrMsg(tranId, extTran.DocType, extTran.RefNbr, inputParams);
		}

		public static string GenerateTranAlreadyRecordedErrMsg(string tranId, CCProcTran procTran, AdditionalParams inputParams)
		{
			return GenerateTranAlreadyRecordedErrMsg(tranId, procTran.DocType, procTran.RefNbr, inputParams);
		}

		public static string GetDocumentName(string docType)
		{
			string docName = PXMessages.LocalizeNoPrefix(Messages.Document);
			if (docType != null)
			{
				ARDocType arDocType = new ARDocType();
				var res = arDocType.ValueLabelPairs.FirstOrDefault(i => i.Value == docType);
				if (res.Label != null)
				{
					docName = PXMessages.LocalizeNoPrefix(res.Label).ToLower();
				}
			}
			return docName;
		}

		public static string GenerateTranAlreadyRecordedErrMsg(string tranId, string docType, string refNbr, AdditionalParams inputParams)
		{
			string ret = null;
			string docName = GetDocumentName(docType);
			string doc = null;

			if (docType != null)
			{
				doc = docType + refNbr;
			}

			if (inputParams.PMInstanceId > 0)
			{
				var repo = inputParams.Repo;
				CustomerPaymentMethod cpm = inputParams.Repo.GetCustomerPaymentMethod(inputParams.PMInstanceId);
				ret = PXMessages.LocalizeFormatNoPrefix(Messages.ERR_TransactionWithCpmIsRecordedForOtherDoc, tranId, cpm.Descr, doc, docName);
			}
			else
			{
				ret = PXMessages.LocalizeFormatNoPrefix(Messages.ERR_TransactionIsRecordedForOtherDoc, tranId, inputParams.ProcessingCenter, doc, docName);
			}
			return ret;
		}
	}
}
