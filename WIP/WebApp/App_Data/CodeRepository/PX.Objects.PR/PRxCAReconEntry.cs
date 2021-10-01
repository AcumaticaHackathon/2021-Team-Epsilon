using System.Collections;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CA;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.PR
{
	public class PRxCAReconEntry : PXGraphExtension<CAReconEntry>
	{
		public PXSelectJoin<CAReconEntry.CATranInBatch,
			InnerJoinSingleTable<PRPayment,
				On<PRPayment.caTranID, Equal<CAReconEntry.CATranInBatch.tranID>>,
			InnerJoin<CABatchDetail,
				On<BatchModule.modulePR, Equal<CABatchDetail.origModule>,
				And<PRPayment.docType, Equal<CABatchDetail.origDocType>,
				And<PRPayment.refNbr, Equal<CABatchDetail.origRefNbr>>>>>>,
			Where<CABatchDetail.batchNbr, Equal<Required<CABatch.batchNbr>>>> PRPaymentTransactionsInBatch;

		public PXSelectJoin<CAReconEntry.CATranInBatch,
			InnerJoinSingleTable<PRDirectDepositSplit,
				On<PRDirectDepositSplit.caTranID, Equal<CAReconEntry.CATranInBatch.tranID>>,
			InnerJoin<CABatchDetail,
				On<BatchModule.modulePR, Equal<CABatchDetail.origModule>,
				And<PRDirectDepositSplit.docType, Equal<CABatchDetail.origDocType>,
				And<PRDirectDepositSplit.refNbr, Equal<CABatchDetail.origRefNbr>,
				And<PRDirectDepositSplit.lineNbr, Equal<CABatchDetail.origLineNbr>>>>>>>,
			Where<CABatchDetail.batchNbr, Equal<Required<CABatch.batchNbr>>>> PRDDSplitTransactionsInBatch;

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		public delegate IEnumerable ViewDocDelegate(PXAdapter adapter);

		[PXOverride]
		public virtual IEnumerable ViewDoc(PXAdapter adapter, ViewDocDelegate baseMethod)
		{
			CAReconEntry.CATranExt caTransaction = Base.CAReconTranRecords.Current;
			if (caTransaction == null || caTransaction.OrigModule != BatchModule.PR)
				return baseMethod(adapter);

			if (caTransaction.OrigTranType == CATranType.CABatch)
			{
				PRCABatch directDepositBatch =
					SelectFrom<PRCABatch>
						.Where<PRCABatch.batchNbr.IsEqual<P.AsString>.And<CABatch.origModule.IsEqual<BatchModule.modulePR>>>.View
						.SelectSingleBound(Base, null, caTransaction.OrigRefNbr);

				PRDirectDepositBatchEntry graph = PXGraph.CreateInstance<PRDirectDepositBatchEntry>();
				graph.Document.Current = directDepositBatch;
				PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.NewWindow);

				return adapter.Get();
			}

			PRPayment payment =
				SelectFrom<PRPayment>
					.Where<PRPayment.refNbr.IsEqual<P.AsString>.And<PRPayment.docType.IsEqual<P.AsString>>>.View
					.SelectSingleBound(Base, null, caTransaction.OrigRefNbr, caTransaction.OrigTranType);

			if (payment != null)
			{
				PRPayChecksAndAdjustments graph = PXGraph.CreateInstance<PRPayChecksAndAdjustments>();
				graph.Document.Current = payment;
				PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.NewWindow);
			}
			return adapter.Get();
		}

		public delegate void UpdateBatchTransactionsDelegate(CAReconEntry.CATranExt row);

		[PXOverride]
		public virtual void UpdateBatchTransactions(CAReconEntry.CATranExt row, UpdateBatchTransactionsDelegate baseMethod)
		{
			baseMethod(row);

			if (row.OrigModule == BatchModule.PR && row.OrigTranType == CATranType.CABatch)
			{
				foreach (CAReconEntry.CATranInBatch innerTran in PRPaymentTransactionsInBatch.Select(row.OrigRefNbr))
				{
					innerTran.CopyFrom(row);
					PRPaymentTransactionsInBatch.Update(innerTran);
				}
				foreach (CAReconEntry.CATranInBatch innerTran in PRDDSplitTransactionsInBatch.Select(row.OrigRefNbr))
				{
					innerTran.CopyFrom(row);
					PRDDSplitTransactionsInBatch.Update(innerTran);
				}

				Base.UpdateBatch(row);
			}
		}
	}
}
