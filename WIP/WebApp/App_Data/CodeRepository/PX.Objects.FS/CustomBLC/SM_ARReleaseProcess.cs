using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.SO;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.FS
{
    public class SM_ARReleaseProcess : PXGraphExtension<ARReleaseProcess>
    {
        #region ItemInfo
        public class ItemInfo
        {
            public virtual string LotSerialNbr { get; set; }
            public virtual string UOM { get; set; }
            public virtual decimal? Qty { get; set; }
            public virtual decimal? BaseQty { get; set; }

            #region Ctors
            public ItemInfo(SOShipLineSplit split)
            {
                LotSerialNbr = split.LotSerialNbr;
                UOM = split.UOM;
                Qty = split.Qty;
                BaseQty = split.BaseQty;
            }
            public ItemInfo(SOLineSplit split)
            {
                LotSerialNbr = split.LotSerialNbr;
                UOM = split.UOM;
                Qty = split.Qty;
                BaseQty = split.BaseQty;
            }
            public ItemInfo(ARTran arTran)
            {
                LotSerialNbr = arTran.LotSerialNbr;
                UOM = arTran.UOM;
                Qty = arTran.Qty;
                BaseQty = arTran.BaseQty;
            }
            #endregion
        }
        #endregion

        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public bool processEquipmentAndComponents = false;

        #region Overrides
        public delegate void PersistDelegate();

        [PXOverride]
        public void Persist(PersistDelegate baseMethod)
        {
            using (PXTransactionScope ts = new PXTransactionScope())
            {
                if (SharedFunctions.isFSSetupSet(Base) == true)
                {
                    ARInvoice arInvoiceRow = Base.ARInvoice_DocType_RefNbr.Current;

                    if (arInvoiceRow != null)
                    {
                        if (arInvoiceRow.DocType == ARDocType.CreditMemo
                            && arInvoiceRow.CreatedByScreenID.Substring(0, 2) != "FS"
                            && arInvoiceRow.Released == true)
                        {
                            CleanPostingInfoCreditMemo(arInvoiceRow);
                        }

                        if (PXAccess.FeatureInstalled<FeaturesSet.equipmentManagementModule>() == true && processEquipmentAndComponents)
                        {
                            Dictionary<int?, int?> newEquiments = new Dictionary<int?, int?>();
                            SMEquipmentMaint graphSMEquipmentMaint = PXGraph.CreateInstance<SMEquipmentMaint>();

                            CreateEquipments(graphSMEquipmentMaint, arInvoiceRow, newEquiments);
                            ReplaceEquipments(graphSMEquipmentMaint, arInvoiceRow);
                            UpgradeEquipmentComponents(graphSMEquipmentMaint, arInvoiceRow, newEquiments);
                            CreateEquipmentComponents(graphSMEquipmentMaint, arInvoiceRow, newEquiments);
                            ReplaceComponents(graphSMEquipmentMaint, arInvoiceRow);
                        }
                    }
                }

                baseMethod();

                ts.Complete();
            }
        }

        public delegate ARRegister OnBeforeReleaseDelegate(ARRegister ardoc);
        
        [PXOverride]
        public virtual ARRegister OnBeforeRelease(ARRegister ardoc, OnBeforeReleaseDelegate del)
        {
            ValidatePostBatchStatus(PXDBOperation.Update, ID.Batch_PostTo.AR, ardoc.DocType, ardoc.RefNbr);

            if (del != null)
            {
                return del(ardoc);
            }

            return null;
        }
        #endregion

        #region Methods
        public virtual void CleanPostingInfoCreditMemo(ARRegister arRegisterRow)
        {
            var anyFSARTranRowRelated = PXSelect<FSARTran,
                                        Where<
                                            FSARTran.tranType, Equal<Required<FSARTran.tranType>>,
                                        And<
                                            FSARTran.refNbr, Equal<Required<FSARTran.refNbr>>>>>
                                        .Select(Base, arRegisterRow.DocType, arRegisterRow.RefNbr)
                                        .RowCast<FSARTran>()
                                        .Where(_ => _.IsFSRelated == true)
                                        .Any();

            if (anyFSARTranRowRelated == true)
            {
                SOInvoice crmSOInvoiceRow = PXSelect<SOInvoice,
                                            Where<
                                                SOInvoice.docType, Equal<Required<SOInvoice.docType>>,
                                            And<
                                                SOInvoice.refNbr, Equal<Required<SOInvoice.refNbr>>>>>
                                            .Select(Base, arRegisterRow.DocType, arRegisterRow.RefNbr);

                if (crmSOInvoiceRow != null)
                {
                    var SOInvoiceGraph = PXGraph.CreateInstance<SOInvoiceEntry>();
                    SM_SOInvoiceEntry extGraph = SOInvoiceGraph.GetExtension<SM_SOInvoiceEntry>();

                    // TODO: Add OrigDocAmt and CuryID validation with the invoice (parent document).

                    extGraph.CleanPostingInfoFromSOCreditMemo(Base, crmSOInvoiceRow);

                    // TODO: Which is the parent document?
                    extGraph.CreateBillHistoryRowsForDocument(Base,
                                FSEntityType.SOCreditMemo, arRegisterRow.DocType, arRegisterRow.RefNbr,
                                null, null, null);
                }
                else
                {
                    var ARInvoiceGraph = PXGraph.CreateInstance<ARInvoiceEntry>();
                    SM_ARInvoiceEntry extGraph = ARInvoiceGraph.GetExtension<SM_ARInvoiceEntry>();

                    ARInvoice origARInvoiceRow = PXSelect<ARInvoice,
                                                 Where<
                                                     ARInvoice.docType, Equal<Required<ARInvoice.docType>>,
                                                 And<
                                                     ARInvoice.refNbr, Equal<Required<ARInvoice.refNbr>>>>>
                                                 .Select(Base, arRegisterRow.OrigDocType, arRegisterRow.OrigRefNbr)
                                                 .FirstOrDefault();

                    // To unlink the FSDocuments from the invoice
                    // the credit memo must reverse the invoice completely
                    if (origARInvoiceRow.OrigDocAmt == arRegisterRow.OrigDocAmt
                        && origARInvoiceRow.CuryID == arRegisterRow.CuryID)
                    {
                        extGraph.CleanPostingInfoLinkedToDoc(origARInvoiceRow);
                        extGraph.CleanContractPostingInfoLinkedToDoc(origARInvoiceRow);

                        extGraph.CreateBillHistoryRowsForDocument(Base,
                                    FSEntityType.ARCreditMemo, arRegisterRow.DocType, arRegisterRow.RefNbr,
                                    FSEntityType.ARInvoice, arRegisterRow.OrigDocType, arRegisterRow.OrigRefNbr);
                    }
                }
            }
        }

        public virtual void CreateEquipments(SMEquipmentMaint graphSMEquipmentMaint,
                                             ARRegister arRegisterRow,
                                             Dictionary<int?, int?> newEquiments)
        {
            var inventoryItemSet = PXSelectJoin<InventoryItem,
                                   InnerJoin<ARTran,
                                            On<ARTran.inventoryID, Equal<InventoryItem.inventoryID>,
                                            And<ARTran.tranType, Equal<ARDocType.invoice>>>,
                                   LeftJoin<SOLine,
                                            On<SOLine.orderType, Equal<ARTran.sOOrderType>,
                                            And<SOLine.orderNbr, Equal<ARTran.sOOrderNbr>,
                                            And<SOLine.lineNbr, Equal<ARTran.sOOrderLineNbr>>>>,
                                   LeftJoin<FSARTran,
                                            On<FSARTran.FK.ARTranLine>,
                                   LeftJoin<FSServiceOrder,
                                            On<FSServiceOrder.srvOrdType, Equal<FSARTran.srvOrdType>,
                                            And<FSServiceOrder.refNbr, Equal<FSARTran.serviceOrderRefNbr>>>,
                                    LeftJoin<FSAppointment,
                                            On<FSAppointment.srvOrdType, Equal<FSARTran.srvOrdType>,
                                            And<FSAppointment.refNbr, Equal<FSARTran.appointmentRefNbr>>>>>>>>,
                                   Where<
                                        ARTran.tranType, Equal<Required<ARInvoice.docType>>,
                                        And<ARTran.refNbr, Equal<Required<ARInvoice.refNbr>>,
                                        And<FSxEquipmentModel.eQEnabled, Equal<True>,
                                        And<FSARTran.equipmentAction, Equal<ListField_EquipmentAction.SellingTargetEquipment>,
                                        And<FSARTran.sMEquipmentID, IsNull,
                                        And<FSARTran.newEquipmentLineNbr, IsNull,
                                        And<FSARTran.componentID, IsNull>>>>>>>,
                                   OrderBy<
                                        Asc<ARTran.tranType,
                                        Asc<ARTran.refNbr,
                                        Asc<ARTran.lineNbr>>>>>
                                   .Select(Base, arRegisterRow.DocType, arRegisterRow.RefNbr);

            Create_Replace_Equipments(graphSMEquipmentMaint, inventoryItemSet, arRegisterRow, newEquiments, ID.Equipment_Action.SELLING_TARGET_EQUIPMENT);
        }

        public virtual void UpgradeEquipmentComponents(SMEquipmentMaint graphSMEquipmentMaint,
                                                       ARRegister arRegisterRow,
                                                       Dictionary<int?, int?> newEquiments)
        {
            var inventoryItemSet = PXSelectJoin<InventoryItem,
                                   InnerJoin<ARTran,
                                            On<ARTran.inventoryID, Equal<InventoryItem.inventoryID>,
                                            And<ARTran.tranType, Equal<ARDocType.invoice>>>,
                                    LeftJoin<SOLine,
                                            On<SOLine.orderType, Equal<ARTran.sOOrderType>,
                                            And<SOLine.orderNbr, Equal<ARTran.sOOrderNbr>,
                                            And<SOLine.lineNbr, Equal<ARTran.sOOrderLineNbr>>>>,
                                    LeftJoin<FSARTran,
                                            On<FSARTran.FK.ARTranLine>,
                                    LeftJoin<FSServiceOrder,
                                            On<FSServiceOrder.srvOrdType, Equal<FSARTran.srvOrdType>,
                                            And<FSServiceOrder.refNbr, Equal<FSARTran.serviceOrderRefNbr>>>,
                                    LeftJoin<FSAppointment,
                                            On<FSAppointment.srvOrdType, Equal<FSARTran.srvOrdType>,
                                            And<FSAppointment.refNbr, Equal<FSARTran.appointmentRefNbr>>>>>>>>,
                                   Where<
                                        ARTran.tranType, Equal<Required<ARInvoice.docType>>,
                                        And<ARTran.refNbr, Equal<Required<ARInvoice.refNbr>>,
                                        And<FSARTran.equipmentAction, Equal<ListField_EquipmentAction.UpgradingComponent>,
                                        And<FSARTran.sMEquipmentID, IsNull,
                                        And<FSARTran.newEquipmentLineNbr, IsNotNull,
                                        And<FSARTran.componentID, IsNotNull,
                                        And<FSARTran.equipmentComponentLineNbr, IsNull>>>>>>>,
                                   OrderBy<
                                        Asc<ARTran.tranType,
                                        Asc<ARTran.refNbr,
                                        Asc<ARTran.lineNbr>>>>>
                                   .Select(Base, arRegisterRow.DocType, arRegisterRow.RefNbr);

            foreach (PXResult<InventoryItem, ARTran, SOLine, FSARTran, FSServiceOrder, FSAppointment> bqlResult in inventoryItemSet)
            {
                ARTran arTranRow = (ARTran)bqlResult;
                SOLine soLineRow = (SOLine)bqlResult;
                InventoryItem inventoryItemRow = (InventoryItem)bqlResult;
                FSARTran fsARTranRow = (FSARTran)bqlResult;
                FSServiceOrder fsServiceOrderRow = (FSServiceOrder)bqlResult;
                FSAppointment fsAppointmentRow = (FSAppointment)bqlResult;

                int? smEquipmentID = -1;
                if (newEquiments.TryGetValue(fsARTranRow.NewEquipmentLineNbr, out smEquipmentID))
                {
                    graphSMEquipmentMaint.EquipmentRecords.Current = graphSMEquipmentMaint.EquipmentRecords.Search<FSEquipment.SMequipmentID>(smEquipmentID);

                    FSEquipmentComponent fsEquipmentComponentRow = graphSMEquipmentMaint.EquipmentWarranties.Select().Where(x => ((FSEquipmentComponent)x).ComponentID == fsARTranRow.ComponentID).FirstOrDefault();

                    if (fsEquipmentComponentRow != null)
                    {
                        fsEquipmentComponentRow.SalesOrderNbr = arTranRow.SOOrderNbr;
                        fsEquipmentComponentRow.SalesOrderType = arTranRow.SOOrderType;
                        fsEquipmentComponentRow.LongDescr = arTranRow.TranDesc;
                        fsEquipmentComponentRow.InvoiceRefNbr = arTranRow.RefNbr;
                        fsEquipmentComponentRow.InstallationDate = arTranRow.TranDate != null ? arTranRow.TranDate : arRegisterRow.DocDate;

                        if (fsARTranRow != null)
                        {
                            if (string.IsNullOrEmpty(fsARTranRow.AppointmentRefNbr) == false)
                            {
                                fsEquipmentComponentRow.InstSrvOrdType = fsARTranRow.SrvOrdType;
                                fsEquipmentComponentRow.InstAppointmentRefNbr = fsARTranRow.AppointmentRefNbr;
                                fsEquipmentComponentRow.InstallationDate = fsAppointmentRow?.ExecutionDate;
                            }
                            else if (string.IsNullOrEmpty(fsARTranRow.ServiceOrderRefNbr) == false)
                            {
                                fsEquipmentComponentRow.InstSrvOrdType = fsARTranRow.SrvOrdType;
                                fsEquipmentComponentRow.InstServiceOrderRefNbr = fsARTranRow.ServiceOrderRefNbr;
                                fsEquipmentComponentRow.InstallationDate = fsServiceOrderRow?.OrderDate;
                            }

                            fsEquipmentComponentRow.Comment = fsARTranRow.Comment;
                        }

                        // Component actions are assumed to always run for only one item per line (BaseQty == 1).
                        fsEquipmentComponentRow.SerialNumber = arTranRow.LotSerialNbr;

                        fsEquipmentComponentRow = graphSMEquipmentMaint.EquipmentWarranties.Update(fsEquipmentComponentRow);

                        graphSMEquipmentMaint.EquipmentWarranties.SetValueExt<FSEquipmentComponent.inventoryID>(fsEquipmentComponentRow, arTranRow.InventoryID);
                        graphSMEquipmentMaint.EquipmentWarranties.SetValueExt<FSEquipmentComponent.salesDate>(fsEquipmentComponentRow, soLineRow != null && soLineRow.OrderDate != null ? soLineRow.OrderDate : arTranRow.TranDate);
                        graphSMEquipmentMaint.Save.Press();
                    }
                }
            }
        }

        public virtual void CreateEquipmentComponents(SMEquipmentMaint graphSMEquipmentMaint,
                                                      ARRegister arRegisterRow,
                                                      Dictionary<int?, int?> newEquiments)
        {
            var inventoryItemSet = PXSelectJoin<InventoryItem,
                                   InnerJoin<ARTran,
                                            On<ARTran.inventoryID, Equal<InventoryItem.inventoryID>,
                                            And<ARTran.tranType, Equal<ARDocType.invoice>>>,
                                   LeftJoin<SOLine,
                                        On<SOLine.orderType, Equal<ARTran.sOOrderType>,
                                        And<SOLine.orderNbr, Equal<ARTran.sOOrderNbr>,
                                            And<SOLine.lineNbr, Equal<ARTran.sOOrderLineNbr>>>>,
                                   LeftJoin<FSARTran,
                                            On<FSARTran.FK.ARTranLine>,
                                   LeftJoin<FSServiceOrder,
                                            On<FSServiceOrder.srvOrdType, Equal<FSARTran.srvOrdType>,
                                            And<FSServiceOrder.refNbr, Equal<FSARTran.serviceOrderRefNbr>>>,
                                    LeftJoin<FSAppointment,
                                            On<FSAppointment.srvOrdType, Equal<FSARTran.srvOrdType>,
                                            And<FSAppointment.refNbr, Equal<FSARTran.appointmentRefNbr>>>>>>>>,
                                   Where<
                                        ARTran.tranType, Equal<Required<ARInvoice.docType>>,
                                        And<ARTran.refNbr, Equal<Required<ARInvoice.refNbr>>,
                                        And<FSARTran.equipmentAction, Equal<ListField_EquipmentAction.CreatingComponent>,
                                        And<FSARTran.componentID, IsNotNull,
                                        And<FSARTran.equipmentComponentLineNbr, IsNull>>>>>,
                                   OrderBy<
                                        Asc<ARTran.tranType,
                                        Asc<ARTran.refNbr,
                                        Asc<ARTran.lineNbr>>>>>
                                   .Select(Base, arRegisterRow.DocType, arRegisterRow.RefNbr);

            foreach (PXResult<InventoryItem, ARTran, SOLine, FSARTran, FSServiceOrder, FSAppointment> bqlResult in inventoryItemSet)
            {
                ARTran arTranRow = (ARTran)bqlResult;
                InventoryItem inventoryItemRow = (InventoryItem)bqlResult;
                SOLine soLineRow = (SOLine)bqlResult;
                FSARTran fsARTranRow = (FSARTran)bqlResult;
                FSServiceOrder fsServiceOrderRow = (FSServiceOrder)bqlResult;
                FSAppointment fsAppointmentRow = (FSAppointment)bqlResult;

                int? smEquipmentID = -1;
                if (fsARTranRow.NewEquipmentLineNbr != null && fsARTranRow.SMEquipmentID == null)
                {
                    if (newEquiments.TryGetValue(fsARTranRow.NewEquipmentLineNbr, out smEquipmentID))
                    {
                        graphSMEquipmentMaint.EquipmentRecords.Current = graphSMEquipmentMaint.EquipmentRecords.Search<FSEquipment.SMequipmentID>(smEquipmentID);
                    }
                }

                if (fsARTranRow.NewEquipmentLineNbr == null && fsARTranRow.SMEquipmentID != null)
                {
                    graphSMEquipmentMaint.EquipmentRecords.Current = graphSMEquipmentMaint.EquipmentRecords.Search<FSEquipment.SMequipmentID>(fsARTranRow.SMEquipmentID);
                }

                if (graphSMEquipmentMaint.EquipmentRecords.Current != null)
                {
                    FSEquipmentComponent fsEquipmentComponentRow = new FSEquipmentComponent();
                    fsEquipmentComponentRow.ComponentID = fsARTranRow.ComponentID;
                    fsEquipmentComponentRow = graphSMEquipmentMaint.EquipmentWarranties.Insert(fsEquipmentComponentRow);

                    fsEquipmentComponentRow.SalesOrderNbr = arTranRow.SOOrderNbr;
                    fsEquipmentComponentRow.SalesOrderType = arTranRow.SOOrderType;
                    fsEquipmentComponentRow.InvoiceRefNbr = arTranRow.RefNbr;
                    fsEquipmentComponentRow.InstallationDate = arTranRow.TranDate != null ? arTranRow.TranDate : arRegisterRow.DocDate;

                    if (fsARTranRow != null)
                    {
                        if (string.IsNullOrEmpty(fsARTranRow.AppointmentRefNbr) == false)
                        {
                            fsEquipmentComponentRow.InstSrvOrdType = fsARTranRow.SrvOrdType;
                            fsEquipmentComponentRow.InstAppointmentRefNbr = fsARTranRow.AppointmentRefNbr;
                            fsEquipmentComponentRow.InstallationDate = fsAppointmentRow?.ExecutionDate;
                        }
                        else if (string.IsNullOrEmpty(fsARTranRow.ServiceOrderRefNbr) == false)
                        {
                            fsEquipmentComponentRow.InstSrvOrdType = fsARTranRow.SrvOrdType;
                            fsEquipmentComponentRow.InstServiceOrderRefNbr = fsARTranRow.ServiceOrderRefNbr;
                            fsEquipmentComponentRow.InstallationDate = fsServiceOrderRow?.OrderDate;
                        }

                        fsEquipmentComponentRow.Comment = fsARTranRow.Comment;
                    }

                    // Component actions are assumed to always run for only one item per line (BaseQty == 1).
                    fsEquipmentComponentRow.SerialNumber = arTranRow.LotSerialNbr;

                    fsEquipmentComponentRow = graphSMEquipmentMaint.EquipmentWarranties.Update(fsEquipmentComponentRow);

                    graphSMEquipmentMaint.EquipmentWarranties.SetValueExt<FSEquipmentComponent.inventoryID>(fsEquipmentComponentRow, arTranRow.InventoryID);
                    graphSMEquipmentMaint.EquipmentWarranties.SetValueExt<FSEquipmentComponent.salesDate>(fsEquipmentComponentRow, soLineRow != null && soLineRow.OrderDate != null ? soLineRow.OrderDate : arTranRow.TranDate);
                    graphSMEquipmentMaint.Save.Press();
                }
            }
        }

        public virtual void ReplaceEquipments(SMEquipmentMaint graphSMEquipmentMaint, ARRegister arRegisterRow)
        {
            var inventoryItemSet = PXSelectJoin<InventoryItem,
                                   InnerJoin<ARTran,
                                            On<ARTran.inventoryID, Equal<InventoryItem.inventoryID>,
                                            And<ARTran.tranType, Equal<ARDocType.invoice>>>,
                                   LeftJoin<SOLine,
                                        On<SOLine.orderType, Equal<ARTran.sOOrderType>,
                                        And<SOLine.orderNbr, Equal<ARTran.sOOrderNbr>,
                                            And<SOLine.lineNbr, Equal<ARTran.sOOrderLineNbr>>>>,
                                   LeftJoin<FSARTran,
                                            On<FSARTran.FK.ARTranLine>,
                                   LeftJoin<FSServiceOrder,
                                            On<FSServiceOrder.srvOrdType, Equal<FSARTran.srvOrdType>,
                                            And<FSServiceOrder.refNbr, Equal<FSARTran.serviceOrderRefNbr>>>,
                                    LeftJoin<FSAppointment,
                                            On<FSAppointment.srvOrdType, Equal<FSARTran.srvOrdType>,
                                            And<FSAppointment.refNbr, Equal<FSARTran.appointmentRefNbr>>>>>>>>,
                                   Where<
                                        ARTran.tranType, Equal<Required<ARInvoice.docType>>,
                                        And<ARTran.refNbr, Equal<Required<ARInvoice.refNbr>>,
                                        And<FSxEquipmentModel.eQEnabled, Equal<True>,
                                        And<FSARTran.equipmentAction, Equal<ListField_EquipmentAction.ReplacingTargetEquipment>,
                                        And<FSARTran.sMEquipmentID, IsNotNull,
                                        And<FSARTran.newEquipmentLineNbr, IsNull,
                                        And<FSARTran.componentID, IsNull>>>>>>>,
                                   OrderBy<
                                        Asc<ARTran.tranType,
                                        Asc<ARTran.refNbr,
                                        Asc<ARTran.lineNbr>>>>>
                                   .Select(Base, arRegisterRow.DocType, arRegisterRow.RefNbr);

            Create_Replace_Equipments(graphSMEquipmentMaint, inventoryItemSet, arRegisterRow, null, ID.Equipment_Action.REPLACING_TARGET_EQUIPMENT);
        }

        public virtual void ReplaceComponents(SMEquipmentMaint graphSMEquipmentMaint, ARRegister arRegisterRow)
        {
            var inventoryItemSet = PXSelectJoin<InventoryItem,
                                   InnerJoin<ARTran,
                                            On<ARTran.inventoryID, Equal<InventoryItem.inventoryID>,
                                            And<ARTran.tranType, Equal<ARDocType.invoice>>>,
                                   LeftJoin<SOLine,
                                        On<SOLine.orderType, Equal<ARTran.sOOrderType>,
                                        And<SOLine.orderNbr, Equal<ARTran.sOOrderNbr>,
                                            And<SOLine.lineNbr, Equal<ARTran.sOOrderLineNbr>>>>,
                                   LeftJoin<FSARTran,
                                            On<FSARTran.FK.ARTranLine>,
                                   LeftJoin<FSServiceOrder,
                                            On<FSServiceOrder.srvOrdType, Equal<FSARTran.srvOrdType>,
                                            And<FSServiceOrder.refNbr, Equal<FSARTran.serviceOrderRefNbr>>>,
                                    LeftJoin<FSAppointment,
                                            On<FSAppointment.srvOrdType, Equal<FSARTran.srvOrdType>,
                                            And<FSAppointment.refNbr, Equal<FSARTran.appointmentRefNbr>>>>>>>>,
                                   Where<
                                        ARTran.tranType, Equal<Required<ARInvoice.docType>>,
                                        And<ARTran.refNbr, Equal<Required<ARInvoice.refNbr>>,
                                        And<FSxEquipmentModel.eQEnabled, Equal<True>,
                                        And<FSARTran.equipmentAction, Equal<ListField_EquipmentAction.ReplacingComponent>,
                                        And<FSARTran.sMEquipmentID, IsNotNull,
                                        And<FSARTran.newEquipmentLineNbr, IsNull,
                                        And<FSARTran.equipmentComponentLineNbr, IsNotNull>>>>>>>,
                                   OrderBy<
                                        Asc<ARTran.tranType,
                                        Asc<ARTran.refNbr,
                                        Asc<ARTran.lineNbr>>>>>
                                   .Select(Base, arRegisterRow.DocType, arRegisterRow.RefNbr);

            foreach (PXResult<InventoryItem, ARTran, SOLine, FSARTran, FSServiceOrder, FSAppointment> bqlResult in inventoryItemSet)
            {
                ARTran arTranRow = (ARTran)bqlResult;
                InventoryItem inventoryItemRow = (InventoryItem)bqlResult;
                SOLine soLineRow = (SOLine)bqlResult;
                FSARTran fsARTranRow = (FSARTran)bqlResult;
                FSServiceOrder fsServiceOrderRow = (FSServiceOrder)bqlResult;
                FSAppointment fsAppointmentRow = (FSAppointment)bqlResult;

                graphSMEquipmentMaint.EquipmentRecords.Current = graphSMEquipmentMaint.EquipmentRecords.Search<FSEquipment.SMequipmentID>(fsARTranRow.SMEquipmentID);

                FSEquipmentComponent fsEquipmentComponentRow = graphSMEquipmentMaint.EquipmentWarranties.Select().Where(x => ((FSEquipmentComponent)x).LineNbr == fsARTranRow.EquipmentComponentLineNbr).FirstOrDefault();

                FSEquipmentComponent fsNewEquipmentComponentRow = new FSEquipmentComponent();
                fsNewEquipmentComponentRow.ComponentID = fsARTranRow.ComponentID;
                fsNewEquipmentComponentRow = graphSMEquipmentMaint.ApplyComponentReplacement(fsEquipmentComponentRow, fsNewEquipmentComponentRow);

                fsNewEquipmentComponentRow.SalesOrderNbr = arTranRow.SOOrderNbr;
                fsNewEquipmentComponentRow.SalesOrderType = arTranRow.SOOrderType;
                fsNewEquipmentComponentRow.InvoiceRefNbr = arTranRow.RefNbr;
                fsNewEquipmentComponentRow.InstallationDate = arTranRow.TranDate != null ? arTranRow.TranDate : arRegisterRow.DocDate;

                if (fsARTranRow != null)
                {
                    if (string.IsNullOrEmpty(fsARTranRow.AppointmentRefNbr) == false)
                    {
                        fsNewEquipmentComponentRow.InstSrvOrdType = fsARTranRow.SrvOrdType;
                        fsNewEquipmentComponentRow.InstAppointmentRefNbr = fsARTranRow.AppointmentRefNbr;
                        fsNewEquipmentComponentRow.InstallationDate = fsAppointmentRow?.ExecutionDate;
                    }
                    else if (string.IsNullOrEmpty(fsARTranRow.ServiceOrderRefNbr) == false)
                    {
                        fsNewEquipmentComponentRow.InstSrvOrdType = fsARTranRow.SrvOrdType;
                        fsNewEquipmentComponentRow.InstServiceOrderRefNbr = fsARTranRow.ServiceOrderRefNbr;
                        fsNewEquipmentComponentRow.InstallationDate = fsServiceOrderRow?.OrderDate;
                    }

                    fsNewEquipmentComponentRow.Comment = fsARTranRow.Comment;
                }

                fsNewEquipmentComponentRow.LongDescr = arTranRow.TranDesc;

                // Component actions are assumed to always run for only one item per line (BaseQty == 1).
                fsNewEquipmentComponentRow.SerialNumber = arTranRow.LotSerialNbr;

                fsNewEquipmentComponentRow = graphSMEquipmentMaint.EquipmentWarranties.Update(fsNewEquipmentComponentRow);

                graphSMEquipmentMaint.EquipmentWarranties.SetValueExt<FSEquipmentComponent.inventoryID>(fsNewEquipmentComponentRow, arTranRow.InventoryID);
                graphSMEquipmentMaint.EquipmentWarranties.SetValueExt<FSEquipmentComponent.salesDate>(fsNewEquipmentComponentRow, soLineRow != null && soLineRow.OrderDate != null ? soLineRow.OrderDate : arTranRow.TranDate);
                graphSMEquipmentMaint.Save.Press();
            }
        }

        // TODO: Change PXResultset<InventoryItem> to PXResult<InventoryItem, ARTran, SOLine>
        public virtual void Create_Replace_Equipments(
            SMEquipmentMaint graphSMEquipmentMaint,
            PXResultset<InventoryItem> arTranLines,
            ARRegister arRegisterRow,
            Dictionary<int?, int?> newEquiments,
            string action)
        {
            PXCache fsARTranCache = Base.Caches[typeof(FSARTran)];
            bool needPersist = false;

            foreach (PXResult<InventoryItem, ARTran, SOLine, FSARTran, FSServiceOrder, FSAppointment> bqlResult in arTranLines)
            {
                ARTran arTranRow = (ARTran)bqlResult;

                //Fetching the cached data record for ARTran that will be updated later
                arTranRow = PXSelect<ARTran,
                            Where<
                                ARTran.tranType, Equal<Required<ARTran.tranType>>,
                                And<ARTran.refNbr, Equal<Required<ARTran.refNbr>>,
                                And<ARTran.lineNbr, Equal<Required<ARTran.lineNbr>>>>>>
                            .Select(Base, arTranRow.TranType, arTranRow.RefNbr, arTranRow.LineNbr);

                InventoryItem inventoryItemRow = (InventoryItem)bqlResult;
                SOLine soLineRow = (SOLine)bqlResult;
                FSARTran fsARTranRow = (FSARTran)bqlResult;
                FSServiceOrder fsServiceOrderRow = (FSServiceOrder)bqlResult;
                FSAppointment fsAppointmentRow = (FSAppointment)bqlResult;

                FSEquipment fsEquipmentRow = null;
                FSxEquipmentModel fsxEquipmentModelRow = PXCache<InventoryItem>.GetExtension<FSxEquipmentModel>(inventoryItemRow);

                foreach (ItemInfo itemInfo in GetDifferentItemList(Base, arTranRow, true))
                {
                    SoldInventoryItem soldInventoryItemRow = new SoldInventoryItem();

                    soldInventoryItemRow.CustomerID = arRegisterRow.CustomerID;
                    soldInventoryItemRow.CustomerLocationID = arRegisterRow.CustomerLocationID;
                    soldInventoryItemRow.InventoryID = inventoryItemRow.InventoryID;
                    soldInventoryItemRow.InventoryCD = inventoryItemRow.InventoryCD;
                    soldInventoryItemRow.InvoiceRefNbr = arTranRow.RefNbr;
                    soldInventoryItemRow.InvoiceLineNbr = arTranRow.LineNbr;
                    soldInventoryItemRow.DocType = arRegisterRow.DocType;
                    soldInventoryItemRow.DocDate = arTranRow.TranDate != null ? arTranRow.TranDate : arRegisterRow.DocDate;

                    if (fsARTranRow != null)
                    {
                        if (string.IsNullOrEmpty(fsARTranRow.AppointmentRefNbr) == false)
                        {
                            soldInventoryItemRow.DocDate = fsAppointmentRow.ExecutionDate;
                        }
                        else if (string.IsNullOrEmpty(fsARTranRow.ServiceOrderRefNbr) == false)
                        {
                            soldInventoryItemRow.DocDate = fsServiceOrderRow.OrderDate;
                        }
                    }

                    soldInventoryItemRow.Descr = inventoryItemRow.Descr;
                    soldInventoryItemRow.SiteID = arTranRow.SiteID;
                    soldInventoryItemRow.ItemClassID = inventoryItemRow.ItemClassID;
                    soldInventoryItemRow.SOOrderType = arTranRow.SOOrderType;
                    soldInventoryItemRow.SOOrderNbr = arTranRow.SOOrderNbr;
                    soldInventoryItemRow.SOOrderDate = soLineRow.OrderDate;
                    soldInventoryItemRow.EquipmentTypeID = fsxEquipmentModelRow.EquipmentTypeID;

                    soldInventoryItemRow.LotSerialNumber = itemInfo.LotSerialNbr;

                    fsEquipmentRow = SharedFunctions.CreateSoldEquipment(graphSMEquipmentMaint, soldInventoryItemRow, arTranRow, fsARTranRow, soLineRow, action, inventoryItemRow);
                }

                if (fsEquipmentRow != null)
                {
                    if (fsARTranRow.ReplaceSMEquipmentID == null
                        && action == ID.Equipment_Action.REPLACING_TARGET_EQUIPMENT)
                    {
                        fsARTranRow.ReplaceSMEquipmentID = fsARTranRow.SMEquipmentID;
                    }

                    fsARTranRow.SMEquipmentID = fsEquipmentRow.SMEquipmentID;
                    fsARTranCache.Update(fsARTranRow);
                    needPersist = true;

                    if (action == ID.Equipment_Action.SELLING_TARGET_EQUIPMENT)
                    {
                        int? smEquipmentID = -1;
                        if (newEquiments.TryGetValue(arTranRow.LineNbr, out smEquipmentID) == false)
                        {
                            newEquiments.Add(
                                arTranRow.LineNbr,
                                fsEquipmentRow.SMEquipmentID);
                        }
                    }
                    else if (action == ID.Equipment_Action.REPLACING_TARGET_EQUIPMENT)
                    {
                        if (fsARTranRow != null)
                        {
                            graphSMEquipmentMaint.EquipmentRecords.Current = graphSMEquipmentMaint.EquipmentRecords.Search<FSEquipment.SMequipmentID>(fsARTranRow.ReplaceSMEquipmentID);
                            graphSMEquipmentMaint.EquipmentRecords.Current.ReplaceEquipmentID = fsEquipmentRow.SMEquipmentID;
                            graphSMEquipmentMaint.EquipmentRecords.Current.Status = ID.Equipment_Status.DISPOSED;
                            graphSMEquipmentMaint.EquipmentRecords.Current.DisposalDate = soLineRow.OrderDate != null ? soLineRow.OrderDate : arTranRow.TranDate;
                            graphSMEquipmentMaint.EquipmentRecords.Current.DispSrvOrdType = fsARTranRow.SrvOrdType;
                            graphSMEquipmentMaint.EquipmentRecords.Current.DispServiceOrderRefNbr = fsARTranRow.ServiceOrderRefNbr;
                            graphSMEquipmentMaint.EquipmentRecords.Current.DispAppointmentRefNbr = fsARTranRow.AppointmentRefNbr;
                            graphSMEquipmentMaint.EquipmentRecords.Cache.SetStatus(graphSMEquipmentMaint.EquipmentRecords.Current, PXEntryStatus.Updated);
                            graphSMEquipmentMaint.Save.Press();
                        }
                    }
                }
            }

            if (needPersist==true) 
            {
                fsARTranCache.Persist(PXDBOperation.Update);
            }
        }

        public virtual List<ItemInfo> GetDifferentItemList(PXGraph graph, ARTran arTran, bool createDifferentEntriesForQtyGreaterThan1)
        {
            if (arTran.InventoryID == null)
            {
                return null;
            }

            var lotSerialList = new List<ItemInfo>();
            PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(graph.Caches[typeof(InventoryItem)], arTran.InventoryID);

            if (item == null || ((INLotSerClass)item).LotSerTrack == INLotSerTrack.NotNumbered)
            {
                var itemInfo = new ItemInfo(arTran);

                // Currently equipment don't have UOM specification,
                // so we use BaseQty to create equipment of the base unit.
                itemInfo.UOM = null;
                itemInfo.Qty = null;

                if (createDifferentEntriesForQtyGreaterThan1 == false)
                {
                    lotSerialList.Add(itemInfo);
                }
                else
                {
                    itemInfo.BaseQty = 1;
                    for (int i = 0; i < arTran.BaseQty; i++)
                    {
                        lotSerialList.Add(itemInfo);
                    }
                }
            }
            else if (arTran.SOShipmentType != null && arTran.SOShipmentNbr != null && arTran.SOShipmentLineNbr != null
                && SOShipment.UK.Find(graph, arTran.SOShipmentType, arTran.SOShipmentNbr) != null) // this is because when there is no Shipment (OrderType == IN), SOShipmentNbr has '<NEW>'
            {
                PXResultset<SOShipLineSplit> lotSerialSplits = PXSelect<SOShipLineSplit,
                    Where<SOShipLineSplit.shipmentNbr, Equal<Required<SOShipLineSplit.shipmentNbr>>,
                        And<SOShipLineSplit.lineNbr, Equal<Required<SOShipLineSplit.lineNbr>>>>>.
                    Select(graph, arTran.SOShipmentNbr, arTran.SOShipmentLineNbr);

                foreach (SOShipLineSplit shipLineSplit in lotSerialSplits)
                {
                    var split = new ItemInfo(shipLineSplit);

                    // Currently equipment don't have UOM specification,
                    // so we use BaseQty to create equipment of the base unit.
                    split.UOM = null;
                    split.Qty = null;

                    if (createDifferentEntriesForQtyGreaterThan1 == false)
                    {
                        lotSerialList.Add(split);
                    }
                    else
                    {
                        split.BaseQty = 1;
                        for (int i = 0; i < shipLineSplit.BaseQty; i++)
                        {
                            lotSerialList.Add(split);
                        }
                    }
                }
            }
            else if (arTran.SOOrderType != null && arTran.SOOrderNbr != null && arTran.SOOrderLineNbr != null)
            {
                PXResultset<SOLineSplit> lotSerialSplits = PXSelect<SOLineSplit,
                    Where<SOLineSplit.orderType, Equal<Required<SOLineSplit.orderType>>,
                        And<SOLineSplit.orderNbr, Equal<Required<SOLineSplit.orderNbr>>,
                        And<SOLineSplit.lineNbr, Equal<Required<SOLineSplit.lineNbr>>>>>>.
                    Select(graph, arTran.SOOrderType, arTran.SOOrderNbr, arTran.SOOrderLineNbr);

                foreach (SOLineSplit soLineSplit in lotSerialSplits.RowCast<SOLineSplit>().Where(e => string.IsNullOrEmpty(e.LotSerialNbr) == false))
                {
                    var split = new ItemInfo(soLineSplit);

                    // Currently equipment don't have UOM specification,
                    // so we use BaseQty to create equipment of the base unit.
                    split.UOM = null;
                    split.Qty = null;

                    if (createDifferentEntriesForQtyGreaterThan1 == false)
                    {
                        lotSerialList.Add(split);
                    }
                    else
                    {
                        split.BaseQty = 1;
                        for (int i = 0; i < soLineSplit.BaseQty; i++)
                        {
                            lotSerialList.Add(split);
                        }
                    }
                }
            }
            else if (item != null && ((INLotSerClass)item).LotSerTrack != INLotSerTrack.NotNumbered)
            {
                var itemInfo = new ItemInfo(arTran);

                // Currently equipment don't have UOM specification,
                // so we use BaseQty to create equipment of the base unit.
                itemInfo.UOM = null;
                itemInfo.Qty = null;

                if (createDifferentEntriesForQtyGreaterThan1 == false)
                {
                    lotSerialList.Add(itemInfo);
                }
                else
                {
                    itemInfo.BaseQty = 1;
                    for (int i = 0; i < arTran.BaseQty; i++)
                    {
                        lotSerialList.Add(itemInfo);
                    }
                }
            }

            return GetVerifiedDifferentItemList(graph, arTran, lotSerialList);
        }

        public virtual List<ItemInfo> GetVerifiedDifferentItemList(PXGraph graph, ARTran arTran, List<ItemInfo> lotSerialList)
        {
            if (lotSerialList == null)
            {
                lotSerialList = new List<ItemInfo>();
            }

            if (lotSerialList.Count > arTran.BaseQty)
            {
                throw new PXException(TX.Error.ThereAreMoreLotSerialNumbersThanQuantitySpecifiedOnTheLine);
            }

            return lotSerialList;
        }

        protected virtual PXResult<InventoryItem, INLotSerClass> ReadInventoryItem(PXCache sender, int? inventoryID)
        {
            if (inventoryID == null)
                return null;
            var inventory = InventoryItem.PK.Find(sender.Graph, inventoryID);
            if (inventory == null)
                throw new PXException(ErrorMessages.ValueDoesntExistOrNoRights, IN.Messages.InventoryItem, inventoryID);
            INLotSerClass lotSerClass;
            if (inventory.StkItem == true)
            {
                lotSerClass = INLotSerClass.PK.Find(sender.Graph, inventory.LotSerClassID);
                if (lotSerClass == null)
                    throw new PXException(ErrorMessages.ValueDoesntExistOrNoRights, IN.Messages.LotSerClass, inventory.LotSerClassID);
            }
            else
            {
                lotSerClass = new INLotSerClass();
            }
            return new PXResult<InventoryItem, INLotSerClass>(inventory, lotSerClass);
        }
        #endregion

        #region Validations
        public virtual void ValidatePostBatchStatus(PXDBOperation dbOperation, string postTo, string createdDocType, string createdRefNbr)
        {
            DocGenerationHelper.ValidatePostBatchStatus<ARRegister>(Base, dbOperation, postTo, createdDocType, createdRefNbr);
        }
        #endregion
    }
}
