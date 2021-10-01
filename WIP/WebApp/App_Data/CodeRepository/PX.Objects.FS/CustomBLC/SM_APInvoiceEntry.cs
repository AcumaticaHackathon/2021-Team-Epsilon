using PX.Common;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static PX.Objects.FS.MessageHelper;

namespace PX.Objects.FS
{
    public class SM_APInvoiceEntry : FSPostingBase<APInvoiceEntry>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public virtual bool IsFSIntegrationEnabled()
        {
            APInvoice apInvoiceRow = Base.Document.Current;

            if (apInvoiceRow != null
                && apInvoiceRow.CreatedByScreenID.Substring(0, 2) == "FS")
            {
                return true;
            }

            return false;
        }

        #region Views
        [PXHidden]
        public PXSelect<FSServiceOrder> ServiceOrderRecords;
        [PXHidden]
        public PXSelect<FSAppointment> AppointmentRecords;
        [PXHidden]
        public PXFilter<CreateAPFilter> apFilter;
        #endregion

        #region CacheAttached
        #region FSServiceOrder_SrvOrdType
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXCustomizeBaseAttribute(typeof(PXSelectorAttribute), nameof(PXSelectorAttribute.DescriptionField), typeof(FSSrvOrdType.srvOrdType))]
        protected virtual void FSServiceOrder_SrvOrdType_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region FSAppointment_SrvOrdType
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXCustomizeBaseAttribute(typeof(PXSelectorAttribute), nameof(PXSelectorAttribute.DescriptionField), typeof(FSSrvOrdType.srvOrdType))]
        protected virtual void FSAppointment_SrvOrdType_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #endregion

        #region Actions
        #region OpenFSDocument
        public PXAction<APInvoice> openFSDocument;
        [PXUIField(MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable OpenFSDocument(PXAdapter adapter)
        {
            APTran apTran = Base.Transactions.Current;
            if (apTran != null)
            {
                FSxAPTran extRow = Base.Transactions.Cache.GetExtension<FSxAPTran>(apTran);
                if (extRow != null)
                {
                    var item = new EntityHelper(Base).GetEntityRow(extRow.RelatedDocNoteID, true);
                    if(item is FSAppointment)
                    {
                        FSAppointment app = (FSAppointment)item;
                        var graph = PXGraph.CreateInstance<AppointmentEntry>();
                        graph.AppointmentRecords.Current = graph.AppointmentRecords.Search<FSAppointment.refNbr>(app.RefNbr, app.SrvOrdType);
                        throw new PXRedirectRequiredException(graph, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                    }
                    else if(item is FSServiceOrder)
                    {
                        FSServiceOrder so = (FSServiceOrder)item;
                        var graph = PXGraph.CreateInstance<ServiceOrderEntry>();
                        graph.ServiceOrderRecords.Current = graph.ServiceOrderRecords.Search<FSServiceOrder.refNbr>(so.RefNbr, so.SrvOrdType);
                        throw new PXRedirectRequiredException(graph, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                    }
                }
            }

            return adapter.Get();
        }
        #endregion
        #endregion

        #region Event Handlers

        #region APInvoice

        #region FieldSelecting
        #endregion
        #region FieldDefaulting
        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated
        #endregion

        protected virtual void _(Events.RowSelecting<APInvoice> e)
        {
        }

        protected virtual void _(Events.RowSelected<APInvoice> e)
        {
        }

        protected virtual void _(Events.RowInserting<APInvoice> e)
        {
        }

        protected virtual void _(Events.RowInserted<APInvoice> e)
        {
        }

        protected virtual void _(Events.RowUpdating<APInvoice> e)
        {
        }

        protected virtual void _(Events.RowUpdated<APInvoice> e)
        {
        }

        protected virtual void _(Events.RowDeleting<APInvoice> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            List<APTran> apTranRecords = GetAPTranRecordsToProcess(e.Row, PXDBOperation.Delete)
                                           .Where(row => Base.Transactions.Cache.GetExtension<FSxAPTran>(row)?.RelatedEntityType != null
                                                      && Base.Transactions.Cache.GetExtension<FSxAPTran>(row)?.RelatedDocNoteID != null)
                                           .ToList();
            if (apTranRecords.Count() > 0
                    && Base.Document.Ask(TX.Warning.APInvoiceLinkedToFSDocument, MessageButtons.OKCancel) != WebDialogResult.OK)
            {
                e.Cancel = true;
            }
        }

        protected virtual void _(Events.RowDeleted<APInvoice> e)
        {
        }

        protected virtual void _(Events.RowPersisting<APInvoice> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            APInvoice apInvoiceRow = (APInvoice)e.Row;
            ValidatePostBatchStatus(e.Operation, ID.Batch_PostTo.AP, apInvoiceRow.DocType, apInvoiceRow.RefNbr);
        }

        protected virtual void _(Events.RowPersisted<APInvoice> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            APInvoice apInvoiceRow = (APInvoice)e.Row;

            if (e.Operation == PXDBOperation.Delete
                    && e.TranStatus == PXTranStatus.Open)
            {
                CleanPostingInfoLinkedToDoc(apInvoiceRow);
            }

            if (e.TranStatus == PXTranStatus.Open
                    && (Base.Accessinfo.ScreenID.Substring(0, 2) != "FS"))
            {
                UpdateFSDocument(apInvoiceRow, e.Operation);
            }
        }
        #endregion

        #region APTran

        #region FieldSelecting
        #endregion
        #region FieldDefaulting
        protected virtual void _(Events.FieldDefaulting<APTran, APTran.projectID> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            if(apFilter.Current?.RelatedDocProjectID != null)
            {
                e.NewValue = apFilter.Current.RelatedDocProjectID;
                e.Cancel = true;
            }
        }

        protected virtual void _(Events.FieldDefaulting<APTran, APTran.taskID> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            if (apFilter.Current?.RelatedDocProjectTaskID != null)
            {
                e.NewValue = apFilter.Current.RelatedDocProjectTaskID;
                e.Cancel = true;
            }
        }

        protected virtual void _(Events.FieldDefaulting<APTran, APTran.costCodeID> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            if (apFilter.Current?.RelatedDocCostCodeID != null)
            {
                e.NewValue = apFilter.Current.RelatedDocCostCodeID;
                e.Cancel = true;
            }
        }
        #endregion
        #region FieldUpdating
        protected virtual void _(Events.FieldUpdating<APTran, APTran.qty> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            APTran apTranRow = (APTran)e.Row;

            if (IsLineCreatedFromAppSO(Base, Base.Document.Current, apTranRow, typeof(APTran.qty).Name) == true)
            {
                throw new PXSetPropertyException(TX.Error.NO_UPDATE_ALLOWED_DOCLINE_LINKED_TO_APP_SO);
            }
        }

        protected virtual void _(Events.FieldUpdating<APTran, APTran.inventoryID> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            APTran apTranRow = (APTran)e.Row;

            if (IsLineCreatedFromAppSO(Base, Base.Document.Current, apTranRow, typeof(APTran.inventoryID).Name) == true)
            {
                throw new PXSetPropertyException(TX.Error.NO_UPDATE_ALLOWED_DOCLINE_LINKED_TO_APP_SO);
            }
        }

        protected virtual void _(Events.FieldUpdating<APTran, APTran.uOM> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            APTran apTranRow = (APTran)e.Row;

            if (IsLineCreatedFromAppSO(Base, Base.Document.Current, apTranRow, typeof(APTran.uOM).Name) == true)
            {
                throw new PXSetPropertyException(TX.Error.NO_UPDATE_ALLOWED_DOCLINE_LINKED_TO_APP_SO);
            }
        }
        #endregion
        #region FieldVerifying
        protected virtual void _(Events.FieldVerifying<APTran, APTran.inventoryID> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            if ((int?)e.NewValue != e.Row.InventoryID)
            {
                VerifyAPFieldCanBeUpdated<APTran.inventoryID>(e.Cache, e.Row);
            }
        }

        protected virtual void _(Events.FieldVerifying<APTran, APTran.qty> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            if ((decimal?)e.NewValue != e.Row.Qty)
            {
                VerifyAPFieldCanBeUpdated<APTran.qty>(e.Cache, e.Row);
            }
        }

        protected virtual void _(Events.FieldVerifying<APTran, APTran.uOM> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            if ((string)e.NewValue != e.Row.UOM)
            {
                VerifyAPFieldCanBeUpdated<APTran.uOM>(e.Cache, e.Row);
            }
        }

        protected virtual void _(Events.FieldVerifying<APTran, APTran.taskID> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            if ((int?)e.NewValue != e.Row.TaskID)
            {
                VerifyAPFieldCanBeUpdated<APTran.taskID>(e.Cache, e.Row);
            }
        }

        protected virtual void _(Events.FieldVerifying<APTran, APTran.costCodeID> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            if ((int?)e.NewValue != e.Row.CostCodeID)
            {
                VerifyAPFieldCanBeUpdated<APTran.costCodeID>(e.Cache, e.Row);
            }
        }
        #endregion
        #region FieldUpdated
        protected virtual void _(Events.FieldUpdated<APTran, APTran.inventoryID> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            UpdateAPLineFieldsFromFSDocument(e.Cache, e.Row, false);
        }

        protected virtual void _(Events.FieldUpdated<FSxAPTran.relatedEntityType> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            if ((string)e.OldValue != (string)e.NewValue)
            {
                e.Cache.SetValueExt<FSxAPTran.relatedDocNoteID>(e.Row, null);
            }
        }

        protected virtual void _(Events.FieldUpdated<FSxAPTran.relatedDocNoteID> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }
            
            APTran row = (APTran)e.Row;

            if((Guid?)e.OldValue != (Guid?)e.NewValue)
            {
                if (e.NewValue != null)
                {
                    UpdateAPLineFieldsFromFSDocument(e.Cache, row, true);
                }
            }
        }
        #endregion

        protected virtual void _(Events.RowSelecting<APTran> e)
        {
        }

        protected virtual void _(Events.RowSelected<APTran> e)
        {
            bool fsIntegrationEnabled = IsFSIntegrationEnabled();
            PXUIFieldAttribute.SetVisible<FSxAPTran.relatedEntityType>(e.Cache, null, fsIntegrationEnabled);
            PXUIFieldAttribute.SetVisible<FSxAPTran.relatedDocNoteID>(e.Cache, null, fsIntegrationEnabled);
            EnableDisableRelatedSMFields(e.Cache, e.Row);
            SetExtensionVisibleInvisible<FSxAPTran>(e.Cache, e.Args, fsIntegrationEnabled, false);
        }

        protected virtual void _(Events.RowInserting<APTran> e)
        {
        }

        protected virtual void _(Events.RowInserted<APTran> e)
        {
        }

        protected virtual void _(Events.RowUpdating<APTran> e)
        {
        }

        protected virtual void _(Events.RowUpdated<APTran> e)
        {
        }

        protected virtual void _(Events.RowDeleting<APTran> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            FSxAPTran extRow = e.Cache.GetExtension<FSxAPTran>(e.Row);
            if(extRow!= null
                && e.ExternalCall == true
                && extRow.RelatedEntityType != null 
                && extRow.RelatedDocNoteID != null 
                && e.Row.CreatedByScreenID.Substring(0, 2) != "FS"
                && Base.Document.Ask(TX.Warning.APLineLinkedToFSDocument, MessageButtons.OKCancel) != WebDialogResult.OK)
            {
                e.Cancel = true;
            }
        }

        protected virtual void _(Events.RowDeleted<APTran> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            APTran apTranRow = (APTran)e.Row;

            if (e.ExternalCall == true)
            {
                if (IsLineCreatedFromAppSO(Base, Base.Document.Current, apTranRow, null) == true)
                {
                    throw new PXException(TX.Error.NO_DELETION_ALLOWED_DOCLINE_LINKED_TO_APP_SO);
                }
            }
        }

        protected virtual void _(Events.RowPersisting<APTran> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            ValidateLinkedFSDocFields(e.Cache, e.Row, null, true);
        }

        protected virtual void _(Events.RowPersisted<APTran> e)
        {
            if (e.Row == null)
            {
                return;
            }

            APTran apTranRow = (APTran)e.Row;

            // We call here cache.GetStateExt for every field when the transaction is aborted
            // to set the errors in the fields and then the Generate Invoice screen can read them
            if (e.TranStatus == PXTranStatus.Aborted && IsInvoiceProcessRunning == true)
            {
                MessageHelper.GetRowMessage(e.Cache, apTranRow, false, false);
            }
        }

        #endregion

        #endregion
        #region Invoicing Methods
        public override List<ErrorInfo> GetErrorInfo()
        {
            return MessageHelper.GetErrorInfo<APTran>(Base.Document.Cache, Base.Document.Current, Base.Transactions);
        }

        public virtual string GetLineDisplayHint(PXGraph graph, string lineRefNbr, string lineDescr, int? inventoryID)
        {
            return MessageHelper.GetLineDisplayHint(graph, lineRefNbr, lineDescr, inventoryID);
        }

        public override void CreateInvoice(PXGraph graphProcess, List<DocLineExt> docLines, short invtMult, DateTime? invoiceDate, string invoiceFinPeriodID, OnDocumentHeaderInsertedDelegate onDocumentHeaderInserted, OnTransactionInsertedDelegate onTransactionInserted, PXQuickProcess.ActionFlow quickProcessFlow)
        {
            if (docLines.Count == 0)
            {
                return;
            }

            bool? initialHold = false;

            FSServiceOrder fsServiceOrderRow = docLines[0].fsServiceOrder;
            FSSrvOrdType fsSrvOrdTypeRow = docLines[0].fsSrvOrdType;
            FSPostDoc fsPostDocRow = docLines[0].fsPostDoc;
            FSAppointment fsAppointmentRow = docLines[0].fsAppointment;

            Vendor vendorRow = GetVendorRow(graphProcess, fsServiceOrderRow.BillCustomerID);

            if (vendorRow == null)
            {
                throw new PXException(TX.Error.AP_POSTING_VENDOR_NOT_FOUND);
            }

            var cancel_defaulting = new PXFieldDefaulting((sender, e) =>
            {
                e.NewValue = fsServiceOrderRow.BranchID;
                e.Cancel = true;
            });

            try
            {
                Base.FieldDefaulting.AddHandler<APInvoice.branchID>(cancel_defaulting);

            APInvoice apInvoiceRow = new APInvoice();

            if (invtMult >= 0)
            {
                apInvoiceRow.DocType = APDocType.DebitAdj;
                CheckAutoNumbering(Base.APSetup.SelectSingle().DebitAdjNumberingID);
            }
            else
            {
                apInvoiceRow.DocType = APDocType.Invoice;
                CheckAutoNumbering(Base.APSetup.SelectSingle().InvoiceNumberingID);
            }

            apInvoiceRow.DocDate = invoiceDate;
            apInvoiceRow.FinPeriodID = invoiceFinPeriodID;
            apInvoiceRow = PXCache<APInvoice>.CreateCopy(Base.Document.Insert(apInvoiceRow));
            initialHold = apInvoiceRow.Hold;
            apInvoiceRow.NoteID = null;
            PXNoteAttribute.GetNoteIDNow(Base.Document.Cache, apInvoiceRow);
            apInvoiceRow.VendorID = fsServiceOrderRow.BillCustomerID;
            apInvoiceRow.VendorLocationID = fsServiceOrderRow.BillLocationID;
            apInvoiceRow.CuryID = fsServiceOrderRow.CuryID;
            apInvoiceRow.TaxZoneID = fsAppointmentRow != null ? fsAppointmentRow.TaxZoneID : fsServiceOrderRow.TaxZoneID;
            apInvoiceRow.TaxCalcMode = fsAppointmentRow != null ? fsAppointmentRow.TaxCalcMode : fsServiceOrderRow.TaxCalcMode;

            apInvoiceRow.SuppliedByVendorLocationID = fsServiceOrderRow.BillLocationID;

            string termsID = GetTermsIDFromCustomerOrVendor(graphProcess, null, fsServiceOrderRow.BillCustomerID);
            if (termsID == null)
            {
                termsID = fsSrvOrdTypeRow.DfltTermIDAP;
            }

            apInvoiceRow.FinPeriodID = invoiceFinPeriodID;
            apInvoiceRow.TermsID = termsID;
            apInvoiceRow.DocDesc = fsServiceOrderRow.DocDesc;
            apInvoiceRow.Hold = true;
            apInvoiceRow = Base.Document.Update(apInvoiceRow);
            apInvoiceRow.TaxCalcMode = PX.Objects.TX.TaxCalculationMode.TaxSetting;
            apInvoiceRow = Base.Document.Update(apInvoiceRow);

            SetContactAndAddress(Base, fsServiceOrderRow);

            if (onDocumentHeaderInserted != null)
            {
                onDocumentHeaderInserted(Base, apInvoiceRow);
            }

            IDocLine docLine = null;
            APTran apTranRow = null;
            FSxAPTran fsxAPTranRow = null;
            PMTask pmTaskRow = null;
            FSAppointmentDet fsAppointmentDetRow = null;
            FSSODet fsSODetRow = null;

            foreach (DocLineExt docLineExt in docLines)
            {
                docLine = docLineExt.docLine;
                fsPostDocRow = docLineExt.fsPostDoc;
                fsServiceOrderRow = docLineExt.fsServiceOrder;
                fsSrvOrdTypeRow = docLineExt.fsSrvOrdType;
                fsAppointmentRow = docLineExt.fsAppointment;
                fsAppointmentDetRow = docLineExt.fsAppointmentDet;
                fsSODetRow = docLineExt.fsSODet;

                apTranRow = new APTran();
                apTranRow = Base.Transactions.Insert(apTranRow);

                Base.Transactions.Cache.SetValueExtIfDifferent<APTran.branchID>(apTranRow, docLine.BranchID);
                Base.Transactions.Cache.SetValueExtIfDifferent<APTran.inventoryID>(apTranRow, docLine.InventoryID);
                Base.Transactions.Cache.SetValueExtIfDifferent<APTran.uOM>(apTranRow, docLine.UOM);

                pmTaskRow = docLineExt.pmTask;

                if (pmTaskRow != null && pmTaskRow.Status == ProjectTaskStatus.Completed)
                {
                    throw new PXException(TX.Error.POSTING_PMTASK_ALREADY_COMPLETED, fsServiceOrderRow.RefNbr, GetLineDisplayHint(Base, docLine.LineRef, docLine.TranDesc, docLine.InventoryID), pmTaskRow.TaskCD);
                }

                if (docLine.AcctID != null)
                {
                    Base.Transactions.Cache.SetValueExtIfDifferent<APTran.accountID>(apTranRow, docLine.AcctID);
                }

                if (docLine.SubID != null)
                {
                    try
                    {
                        Base.Transactions.Cache.SetValueExtIfDifferent<APTran.subID>(apTranRow, docLine.SubID);
                    }
                    catch (PXException)
                    {
                        apTranRow.SubID = null;
                    }
                }
                else
                {
                    SetCombinedSubID(graphProcess,
                                                        Base.Transactions.Cache,
                                                        null,
                                                        apTranRow,
                                                        null,
                                                        fsSrvOrdTypeRow,
                                                        apTranRow.BranchID,
                                                        apTranRow.InventoryID,
                                                        apInvoiceRow.VendorLocationID,
                                                        fsServiceOrderRow.BranchLocationID,
                                                        fsServiceOrderRow.SalesPersonID,
                                                        docLine.IsService);
                }

                if (docLine.ProjectID != null && docLine.ProjectTaskID != null)
                {
                    Base.Transactions.Cache.SetValueExtIfDifferent<APTran.taskID>(apTranRow, docLine.ProjectTaskID);
                }

                Base.Transactions.Cache.SetValueExtIfDifferent<APTran.qty>(apTranRow, docLine.GetQty(FieldType.BillableField));
                Base.Transactions.Cache.SetValueExtIfDifferent<APTran.tranDesc>(apTranRow, docLine.TranDesc);

                apTranRow = Base.Transactions.Update(apTranRow);
                Base.Transactions.Cache.SetValueExtIfDifferent<APTran.manualPrice>(apTranRow, docLine.ManualPrice);
                Base.Transactions.Cache.SetValueExtIfDifferent<APTran.curyUnitCost>(apTranRow, (docLine.IsFree == false ? docLine.CuryUnitPrice * invtMult : 0m));

                if (docLine.ProjectID != null)
                {
                    Base.Transactions.Cache.SetValueExtIfDifferent<APTran.projectID>(apTranRow, docLine.ProjectID);
                }

                Base.Transactions.Cache.SetValueExtIfDifferent<APTran.taxCategoryID>(apTranRow, docLine.TaxCategoryID);
                Base.Transactions.Cache.SetValueExtIfDifferent<APTran.costCodeID>(apTranRow, docLine.CostCodeID);
                Base.Transactions.Cache.SetValueExtIfDifferent<APTran.discPct>(apTranRow, docLine.DiscPct);

                fsxAPTranRow = Base.Transactions.Cache.GetExtension<FSxAPTran>(apTranRow);

                fsxAPTranRow.SrvOrdType = fsServiceOrderRow.SrvOrdType;
                fsxAPTranRow.ServiceOrderRefNbr = fsServiceOrderRow.RefNbr;
                fsxAPTranRow.AppointmentRefNbr = fsAppointmentRow?.RefNbr;
                fsxAPTranRow.ServiceOrderLineNbr = fsSODetRow?.LineNbr;
                fsxAPTranRow.AppointmentLineNbr = fsAppointmentDetRow?.LineNbr;

                if (docLine.BillingBy == ID.Billing_By.APPOINTMENT)
                {
                    fsxAPTranRow.RelatedEntityType = ID.FSEntityType.Appointment;
                    fsxAPTranRow.RelatedDocNoteID = fsAppointmentRow.NoteID;
                    //// TODO AC-142850
                    ////fsxAPTranRow.AppointmentDate = new DateTime(
                    ////                                fsAppointmentRow.ActualDateTimeBegin.Value.Year,
                    ////                                fsAppointmentRow.ActualDateTimeBegin.Value.Month,
                    ////                                fsAppointmentRow.ActualDateTimeBegin.Value.Day,
                    ////                                0,
                    ////                                0,
                    ////                                0);
                }
                else if(docLine.BillingBy == ID.Billing_By.SERVICE_ORDER)
                {
                    fsxAPTranRow.RelatedEntityType = ID.FSEntityType.ServiceOrder;
                    fsxAPTranRow.RelatedDocNoteID = fsServiceOrderRow.NoteID;
                }

                fsxAPTranRow.Mem_PreviousPostID = docLine.PostID;
                fsxAPTranRow.Mem_TableSource = docLine.SourceTable;

                SharedFunctions.CopyNotesAndFiles(Base.Transactions.Cache, apTranRow, docLine, fsSrvOrdTypeRow);
                fsPostDocRow.DocLineRef = apTranRow = Base.Transactions.Update(apTranRow);

                if (onTransactionInserted != null)
                {
                    onTransactionInserted(Base, apTranRow);
                }
            }

            apInvoiceRow = Base.Document.Update(apInvoiceRow);

            if (Base.APSetup.Current.RequireControlTotal == true)
            {
                Base.Document.Cache.SetValueExtIfDifferent<APInvoice.curyOrigDocAmt>(apInvoiceRow, apInvoiceRow.CuryDocBal);
            }

            if (initialHold != true)
            {
                Base.Document.Cache.SetValueExtIfDifferent<APInvoice.hold>(apInvoiceRow, false);
            }

            apInvoiceRow = Base.Document.Update(apInvoiceRow);
        }
            finally
            {
                Base.FieldDefaulting.RemoveHandler<APInvoice.branchID>(cancel_defaulting);
            }
        }

        public override FSCreatedDoc PressSave(int batchID, List<DocLineExt> docLines, BeforeSaveDelegate beforeSave)
        {
            if (Base.Document.Current == null)
            {
                throw new SharedClasses.TransactionScopeException();
            }

            if (beforeSave != null)
            {
                beforeSave(Base);
            }

            APInvoiceEntryExternalTax TaxGraphExt = Base.GetExtension<APInvoiceEntryExternalTax>();

            if (TaxGraphExt != null)
            {
                TaxGraphExt.SkipTaxCalcAndSave();
            }
            else
            {
            Base.Save.Press();
            }

            string docType = Base.Document.Current.DocType;
            string refNbr = Base.Document.Current.RefNbr;

            // Reload APInvoice to get the current value of IsTaxValid
            Base.Clear();
            Base.Document.Current = PXSelect<APInvoice, Where<APInvoice.docType, Equal<Required<APInvoice.docType>>, And<APInvoice.refNbr, Equal<Required<APInvoice.refNbr>>>>>.Select(Base, docType, refNbr);
            APInvoice apInvoiceRow = Base.Document.Current;

            var fsCreatedDocRow = new FSCreatedDoc()
            {
                BatchID = batchID,
                PostTo = ID.Batch_PostTo.AP,
                CreatedDocType = apInvoiceRow.DocType,
                CreatedRefNbr = apInvoiceRow.RefNbr
            };

            return fsCreatedDocRow;
        }

        public override void Clear()
        {
            Base.Clear(PXClearOption.ClearAll);
        }

        public override PXGraph GetGraph()
        {
            return Base;
        }

        public override void DeleteDocument(FSCreatedDoc fsCreatedDocRow)
        {
            Base.Document.Current = Base.Document.Search<APInvoice.refNbr>(fsCreatedDocRow.CreatedRefNbr, fsCreatedDocRow.CreatedDocType);

            if (Base.Document.Current != null)
            {
                if (Base.Document.Current.RefNbr == fsCreatedDocRow.CreatedRefNbr
                        && Base.Document.Current.DocType == fsCreatedDocRow.CreatedDocType)
                {
                    Base.Delete.Press();
                }
            }
        }

        public override void CleanPostInfo(PXGraph cleanerGraph, FSPostDet fsPostDetRow)
        {
            PXUpdate<
                Set<FSPostInfo.aPLineNbr, Null,
                Set<FSPostInfo.apRefNbr, Null,
                Set<FSPostInfo.apDocType, Null,
                Set<FSPostInfo.aPPosted, False>>>>,
            FSPostInfo,
            Where<
                FSPostInfo.postID, Equal<Required<FSPostInfo.postID>>,
                And<FSPostInfo.aPPosted, Equal<True>>>>
            .Update(cleanerGraph, fsPostDetRow.PostID);
        }

        public virtual bool IsLineCreatedFromAppSO(PXGraph cleanerGraph, object document, object lineDoc, string fieldName)
        {
            if (document == null || lineDoc == null
                || Base.Accessinfo.ScreenID.Replace(".", "") == ID.ScreenID.SERVICE_ORDER
                || Base.Accessinfo.ScreenID.Replace(".", "") == ID.ScreenID.APPOINTMENT
                || Base.Accessinfo.ScreenID.Replace(".", "") == ID.ScreenID.INVOICE_BY_SERVICE_ORDER
                || Base.Accessinfo.ScreenID.Replace(".", "") == ID.ScreenID.INVOICE_BY_APPOINTMENT)
            {
                return false;
            }

            string refNbr = ((APInvoice)document).RefNbr;
            string docType = ((APInvoice)document).DocType;
            int? lineNbr = ((APTran)lineDoc).LineNbr;

            return PXSelect<FSPostInfo,
                   Where<
                       FSPostInfo.apRefNbr, Equal<Required<FSPostInfo.apRefNbr>>,
                       And<FSPostInfo.apDocType, Equal<Required<FSPostInfo.apDocType>>,
                       And<FSPostInfo.aPLineNbr, Equal<Required<FSPostInfo.aPLineNbr>>,
                       And<FSPostInfo.aPPosted, Equal<True>>>>>>
                   .Select(cleanerGraph, refNbr, docType, lineNbr).Count() > 0;
        }

        public override void UpdateCostAndPrice(List<DocLineExt> docLines)
        {
        }

        public virtual Vendor GetVendorRow(PXGraph graph, int? vendorID)
        {
            if (vendorID == null)
            {
                return null;
        }

            return PXSelect<Vendor,
                   Where<
                        Vendor.bAccountID, Equal<Required<Vendor.bAccountID>>>>
                   .Select(graph, vendorID);
        }

        #endregion
        #region Handler Methods
        public virtual void EnableDisableRelatedSMFields(PXCache cache, APTran row)
        {
            if (row == null)
            {
                return;
            }

            bool enableRelatedFSFields = false;
            InventoryItem item = (InventoryItem)PXSelectorAttribute.Select<APTran.inventoryID>(cache, row);
            if (item?.StkItem == false)
            {
                FSxAPTran extRow = cache.GetExtension<FSxAPTran>(row);
                bool lineComingFromFS = row.CreatedByScreenID.Substring(0, 2) == "FS";
                enableRelatedFSFields = row.InventoryID != null
                                        && lineComingFromFS == false 
                                        && row.TranType == APDocType.Invoice;
            }

            PXUIFieldAttribute.SetEnabled<FSxAPTran.relatedEntityType>(cache, row, enableRelatedFSFields);
            PXUIFieldAttribute.SetEnabled<FSxAPTran.relatedDocNoteID>(cache, row, enableRelatedFSFields);
        }

        public virtual void UpdateAPLineFieldsFromFSDocument(PXCache cache, APTran row, bool runDefaults)
        {
            if (row == null)
            {
                return;
            }

            InventoryItem inventoryItem = (InventoryItem)PXSelectorAttribute.Select<APTran.inventoryID>(cache, row);

            if(inventoryItem?.StkItem == false)
            {
                FSxAPTran extRow = cache.GetExtension<FSxAPTran>(row);

                var fsDocument = new EntityHelper(Base).GetEntityRow(extRow?.RelatedDocNoteID, true);

                if (fsDocument != null && row.CreatedByScreenID.Substring(0, 2) != "FS")
                {
                    if (runDefaults)
                    {
                        string serviceOrderType = string.Empty;

                        if (extRow.RelatedEntityType == ID.FSEntityType.Appointment)
                        {
                            FSAppointment appointment = (FSAppointment)fsDocument;

                            cache.SetValueExt<APTran.branchID>(row, appointment.BranchID);
                            cache.SetValueExt<APTran.projectID>(row, appointment.ProjectID);
                            cache.SetValueExt<APTran.taskID>(row, appointment.DfltProjectTaskID);

                            serviceOrderType = appointment.SrvOrdType;

                        }
                        else if (extRow.RelatedEntityType == ID.FSEntityType.ServiceOrder)
                        {
                            FSServiceOrder serviceOrder = ((FSServiceOrder)fsDocument);

                            cache.SetValueExt<APTran.branchID>(row, serviceOrder.BranchID);
                            cache.SetValueExt<APTran.projectID>(row, serviceOrder.ProjectID);
                            cache.SetValueExt<APTran.taskID>(row, serviceOrder.DfltProjectTaskID);

                            serviceOrderType = serviceOrder.SrvOrdType;
                        }

                        if (ProjectDefaultAttribute.IsNonProject(row.ProjectID) == false)
                        {
                            FSSrvOrdType srvOrderTypeRow = PXSelect<FSSrvOrdType,
                                                    Where<FSSrvOrdType.srvOrdType, Equal<Required<FSSrvOrdType.srvOrdType>>>>
                                                   .Select(Base, serviceOrderType);
                            cache.SetValueExt<APTran.costCodeID>(row, srvOrderTypeRow?.DfltCostCodeID);
                        }
                    }
                }
            }
        }

        public virtual void UpdateFSDocument(APInvoice apInvoice, PXDBOperation operation)
        {
            PXGraph graphHelper = null;
            Guid? lastDocNoteID = null;

            List<APTran> apTranRecords = GetAPTranRecordsToProcess(apInvoice, operation);

            foreach (APTran row in apTranRecords)
            {
                PXEntryStatus apTranStatus = Base.Transactions.Cache.GetStatus(row);
                if (apTranStatus == PXEntryStatus.Inserted
                    || apTranStatus == PXEntryStatus.Updated
                    || apTranStatus == PXEntryStatus.Deleted)
                {
                    if (row.InventoryID == null)
                    {
                        int? oriInventoryID = (int?)Base.Transactions.Cache.GetValueOriginal<APTran.inventoryID>(row);
                        if(oriInventoryID == row.InventoryID)
                        {
                            continue;
                        }
                    }

                    FSxAPTran extRow = Base.Transactions.Cache.GetExtension<FSxAPTran>(row);
                    if (row.CreatedByScreenID.Substring(0, 2) != "FS")
                    {
                        Guid? oriRelatedDocNoteID = (Guid?)Base.Transactions.Cache.GetValueOriginal<FSxAPTran.relatedDocNoteID>(row);
                        string oriRelatedEntityType = (string)Base.Transactions.Cache.GetValueOriginal<FSxAPTran.relatedEntityType>(row);

                        if (oriRelatedDocNoteID != null && oriRelatedDocNoteID != extRow.RelatedDocNoteID)
                        {
                            InsertUpdateDeleteFSDocumentLine(apInvoice, row, oriRelatedEntityType, oriRelatedDocNoteID, PXEntryStatus.Deleted, ref lastDocNoteID, ref graphHelper);
                        }

                        if (extRow.RelatedDocNoteID != null)
                        {
                            InsertUpdateDeleteFSDocumentLine(apInvoice, row, extRow.RelatedEntityType, extRow.RelatedDocNoteID, apTranStatus, ref lastDocNoteID, ref graphHelper);
                        }
                    }
                }
            }

            SaveFSGraphHelper(graphHelper);
        }

        public virtual List<APTran> GetAPTranRecordsToProcess(APInvoice apInvoice, PXDBOperation operation)
        {
            List<APTran> apTranRecords = null;
            List<APTran> delTranRecords = null;

            if (operation == PXDBOperation.Insert)
            {
                apTranRecords = Base.Transactions.Cache.Inserted.RowCast<APTran>()
                    .Where(row => row.CreatedByScreenID.Substring(0, 2) != "FS")
                    .OrderBy(row => Base.Transactions.Cache.GetExtension<FSxAPTran>(row)?.RelatedEntityType)
                    .ThenBy(row => Base.Transactions.Cache.GetExtension<FSxAPTran>(row)?.RelatedDocNoteID)
                    .ToList();
            }
            else
            {
                var fsAPTranRecords =
                        PXSelect<APTran,
                        Where<APTran.tranType, Equal<Required<APInvoice.docType>>,
                         And<APTran.refNbr, Equal<Required<APInvoice.refNbr>>>>, 
                        OrderBy<Asc<FSxAPTran.relatedEntityType,
                                Asc<FSxAPTran.relatedDocNoteID>>>>
                        .Select(Base, apInvoice.DocType, apInvoice.RefNbr);

                apTranRecords = fsAPTranRecords.RowCast<APTran>().Where(x => x.CreatedByScreenID.Contains("FS") == false ).ToList();
            }

            delTranRecords = GetAPTranRecordsToDelete();

            if (delTranRecords.Count > 0)
            {
                apTranRecords = apTranRecords.Concat(delTranRecords).ToList();
            }

            return apTranRecords;
        }

        public virtual List<APTran> GetAPTranRecordsToDelete()
        {
            return Base.Transactions.Cache.Deleted.RowCast<APTran>()
                    .Where(row => row.CreatedByScreenID.Substring(0, 2) != "FS")
                    .OrderBy(row => Base.Transactions.Cache.GetExtension<FSxAPTran>(row)?.RelatedEntityType)
                    .ThenBy(row => Base.Transactions.Cache.GetExtension<FSxAPTran>(row)?.RelatedDocNoteID)
                    .ToList();
        }

        public virtual void SaveFSGraphHelper(PXGraph graphHelper)
        {
            if (graphHelper != null && graphHelper.IsDirty)
            {
                graphHelper.GetSaveAction().Press();
                UpdateFSCacheInAPDoc(graphHelper);
            }
        }

        public virtual void UpdateFSCacheInAPDoc(PXGraph graphHelper)
        {
            if (graphHelper is AppointmentEntry)
            {
                var appGraph = (AppointmentEntry)graphHelper;
                if (appGraph.AppointmentRecords.Current != null && Base.Caches[typeof(FSAppointment)].GetStatus(appGraph.AppointmentRecords.Current) == PXEntryStatus.Updated)
                {
                    Base.Caches[typeof(FSAppointment)].Update(appGraph.AppointmentRecords.Current);
                    Base.SelectTimeStamp();
                }
            }
            else if (graphHelper is ServiceOrderEntry)
            {
                var soGraph = (ServiceOrderEntry)graphHelper;
                if (soGraph.ServiceOrderRecords.Current != null && Base.Caches[typeof(FSServiceOrder)].GetStatus(soGraph.ServiceOrderRecords.Current) == PXEntryStatus.Updated)
                {
                    Base.Caches[typeof(FSServiceOrder)].Update(soGraph.ServiceOrderRecords.Current);
                    Base.SelectTimeStamp();
                }
            }
        }

        public virtual void InsertUpdateDeleteFSDocumentLine(APInvoice apInvoiceRow, APTran row, string relatedEntityType, Guid? relatedDocNoteID, PXEntryStatus apTranStatus, ref Guid? lastDocNoteID, ref PXGraph graphHelper)
        {
            var item = new EntityHelper(Base).GetEntityRow(relatedDocNoteID, true);
            if (item != null)
            {
                if (relatedEntityType == ID.FSEntityType.Appointment && item is FSAppointment)
                {
                    InsertUpdateDeleteAppointmentLine(apInvoiceRow, row, relatedDocNoteID, apTranStatus, (FSAppointment)item, ref graphHelper, ref lastDocNoteID);
                }
                else if (relatedEntityType == ID.FSEntityType.ServiceOrder && item is FSServiceOrder)
                {
                    InsertUpdateDeleteServiceOrderLine(apInvoiceRow, row, relatedDocNoteID, apTranStatus, (FSServiceOrder)item, ref graphHelper, ref lastDocNoteID);
                }
            }
        }

        public virtual void InsertUpdateDeleteServiceOrderLine(APInvoice apInvoiceRow, APTran row, Guid? relatedDocNoteID, PXEntryStatus apTranStatus, FSServiceOrder serviceOrder, ref PXGraph graphHelper, ref Guid? lastDocNoteID)
        {
            if (graphHelper == null || graphHelper is AppointmentEntry)
            {
                SaveFSGraphHelper(graphHelper);
                graphHelper = PXGraph.CreateInstance<ServiceOrderEntry>();
            }

            var graph = (ServiceOrderEntry)graphHelper;

            if (lastDocNoteID == null || lastDocNoteID != relatedDocNoteID)
            {
                if (graphHelper.IsDirty)
                {
                    graph.GetSaveAction().Press();
                    graph.Clear(PXClearOption.ClearAll);
                }

                graph.ServiceOrderRecords.Current = graph.ServiceOrderRecords.Search<FSServiceOrder.refNbr>(serviceOrder.RefNbr, serviceOrder.SrvOrdType);
            }

            //No need to run any field validation when deleting a detail line.
            if (apTranStatus == PXEntryStatus.Deleted || ValidateLinkedFSDocFields(Base.Transactions.Cache, row, graph.ServiceOrderDetails.Current, false) == true)
            {
                InsertUpdateDeleteDocDetail(graph.ServiceOrderDetails.Cache, graph.ServiceOrderDetails.Select().RowCast<FSSODet>(), apInvoiceRow, row, apTranStatus, graph.ServiceOrderRecords.Current.IsBilledOrClosed);
            }

            lastDocNoteID = serviceOrder.NoteID;
        }

        public virtual void InsertUpdateDeleteAppointmentLine(APInvoice apInvoiceRow, APTran row, Guid? relatedDocNoteID, PXEntryStatus apTranStatus, FSAppointment appointment, ref PXGraph graphHelper, ref Guid? lastDocNoteID)
        {
            if (graphHelper == null || graphHelper is ServiceOrderEntry)
            {
                SaveFSGraphHelper(graphHelper);
                graphHelper = PXGraph.CreateInstance<AppointmentEntry>();
            }

            var graph = (AppointmentEntry)graphHelper;

            if (lastDocNoteID == null || lastDocNoteID != relatedDocNoteID)
            {
                if (graphHelper.IsDirty)
                {
                    graph.GetSaveAction().Press();
                    graph.Clear(PXClearOption.ClearAll);
                }

                graph.AppointmentRecords.Current = graph.AppointmentRecords.Search<FSAppointment.refNbr>(appointment.RefNbr, appointment.SrvOrdType);
            }

            //No need to run any field validation when deleting a detail line.
            if (apTranStatus == PXEntryStatus.Deleted || ValidateLinkedFSDocFields(Base.Transactions.Cache, row, graph.AppointmentRecords.Current, false) == true)
            {
                InsertUpdateDeleteDocDetail(graph.AppointmentDetails.Cache, graph.AppointmentDetails.Select().RowCast<FSAppointmentDet>(), apInvoiceRow, row, apTranStatus, graph.AppointmentRecords.Current.IsBilledOrClosed);
            }

            lastDocNoteID = appointment.NoteID;
        }

        public virtual bool ValidateLinkedFSDocFields(PXCache apTranCache, APTran row, object fsDoc, bool throwError)
        {
            PXSetPropertyException exception = null;
            FSxAPTran extRow = null;

            if (fsDoc == null)
            {
                extRow = apTranCache.GetExtension<FSxAPTran>(row);
                PXEntryStatus apTranStatus = apTranCache.GetStatus(row);
                if (apTranStatus == PXEntryStatus.Inserted || apTranStatus == PXEntryStatus.Updated)
                {
                    if (extRow != null && extRow.RelatedDocNoteID != null)
                    {
                        fsDoc = new EntityHelper(Base).GetEntityRow(extRow.RelatedDocNoteID, true);
                    }
                }
            }

            if (fsDoc != null)
            {
                if (row.InventoryID == null)
                {
                    exception = new PXSetPropertyException(TX.Error.InventoryItemCannotBeEmptyForAPBill, PXErrorLevel.Error);
                }
                else if (row.InventoryID != null)
                {
                    InventoryItem inventoryItem = (InventoryItem)PXSelectorAttribute.Select<APTran.inventoryID>(apTranCache, row);
                    if (inventoryItem?.StkItem == true)
                    {
                        exception = new PXSetPropertyException(TX.Error.InventoryItemCannotBeStockItemForAPBill, PXErrorLevel.Error);
                    }
                }
                
                if (fsDoc is FSAppointment)
                {
                    FSAppointment appRow = (FSAppointment)fsDoc;
                    if (appRow.ProjectID != row.ProjectID)
                    {
                        PMProject pmProjectRow = PMProject.PK.Find(Base, appRow.ProjectID);
                        exception = new PXSetPropertyException(TX.Error.FSRelatedDocumentNotMatchCurrentDocumentProject,
                                                               TX.Linked_Entity_Type.APBill,
                                                               pmProjectRow?.ContractCD,
                                                               apTranCache.GetValueExt<APTran.projectID>(row), PXErrorLevel.Error);
                    }
                }
                else if (fsDoc is FSServiceOrder)
                {
                    FSServiceOrder soRow = (FSServiceOrder)fsDoc;
                    if (soRow.ProjectID != row.ProjectID)
                    {
                        PMProject pmProjectRow = PMProject.PK.Find(Base, soRow.ProjectID);
                        exception = new PXSetPropertyException(TX.Error.FSRelatedDocumentNotMatchCurrentDocumentProject,
                                                               TX.Linked_Entity_Type.APBill,
                                                               pmProjectRow?.ContractCD,
                                                               apTranCache.GetValueExt<APTran.projectID>(row), PXErrorLevel.Error);
                    }
                }
            }

            if (throwError && exception != null)
            {
                apTranCache.RaiseExceptionHandling<FSxAPTran.relatedDocNoteID>(row, extRow.RelatedDocNoteID, exception);
            }

            return exception == null;
        }

        public virtual void InsertUpdateDeleteDocDetail(PXCache cache, IEnumerable<IFSSODetBase> rowList, APInvoice apInvoiceRow, APTran apTranRow, PXEntryStatus apTranStatus, bool IsDocBilledOrClosed)
        {
            if (apInvoiceRow == null || apTranRow == null)
                return;

            IFSSODetBase detRow = null;
            var existingRow = rowList.Where(det => det.LinkedEntityType == ListField_Linked_Entity_Type.APBill
                                                && det.LinkedDocType?.Trim() == apTranRow.TranType
                                                && det.LinkedDocRefNbr?.Trim() == apTranRow.RefNbr.Trim()
                                                && det.LinkedLineNbr == apTranRow.LineNbr).ToList();

            IFSSODetBase deleteRow = null;
            if (existingRow.Count > 0)
            {
                detRow = existingRow.First();
                if (detRow.InventoryID != apTranRow.InventoryID || apTranStatus == PXEntryStatus.Deleted)
                {
                    deleteRow = detRow;
                    detRow = null;
                }
            }

            if (apTranStatus != PXEntryStatus.Deleted && apTranRow.InventoryID != null)
            {
                if (detRow == null)
                {
                    detRow = (IFSSODetBase)cache.CreateInstance();

                    detRow.InventoryID = apTranRow.InventoryID;
                    detRow.BillingRule = ID.BillingRule.FLAT_RATE;
                    detRow.TranDesc = apTranRow.TranDesc;
                    detRow.ProjectID = apTranRow.ProjectID;
                    detRow.EstimatedDuration = 0;
                    detRow.CuryUnitPrice = 0;
                    detRow.UOM = apTranRow.UOM;
                    detRow.EstimatedQty = apTranRow.Qty;
                    detRow.ProjectTaskID = apTranRow.TaskID;
                    detRow.CostCodeID = apTranRow.CostCodeID;
                    detRow.LinkedEntityType = ListField_Linked_Entity_Type.APBill;
                    detRow.LinkedDocType = apInvoiceRow.DocType;
                    detRow.LinkedDocRefNbr = apInvoiceRow.RefNbr;
                    detRow.LinkedLineNbr = apTranRow.LineNbr;
                    detRow.IsBillable = false;

                    detRow = (IFSSODetBase)cache.Insert(detRow);
                }

                detRow = (IFSSODetBase)cache.CreateCopy(detRow);
                bool rowUpdated = false;

                if (detRow.TranDesc != apTranRow.TranDesc)
                {
                    detRow.TranDesc = apTranRow.TranDesc;
                    rowUpdated = true;
                }

                if (detRow.CuryUnitCost != apTranRow.CuryUnitCost)
                {
                    detRow.CuryUnitCost = apTranRow.CuryUnitCost;
                    rowUpdated = true;
                }

                if (detRow.CuryExtCost != apTranRow.CuryLineAmt)
                {
                    detRow.CuryExtCost = apTranRow.CuryLineAmt;
                    rowUpdated = true;
                }

                // Following fields can only be synchronized if document is NOT closed/billed
                if (IsDocBilledOrClosed == false)
                {
                    if (detRow.UOM != apTranRow.UOM)
                    {
                        detRow.UOM = apTranRow.UOM;
                        rowUpdated = true;
                    }

                    if (detRow.EstimatedQty != apTranRow.Qty)
                    {
                        detRow.EstimatedQty = apTranRow.Qty;
                        rowUpdated = true;
                    }

                    if (detRow.ProjectTaskID != apTranRow.TaskID)
                    {
                        detRow.ProjectTaskID = apTranRow.TaskID;
                        rowUpdated = true;
                    }

                    if (detRow.CostCodeID != apTranRow.CostCodeID)
                    {
                        detRow.CostCodeID = apTranRow.CostCodeID;
                        rowUpdated = true;
                    }

                    if (detRow.IsBillable == true)
                    {
                        if (detRow.CuryBillableExtPrice != detRow.CuryExtCost)
                        {
                            detRow.CuryBillableExtPrice = detRow.CuryExtCost;
                            rowUpdated = true;
                        }
                    }
                }

                if (rowUpdated == true)
                {
                    detRow = (IFSSODetBase)cache.Update(detRow);
                    rowUpdated = false;
                }
            }

            if (deleteRow != null)
            {
                cache.Delete(deleteRow);
            }
        }

        public virtual void VerifyAPFieldCanBeUpdated<Field>(PXCache cache, APTran row)
            where Field : class, IBqlField
        {
            FSxAPTran extRow = cache.GetExtension<FSxAPTran>(row);

            if (extRow.RelatedDocNoteID != null && row.CreatedByScreenID.Substring(0, 2) != "FS")
            {
                if (extRow.IsDocBilledOrClosed == null)
                {
                    var item = new EntityHelper(Base).GetEntityRow(extRow.RelatedDocNoteID, true);
                    if (item != null)
                    {
                        extRow.IsDocBilledOrClosed = IsFSDocumentBilledOrClosed(item);
                    }
                }

                Guid? oriRelatedDocNoteID = (Guid?)cache.GetValueOriginal<FSxAPTran.relatedDocNoteID>(row);

                //Exception message should not be shown while the detail line is not yet inserted in the FS document,
                //If DocNoteID has not changed then it means the line was already inserted in the FS document.
                if (extRow.IsDocBilledOrClosed == true && oriRelatedDocNoteID == extRow.RelatedDocNoteID)
                {
                    throw new PXSetPropertyException(TX.Error.CannotChangeAPFieldBecauseFSDocIsClosedBilled,
                        PXUIFieldAttribute.GetDisplayName<Field>(cache));
                }
            }
        }

        public virtual bool IsFSDocumentBilledOrClosed(object row)
        {
            if (row == null)
            {
                return false;
            }

            if (row is FSAppointment)
            {
                FSAppointment appointment = (FSAppointment)row;
                return appointment.IsBilledOrClosed;
            }

            if (row is FSServiceOrder)
            {
                FSServiceOrder serviceOrder = (FSServiceOrder)row;
                return serviceOrder.IsBilledOrClosed;
            }

            return false;
        }
        #endregion

        #region Validations
        public virtual void ValidatePostBatchStatus(PXDBOperation dbOperation, string postTo, string createdDocType, string createdRefNbr)
        {
            DocGenerationHelper.ValidatePostBatchStatus<APInvoice>(Base, dbOperation, postTo, createdDocType, createdRefNbr);
        }
        #endregion
    }
}
