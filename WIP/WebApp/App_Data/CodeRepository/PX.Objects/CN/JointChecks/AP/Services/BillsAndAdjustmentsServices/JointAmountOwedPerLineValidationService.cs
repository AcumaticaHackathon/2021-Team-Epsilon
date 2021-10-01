using System.Linq;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.CN.Common.Services.DataProviders;
using PX.Objects.CN.JointChecks.AP.CacheExtensions;
using PX.Objects.CN.JointChecks.AP.DAC;
using PX.Objects.CN.JointChecks.AP.Services.DataProviders;
using PX.Objects.CN.JointChecks.Descriptor;

namespace PX.Objects.CN.JointChecks.AP.Services.BillsAndAdjustmentsServices
{
    public class JointAmountOwedPerLineValidationService : JointAmountOwedValidationService
    {
        private ApTranExt transactionExtension;

        public JointAmountOwedPerLineValidationService(PXSelect<JointPayee> jointPayees, APInvoiceEntry graph)
            : base(jointPayees, graph)
        {
        }

        public override void ValidateAmountOwed(JointPayee jointPayee)
        {
            if (jointPayee.BillLineNumber == null)
            {
                return;
            }
            var transaction = TransactionDataProvider.GetTransaction(Graph, CurrentBill.DocType, CurrentBill.RefNbr,
                jointPayee.BillLineNumber);
            transactionExtension = PXCache<APTran>.GetExtension<ApTranExt>(transaction);
            ValidateJointAmountOwedPerLine(jointPayee);
            base.ValidateAmountOwed(jointPayee);
        }

        public override void RecalculateTotalJointAmount()
        {
            var jointPayeesByLineGroups = JointPayees.SelectMain().GroupBy(jp => jp.BillLineNumber)
                .Where(jp => jp.Key != null);
            foreach (var jointPayeesByLine in jointPayeesByLineGroups)
            {
                APTran transaction;
                if (CurrentBill.IsRetainageDocument == true)
                {
                    transaction = GetTransactionByOriginalLineNbr(Graph, CurrentBill.DocType, CurrentBill.RefNbr,
                        jointPayeesByLine.Key);
                }
                else
                {
                    transaction = TransactionDataProvider.GetTransaction(Graph, CurrentBill.DocType, CurrentBill.RefNbr, 
                        jointPayeesByLine.Key);
                }

                transactionExtension = PXCache<APTran>.GetExtension<ApTranExt>(transaction);
                transactionExtension.TotalJointAmountPerLine = jointPayeesByLine.Sum(jp => jp.JointAmountOwed);
            }
        }

        private APTran GetTransactionByOriginalLineNbr(PXGraph graph, string docType, string refNbr, int? origLineNbr)
        {
            var select = new PXSelect<APTran, Where<APTran.tranType, Equal<Required<APTran.tranType>>,
                And<APTran.refNbr, Equal<Required<APTran.refNbr>>,
                And<APTran.origLineNbr, Equal<Required<APTran.origLineNbr>>>>>>(graph);

            return select.SelectSingle(docType, refNbr, origLineNbr);
        }

        protected override void ValidateTotalJointAmountOwed(JointPayee jointPayee)
        {
            var cashDiscount = GetCurrentCashDiscountValue(jointPayee);
            if (transactionExtension.TotalJointAmountPerLine > jointPayee.BillLineAmount - cashDiscount)
            {
                ShowAmountOwedExceedsBillAmountException(jointPayee, cashDiscount == 0,
                    JointCheckMessages.TotalJointAmountOwedExceedsApBillLineAmount,
                    JointCheckMessages.TotalJointAmountOwedExceedsApBillLineAmountWithCashDiscount);
            }
        }

        private void ValidateJointAmountOwedPerLine(JointPayee jointPayee)
        {
            var cashDiscount = GetCurrentCashDiscountValue(jointPayee);
            if (jointPayee.JointAmountOwed > jointPayee.BillLineAmount - cashDiscount)
            {
                ShowAmountOwedExceedsBillAmountException(jointPayee, cashDiscount == 0,
                    JointCheckMessages.JointAmountOwedExceedsApBillLineAmount,
                    JointCheckMessages.JointAmountOwedExceedsApBillLineAmountWithCashDiscount);
            }
        }

        private decimal? GetCurrentCashDiscountValue(JointPayee jointPayee)
        {
            return CurrentBill.CuryLineTotal == decimal.Zero
                ? decimal.Zero
                : jointPayee.BillLineAmount / CurrentBill.CuryLineTotal * CurrentBill.CuryOrigDiscAmt;
        }
    }
}