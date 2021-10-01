using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.CN.Common.Extensions;
using PX.Objects.CN.Common.Services.DataProviders;
using PX.Objects.CN.JointChecks.AP.CacheExtensions;
using PX.Objects.CN.JointChecks.AP.DAC;
using PX.Objects.CN.JointChecks.AP.Services.DataProviders;
using PX.Objects.Common;
using PX.Objects.CS;

namespace PX.Objects.CN.JointChecks.AP.GraphExtensions.PreparePayments
{
    public class ApPayBillsExt : PXGraphExtension<APPayBills>
    {
        public PXFilter<DocumentFilter> SelectedDocument;
        public PXSelect<JointPayeePayment> JointPayeePayments;
        public PXSelect<JointPayee> JointPayees;

        public SelectFrom<APInvoice>
            .Where<APInvoice.refNbr.IsEqual<DocumentFilter.adjdRefNbr.FromCurrent>
                .And<APInvoice.docType.IsEqual<DocumentFilter.adjdDocType.FromCurrent>>>.View CurrentBill;
        public APInvoice CurrentInvoice =>
            InvoiceDataProvider.GetInvoice(Base, SelectedDocument.Current?.AdjdDocType, SelectedDocument.Current?.AdjdRefNbr);

        public APInvoice OriginalInvoice => InvoiceDataProvider.GetOriginalInvoice(Base, CurrentInvoice);

        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.construction>();
        }

        public IEnumerable jointPayeePayments()
        {
            var selectedLine = GetSelectedAdjustmentLine();

            return selectedLine != null
                ? GetJointPayeePaymentsRelatedToBill(selectedLine)
                : GetCurrentJointPayeePayments();
        }

        public IEnumerable jointPayees()
        {
            return SelectFrom<JointPayee>.Where<JointPayee.billId.IsEqual<P.AsGuid>>.View.Select(Base,
                OriginalInvoice?.NoteID);
        }

        public virtual void _(Events.RowInserted<APAdjust> args)
        {
            if (args.Row is APAdjust adjustment && IsJointPayees(adjustment))
            {
                CreateJointPayeePayments(adjustment);
                SetCashDiscountBalance(adjustment);
                adjustment.CuryAdjgDiscAmt = 0;
            }
        }

        public virtual void _(Events.RowSelected<APAdjust> args)
        {
            if (args.Row is APAdjust adjustment && !PXLongOperation.Exists(Base.UID))
            {
                UpdateAmountPaidAvailability(adjustment);
            }
        }

        public virtual void _(Events.RowUpdated<PayBillsFilter> args)
        {
            JointPayeePayments.Cache.Clear();
            JointPayeePayments.Cache.ClearQueryCache();
        }

        private void SetCashDiscountBalance(APAdjust adjustment)
        {
            var invoice = InvoiceDataProvider.GetInvoice(Base, adjustment.AdjdDocType, adjustment.AdjdRefNbr);
            adjustment.CuryDiscBal = invoice.PaymentsByLinesAllowed == true
                ? TransactionDataProvider.GetTransaction(Base, invoice.DocType, invoice.RefNbr, adjustment.AdjdLineNbr)
                    .CuryCashDiscBal
                : invoice.CuryOrigDiscAmt;
        }

        private void UpdateAmountPaidAvailability(IDocumentAdjustment adjustment)
        {
            var isJointPayees = IsJointPayees(adjustment);
            PXUIFieldAttribute.SetReadOnly<APAdjust.curyAdjgAmt>(Base.APDocumentList.Cache, adjustment, isJointPayees);
            PXUIFieldAttribute.SetReadOnly<APAdjust.curyAdjgDiscAmt>(Base.APDocumentList.Cache, adjustment, isJointPayees);
        }

        private bool IsJointPayees(IDocumentAdjustment adjustment)
        {
            var invoice = InvoiceDataProvider.GetInvoice(Base, adjustment.AdjdDocType, adjustment.AdjdRefNbr);
            return PXCache<APInvoice>.GetExtension<APInvoiceJCExt>(invoice).IsJointPayees == true;
        }

        private void CreateJointPayeePayments(APAdjust adjustment)
        {
            var originalInvoice =
                InvoiceDataProvider.GetOriginalInvoice(Base, adjustment.AdjdRefNbr, adjustment.AdjdDocType);
            var jointPayees =
                JointPayeeDataProvider.GetJointPayees(Base, originalInvoice.RefNbr, adjustment.AdjdLineNbr);
            var jointPayeePayments = jointPayees
                .Select(jointPayee => CreateJointPayeePayment(jointPayee.JointPayeeId, adjustment));
            JointPayeePayments.Cache.InsertAll(jointPayeePayments);
        }

        private static JointPayeePayment CreateJointPayeePayment(int? jointPayeeId, IDocumentAdjustment adjustment)
        {
            return new JointPayeePayment
            {
                JointPayeeId = jointPayeeId,
                PaymentDocType = adjustment.AdjdDocType,
                PaymentRefNbr = adjustment.AdjdRefNbr,
                JointAmountToPay = 0,
                AdjustmentNumber = 0
            };
        }

        private IEnumerable<PXResult<JointPayeePayment>> GetJointPayeePaymentsRelatedToBill(APAdjust adjustment)
        {
            return GetCurrentJointPayeePayments().Where(x => IsRelatedToBill(x, adjustment));
        }

        private static bool IsRelatedToBill(PXResult jointPayeePaymentResult, APAdjust adjustment)
        {
            var jointPayeePayment = jointPayeePaymentResult.GetItem<JointPayeePayment>();
            var isRelatedToBill = jointPayeePayment.PaymentDocType == adjustment.AdjdDocType
                && jointPayeePayment.PaymentRefNbr == adjustment.AdjdRefNbr;
            return adjustment.AdjdLineNbr != 0
                ? isRelatedToBill && jointPayeePayment.BillLineNumber == adjustment.AdjdLineNbr
                : isRelatedToBill;
        }

        private PXResultset<JointPayeePayment> GetCurrentJointPayeePayments()
        {
            return OriginalInvoice == null
                ? new PXResultset<JointPayeePayment, JointPayee>()
                : JointPayeePaymentDataProvider.GetCurrentJointPayeePayments(Base, OriginalInvoice, CurrentInvoice);
        }

        public APAdjust GetSelectedAdjustmentLine()
        {

            APAdjust key = new APAdjust();
            key.AdjgDocType = SelectedDocument.Current.AdjgDocType;
            key.AdjgRefNbr = SelectedDocument.Current.AdjgRefNbr;
            key.AdjNbr = SelectedDocument.Current.AdjNbr;
            key.AdjdDocType = SelectedDocument.Current.AdjdDocType;
            key.AdjdRefNbr = SelectedDocument.Current.AdjdRefNbr;
            key.AdjdLineNbr = SelectedDocument.Current.AdjdLineNbr;
            APAdjust adj = Base.APDocumentList.Locate(key);

            if (adj == null)
            {
                adj = APAdjust.PK.Find(Base,
                SelectedDocument.Current.AdjgDocType,
                SelectedDocument.Current.AdjgRefNbr,
                SelectedDocument.Current.AdjNbr,
                SelectedDocument.Current.AdjdDocType,
                SelectedDocument.Current.AdjdRefNbr,
                SelectedDocument.Current.AdjdLineNbr);
            }

            return adj;
        }

        [PXHidden]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public class DocumentFilter : IBqlTable
        {
            #region AdjgDocType
            public abstract class adjgDocType : PX.Data.BQL.BqlString.Field<adjgDocType> { }

            /// <summary>
            /// [key] The type of the adjusting document.
            /// </summary>
            [PXDBString(3, IsKey = true, IsFixed = true, InputMask = "")]
            [PXUIField(DisplayName = "AdjgDocType", Visibility = PXUIVisibility.Visible, Visible = false)]
            public virtual string AdjgDocType
            {
                get;
                set;
            }
            #endregion
            #region AdjgRefNbr
            public abstract class adjgRefNbr : PX.Data.BQL.BqlString.Field<adjgRefNbr> { }

            /// <summary>
            /// [key] Reference number of the adjusting document.
            /// </summary>
            [PXDBString(15, IsUnicode = true, IsKey = true)]
            [PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.Visible, Visible = false)]
            public virtual string AdjgRefNbr
            {
                get;
                set;
            }
            #endregion
            #region AdjdDocType
            public abstract class adjdDocType : PX.Data.BQL.BqlString.Field<adjdDocType> { }

            /// <summary>
            /// [key] The type of the adjusted document.
            /// </summary>
            [PXDBString(3, IsKey = true, IsFixed = true, InputMask = "")]
            [PXUIField(DisplayName = "Document Type", Visibility = PXUIVisibility.Visible)]
            [APInvoiceType.AdjdList()]
            public virtual string AdjdDocType
            {
                get;
                set;
            }
            #endregion
            #region AdjdRefNbr
            public abstract class adjdRefNbr : PX.Data.BQL.BqlString.Field<adjdRefNbr> { }

            /// <summary>
            /// [key] Reference number of the adjusted document.
            /// </summary>
            [PXDBString(15, IsKey = true, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
            [PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.Visible)]
            public virtual string AdjdRefNbr
            {
                get;
                set;
            }
            #endregion
            #region AdjdLineNbr
            public abstract class adjdLineNbr : PX.Data.BQL.BqlInt.Field<adjdLineNbr> { }

            [PXDBInt(IsKey = true)]
            [PXUIField(DisplayName = "Line Nbr.", Visibility = PXUIVisibility.Visible, FieldClass = nameof(FeaturesSet.PaymentsByLines))]
            public virtual int? AdjdLineNbr
            {
                get;
                set;
            }
            #endregion
            #region AdjNbr
            /// <summary>
            /// The number of the adjustment.
            /// </summary>
            /// <value>
            /// Defaults to the current <see cref="APPayment.AdjCntr">number of lines</see> in the related payment document.
            /// </value>
            [PXDBInt(IsKey = true)]
            [PXUIField(DisplayName = "Adjustment Nbr.", Visibility = PXUIVisibility.Visible, Visible = false, Enabled = false)]
            public virtual int? AdjNbr
            {
                get;
                set;
            }
            #endregion
        }
    }
}