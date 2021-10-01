using PX.Data;
using PX.Objects.AR;
using PX.Objects.Common.Extensions;
using PX.Objects.CS;
using PX.Objects.PM;
using PX.Objects.SO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static PX.Objects.FS.MessageHelper;

namespace PX.Objects.FS
{
    public class SM_SOInvoiceEntry : FSPostingBase<SOInvoiceEntry>
    {
        #region Views
        [PXHidden]
        public PXSelect<FSBillHistory> BillHistoryRecords;
        #endregion

        #region Functions
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public virtual bool IsFSIntegrationEnabled()
        {
            if (IsActive() == false)
            {
                return false;
            }


            ARInvoice arInvoiceRow = Base.Document.Current;
            FSxARInvoice fsxARInvoiceRow = Base.Document.Cache.GetExtension<FSxARInvoice>(arInvoiceRow);

            return SM_ARInvoiceEntry.IsFSIntegrationEnabled(arInvoiceRow, fsxARInvoiceRow);
        }
        #endregion

        [PXOverride]
        public virtual IEnumerable Release(PXAdapter adapter, Func<PXAdapter, IEnumerable> baseMethod)
        {
            if (PXAccess.FeatureInstalled<FeaturesSet.equipmentManagementModule>())
            {
                PXGraph.InstanceCreated.AddHandler<ARReleaseProcess>((graph) =>
                {
                    graph.GetExtension<SM_ARReleaseProcess>().processEquipmentAndComponents = true;
                });
            }
            return baseMethod(adapter);
        }

        public delegate void NonTransferApplicationQueryDelegate(PXSelectBase<ARPayment> cmd);

        /// <summary>
        /// Overrides <see cref="SOInvoiceEntry.NonTransferApplicationQuery(PXSelectBase<ARPayment>)"/>
        /// </summary>
        [PXOverride]
        public virtual void NonTransferApplicationQuery(PXSelectBase<ARPayment> cmd, NonTransferApplicationQueryDelegate baseMethod)
        {
            cmd.Join<LeftJoin<ARAdjust2,
                            On<ARAdjust2.adjgDocType, Equal<ARPayment.docType>,
                                And<ARAdjust2.adjgRefNbr, Equal<ARPayment.refNbr>,
                                And<ARAdjust2.adjNbr, Equal<ARPayment.adjCntr>,
                                And<ARAdjust2.released, Equal<False>,
                                And<ARAdjust2.hold, Equal<True>,
                                And<ARAdjust2.voided, Equal<False>,
                                And<
                                    Where<ARAdjust2.adjdDocType, NotEqual<Current<ARInvoice.docType>>,
                                        Or<ARAdjust2.adjdRefNbr, NotEqual<Current<ARInvoice.refNbr>>>
                                    >>>>>>>>>>();
            cmd.Join<LeftJoin<FSAdjust,
            On<FSAdjust.adjgDocType, Equal<ARPayment.docType>,
                And<FSAdjust.adjgRefNbr, Equal<ARPayment.refNbr>>>>>();

            cmd.Join<LeftJoin<SOAdjust,
            On<SOAdjust.adjgDocType, Equal<ARPayment.docType>,
                And<SOAdjust.adjgRefNbr, Equal<ARPayment.refNbr>,
                And<SOAdjust.adjAmt, Greater<decimal0>>>>>>();

            cmd.WhereAnd<Where<ARPayment.finPeriodID, LessEqual<Current<ARInvoice.finPeriodID>>,
                And2<Where2<
                        Where<ARPayment.released, Equal<True>>,
                         Or<
                            Where<FSAdjust.adjdOrderType, IsNotNull,
                                And<FSAdjust.adjdOrderNbr, IsNotNull>>>>,
                And<ARAdjust2.adjdRefNbr, IsNull,
                And<SOAdjust.adjgRefNbr, IsNull>>>>>();
        }

        public virtual string GetLineDisplayHint(PXGraph graph, string lineRefNbr, string lineDescr, int? inventoryID)
        {
            return MessageHelper.GetLineDisplayHint(graph, lineRefNbr, lineDescr, inventoryID);
        }

        #region Events Handlers

        #region ARInvoice

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

        protected virtual void _(Events.RowSelecting<ARInvoice> e)
        {
            SM_ARInvoiceEntry.SetUnpersistedFSInfo(e.Cache, e.Args);
        }

        protected virtual void _(Events.RowSelected<ARInvoice> e)
        {
        }

        protected virtual void _(Events.RowInserting<ARInvoice> e)
        {
        }

        protected virtual void _(Events.RowInserted<ARInvoice> e)
        {
        }

        protected virtual void _(Events.RowUpdating<ARInvoice> e)
        {
        }

        protected virtual void _(Events.RowUpdated<ARInvoice> e)
        {
        }

        protected virtual void _(Events.RowDeleting<ARInvoice> e)
        {
        }

        protected virtual void _(Events.RowDeleted<ARInvoice> e)
        {
        }

        protected virtual void _(Events.RowPersisting<ARInvoice> e)
        {
        }

        protected virtual void _(Events.RowPersisted<ARInvoice> e)
        {
        }

        #endregion

        #region SOInvoice

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

        protected virtual void _(Events.RowSelecting<SOInvoice> e)
        {
        }

        protected virtual void _(Events.RowSelected<SOInvoice> e)
        {
        }

        protected virtual void _(Events.RowInserting<SOInvoice> e)
        {
        }

        protected virtual void _(Events.RowInserted<SOInvoice> e)
        {
        }

        protected virtual void _(Events.RowUpdating<SOInvoice> e)
        {
        }

        protected virtual void _(Events.RowUpdated<SOInvoice> e)
        {
        }

        protected virtual void _(Events.RowDeleting<SOInvoice> e)
        {
        }

        protected virtual void _(Events.RowDeleted<SOInvoice> e)
        {
        }

        protected virtual void _(Events.RowPersisting<SOInvoice> e)
        {
        }

        protected virtual void _(Events.RowPersisted<SOInvoice> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (e.TranStatus == PXTranStatus.Open)
            {
                if (e.Operation == PXDBOperation.Delete)
                {
                    var requiredAllocationList = FSAllocationProcess.GetRequiredAllocationList(Base, e.Row);

                    FSAllocationProcess.ReallocateServiceOrderSplits(requiredAllocationList);

                    CleanPostingInfoLinkedToDoc(e.Row);
                }
            }
        }
        #endregion

        #region ARTran

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

        protected virtual void _(Events.RowSelecting<ARTran> e)
        {
        }

        protected virtual void _(Events.RowSelected<ARTran> e)
        {
            bool fsIntegrationEnabled = IsFSIntegrationEnabled();
            DACHelper.SetExtensionVisibleInvisible(typeof(FSARTran), e.Cache, e.Args, fsIntegrationEnabled, false);
        }

        protected virtual void _(Events.RowInserting<ARTran> e)
        {
        }

        protected virtual void _(Events.RowInserted<ARTran> e)
        {
        }

        protected virtual void _(Events.RowUpdating<ARTran> e)
        {
            if (e.Row == null 
                || Base.Document.Current == null 
                || Base.Document.Current.DocType != ARInvoiceType.CreditMemo)
            {
                return;
            }

            //Using Orig fields prevents any update in the line even when it is not saved yet.
            FSARTran fsRow = FSARTran.PK.Find(Base, e.Row.OrigInvoiceType, e.Row.OrigInvoiceNbr, e.Row.OrigInvoiceLineNbr);
            
            if (fsRow?.IsFSRelated == true && e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted)
            {
                bool updtAcctSub = e.Cache.ObjectsEqualExceptFields<ARTran.accountID, ARTran.subID>(e.Row, e.NewRow);

                if (updtAcctSub == false)
                {
                    throw new PXSetPropertyException(TX.Error.CannotUpdateCreditMemoLineRelatedToFSDocument);
                }
            }
        }

        protected virtual void _(Events.RowUpdated<ARTran> e)
        {
        }

        protected virtual void _(Events.RowDeleting<ARTran> e)
        {
            if (e.Row == null 
                || Base.Document.Current == null 
                || Base.Document.Current.DocType != ARInvoiceType.CreditMemo
                || Base.Document.Cache.GetStatus(Base.Document.Current) == PXEntryStatus.Deleted)
            {
                return;
            }

            FSARTran fsRow = FSARTran.PK.Find(Base, e.Row.TranType, e.Row.RefNbr, e.Row.LineNbr);
            if (fsRow?.IsFSRelated == true)
            {
                throw new PXSetPropertyException(TX.Error.CannotDeleteCreditMemoLineRelatedToFSDocument); 
            }
        }

        protected virtual void _(Events.RowDeleted<ARTran> e)
        {
        }

        protected virtual void _(Events.RowPersisting<ARTran> e)
        {
        }

        protected virtual void _(Events.RowPersisted<ARTran> e)
        {
            if (e.Row == null)
            {
                return;
            }

            var soInvoiceRow = Base.SODocument.Current;

            if (e.TranStatus == PXTranStatus.Open)
            {
                if (e.Operation == PXDBOperation.Insert
                        && soInvoiceRow.DocType != ARDocType.CreditMemo)
                {
                    FSARTran fsARTran = FSARTran.PK.FindDirty(Base, e.Row.TranType, e.Row.RefNbr, e.Row.LineNbr);

                    if (fsARTran != null)
                    {
                        var fsBillHistory = CreateBillHistoryRowsFromARTran(BillHistoryRecords.Cache, fsARTran,
                                FSEntityType.SOInvoice, soInvoiceRow.DocType, soInvoiceRow.RefNbr,
                                FSEntityType.SalesOrder, null, null,
                                true);

                        if (fsBillHistory != null)
                        {
                            BillHistoryRecords.Cache.Persist(fsBillHistory, PXDBOperation.Insert);
                        }
                    }
                }
            }

            // We call here cache.GetStateExt for every field when the transaction is aborted
            // to set the errors in the fields and then the Generate Invoice screen can read them
            if (e.TranStatus == PXTranStatus.Aborted && IsInvoiceProcessRunning == true)
            {
                MessageHelper.GetRowMessage(e.Cache, e.Row, false, false);
            }
        }
        #endregion

        #endregion

        #region Invoicing Methods
        public override List<ErrorInfo> GetErrorInfo()
        {
            return MessageHelper.GetErrorInfo<ARTran>(Base.Document.Cache, Base.Document.Current, Base.Transactions);
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

            PXSelect<FSARTran> fsARTranView = new PXSelect<FSARTran>(Base);

            if (!Base.Views.Caches.Contains(typeof(FSARTran)))
                Base.Views.Caches.Add(typeof(FSARTran));

            var cancel_defaulting = new PXFieldDefaulting((sender, e) =>
            {
                e.NewValue = fsServiceOrderRow.BranchID;
                e.Cancel = true;
            });

            try
            {
                Base.FieldDefaulting.AddHandler<ARInvoice.branchID>(cancel_defaulting);

                ARInvoice arInvoiceRow = new ARInvoice();

                if (invtMult >= 0)
                {
                    arInvoiceRow.DocType = ARInvoiceType.Invoice;
                    CheckAutoNumbering(Base.ARSetup.SelectSingle().InvoiceNumberingID);
                }
                else
                {
                    arInvoiceRow.DocType = ARInvoiceType.CreditMemo;
                    CheckAutoNumbering(Base.ARSetup.SelectSingle().CreditAdjNumberingID);
                }

                arInvoiceRow.DocDate = invoiceDate;
                arInvoiceRow.FinPeriodID = invoiceFinPeriodID;
                arInvoiceRow.InvoiceNbr = fsServiceOrderRow.CustPORefNbr;
                arInvoiceRow = Base.Document.Insert(arInvoiceRow);
                initialHold = arInvoiceRow.Hold;
                arInvoiceRow.NoteID = null;
                PXNoteAttribute.GetNoteIDNow(Base.Document.Cache, arInvoiceRow);

                Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.hold>(arInvoiceRow, true);
                Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.customerID>(arInvoiceRow, fsServiceOrderRow.BillCustomerID);
                Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.customerLocationID>(arInvoiceRow, fsServiceOrderRow.BillLocationID);
                Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.curyID>(arInvoiceRow, fsServiceOrderRow.CuryID);

                Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.taxZoneID>(arInvoiceRow, fsAppointmentRow != null ? fsAppointmentRow.TaxZoneID : fsServiceOrderRow.TaxZoneID);
                Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.taxCalcMode>(arInvoiceRow, fsAppointmentRow != null ? fsAppointmentRow.TaxCalcMode : fsServiceOrderRow.TaxCalcMode);

                string termsID = GetTermsIDFromCustomerOrVendor(graphProcess, fsServiceOrderRow.BillCustomerID, null);
                if (termsID != null)
                {
                    Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.termsID>(arInvoiceRow, termsID);
                }
                else
                {
                    Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.termsID>(arInvoiceRow, fsSrvOrdTypeRow.DfltTermIDARSO);
                }

                if (fsServiceOrderRow.ProjectID != null)
                {
                    Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.projectID>(arInvoiceRow, fsServiceOrderRow.ProjectID);
                }

                Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.docDesc>(arInvoiceRow, fsServiceOrderRow.DocDesc);
                arInvoiceRow.FinPeriodID = invoiceFinPeriodID;
                arInvoiceRow = Base.Document.Update(arInvoiceRow);
            
	            SetContactAndAddress(Base, fsServiceOrderRow);

			    if (onDocumentHeaderInserted != null)
                {
                    onDocumentHeaderInserted(Base, arInvoiceRow);
                }

                List<SharedClasses.SOARLineEquipmentComponent> componentList = new List<SharedClasses.SOARLineEquipmentComponent>();
                FSSODet soDet = null;
                FSAppointmentDet appointmentDet = null;
                ARTran arTranRow = null;

                foreach (DocLineExt docLineExt in docLines)
                {
                    soDet = docLineExt.fsSODet;
                    appointmentDet = docLineExt.fsAppointmentDet;

                    bool isLotSerialRequired = SharedFunctions.IsLotSerialRequired(Base.Transactions.Cache, docLineExt.docLine.InventoryID);

                    if (isLotSerialRequired)
                    {
                        bool validateLotSerQty = false;
                        decimal? qtyInserted = 0m;
                        if (fsAppointmentRow == null) // is posted by Service Order
                        {
                            foreach (FSSODetSplit split in PXSelect<FSSODetSplit,
                                                           Where<
                                                                FSSODetSplit.srvOrdType, Equal<Required<FSSODetSplit.srvOrdType>>,
                                                                And<FSSODetSplit.refNbr, Equal<Required<FSSODetSplit.refNbr>>,
                                                                And<FSSODetSplit.lineNbr, Equal<Required<FSSODetSplit.lineNbr>>>>>,
                                                           OrderBy<Asc<FSSODetSplit.splitLineNbr>>>
                                                           .Select(Base, soDet.SrvOrdType, soDet.RefNbr, soDet.LineNbr)
                                                           .RowCast<FSSODetSplit>()
                                                           .Where(x => x.POCreate == false && string.IsNullOrEmpty(x.LotSerialNbr) == false))
                            {
                                arTranRow = InsertSOInvoiceLine(
                                                                graphProcess,
                                                                fsARTranView,
                                                                arInvoiceRow,
                                                                docLineExt,
                                                                invtMult,
                                                                split.Qty,
                                                                split.UOM,
                                                                split.SiteID,
                                                                split.LocationID,
                                                                split.LotSerialNbr,
                                                                onTransactionInserted,
                                                                componentList);
                                validateLotSerQty = true;
                                qtyInserted += arTranRow.Qty;
                            }
                        }
                        else if (fsAppointmentRow != null)
                        {
                            foreach (FSApptLineSplit split in PXSelect<FSApptLineSplit,
                                                               Where<
                                                                    FSApptLineSplit.srvOrdType, Equal<Required<FSApptLineSplit.srvOrdType>>,
                                                                    And<FSApptLineSplit.apptNbr, Equal<Required<FSApptLineSplit.apptNbr>>,
                                                                    And<FSApptLineSplit.lineNbr, Equal<Required<FSApptLineSplit.lineNbr>>>>>,
                                                               OrderBy<Asc<FSApptLineSplit.splitLineNbr>>>
                                                               .Select(Base, appointmentDet.SrvOrdType, appointmentDet.RefNbr, appointmentDet.LineNbr)
                                                               .RowCast<FSApptLineSplit>()
                                                               .Where(x => string.IsNullOrEmpty(x.LotSerialNbr) == false))
                            {
                                arTranRow = InsertSOInvoiceLine(
                                                                graphProcess,
                                                                fsARTranView,
                                                                arInvoiceRow,
                                                                docLineExt,
                                                                invtMult,
                                                                split.Qty,
                                                                split.UOM,
                                                                split.SiteID,
                                                                split.LocationID,
                                                                split.LotSerialNbr,
                                                                onTransactionInserted,
                                                                componentList);
                                validateLotSerQty = true;
                                qtyInserted += arTranRow.Qty;
                            }
                        }

                        if (validateLotSerQty == false)
                        {
                            arTranRow = InsertSOInvoiceLine(
                                                        graphProcess,
                                                        fsARTranView,
                                                        arInvoiceRow,
                                                        docLineExt,
                                                        invtMult,
                                                        docLineExt.docLine.GetQty(FieldType.BillableField),
                                                        docLineExt.docLine.UOM,
                                                        docLineExt.docLine.SiteID,
                                                        docLineExt.docLine.SiteLocationID,
                                                        docLineExt.docLine.LotSerialNbr,
                                                        onTransactionInserted,
                                                        componentList);
                        }
                        else
                        {
                            if (qtyInserted != docLineExt.docLine.GetQty(FieldType.BillableField))
                            {
                                throw new PXException(TX.Error.QTY_POSTED_ERROR);
                            }
                        }
                    }
                    else
                    {
                        arTranRow = InsertSOInvoiceLine(
                                                        graphProcess,
                                                        fsARTranView,
                                                        arInvoiceRow,
                                                        docLineExt,
                                                        invtMult,
                                                        docLineExt.docLine.GetQty(FieldType.BillableField),
                                                        docLineExt.docLine.UOM,
                                                        docLineExt.docLine.SiteID,
                                                        docLineExt.docLine.SiteLocationID,
                                                        docLineExt.docLine.LotSerialNbr,
                                                        onTransactionInserted,
                                                        componentList);
                    }
                }

                if (componentList.Count > 0)
                {
                    //Assigning the NewTargetEquipmentLineNbr field value for the component type records
                    foreach (SharedClasses.SOARLineEquipmentComponent currLineModel in componentList.Where(x => x.equipmentAction == ID.Equipment_Action.SELLING_TARGET_EQUIPMENT))
                    {
                        fsARTranView.Insert(currLineModel.fsARTranRow);
                        foreach (SharedClasses.SOARLineEquipmentComponent currLineComponent in componentList.Where(x => (x.equipmentAction == ID.Equipment_Action.CREATING_COMPONENT
                                                                                                                        || x.equipmentAction == ID.Equipment_Action.UPGRADING_COMPONENT
                                                                                                                        || x.equipmentAction == ID.Equipment_Action.NONE)))
                        {
                            if (currLineComponent.sourceNewTargetEquipmentLineNbr == currLineModel.sourceLineRef)
                            {
                                currLineComponent.fsARTranRow.ComponentID = currLineComponent.componentID;
                                currLineComponent.fsARTranRow.NewEquipmentLineNbr = currLineModel.currentLineRef;
                                fsARTranView.Insert(currLineComponent.fsARTranRow);
                            }
                        }
                    }
                }

                arInvoiceRow = Base.Document.Update(arInvoiceRow);

                if (Base.ARSetup.Current.RequireControlTotal == true)
                {
                    Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.curyOrigDocAmt>(arInvoiceRow, arInvoiceRow.CuryDocBal);
                }

                if (initialHold != true || quickProcessFlow != PXQuickProcess.ActionFlow.NoFlow)
                {
                    Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.hold>(arInvoiceRow, false);
                }

                arInvoiceRow = Base.Document.Update(arInvoiceRow);
            }
            finally
            {
                Base.FieldDefaulting.RemoveHandler<ARInvoice.branchID>(cancel_defaulting);
            }
        }

        public virtual ARTran InsertSOInvoiceLine(PXGraph graphProcess,
                                                  PXSelect<FSARTran> fsARTranView,
                                                  ARInvoice arInvoiceRow,
                                                  DocLineExt docLineExt,
                                                  int? invtMult,
                                                  decimal? qty,
                                                  string UOM,
                                                  int? siteID,
                                                  int? locationID,
                                                  string lotSerialNbr,
                                                  OnTransactionInsertedDelegate onTransactionInserted,
                                                  List<SharedClasses.SOARLineEquipmentComponent> componentList)
        {
            IDocLine docLine;
            ARTran arTranRow;
            FSARTran fsARTranRow;
            PMTask pmTaskRow;

            int? acctID;
            docLine = docLineExt.docLine;

            FSPostDoc fsPostDocRow = docLineExt.fsPostDoc;
            FSServiceOrder fsServiceOrderRow = docLineExt.fsServiceOrder;
            FSSrvOrdType fsSrvOrdTypeRow = docLineExt.fsSrvOrdType;
            FSAppointment fsAppointmentRow = docLineExt.fsAppointment;
            FSAppointmentDet fsAppointmentDetRow = docLineExt.fsAppointmentDet;
            FSSODet fsSODetRow = docLineExt.fsSODet;

            arTranRow = new ARTran();
            arTranRow = Base.Transactions.Insert(arTranRow);

            arTranRow = (ARTran)Base.Transactions.Cache.CreateCopy(arTranRow);

            arTranRow.BranchID = docLine.BranchID;
            arTranRow.InventoryID = docLine.InventoryID;

            pmTaskRow = docLineExt.pmTask;

            if (pmTaskRow != null && pmTaskRow.Status == ProjectTaskStatus.Completed)
            {
                throw new PXException(TX.Error.POSTING_PMTASK_ALREADY_COMPLETED, fsServiceOrderRow.RefNbr, docLine.LineRef, pmTaskRow.TaskCD);
            }

            if (docLine.ProjectID != null && docLine.ProjectTaskID != null)
            {
                    arTranRow.TaskID = docLine.ProjectTaskID;
            }

            arTranRow.UOM = UOM;
            arTranRow.SiteID = siteID;

            arTranRow = Base.Transactions.Update(arTranRow);

            Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.locationID>(arTranRow, locationID);
            Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.lotSerialNbr>(arTranRow, lotSerialNbr);

            arTranRow = Base.Transactions.Update(arTranRow);
            Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.qty>(arTranRow, qty);            

            Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.tranDesc>(arTranRow, docLine.TranDesc);
            Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.salesPersonID>(arTranRow, fsAppointmentRow == null ? fsServiceOrderRow.SalesPersonID : fsAppointmentRow.SalesPersonID);

            if (docLine.AcctID != null)
            {
                acctID = docLine.AcctID;
            }
            else
            {
                acctID = Get_TranAcctID_DefaultValue(graphProcess,
                                                                      fsSrvOrdTypeRow.SalesAcctSource,
                                                                      docLine.InventoryID,
                                                                      docLine.SiteID,
                                                                      fsServiceOrderRow);
            }

            Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.accountID>(arTranRow, acctID);

            if (docLine.SubID != null)
            {
                try
                {
                    Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.subID>(arTranRow, docLine.SubID);
                }
                catch (PXException)
                {
                    arTranRow.SubID = null;
                }
            }
            else
            {
                SetCombinedSubID(graphProcess,
                                                    Base.Transactions.Cache,
                                                    arTranRow,
                                                    null,
                                                    null,
                                                    fsSrvOrdTypeRow,
                                                    arTranRow.BranchID,
                                                    arTranRow.InventoryID,
                                                    arInvoiceRow.CustomerLocationID,
                                                    fsServiceOrderRow.BranchLocationID,
                                                    fsServiceOrderRow.SalesPersonID,
                                                    docLine.IsService);
            }

            if (docLine.IsFree == true)
            {
                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.manualPrice>(arTranRow, true);
                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.curyUnitPrice>(arTranRow, 0m);

                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.manualDisc>(arTranRow, true);
                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.discPct>(arTranRow, 0m);
            }
            else
            {
            Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.manualPrice>(arTranRow, docLine.ManualPrice);
                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.curyUnitPrice>(arTranRow, docLine.CuryUnitPrice * invtMult);

                bool manualDisc = false;
                if (docLineExt.fsAppointmentDet != null)
                {
                    manualDisc = (bool)docLineExt.fsAppointmentDet.ManualDisc;
                }
                else if (docLineExt.fsSODet != null)
                {
                    manualDisc = (bool)docLineExt.fsSODet.ManualDisc;
                }

                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.manualDisc>(arTranRow, manualDisc);
                if (manualDisc == true)
                {
                    Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.discPct>(arTranRow, docLine.DiscPct);
                }
            }

            Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.taxCategoryID>(arTranRow, docLine.TaxCategoryID);
            Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.commissionable>(arTranRow, fsAppointmentRow?.Commissionable ?? fsServiceOrderRow.Commissionable ?? false);
            Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.costCodeID>(arTranRow, docLine.CostCodeID);

            // TODO: review this operation
            decimal? curyExtPrice = qty != 0 ? ((docLine.CuryBillableExtPrice * invtMult) / qty) * arTranRow.Qty : 0m;

            curyExtPrice = CM.PXCurrencyAttribute.Round(Base.Transactions.Cache, arTranRow, curyExtPrice ?? 0m, CM.CMPrecision.TRANCURY);

            Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.curyExtPrice>(arTranRow, curyExtPrice);

            fsARTranRow = new FSARTran();

            fsARTranRow.TranType = arTranRow.TranType;
            fsARTranRow.LineNbr = arTranRow.LineNbr;
            fsARTranRow.SrvOrdType = fsServiceOrderRow.SrvOrdType;
            fsARTranRow.ServiceOrderRefNbr = fsServiceOrderRow.RefNbr;
            fsARTranRow.AppointmentRefNbr = fsAppointmentRow?.RefNbr;
            fsARTranRow.ServiceOrderLineNbr = fsSODetRow?.LineNbr;
            fsARTranRow.AppointmentLineNbr = fsAppointmentDetRow?.LineNbr;

            SharedFunctions.CopyNotesAndFiles(Base.Transactions.Cache, arTranRow, docLine, fsSrvOrdTypeRow);
            fsPostDocRow.DocLineRef = arTranRow = Base.Transactions.Update(arTranRow);

            if (PXAccess.FeatureInstalled<FeaturesSet.equipmentManagementModule>())
            {
                if (docLine.EquipmentAction != null)
                {
                    fsARTranRow.EquipmentAction = docLine.EquipmentAction;
                    fsARTranRow.SMEquipmentID = docLine.SMEquipmentID;
                    fsARTranRow.EquipmentComponentLineNbr = docLine.EquipmentLineRef;
                    fsARTranRow.Comment = docLine.Comment;

                    if (docLine.EquipmentAction == ID.Equipment_Action.SELLING_TARGET_EQUIPMENT
                        || ((docLine.EquipmentAction == ID.Equipment_Action.CREATING_COMPONENT
                             || docLine.EquipmentAction == ID.Equipment_Action.UPGRADING_COMPONENT
                             || docLine.EquipmentAction == ID.Equipment_Action.NONE)
                                && string.IsNullOrEmpty(docLine.NewTargetEquipmentLineNbr) == false))
                    {
                        componentList.Add(new SharedClasses.SOARLineEquipmentComponent(docLine, arTranRow, fsARTranRow));
                    }
                    else
                    {
                        fsARTranRow.ComponentID = docLine.ComponentID;
                        fsARTranView.Insert(fsARTranRow);
                    }
                }
            }
            else 
            {
                fsARTranView.Insert(fsARTranRow);
            }

            if (onTransactionInserted != null)
            {
                onTransactionInserted(Base, arTranRow);
            }

            return arTranRow;
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

            Base.SelectTimeStamp();
            Base.Save.Press();

            ARInvoice arInvoice = Base.Document.Current;

            var fsCreatedDocRow = new FSCreatedDoc()
            {
                BatchID = batchID,
                PostTo = ID.Batch_PostTo.SI,
                CreatedDocType = arInvoice.DocType,
                CreatedRefNbr = arInvoice.RefNbr
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
            Base.Document.Current = Base.Document.Search<ARInvoice.refNbr>(fsCreatedDocRow.CreatedRefNbr, fsCreatedDocRow.CreatedDocType);

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
                Set<FSPostInfo.sOInvLineNbr, Null,
                Set<FSPostInfo.sOInvRefNbr, Null,
                Set<FSPostInfo.sOInvDocType, Null,
                Set<FSPostInfo.sOInvPosted, False>>>>,
            FSPostInfo,
            Where<
                FSPostInfo.postID, Equal<Required<FSPostInfo.postID>>,
                And<FSPostInfo.sOInvPosted, Equal<True>>>>
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

            string refNbr = ((ARInvoice)document).RefNbr;
            string docType = ((ARInvoice)document).DocType;
            int? lineNbr = ((ARTran)lineDoc).LineNbr;

            return PXSelect<FSPostInfo,
                   Where<
                       FSPostInfo.sOInvRefNbr, Equal<Required<FSPostInfo.sOInvRefNbr>>,
                       And<FSPostInfo.sOInvDocType, Equal<Required<FSPostInfo.sOInvDocType>>,
                       And<FSPostInfo.sOInvLineNbr, Equal<Required<FSPostInfo.sOInvLineNbr>>,
                       And<FSPostInfo.sOInvPosted, Equal<True>>>>>>
                   .Select(cleanerGraph, refNbr, docType, lineNbr).Count() > 0;
        }

        public override void UpdateCostAndPrice(List<DocLineExt> docLines)
        {
        }
        #endregion

        #region Virtual Functions
        public virtual int? Get_TranAcctID_DefaultValue(PXGraph graph, string salesAcctSource, int? inventoryID, int? siteID, FSServiceOrder fsServiceOrderRow)
        {
            return ServiceOrderEntry.Get_TranAcctID_DefaultValueInt(graph, salesAcctSource, inventoryID, siteID, fsServiceOrderRow);
        }
        #endregion
    }
}
