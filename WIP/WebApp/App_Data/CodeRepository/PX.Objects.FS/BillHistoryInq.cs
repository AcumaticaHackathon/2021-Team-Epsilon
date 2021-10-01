using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.SO;
using PX.SM;
using System.Collections;

namespace PX.Objects.FS
{
    public class BillHistoryInq : PXGraph<BillHistoryInq>
    {
        public BillHistoryInq()
            : base()
        {
            menuActions.AddMenuAction(runAppointmentBilling);
            menuActions.AddMenuAction(runServiceOrderBilling);
            menuActions.AddMenuAction(runServiceContractBilling);
        }

        #region Select
        [PXHidden]
        public PXSetup<FSSetup> SetupRecord;

        [PXFilterable]
        public PXSelectJoin<FSBillHistory,
                    LeftJoin<SOOrder,
                            On<SOOrder.orderType, Equal<FSBillHistory.childDocType>,
                                And<SOOrder.orderNbr, Equal<FSBillHistory.childRefNbr>>>,
                    LeftJoin<ARInvoice,
                            On<ARInvoice.docType, Equal<FSBillHistory.childDocType>,
                                And<ARInvoice.refNbr, Equal<FSBillHistory.childRefNbr>>>,
                    LeftJoin<SOInvoice,
                            On<SOInvoice.docType, Equal<FSBillHistory.childDocType>,
                                And<SOInvoice.refNbr, Equal<FSBillHistory.childRefNbr>>>,
                    LeftJoin<APInvoice,
                            On<APInvoice.docType, Equal<FSBillHistory.childDocType>,
                                And<APInvoice.refNbr, Equal<FSBillHistory.childRefNbr>>>,
                    LeftJoin<PMRegister,
                            On<PMRegister.module, Equal<FSBillHistory.childDocType>,
                                And<PMRegister.refNbr, Equal<FSBillHistory.childRefNbr>>>,
                    LeftJoin<INRegister,
                            On<INRegister.docType, Equal<FSBillHistory.childDocType>,
                                And<INRegister.refNbr, Equal<FSBillHistory.childRefNbr>>>,
                    LeftJoin<FSContractPostDoc,
                            On<FSContractPostDoc.postDocType, Equal<FSBillHistory.childDocType>,
                                And<FSContractPostDoc.postRefNbr, Equal<FSBillHistory.childRefNbr>>>>>>>>>>> BillHistoryRecords;
        #endregion

        #region CacheAttached
        #region FSBillHistory_ChildDocDate
        [PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), "DisplayName", "Doc. Date")]
        protected virtual void FSBillHistory_ChildDocDate_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region FSBillHistory_ChildDocStatus
        [PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), "DisplayName", "Doc. Status")]
        protected virtual void FSBillHistory_ChildDocStatus_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #endregion

        #region Actions
        #region MenuActions
        public PXMenuAction<FSBillHistory> menuActions;
        [PXButton(MenuAutoOpen = true, SpecialType = PXSpecialButtonType.ActionsFolder)]
        [PXUIField(DisplayName = "Actions")]
        public virtual IEnumerable MenuActions(PXAdapter adapter)
        {
            return adapter.Get();
        }
        #endregion
        #region RunAppointmentBilling
        public PXAction<FSBillHistory> runAppointmentBilling;
        [PXUIField(DisplayName = "Run Appointment Billing", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable RunAppointmentBilling(PXAdapter adapter)
        {
            if (!adapter.MassProcess)
            {
                throw new PXRedirectRequiredException(PXGraph.CreateInstance<CreateInvoiceByAppointmentPost>(), null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }

            return adapter.Get();
        }
        #endregion
        #region RunServiceOrderBilling
        public PXAction<FSBillHistory> runServiceOrderBilling;
        [PXUIField(DisplayName = "Run Service Order Billing", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable RunServiceOrderBilling(PXAdapter adapter)
        {
            if (!adapter.MassProcess)
            {
                throw new PXRedirectRequiredException(PXGraph.CreateInstance<CreateInvoiceByServiceOrderPost>(), null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }

            return adapter.Get();
        }
        #endregion
        #region RunServiceContractBilling
        public PXAction<FSBillHistory> runServiceContractBilling;
        [PXUIField(DisplayName = "Run Service Contract Billing", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable RunServiceContractBilling(PXAdapter adapter)
        {
            if (!adapter.MassProcess)
            {
                throw new PXRedirectRequiredException(PXGraph.CreateInstance<CreateInvoiceByContractPost>(), null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }

            return adapter.Get();
        }
        #endregion
        #region OpenPostBatch
        public ViewPostBatch<FSBillHistory> openPostBatch;
        #endregion
        #endregion

        #region Delegate
        public virtual IEnumerable billHistoryRecords()
        {
            PXView view = new PXView(this, true, BillHistoryRecords.View.BqlSelect);

            int startRow = PXView.StartRow;
            int totalRows = 0;

            var list = view.Select(PXView.Currents,
                                    PXView.Parameters,
                                    PXView.Searches,
                                    PXView.SortColumns,
                                    PXView.Descendings,
                                    PXView.Filters,
                                    ref startRow,
                                    PXView.MaximumRows,
                                    ref totalRows);

            PXView.StartRow = 0;
            PXCache cache = view.Cache;

            foreach (PXResult<FSBillHistory, SOOrder, ARInvoice, SOInvoice, APInvoice, PMRegister, INRegister, FSContractPostDoc> row in list) 
            {
                FSBillHistory fsBillHistoryRow = (FSBillHistory)row;
                SOOrder sOOrderRow = (SOOrder)row;
                INRegister inRegisterRow = (INRegister)row;
                ARInvoice arInvoiceRow = (ARInvoice)row;
                SOInvoice soInvoiceRow = (SOInvoice)row;
                APInvoice apInvoiceRow = (APInvoice)row;
                PMRegister pmRegisterRow = (PMRegister)row;
                FSContractPostDoc fsContractPostDocRow = (FSContractPostDoc)row;

                if (fsBillHistoryRow.ChildEntityType == FSEntityType.SalesOrder)
                {
                    if (sOOrderRow != null 
                        && string.IsNullOrEmpty(sOOrderRow.RefNbr) == false)
                    {
                        fsBillHistoryRow.ChildDocDate = sOOrderRow.OrderDate;
                        fsBillHistoryRow.ChildDocDesc = sOOrderRow.OrderDesc;
                        fsBillHistoryRow.ChildAmount = sOOrderRow.CuryOrderTotal;
                        fsBillHistoryRow.ChildDocStatus = PXStringListAttribute.GetLocalizedLabel<SOOrder.status>(new PXCache<SOOrder>(cache.Graph), sOOrderRow, sOOrderRow.Status);
                    }
                }
                else if (fsBillHistoryRow.ChildEntityType == FSEntityType.ARInvoice)
                {
                    if (arInvoiceRow != null
                        && string.IsNullOrEmpty(arInvoiceRow.RefNbr) == false)
                    {
                        fsBillHistoryRow.ChildDocDate = arInvoiceRow.DocDate;
                        fsBillHistoryRow.ChildDocDesc = arInvoiceRow.DocDesc;
                        fsBillHistoryRow.ChildAmount = arInvoiceRow.CuryOrigDocAmt;
                        fsBillHistoryRow.ChildDocStatus = PXStringListAttribute.GetLocalizedLabel<ARInvoice.status>(new PXCache<ARInvoice>(cache.Graph), arInvoiceRow, arInvoiceRow.Status);
                    }
                }
                else if (fsBillHistoryRow.ChildEntityType == FSEntityType.SOInvoice)
                {
                    if (soInvoiceRow != null
                        && string.IsNullOrEmpty(soInvoiceRow.RefNbr) == false)
                    {
                        fsBillHistoryRow.ChildDocDate = soInvoiceRow.DocDate;
                        fsBillHistoryRow.ChildDocDesc = soInvoiceRow.DocDesc;
                        fsBillHistoryRow.ChildDocStatus = PXStringListAttribute.GetLocalizedLabel<SOInvoice.status>(new PXCache<SOInvoice>(cache.Graph), soInvoiceRow, soInvoiceRow.Status);
                    }
                }
                else if (fsBillHistoryRow.ChildEntityType == FSEntityType.APInvoice)
                {
                    if (apInvoiceRow != null
                        && string.IsNullOrEmpty(apInvoiceRow.RefNbr) == false)
                    {
                        fsBillHistoryRow.ChildDocDate = apInvoiceRow.DocDate;
                        fsBillHistoryRow.ChildDocDesc = apInvoiceRow.DocDesc;
                        fsBillHistoryRow.ChildDocStatus = PXStringListAttribute.GetLocalizedLabel<APInvoice.status>(new PXCache<APInvoice>(cache.Graph), apInvoiceRow, apInvoiceRow.Status);
                    }
                }
                else if (fsBillHistoryRow.ChildEntityType == FSEntityType.PMRegister)
                {
                    if (pmRegisterRow != null
                        && string.IsNullOrEmpty(pmRegisterRow.RefNbr) == false)
                    {
                        fsBillHistoryRow.ChildDocDesc = pmRegisterRow.Description;
                        fsBillHistoryRow.ChildDocStatus = PXStringListAttribute.GetLocalizedLabel<PMRegister.status>(new PXCache<PMRegister>(cache.Graph), pmRegisterRow, pmRegisterRow.Status);
                    }
                }
                else if (fsBillHistoryRow.ChildEntityType == FSEntityType.INIssue || fsBillHistoryRow.ChildEntityType == FSEntityType.INReceipt)
                {
                    if (inRegisterRow != null
                        && string.IsNullOrEmpty(inRegisterRow.RefNbr) == false)
                    {
                        fsBillHistoryRow.ChildDocDate = inRegisterRow.TranDate;
                        fsBillHistoryRow.ChildDocDesc = inRegisterRow.TranDesc;
                        fsBillHistoryRow.ChildDocStatus = PXStringListAttribute.GetLocalizedLabel<INRegister.status>(new PXCache<INRegister>(cache.Graph), inRegisterRow, inRegisterRow.Status);
                    }
                }

                fsBillHistoryRow.ServiceContractPeriodID = fsContractPostDocRow.ContractPeriodID;

                if (string.IsNullOrEmpty(fsBillHistoryRow.ChildDocStatus))
                {
                    fsBillHistoryRow.ChildDocStatus = EP.Messages.Deleted;
                }
            }

            return list;
        }
        #endregion
    }
}