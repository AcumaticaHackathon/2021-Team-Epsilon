using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AM.Attributes;
using PX.Objects.AM.CacheExtensions;
using PX.Objects.AM.GraphExtensions;
using PX.Objects.AR;
using PX.Objects.CN.Common.Descriptor;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.SO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.AM
{
    public class CTPProcess : PXGraph<CTPProcess>
    {
        /// <summary>
        /// Processing page filter
        /// </summary>
        public PXFilter<CTPFilter> Filter;
        public PXCancel<CTPFilter> Cancel;
        [PXHidden]
        public PXFilter<QtyAvailFilter> QtyAvailableFilter;

        /// <summary>
        /// Processing records
        /// </summary>
        [PXFilterable]
        public PXFilteredProcessingJoin<CTPLine, CTPFilter,
            InnerJoin<SOOrder, On<SOOrder.orderType, Equal<CTPLine.orderType>,
                And<SOOrder.orderNbr, Equal<CTPLine.orderNbr>>>>,
            Where<Brackets<CTPLine.orderType.IsEqual<CTPFilter.sOOrderType.FromCurrent>
                    .Or<CTPFilter.sOOrderType.FromCurrent.IsNull>>
                .And<CTPLine.orderNbr.IsEqual<CTPFilter.sOOrderNbr.FromCurrent>
                    .Or<CTPFilter.sOOrderNbr.FromCurrent.IsNull>>
                .And<SOOrder.status.IsEqual<SOOrderStatus.open>
                    .Or<SOOrder.status.IsEqual<SOOrderStatus.hold>
                    .Or<SOOrder.status.IsEqual<SOOrderStatus.shipping>>>>
                .And<CTPLine.baseOpenQty.IsGreater<CTPLine.baseShippedQty>>>> ProcessingRecords;

        

        public PXSetup<AMPSetup> ampsetup;

        public PXAction<CTPLine> QtyAvailable;

        [PXUIField(DisplayName = Messages.QuantityAvailable, MapEnableRights = PXCacheRights.Update,
            MapViewRights = PXCacheRights.Update)]
        [PXButton]
        public virtual IEnumerable qtyAvailable(PXAdapter adapter)
        {
            if (Filter.Current == null)
            {
                return adapter.Get();
            }
            CalculateQuantityAvailable();

            QtyAvailableFilter.AskExt();

            return adapter.Get();
        }

        protected virtual void CalculateQuantityAvailable()
        {
            //Requested
            var soline = ProcessingRecords.Current;
            if (soline != null)
            {
                QtyAvailableFilter.Current.RequestQty = soline.OpenQty;
            }

            //Available and Available for Shipment
            decimal? resultQty = 0;
            var siteStatus = InventoryHelper.GetLocationStatusSum(this, soline.InventoryID, soline.SubItemID, soline.SiteID, null, null, true, true);
            if (siteStatus != null)
            {
                UomHelper.TryConvertFromBaseQty<CTPLine.inventoryID>(this.Caches<CTPLine>(),
                    soline, soline.UOM, siteStatus.QtyAvail.GetValueOrDefault(), out resultQty);
                QtyAvailableFilter.Current.QtyAvail = resultQty;

                UomHelper.TryConvertFromBaseQty<CTPLine.inventoryID>(this.Caches<CTPLine>(),
                    soline, soline.UOM, siteStatus.QtyHardAvail.GetValueOrDefault(), out resultQty);
                QtyAvailableFilter.Current.QtyHardAvail = resultQty;
            }

            //Supply and Production Available
            var itemplans = SelectFrom<INItemPlan>.Where<INItemPlan.inventoryID.IsEqual<@P.AsInt>.
                And<INItemPlan.siteID.IsEqual<@P.AsInt>>.
                And<INItemPlan.planDate.IsLessEqual<@P.AsDateTime>>>.View.Select(this, soline.InventoryID, soline.SiteID, soline.RequestDate);

            decimal? baseSupply = 0;
            decimal? baseProd = 0;
            foreach (INItemPlan itemplan in itemplans)
            {
                if (INPlanTypeHelper.IsCTPINSupply(itemplan.PlanType))
                    baseSupply += itemplan.PlanQty;
                if (INPlanTypeHelper.IsCTPProdSupply(itemplan.PlanType))
                    baseProd += itemplan.PlanQty;
            }               
            
            UomHelper.TryConvertFromBaseQty<CTPLine.inventoryID>(this.Caches<CTPLine>(),
                soline, soline.UOM, baseSupply.GetValueOrDefault(), out resultQty);
            QtyAvailableFilter.Current.SupplyAvail = resultQty;
            UomHelper.TryConvertFromBaseQty<CTPLine.inventoryID>(this.Caches<CTPLine>(),
                soline, soline.UOM, baseProd.GetValueOrDefault(), out resultQty);
            QtyAvailableFilter.Current.ProdAvail = resultQty;

            //add in MPS as ProdAvail
            List<AMMPS> mpsRecs = SelectFrom<AMMPS>.Where<AMMPS.inventoryID.IsEqual<@P.AsInt>.
                And<AMMPS.siteID.IsEqual<@P.AsInt>>.
                And<AMMPS.planDate.IsLessEqual<@P.AsDateTime>>>.View.Select(this, soline.InventoryID, soline.SiteID, soline.RequestDate).FirstTableItems.ToList();

            UomHelper.TryConvertFromBaseQty<CTPLine.inventoryID>(this.Caches<CTPLine>(),
                soline, soline.UOM, mpsRecs.Sum(x => x.BaseQty).GetValueOrDefault(), out resultQty);

            QtyAvailableFilter.Current.ProdAvail += resultQty;

            //Total
            QtyAvailableFilter.Current.TotalAvail = QtyAvailableFilter.Current.QtyHardAvail + 
                QtyAvailableFilter.Current.SupplyAvail + QtyAvailableFilter.Current.ProdAvail;
        }

        public string CTPORDERTYPE => ampsetup.Current.CTPOrderType;
        public bool IsManualOrder = false;

        public CTPProcess()
        {
            var currentFilter = Filter?.Current;
            PXUIFieldAttribute.SetEnabled<CTPFilter.sOOrderType>(Filter.Cache, null, currentFilter.Redirect != true);
            PXUIFieldAttribute.SetEnabled<CTPFilter.sOOrderNbr>(Filter.Cache, null, currentFilter.Redirect != true);
            //Cancel.SetEnabled(currentFilter.Redirect != true);

            PXUIFieldAttribute.SetVisible<CTPLine.orderType>(ProcessingRecords.Cache, null, true);
            PXUIFieldAttribute.SetVisible<CTPLine.orderNbr>(ProcessingRecords.Cache, null, true);

            ProcessingRecords.SetProcessDelegate(
                delegate (List<CTPLine> list) { ProcessLines(list, currentFilter); }
            );
        }

        public static void ProcessLines(List<CTPLine> list, CTPFilter filter)
        {
            if (filter?.ProcessAction == null || list == null || list.Count == 0)
            {
                return;
            }

            var graph = CreateInstance<CTPProcess>();

            switch (filter.ProcessAction)
            {
                case CTPProcessActionOptions.RunCTP:
                    if (graph.CTPORDERTYPE == null)
                    {
                        PXProcessing.SetError(new PXException(Messages.GetLocal(Messages.NoDefaultCTPOrderType)));
                    }
                    graph.RunCTPProcess(list);
                    break;
                case CTPProcessActionOptions.Accept:
                    if (filter.DefaultOrderType == null)
                    {
                        throw new PXSetPropertyException(Messages.GetLocal(PXUIFieldAttribute.GetDisplayName<CTPFilter.defaultOrderType>(graph.Filter.Cache),
                            Messages.GetLocal(Messages.DefaultOrderTypeCannotBeNull)));
                    }
                    graph.AcceptCTPProcess(list, filter);
                    break;
                case CTPProcessActionOptions.Reject:
                    graph.RejectCTPProcess(list);
                    break;
            }
        }

        protected virtual void RejectCTPProcess(List<CTPLine> list)
        {
            var errorsFound = false;

            var orderEntry = GetOrderEntry(list);
            var prodGraph = PXGraph.CreateInstance<ProdMaint>();
            prodGraph.IsImport = true;

            for (var i = 0; i < list.Count; i++)
            {
                var line = list[i];

                try
                {
                    RejectCTPProcess(orderEntry, prodGraph, line);
                    PXProcessing<CTPLine>.SetInfo(i, ActionsMessages.RecordProcessed);
                }
                catch (Exception exception)
                {
                    PXTrace.WriteError(exception);
                    PXProcessing<CTPLine>.SetError(i, exception.Message);
                    errorsFound = true;
                }
            }

            if (errorsFound)
            {
                throw new PXOperationCompletedWithErrorException(PX.Data.ErrorMessages.SeveralItemsFailed);
            }

            orderEntry.Persist();
            prodGraph.Persist();
        }

        protected virtual void RejectCTPProcess(SOOrderEntry orderEntry, ProdMaint prodGraph, CTPLine ctpLine)
        {
            if (ctpLine.ProdOrdID == null)
            {
                throw new PXException(Messages.CTPProcessHasNotBeenRun);
            }

            var soLine = orderEntry.Transactions.Locate(new SOLine
            {
                OrderType = ctpLine.OrderType,
                OrderNbr = ctpLine.OrderNbr,
                LineNbr = ctpLine.LineNbr
                            }) ?? (SOLine)PXSelect<SOLine,
                    Where<SOLine.orderType, Equal<Required<SOLine.orderType>>,
                        And<SOLine.orderNbr, Equal<Required<SOLine.orderNbr>>,
                            And<SOLine.lineNbr, Equal<Required<SOLine.lineNbr>>>>>
                >.SelectWindowed(this, 0, 1, ctpLine?.OrderType, ctpLine?.OrderNbr, ctpLine?.LineNbr);

            if (soLine == null)
            {
                return;
            }

            var soLineExt = PXCache<SOLine>.GetExtension<SOLineExt>(soLine);
            if (soLineExt == null)
            {
                return;
            }
            if (soLineExt.AMCTPAccepted == true)
            {
                throw new PXException(Messages.CTPHasAlreadyBeenAccepted);
            }

            //Some strange reason the order type is getting set just querying the record
            if (soLineExt.AMProdOrdID == null)
            {
                soLineExt.AMOrderType = null;
            }

            var prodItem = SelectFrom<AMProdItem>.Where<AMProdItem.orderType.IsEqual<@P.AsString>.
                And<AMProdItem.prodOrdID.IsEqual<@P.AsString>>>.View.Select(this, ctpLine.AMCTPOrderType, ctpLine.ProdOrdID);
            if (prodItem != null)
            {
                prodGraph.ProdMaintRecords.Current = prodItem;
                prodGraph.ProdMaintRecords.Delete(prodItem);
            }

            orderEntry.Transactions.Update(soLine);
        }

        protected virtual void RunCTPProcess(List<CTPLine> list)
        {
            var errorsFound = false;
            List<AMSchdItem> failedOrders = new List<AMSchdItem>();

            //ProcessSchedule
            var schdEngine = CreateInstance<ProductionScheduleEngineAdv>();
            schdEngine.IsCTP = true;
            AMSchdItem schd = null;

            for (var i = 0; i < list.Count; i++)
            {
                try
                {
                    var line = list[i];

                    schd = TryRunCTPLine(line, out var lineException);
                    if (lineException == null && schd != null)
                    {
                        schdEngine.Process(schd);
                        schdEngine.Persist();
                        PXProcessing<CTPLine>.SetInfo(i, ActionsMessages.RecordProcessed);
                    }
                    else
                    {
                        
                        throw lineException != null ? lineException : new Exception($"{line.OrderType}:{line.OrderNbr}:{line.LineNbr}");
                    }
                }
                catch (Exception ex)
                {
                    errorsFound = true;
                    PXProcessing<CTPLine>.SetError(i, ex);
                    failedOrders.Add(schd);
                }
            }

            if (errorsFound)
            {
                DeleteFailedOrders(failedOrders);
                throw new PXOperationCompletedWithErrorException(PX.Data.ErrorMessages.SeveralItemsFailed);
            }
        }

        protected virtual void DeleteFailedOrders(List<AMSchdItem> failedOrders)
        {
            var prodMaintGraph = CreateInstance<ProdMaint>();
            prodMaintGraph.IsImport = true;
            foreach(var schd in failedOrders)
            {
                var prodItem = AMProdItem.PK.Find(prodMaintGraph, schd.OrderType, schd.ProdOrdID);
                prodMaintGraph.ProdMaintRecords.Current = prodItem;
                prodMaintGraph.ProdMaintRecords.Delete(prodItem);
            }
            prodMaintGraph.Persist();
        }

        protected virtual AMSchdItem TryRunCTPLine(CTPLine line, out Exception exception)
        {
            exception = null;            

            var lineExt = PXCache<SOLine>.GetExtension<SOLineExt>(line); //PXCache<CTPLine>.GetExtension<CTPLineExtension>(line);

            if (lineExt == null)
            {
                exception = new PXException(Messages.UnableToGetCTPLineExt);
                return null;
            }

            if(lineExt.AMCTPAccepted == true)
            {
                exception = new PXException(Messages.CTPHasAlreadyBeenAccepted);
                return null;
            }

            var lineHasExistingCTPOrder = lineExt.AMSchdNoteID != null;

            AMSchdItem schdItem = null;
            if (lineHasExistingCTPOrder)
            {
                schdItem =
                    (AMSchdItem)PXSelect<
                            AMSchdItem,
                        Where<AMSchdItem.noteID, Equal<Required<AMSchdItem.noteID>>>>
                        .Select(this,
                        lineExt.AMSchdNoteID);

                if (!string.IsNullOrWhiteSpace(schdItem?.ProdOrdID))
                {
                    RejectCTPProcess(new List<CTPLine>() { line });
                }
                lineHasExistingCTPOrder = false;
            }

            // We will use the shipdate and later add back the CLeadTime to account for the correct request date
            var planDate = line.ShipDate.GetValueOrDefault();

            // Create production order (if not already exists)
            if (!lineHasExistingCTPOrder)
            {
                //Create new order
                schdItem = CreateCTPOrder(line, CTPORDERTYPE, planDate);
            }

            if (schdItem == null)
            {
                exception = new PXException(Messages.UnableToGetCTPSchdOrder);
                return null;
            }

            schdItem.ConstDate = planDate;
            schdItem.SchedulingMethod = ScheduleMethod.FinishOn;

            return schdItem;
        }

        protected virtual void AcceptCTPProcess(List<CTPLine> list, CTPFilter filter)
        {

            var errorsFound = false;
            var prodGraph = PXGraph.CreateInstance<ProdMaint>();
            prodGraph.IsImport = true;

            for (var i = 0; i < list.Count; i++)
            {
                var line = list[i];

                try
                {
                    AcceptCTPProcess(prodGraph, line, filter);
                    PXProcessing<CTPLine>.SetInfo(i, ActionsMessages.RecordProcessed);
                }
                catch (Exception exception)
                {
                    PXTrace.WriteError(exception);
                    PXProcessing<CTPLine>.SetError(i, exception.Message);
                    errorsFound = true;
                }
            }

            if (errorsFound)
            {
                throw new PXOperationCompletedWithErrorException(PX.Data.ErrorMessages.SeveralItemsFailed);
            }
        }

        private AMSchdItem CreateCTPOrder(CTPLine ctpLine, string ctpOrderType, DateTime effEndDate)
        {
            SOLineExt soLineAMExtension = null;

            var soLine = (SOLine)PXSelect<SOLine,
                Where<SOLine.orderType, Equal<Required<SOLine.orderType>>,
                    And<SOLine.orderNbr, Equal<Required<SOLine.orderNbr>>,
                        And<SOLine.lineNbr, Equal<Required<SOLine.lineNbr>>>>>
            >.SelectWindowed(this, 0, 1, ctpLine?.OrderType, ctpLine?.OrderNbr, ctpLine?.LineNbr);
            if (soLine != null)
            {
                soLineAMExtension = PXCache<SOLine>.GetExtension<SOLineExt>(soLine);
            }

            if (ctpLine == null || soLine == null || soLineAMExtension == null)
            {
                return null;
            }

            var prodMaintGraph = CreateInstance<ProdMaint>();

            var amProdItem = prodMaintGraph.ProdMaintRecords.Insert(new AMProdItem
            {
                OrderType = ctpOrderType,
                ProdOrdID = null,
                BOMEffDate = effEndDate
            });


            //amProdItem.Descr = demand.Descr;
            amProdItem.ProdDate = prodMaintGraph.Accessinfo.BusinessDate;
            amProdItem.InventoryID = ctpLine.InventoryID;
            amProdItem.SubItemID = ctpLine.SubItemID;
            amProdItem.ConstDate = effEndDate;
            amProdItem.SchedulingMethod = ScheduleMethod.FinishOn;            
            amProdItem.UOM = InventoryHelper.GetbaseUOM(prodMaintGraph, ctpLine.InventoryID);
            amProdItem = prodMaintGraph.ProdMaintRecords.Update(amProdItem);

            amProdItem.QtytoProd = ctpLine.BaseOpenQty;
            amProdItem.Qty = ctpLine.BaseOpenQty;
            amProdItem = prodMaintGraph.ProdMaintRecords.Update(amProdItem);

            amProdItem.OrdLineRef = ctpLine.LineNbr;
            amProdItem.OrdTypeRef = ctpLine.OrderType;
            amProdItem.OrdNbr = ctpLine.OrderNbr;
            amProdItem.UpdateProject = false;
            amProdItem.CustomerID = ctpLine.CustomerID;

            var customer = (BAccountR)PXSelectorAttribute.Select<AMProdItem.customerID>(prodMaintGraph.ProdMaintRecords.Cache, amProdItem);
            if (customer?.BAccountID == null || customer.Type != BAccountType.CustomerType && customer.Type != BAccountType.CombinedType)
            {
                //not a valid CustomerID (such as planning from a Transfer order)
                amProdItem.CustomerID = null;
            }

            var productionDetailSource = ProductionDetailSource.NoSource;

            if (soLine.ProjectID != null)
            {
                amProdItem.ProjectID = soLine.ProjectID;
                amProdItem.TaskID = soLine.TaskID;
                amProdItem.CostCodeID = soLine.CostCodeID;
            }

            if (soLineAMExtension.IsConfigurable.GetValueOrDefault())
            {
                //TODO: work with configured items

            }

            if (productionDetailSource == ProductionDetailSource.NoSource &&
                PXAccess.FeatureInstalled<FeaturesSet.manufacturingEstimating>() &&
                !string.IsNullOrWhiteSpace(soLineAMExtension.AMEstimateID) &&
                !string.IsNullOrWhiteSpace(soLineAMExtension.AMEstimateRevisionID))
            {
                productionDetailSource = ProductionDetailSource.Estimate;
                amProdItem.EstimateID = soLineAMExtension.AMEstimateID;
                amProdItem.EstimateRevisionID = soLineAMExtension.AMEstimateRevisionID;
            }

            if (productionDetailSource == ProductionDetailSource.NoSource)
            {
                productionDetailSource = ProductionDetailSource.BOM;
            }

            amProdItem.DetailSource = productionDetailSource;
            amProdItem.SiteID = soLine.SiteID;
            amProdItem.LocationID = soLine.LocationID ?? InventoryHelper.DfltLocation.GetDefault(prodMaintGraph,
                                        InventoryHelper.DfltLocation.BinType.Receipt, soLine.InventoryID, soLine.SiteID, true);

            var amProdItem2 = prodMaintGraph.ProdMaintRecords.Update(amProdItem);

            amProdItem2.Reschedule = false;
            amProdItem2.BuildProductionBom = true;
            amProdItem2.DetailSource = productionDetailSource;
            amProdItem2 = prodMaintGraph.ProdMaintRecords.Update(amProdItem2);

            var schdItem = (AMSchdItem)prodMaintGraph.SchdItemRecords.SelectWindowed(0, 1);

            if (schdItem == null)
            {
                schdItem = ProductionScheduleEngine.CreateSchdItem(amProdItem2);
                schdItem.SchedulingMethod = ScheduleMethod.FinishOn;
                schdItem = prodMaintGraph.SchdItemRecords.Insert(schdItem);
            }

            var soOrder = (SOOrder)PXSelect<SOOrder,
                Where<SOOrder.orderType, Equal<Required<SOOrder.orderType>>,
                    And<SOOrder.orderNbr, Equal<Required<SOOrder.orderNbr>>>>
            >.SelectWindowed(prodMaintGraph, 0, 1, soLine?.OrderType, soLine?.OrderNbr);

            prodMaintGraph.InsertCreatedOrderEventMessage(GetProductionCreatedEventMessage(amProdItem2, soLine), soOrder?.NoteID);

            soLineAMExtension.AMSchdNoteID = schdItem?.NoteID;
            if (soLineAMExtension.AMSchdNoteID != null)
            {
                Common.Cache.AddCacheView<SOLine>(prodMaintGraph);
                prodMaintGraph.Caches<SOLine>().Update(soLine);
            }

            prodMaintGraph.Actions.PressSave();

            return prodMaintGraph.SchdItemRecords?.Current;
        }

        protected virtual string GetProductionCreatedEventMessage(AMProdItem prodItem, SOLine soLine)
        {
            if (soLine?.OrderNbr != null && soLine.OrderType != null)
            {
                return Messages.GetLocal(Messages.CTPCreatedFromOrderTypeOrderNbr, soLine.OrderType.Trim(), soLine.OrderNbr.Trim());
            }

            var functionDesc = prodItem?.Function == null
                ? string.Empty
                : OrderTypeFunction.GetDescription(prodItem.Function);

            return Messages.GetLocal(Messages.CreatedOrder, functionDesc);
        }

        private SOOrderEntry GetOrderEntry(List<CTPLine> list)
        {
            return (list?.Count ?? 0) == 0 ? null : GetOrderEntry(list.FirstOrDefault());
        }

        protected virtual SOOrderEntry GetOrderEntry(CTPLine line)
        {
            var orderEntry = CreateInstance<SOOrderEntry>();
            orderEntry.Document.Current = orderEntry.Document.Search<SOOrder.orderNbr>(line?.OrderNbr, line?.OrderType);
            return orderEntry;
        }

        protected virtual void AcceptCTPProcess(ProdMaint prodGraph, CTPLine ctpLine, CTPFilter filter)
        {
            var ctpLineExt = PXCache<SOLine>.GetExtension<SOLineExt>(ctpLine);
            if (ctpLineExt == null)
            {
                return;
            }
            if (ctpLineExt.AMCTPAccepted == true)
            {
                throw new PXException(Messages.CTPHasAlreadyBeenAccepted);
            }
            if(ctpLine.ProdOrdID == null)
            {
                throw new PXException(Messages.CTPProcessHasNotBeenRun);
            }

            AMProdItem newProdOrder = null;
            //if regular production order has already been created, just mark row as accepted
            if (ctpLine.AMCTPOrderType != filter.DefaultOrderType)
            {
                newProdOrder = CopyCTPtoRegularProdOrder(prodGraph, ctpLine, filter);
            }
        }

        protected virtual AMProdItem CopyCTPtoRegularProdOrder(ProdMaint prodGraph, CTPLine ctpLine, CTPFilter filter)
        {
            AMProdItem ctpOrder = SelectFrom<AMProdItem>.Where<AMProdItem.orderType.IsEqual<@P.AsString>.
                And<AMProdItem.prodOrdID.IsEqual<@P.AsString>>>.View.Select(this, ctpLine.AMCTPOrderType, ctpLine.ProdOrdID);
            if (ctpOrder != null)
            {

                AMProdItem newItem = prodGraph.ProdMaintRecords.Insert(new AMProdItem { 
                    OrderType = filter.DefaultOrderType
                });

                if (ctpLine.ManualProdOrdID != null)
                    newItem.ProdOrdID = ctpLine.ManualProdOrdID;
                newItem.InventoryID = ctpOrder.InventoryID;
                newItem.SiteID = ctpOrder.SiteID;
                newItem.LocationID = ctpOrder.LocationID;
                newItem.QtytoProd = ctpOrder.QtytoProd;
                newItem.DetailSource = ProductionDetailSource.ProductionRef;
                newItem.SourceOrderType = ctpOrder.OrderType;
                newItem.SourceProductionNbr = ctpOrder.ProdOrdID;
                newItem.SchedulingMethod = ctpOrder.SchedulingMethod;
                newItem.ConstDate = ctpOrder.ConstDate;
                newItem.StartDate = ctpOrder.StartDate;
                newItem.EndDate = ctpOrder.EndDate;
                newItem.Reschedule = false;
                newItem = prodGraph.ProdMaintRecords.Update(newItem);
                prodGraph.Persist();

                ctpOrder = (AMProdItem)prodGraph.ProdMaintRecords.Cache.CreateCopy(ctpOrder);
                ctpOrder.OrdTypeRef = null;
                ctpOrder.OrdNbr = null;
                ctpOrder.OrdLineRef = null;
                prodGraph.ProdMaintRecords.Update(ctpOrder);
                prodGraph.ProdMaintRecords.Current = ctpOrder;
                prodGraph.ProdMaintRecords.Delete(ctpOrder);
                prodGraph.Persist();

                return newItem;
            }
            return null;
        }

        protected virtual void _(Events.RowSelected<CTPFilter> e)
        {
            CTPFilter filter = e.Row;
            if (filter != null && filter.DefaultOrderType != null)
            {
                PXResult<AMOrderType, Numbering> result = (PXResult<AMOrderType, Numbering>)SelectFrom<AMOrderType>.InnerJoin<Numbering>.
                    On<AMOrderType.prodNumberingID.IsEqual<Numbering.numberingID>>.
                    Where<AMOrderType.orderType.IsEqual<@P.AsString>>.View.Select(this, filter.DefaultOrderType).FirstOrDefault();
                if (result != null && ((Numbering)result).UserNumbering == true)
                {
                    PXUIFieldAttribute.SetVisible<CTPLine.manualProdOrdID>(ProcessingRecords.Cache, null, true);
                    return;
                }
            }
            PXUIFieldAttribute.SetVisible<CTPLine.manualProdOrdID>(ProcessingRecords.Cache, null, false);
        }

        protected virtual void _(Events.RowSelected<CTPLine> e)
        {
            CTPLine line = e.Row;
            if(line != null)
            {
                var soLineExt = PXCache<SOLine>.GetExtension<SOLineExt>(line);
                if (soLineExt == null)
                {
                    return;
                }
                PXUIFieldAttribute.SetEnabled<CTPLine.selected>(e.Cache, line, soLineExt.AMCTPAccepted != true);
                PXUIFieldAttribute.SetEnabled<CTPLine.manualProdOrdID>(e.Cache, line, true);
            }
            
        }

        protected virtual void CTPFilter_SOOrderType_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            var row = (CTPFilter)e.Row;
            if (row == null)
            {
                return;
            }

            row.SOOrderNbr = null;
        }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = "SO Type", Visible = false, Enabled = false)]
        protected virtual void CTPLine_OrderType_CacheAttached(PXCache cache)
        { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = "SO Nbr.", Visible = false, Enabled = false)]
        protected virtual void CTPLine_OrderNbr_CacheAttached(PXCache cache)
        { }

        [PXCacheName("CTP Filter")]
        [Serializable]
        public class CTPFilter : IBqlTable
        {
            #region ProcessAction

            public abstract class processAction : PX.Data.BQL.BqlString.Field<processAction> { }


            protected string _ProcessAction;

            [PXString(1, IsFixed = true)]
            [PXUnboundDefault(CTPProcessActionOptions.RunCTP)]
            [PXUIField(DisplayName = "Action")]
            [CTPProcessActionOptions.List]
            public virtual string ProcessAction
            {
                get { return this._ProcessAction; }
                set { this._ProcessAction = value; }
            }

            #endregion
            #region SOOrderType
            public abstract class sOOrderType : PX.Data.BQL.BqlString.Field<sOOrderType> { }
            protected String _SOOrderType;
            [PXDBString(2, IsFixed = true, InputMask = ">aa")]
            [PXSelector(typeof(Search<SOOrderType.orderType, Where<SOOrderType.active, Equal<boolTrue>>>))]
            [PXUIField(DisplayName = "SO Type", Visibility = PXUIVisibility.SelectorVisible)]
            public virtual String SOOrderType
            {
                get
                {
                    return this._SOOrderType;
                }
                set
                {
                    this._SOOrderType = value;
                }
            }
            #endregion
            #region SOOrderNbr
            public abstract class sOOrderNbr : PX.Data.BQL.BqlString.Field<sOOrderNbr> { }
            protected String _SOOrderNbr;
            [PXDBString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
            [PXUIField(DisplayName = "SO Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
            [PXSelector(typeof(Search<SOOrder.orderNbr, Where<SOOrder.orderType, Equal<Current<CTPFilter.sOOrderType>>>>))]
            public virtual String SOOrderNbr
            {
                get
                {
                    return this._SOOrderNbr;
                }
                set
                {
                    this._SOOrderNbr = value;
                }
            }
            #endregion
            #region DefaultOrderType
            public abstract class defaultOrderType : PX.Data.BQL.BqlString.Field<defaultOrderType> { }

            protected String _DefaultOrderType;
            [PXDefault(typeof(AMPSetup.defaultOrderType))]
            [AMOrderTypeField(Visibility = PXUIVisibility.SelectorVisible, DisplayName = "Regular Production Order Type")]
            [PXRestrictor(typeof(Where<AMOrderType.active, Equal<True>, And<AMOrderType.function, Equal<OrderTypeFunction.regular>>>), PX.Objects.SO.Messages.OrderTypeInactive)]
            [AMOrderTypeSelector]
            public virtual String DefaultOrderType
            {
                get
                {
                    return this._DefaultOrderType;
                }
                set
                {
                    this._DefaultOrderType = value;
                }
            }
            #endregion
            #region Redirect
            public abstract class redirect : PX.Data.BQL.BqlBool.Field<redirect> { }
            protected bool? _Redirect = false;
            [PXBool]
            [PXUnboundDefault(false)]
            public virtual bool? Redirect
            {
                get
                {
                    return _Redirect;
                }
                set
                {
                    _Redirect = value;
                }
            }
            #endregion
        }

        [PXProjection(typeof(Select2<SOLine,
            InnerJoin<InventoryItem,
                On<InventoryItem.inventoryID, Equal<SOLine.inventoryID>>,
            LeftJoin<AMSchdItem,
                On<AMSchdItem.noteID, Equal<SOLineExt.aMSchdNoteID>>>>,
            Where<InventoryItemExt.aMCTPItem, Equal<True>>>), Persistent = false)]
        [PXCacheName("CTP Line")]
        [Serializable]
        public class CTPLine : SOLine
        {
            #region Selected
            public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }
            protected bool? _Selected = false;
            [PXBool]
            [PXUnboundDefault(false)]
            [PXUIField(DisplayName = "Selected")]
            public virtual bool? Selected
            {
                get
                {
                    return _Selected;
                }
                set
                {
                    _Selected = value;
                }
            }
            #endregion

            #region AMCTPOrderType
            public abstract class aMCTPOrderType : PX.Data.BQL.BqlString.Field<aMCTPOrderType> { }

            protected String _AMCTPOrderType;
            [AMOrderTypeField(Enabled = false, DisplayName = "Prod. Order Type", BqlField = typeof(AMSchdItem.orderType))]
            public virtual String AMCTPOrderType
            {
                get
                {
                    return this._AMCTPOrderType;
                }
                set
                {
                    this._AMCTPOrderType = value;
                }
            }
            #endregion
            #region ProdOrdID
            public abstract class prodOrdID : PX.Data.BQL.BqlString.Field<prodOrdID> { }

            protected string _ProdOrdID;
            [ProductionNbr(Enabled = false, DisplayName = "Prod. Order Nbr.", BqlField = typeof(AMSchdItem.prodOrdID))]
            [ProductionOrderSelector(typeof(AMSchdItem.orderType), includeAll: true, ValidateValue = false)]
            public virtual string ProdOrdID
            {
                get
                {
                    return this._ProdOrdID;
                }
                set
                {
                    this._ProdOrdID = value;
                }
            }
            #endregion
            #region ManualProdOrdID
            public abstract class manualProdOrdID : PX.Data.BQL.BqlString.Field<manualProdOrdID> { }

            protected string _ManualProdOrdID;
            [PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
            [PXUIField(DisplayName = "Manual Order Nbr", Visibility = PXUIVisibility.SelectorVisible)]
            [ProductionOrderSelector(typeof(CTPFilter.defaultOrderType), includeAll: true, ValidateValue = false)]
            public virtual string ManualProdOrdID
            {
                get
                {
                    return this._ManualProdOrdID;
                }
                set
                {
                    this._ManualProdOrdID = value;
                }
            }
            #endregion
            #region CTPDate
            public abstract class cTPDate : PX.Data.BQL.BqlDateTime.Field<cTPDate> { }

            protected DateTime? _CTPDate;
            [PXDBDate(BqlField = typeof(AMSchdItem.endDate))]
            [PXUIField(DisplayName = "CTP Date", Enabled = false)]
            public virtual DateTime? CTPDate
            {
                get
                {
                    return this._CTPDate;
                }
                set
                {
                    this._CTPDate = value;
                }
            }
            #endregion
        }

        [Serializable]
        [PXCacheName("Quantity Available Filter")]
        public class QtyAvailFilter : IBqlTable
        {
            #region OrderType
            public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }
            protected String _OrderType;
            [PXDBString(2, IsKey = true, IsFixed = true)]
            [PXDefault(typeof(CTPFilter.sOOrderType))]
            [PXUIField(DisplayName = "Order Type", Visible = false, Enabled = false)]
            [PXSelector(typeof(Search<SOOrderType.orderType>), CacheGlobal = true)]
            public virtual String OrderType
            {
                get
                {
                    return this._OrderType;
                }
                set
                {
                    this._OrderType = value;
                }
            }
            #endregion
            #region OrderNbr
            public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }
            protected String _OrderNbr;
            [PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "")]
            [PXDBDefault(typeof(CTPFilter.sOOrderNbr), DefaultForUpdate = false)]
            [PXUIField(DisplayName = "Order Nbr.", Visible = false, Enabled = false)]
            public virtual String OrderNbr
            {
                get
                {
                    return this._OrderNbr;
                }
                set
                {
                    this._OrderNbr = value;
                }
            }
            #endregion
            #region LineNbr
            public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
            protected Int32? _LineNbr;
            [PXDBInt(IsKey = true)]
            [PXUIField(DisplayName = "Line Nbr.", Visible = false)]
            public virtual Int32? LineNbr
            {
                get
                {
                    return this._LineNbr;
                }
                set
                {
                    this._LineNbr = value;
                }
            }
            #endregion
            #region RequestQty
            public abstract class requestQty : PX.Data.BQL.BqlDecimal.Field<requestQty> { }
            protected Decimal? _RequestQty;
            [PXQuantity()]
            [PXUnboundDefault(TypeCode.Decimal, "0.0")]
            [PXUIField(DisplayName = "Requested", Enabled = false)]
            public virtual Decimal? RequestQty
            {
                get
                {
                    return this._RequestQty;
                }
                set
                {
                    this._RequestQty = value;
                }
            }
            #endregion
            #region QtyAvail
            public abstract class qtyAvail : PX.Data.BQL.BqlDecimal.Field<qtyAvail> { }
            protected Decimal? _QtyAvail;
            [PXQuantity()]
            [PXUnboundDefault(TypeCode.Decimal, "0.0")]
            [PXUIField(DisplayName = "Available", Enabled = false)]
            public virtual Decimal? QtyAvail
            {
                get
                {
                    return this._QtyAvail;
                }
                set
                {
                    this._QtyAvail = value;
                }
            }
            #endregion
            #region QtyHardAvail
            public abstract class qtyHardAvail : PX.Data.BQL.BqlDecimal.Field<qtyHardAvail> { }
            protected Decimal? _QtyHardAvail;
            [PXDBQuantity()]
            [PXDefault(TypeCode.Decimal, "0.0")]
            [PXUIField(DisplayName = "Available for Shipment", Enabled = false)]
            public virtual Decimal? QtyHardAvail
            {
                get
                {
                    return this._QtyHardAvail;
                }
                set
                {
                    this._QtyHardAvail = value;
                }
            }
            #endregion
            #region SupplyAvail
            public abstract class supplyAvail : PX.Data.BQL.BqlDecimal.Field<supplyAvail> { }
            protected Decimal? _SupplyAvail;
            [PXQuantity()]
            [PXUnboundDefault(TypeCode.Decimal, "0.0")]
            [PXUIField(DisplayName = "Supply Available by Requested Date", Enabled = false)]
            public virtual Decimal? SupplyAvail
            {
                get
                {
                    return this._SupplyAvail;
                }
                set
                {
                    this._SupplyAvail = value;
                }
            }
            #endregion
            #region ProdAvail
            public abstract class prodAvail : PX.Data.BQL.BqlDecimal.Field<prodAvail> { }
            protected Decimal? _ProdAvail;
            [PXQuantity()]
            [PXUnboundDefault(TypeCode.Decimal, "0.0")]
            [PXUIField(DisplayName = "Production Available by Requested Date", Enabled = false)]
            public virtual Decimal? ProdAvail
            {
                get
                {
                    return this._ProdAvail;
                }
                set
                {
                    this._ProdAvail = value;
                }
            }
            #endregion
            #region TotalAvail
            public abstract class totalAvail : PX.Data.BQL.BqlDecimal.Field<totalAvail> { }
            protected Decimal? _TotalAvail;
            [PXQuantity()]
            [PXUnboundDefault(TypeCode.Decimal, "0.0")]
            [PXUIField(DisplayName = "Total Available by Requested Date", Enabled = false)]
            public virtual Decimal? TotalAvail
            {
                get
                {
                    return this._TotalAvail;
                }
                set
                {
                    this._TotalAvail = value;
                }
            }
            #endregion
        }
    }
}
