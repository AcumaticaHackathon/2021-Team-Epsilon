using PX.Data;
using PX.Objects.AP;
using PX.Objects.CN.Common.Services.DataProviders;
using PX.Objects.CN.JointChecks.AP.CacheExtensions;
using PX.Objects.CN.JointChecks.Descriptor;

namespace PX.Objects.CN.JointChecks.AP.Services.ChecksAndPaymentsServices.Validation
{
    public class AdjustmentCurrencyValidationService : ValidationServiceBase
    {
        public AdjustmentCurrencyValidationService(APPaymentEntry graph)
            : base(graph)
        {
        }

        public void Validate()
        {
            var payment = Graph.Document.Current;
            if (payment.Hold == true) return;
            var paymentExt = PXCache<APPayment>.GetExtension<ApPaymentExt>(payment);
            foreach (var adjustment in ActualBillAdjustments)
            {
                var invoice = InvoiceDataProvider.GetInvoice(Graph, adjustment.AdjdDocType, adjustment.AdjdRefNbr);
                if (paymentExt.IsJointCheck == true && paymentExt.JointPaymentAmount > 0 && payment.CuryID != invoice.CuryID)
                {
                    ShowErrorMessage<APAdjust.curyAdjgAmt>(adjustment, JointCheckMessages.PaymentCurrencyDiffersFromBill);
                    ShowErrorOnPersistIfRequired(Graph.Adjustments.Cache, true);
                }
            }
        }
    }
}