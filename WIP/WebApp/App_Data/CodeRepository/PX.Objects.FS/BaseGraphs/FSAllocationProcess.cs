using PX.Common;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static PX.Objects.FS.AllocationHelper;

namespace PX.Objects.FS
{
    public static class AllocationHelper
    {
        public class AllocationInfo
        {
            public virtual string SrvOrdType { get; set; }
            public virtual string RefNbr { get; set; }
            public virtual int LineNbr { get; set; }
            public virtual int SODetID { get; set; }

            private string _LotSerialNbr;
            public virtual string LotSerialNbr
            {
                get => _LotSerialNbr;
                set
                {
                    if (value == string.Empty)
                    {
                        _LotSerialNbr = null;
                    }
                    else
                    {
                        _LotSerialNbr = value;
                    }
                }
            }

            public virtual decimal Qty { get; set; }

            private string _Key;
            public virtual string Key { get => _Key; }

            public AllocationInfo(FSSODetSplit split, FSSODet srvOrdLine)
            {
                SetSrvOrdLineValues(srvOrdLine);

                LotSerialNbr = split.LotSerialNbr;
                Qty = (decimal)split.Qty;

                SetKey();
            }

            public AllocationInfo(FSApptLineSplit split, FSSODet srvOrdLine)
            {
                SetSrvOrdLineValues(srvOrdLine);

                LotSerialNbr = split.LotSerialNbr;
                Qty = (decimal)split.Qty;

                SetKey();
            }

            public AllocationInfo(decimal? qty, FSSODet srvOrdLine)
            {
                SetSrvOrdLineValues(srvOrdLine);

                LotSerialNbr = null;
                Qty = (decimal)qty;

                SetKey();
            }

            protected virtual void SetSrvOrdLineValues(FSSODet srvOrdLine)
            {
                SrvOrdType = srvOrdLine.SrvOrdType;
                RefNbr = srvOrdLine.RefNbr;
                LineNbr = (int)srvOrdLine.LineNbr;
                SODetID = (int)srvOrdLine.SODetID;
            }

            protected virtual void SetKey()
            {
                _Key = SODetID.ToString() + "|" + LotSerialNbr;
            }
        }

        public static AllocationInfo Add(this Dictionary<string, AllocationInfo> splitLines, AllocationInfo splitLine)
        {
            AllocationInfo existingLine;

            if (splitLines.TryGetValue(splitLine.Key, out existingLine))
            {
                existingLine.Qty += splitLine.Qty;
                return existingLine;
            }
            else
            {
                splitLines.Add(splitLine.Key, splitLine);
                return splitLine;
            }
        }
    }

    public class FSAllocationProcess : PXGraph<FSAllocationProcess>
    {
        private static readonly Lazy<FSAllocationProcess> _fsDeallocateProcess = new Lazy<FSAllocationProcess>(CreateInstance<FSAllocationProcess>);
        public static FSAllocationProcess SingleFSDeallocateProcess => _fsDeallocateProcess.Value;

        public static void DeallocateServiceOrderSplits(ServiceOrderEntry docgraph, List<FSSODetSplit> splitsToDeallocate, bool calledFromServiceOrder)
            => SingleFSDeallocateProcess.DeallocateServiceOrderSplitsInt(docgraph, splitsToDeallocate, calledFromServiceOrder);

        public static void ReallocateServiceOrderSplits(List<AllocationInfo> requiredAllocationList)
            => SingleFSDeallocateProcess.ReallocateServiceOrderSplitsInt(requiredAllocationList);

        public virtual void DeallocateServiceOrderSplitsInt(ServiceOrderEntry docgraph, List<FSSODetSplit> splitsToDeallocate, bool calledFromServiceOrder)
        {
            IEnumerable<IGrouping<(string, string), FSSODetSplit>> orderGroups = splitsToDeallocate.GroupBy(x => (x.SrvOrdType, x.RefNbr));

            foreach (IGrouping<(string, string), FSSODetSplit> orderGroup in orderGroups)
            {
                FSSODetSplit firstSplit = orderGroup.First();
                FSServiceOrder currentServiceOrder = null;

                if (calledFromServiceOrder == false)
                {
                    docgraph.Clear();
                    currentServiceOrder = docgraph.ServiceOrderRecords.Current = docgraph.ServiceOrderRecords.Search<FSServiceOrder.refNbr>(firstSplit.RefNbr, firstSplit.SrvOrdType);
                }
                else
                {
                    currentServiceOrder = docgraph.ServiceOrderRecords.Current;
                }

                if (currentServiceOrder.SrvOrdType != firstSplit.SrvOrdType || currentServiceOrder.RefNbr != firstSplit.RefNbr)
                {
                    throw new PXException(TX.Error.SERVICE_ORDER_NOT_FOUND);
                }

                IEnumerable<IGrouping<(string, string, int?), FSSODetSplit>> lineGroups = orderGroup.GroupBy(x => (x.SrvOrdType, x.RefNbr, x.LineNbr));

                foreach (IGrouping<(string, string, int?), FSSODetSplit> lineGroup in lineGroups)
                {
                    FSSODetSplit firstSplit2 = lineGroup.First();

                    FSSODet soDetLine = docgraph.ServiceOrderDetails.Current = docgraph.ServiceOrderDetails.Search<FSSODet.lineNbr>(firstSplit2.LineNbr);

                    if (soDetLine.LineNbr != firstSplit2.LineNbr)
                    {
                        throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(FSSODet)));
                    }

                    foreach (FSSODetSplit forSplit in lineGroup)
                    {
                        FSSODetSplit realSplit = docgraph.Splits.Current = docgraph.Splits.Search<FSSODetSplit.splitLineNbr>(forSplit.SplitLineNbr);

                        if (forSplit.SplitLineNbr != realSplit.SplitLineNbr)
                        {
                            throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(FSSODetSplit)));
                        }

                        if (CanUpdateRealSplit(realSplit, forSplit) == true)
                        {
                            decimal? baseDeallocateQty = realSplit.BaseQty - forSplit.BaseQty;
                            DeallocateSplitLine(docgraph, soDetLine, baseDeallocateQty, realSplit);
                        }
                    }
                }

                //docgraph.SelectTimeStamp(); check
                if (calledFromServiceOrder == false)
                {
                    docgraph.SkipTaxCalcAndSave();
                }
            }
        }

        public virtual void ReallocateServiceOrderSplitsInt(List<AllocationInfo> requiredAllocationList)
        {
            ServiceOrderEntry docgraph = PXGraph.CreateInstance<ServiceOrderEntry>();

            PXRowUpdating cancelRowUpdating_handler = new PXRowUpdating((sender, e) => { e.Cancel = true; });

            IEnumerable<IGrouping<(string, string), AllocationInfo>> docGroups = requiredAllocationList.GroupBy(x => (x.SrvOrdType, x.RefNbr));

            foreach (IGrouping<(string, string), AllocationInfo> docGroup in docGroups)
            {
                AllocationInfo docIdentifier = docGroup.First();

                docgraph.Clear();
                FSServiceOrder serviceOrder = docgraph.ServiceOrderRecords.Current = docgraph.ServiceOrderRecords.Search<FSServiceOrder.refNbr>(docIdentifier.RefNbr, docIdentifier.SrvOrdType);

                if (serviceOrder.SrvOrdType != docIdentifier.SrvOrdType || serviceOrder.RefNbr != docIdentifier.RefNbr)
                {
                    throw new PXException(TX.Error.SERVICE_ORDER_NOT_FOUND);
                }

                IEnumerable<IGrouping<(string, string, int), AllocationInfo>> lineGroups = docGroup.GroupBy(x => (x.SrvOrdType, x.RefNbr, x.LineNbr));

                docgraph.lsFSSODetSelect.SuppressedMode = true;

                foreach (IGrouping<(string, string, int), AllocationInfo> lineGroup in lineGroups)
                {
                    AllocationInfo lineIdentifier = lineGroup.First();

                    FSSODet soDet = docgraph.ServiceOrderDetails.Current = docgraph.ServiceOrderDetails.Search<FSSODet.lineNbr>(lineIdentifier.LineNbr);

                    if (soDet.LineNbr != lineIdentifier.LineNbr)
                    {
                        throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(FSSODet)));
                    }

                    docgraph.RowUpdating.AddHandler<FSSODet>(cancelRowUpdating_handler);

                    try
                    {
                        foreach (AllocationInfo requiredAlloc in lineGroup)
                        {
                            ReallocateItemIntoCurrentLineSplit(docgraph, requiredAlloc);
                        }

                        MergeEqualSplitsForCurrentLine(docgraph, s => s.Completed == true && s.PlanID == null);
                        MergeEqualSplitsForCurrentLine(docgraph, s => s.Completed == false);
                    }
                    finally
                    {
                        docgraph.RowUpdating.RemoveHandler<FSSODet>(cancelRowUpdating_handler);
                    }

                    UpdateCurrentLineBasedOnSplit(docgraph);
                }

                docgraph.lsFSSODetSelect.SuppressedMode = false;
                docgraph.Save.Press();
            }
        }

        public virtual void ReallocateItemIntoCurrentLineSplit(ServiceOrderEntry graph, AllocationInfo allocationInfo, int recursionLevel = 0)
        {
            if (allocationInfo.Qty <= 0m)
            {
                return;
            }

            FSSODetSplit currentSplit = graph.Splits.Select().
                            RowCast<FSSODetSplit>().
                            Where(s => s.Completed == true && s.POCreate == false && s.ShippedQty > 0m
                                && (
                                    (string.IsNullOrEmpty(s.LotSerialNbr) && string.IsNullOrEmpty(allocationInfo.LotSerialNbr))
                                    ||
                                    (s.LotSerialNbr == allocationInfo.LotSerialNbr)
                                )
                            ).
                            // Sorted first by quantity to minimize the creation of new splits
                            OrderBy(s => s.ShippedQty).ThenBy(s => s.SplitLineNbr).
                            FirstOrDefault();

            if (currentSplit == null)
            {
                if (recursionLevel == 0 && allocationInfo.LotSerialNbr == null)
                {
                    return;
                }
                else
                {
                    throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(FSSODetSplit)));
                }
            }

            decimal reallocationQty = allocationInfo.Qty;

            if (reallocationQty > currentSplit.ShippedQty)
            {
                reallocationQty = (decimal)currentSplit.ShippedQty;
            }

            FSSODetSplit completedSplit = null;
            decimal? completedSplitQty = null;
            FSSODetSplit openSplit = null;
            decimal? openSplitQty = null;

            if (reallocationQty < currentSplit.ShippedQty)
            {
                completedSplit = currentSplit;
                completedSplitQty = currentSplit.ShippedQty - reallocationQty;

                openSplitQty = reallocationQty;

                openSplit = (FSSODetSplit)graph.Splits.Cache.CreateCopy(currentSplit);

                openSplit.ParentSplitLineNbr = currentSplit.SplitLineNbr;
                openSplit.SplitLineNbr = null;
                openSplit.PlanID = null;

                openSplit = graph.Splits.Insert(openSplit);
            }
            else
            {
                openSplit = currentSplit;
                openSplitQty = currentSplit.Qty;
            }

            if (completedSplit != null)
            {
                if (completedSplit.OpenQty != 0m || completedSplit.BaseOpenQty != 0m)
                {
                    throw new PXInvalidOperationException();
                }

                completedSplit.Qty = completedSplitQty;
                completedSplit.BaseQty = INUnitAttribute.ConvertToBase(graph.Splits.Cache, completedSplit.InventoryID, completedSplit.UOM, completedSplit.Qty.Value, INPrecision.QUANTITY);

                completedSplit.ShippedQty = completedSplit.Qty;
                completedSplit.BaseShippedQty = completedSplit.BaseQty;

                graph.Splits.Update(completedSplit);
            }

            if (openSplit != null)
            {
                openSplit.Qty = openSplitQty;
                openSplit.BaseQty = INUnitAttribute.ConvertToBase(graph.Splits.Cache, openSplit.InventoryID, openSplit.UOM, openSplit.Qty.Value, INPrecision.QUANTITY);

                openSplit.OpenQty = openSplit.Qty;
                openSplit.BaseOpenQty = openSplit.BaseQty;
                openSplit.ShippedQty = 0m;
                openSplit.BaseShippedQty = 0m;
                openSplit.Completed = false;

                CreateAndAssignPlan(graph.Splits.Cache, openSplit);

                graph.Splits.Update(openSplit);
            }

            allocationInfo.Qty -= reallocationQty;

            if (allocationInfo.Qty > 0m)
            {
                ReallocateItemIntoCurrentLineSplit(graph, allocationInfo, recursionLevel + 1);
            }
        }

        public static List<AllocationInfo> GetRequiredAllocationList(PXGraph graph, object createdDoc)
            => SingleFSDeallocateProcess.GetRequiredAllocationListInt(graph, createdDoc);
        public virtual List<AllocationInfo> GetRequiredAllocationListInt(PXGraph graph, object createdDoc)
        {
            PXResultset<FSPostDet> fsPostDetRows = null;
            PXResultset<FSContractPostDet> fsContractPostDetRows = null;

            if (createdDoc is SOOrder)
            {
                SOOrder soOrderRow = (SOOrder)createdDoc;

                fsPostDetRows = PXSelectJoin<
                                FSPostDet,
                                LeftJoin<FSSODet,
                                    On<FSSODet.postID, Equal<FSPostDet.postID>>,
                                LeftJoin<FSAppointmentDet,
                                    On<FSAppointmentDet.postID, Equal<FSPostDet.postID>>>>,
                                Where<
                                    FSPostDet.sOOrderNbr, Equal<Required<FSPostDet.sOOrderNbr>>,
                                    And<FSPostDet.sOOrderType, Equal<Required<FSPostDet.sOOrderType>>>>>
                                .Select(graph, soOrderRow.RefNbr, soOrderRow.OrderType);

                if (fsPostDetRows != null
                    && fsPostDetRows.Count == 0) 
                { 
                    fsContractPostDetRows = PXSelectJoin<
                                                FSContractPostDet,
                                            InnerJoin<FSContractPostDoc,
                                                On<FSContractPostDoc.contractPostDocID, Equal<FSContractPostDet.contractPostDocID>>,
                                            LeftJoin<FSSODet,
                                                On<FSSODet.sODetID, Equal<FSContractPostDet.sODetID>>,
                                            LeftJoin<FSAppointmentDet,
                                                On<FSAppointmentDet.appDetID, Equal<FSContractPostDet.appDetID>>>>>,
                                            Where<
                                                FSContractPostDoc.postedTO, Equal<Required<FSContractPostDoc.postedTO>>,
                                                And<FSContractPostDoc.postDocType, Equal<Required<FSContractPostDoc.postDocType>>,
                                                And<FSContractPostDoc.postRefNbr, Equal<Required<FSContractPostDoc.postRefNbr>>>>>>
                                            .Select(graph, ID.Batch_PostTo.SO, soOrderRow.OrderType, soOrderRow.RefNbr);
                }
            }
            else if (createdDoc is SOInvoice)
            {
                SOInvoice soInvoiceRow = (SOInvoice)createdDoc;

                fsPostDetRows = PXSelectJoin<
                                FSPostDet,
                                LeftJoin<FSSODet,
                                    On<FSSODet.postID, Equal<FSPostDet.postID>>,
                                LeftJoin<FSAppointmentDet,
                                    On<FSAppointmentDet.postID, Equal<FSPostDet.postID>>>>,
                                Where<
                                    FSPostDet.sOInvRefNbr, Equal<Required<FSPostDet.sOInvRefNbr>>,
                                    And<FSPostDet.sOInvDocType, Equal<Required<FSPostDet.sOInvDocType>>>>>
                                .Select(graph, soInvoiceRow.RefNbr, soInvoiceRow.DocType);
            }
            else if (createdDoc is INRegister)
            {
                INRegister inRegisterRow = (INRegister)createdDoc;

                fsPostDetRows = PXSelectJoin<
                                FSPostDet,
                                LeftJoin<FSSODet,
                                    On<FSSODet.postID, Equal<FSPostDet.postID>>,
                                LeftJoin<FSAppointmentDet,
                                    On<FSAppointmentDet.postID, Equal<FSPostDet.postID>>>>,
                                Where<
                                    FSPostDet.iNRefNbr, Equal<Required<FSPostDet.iNRefNbr>>,
                                    And<FSPostDet.iNDocType, Equal<Required<FSPostDet.iNDocType>>>>>
                                .Select(graph, inRegisterRow.RefNbr, inRegisterRow.DocType);
            }
            else
            {
                throw new NotImplementedException();
            }

            var requiredAllocationList = new Dictionary<string, AllocationInfo>();

            foreach (PXResult<FSPostDet, FSSODet, FSAppointmentDet> row in fsPostDetRows)
            {
                AddRequiredAllocationToList(graph, requiredAllocationList, (FSSODet)row, (FSAppointmentDet)row);
            }

            if (fsContractPostDetRows != null) 
            { 
                foreach (PXResult<FSContractPostDet, FSContractPostDoc, FSSODet, FSAppointmentDet> row in fsContractPostDetRows)
                {
                    AddRequiredAllocationToList(graph, requiredAllocationList, (FSSODet)row, (FSAppointmentDet)row);
                }
            }

            return requiredAllocationList.Values.ToList();
        }

        public virtual void AddRequiredAllocationToList(PXGraph graph, Dictionary<string, AllocationInfo>  requiredAllocationList, FSSODet soDetLine, FSAppointmentDet apptLine) 
        {
            if (apptLine == null || apptLine.RefNbr == null)
            {
                if (soDetLine == null)
                {
                    soDetLine = FSSODet.UK.Find(graph, apptLine.SODetID);
                }

                var splitLines = PXSelect<FSSODetSplit,
                                Where<
                                    FSSODetSplit.srvOrdType, Equal<Required<FSSODetSplit.srvOrdType>>,
                                    And<FSSODetSplit.refNbr, Equal<Required<FSSODetSplit.refNbr>>,
                                    And<FSSODetSplit.lineNbr, Equal<Required<FSSODetSplit.lineNbr>>,
                                    And<FSSODetSplit.pOCreate, Equal<False>,
                                    And<FSSODetSplit.completed, Equal<True>>>>>>>
                                .Select(graph, soDetLine.SrvOrdType, soDetLine.RefNbr, soDetLine.LineNbr);

                foreach (FSSODetSplit split in splitLines)
                {
                    var allocationInfo = new AllocationInfo(split, soDetLine);
                    requiredAllocationList.Add(allocationInfo);
                }
            }
            else
            {
                var relatedSrvOrdLine = FSSODet.UK.Find(graph, apptLine.SODetID);

                if (relatedSrvOrdLine == null)
                {
                    throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(FSSODet)));
                }

                var splitLines = PXSelect<FSApptLineSplit,
                                Where<
                                    FSApptLineSplit.srvOrdType, Equal<Required<FSApptLineSplit.srvOrdType>>,
                                    And<FSApptLineSplit.apptNbr, Equal<Required<FSApptLineSplit.apptNbr>>,
                                    And<FSApptLineSplit.lineNbr, Equal<Required<FSApptLineSplit.lineNbr>>>>>>
                                .Select(graph, apptLine.SrvOrdType, apptLine.RefNbr, apptLine.LineNbr);

                if (splitLines.Count > 0)
                {
                    foreach (FSApptLineSplit split in splitLines)
                    {
                        var allocationInfo = new AllocationInfo(split, relatedSrvOrdLine);
                        requiredAllocationList.Add(allocationInfo);
                    }
                }
                else
                {
                    // If the item has not Lot/Serial tracking
                    // and the Appointment line is not related to a ServiceOrder line marked for PO
                    // then the Appointment split has not detail about the allocation
                    var allocationInfo = new AllocationInfo(apptLine.BillableQty, relatedSrvOrdLine);
                    requiredAllocationList.Add(allocationInfo);
                }
            }
        }


        public virtual bool CanUpdateRealSplit(FSSODetSplit realSplit, FSSODetSplit forSplit)
        {
            if (realSplit.Completed == true)
            {
                return false;
            }

            if (string.IsNullOrEmpty(realSplit.LotSerialNbr) == false
                    && realSplit.LotSerialNbr != forSplit.LotSerialNbr)
            {
                return false;
            }

            if (realSplit.SiteID != null && realSplit.SiteID != forSplit.SiteID)
            {
                return false;
            }

            if (realSplit.LocationID != null && realSplit.LocationID != forSplit.LocationID)
            {
                return false;
            }

            return true;
        }

        public virtual decimal? DeallocateSplitLine(ServiceOrderEntry docgraph,
                                                                           FSSODet line,
                                                                           decimal? baseDeallocationQty,
                                                                           FSSODetSplit split)
        {
            if (baseDeallocationQty <= 0m)
            {
                return 0m;
            }

            PXRowUpdating cancel_handler = new PXRowUpdating((sender, e) => { e.Cancel = true; });
            docgraph.RowUpdating.AddHandler<FSSODet>(cancel_handler);

            PXCache splitsCache = docgraph.Splits.Cache;

            decimal? baseOpenQty = split.BaseQty - split.BaseShippedQty;
            decimal? baseNewSplitQty;
            if (baseOpenQty <= baseDeallocationQty)
            {
                baseNewSplitQty = 0m;
                split.BaseShippedQty += baseOpenQty;
                baseDeallocationQty -= baseOpenQty;
            }
            else
            {
                baseNewSplitQty = baseOpenQty - baseDeallocationQty;
                split.BaseQty = baseDeallocationQty;
                split.Qty = INUnitAttribute.ConvertFromBase(splitsCache, split.InventoryID, split.UOM, (decimal)split.BaseQty, INPrecision.QUANTITY);
                split.BaseShippedQty = baseDeallocationQty;
                baseDeallocationQty = 0;
            }

            split.ShippedQty = INUnitAttribute.ConvertFromBase(splitsCache, split.InventoryID, split.UOM, (decimal)split.BaseShippedQty, INPrecision.QUANTITY);
            split.Completed = true;

            INItemPlan plan = PXSelect<INItemPlan, Where<INItemPlan.planID, Equal<Required<INItemPlan.planID>>>>.Select(docgraph, split.PlanID);
            if (plan != null)
            {
                docgraph.Caches[typeof(INItemPlan)].Delete(plan);
                split.PlanID = null;
            }

            docgraph.lsFSSODetSelect.SuppressedMode = true;
            split = docgraph.Splits.Update(split);
            docgraph.lsFSSODetSelect.SuppressedMode = false;

            if (baseNewSplitQty > 0m)
            {
                if (split.POCreate == true)
                {
                    throw new PXInvalidOperationException();
                }

                FSSODetSplit newSplit = (FSSODetSplit)docgraph.Splits.Cache.CreateCopy(split);

                FSPOReceiptProcess.ClearScheduleReferences(ref newSplit);

                newSplit.POReceiptType = split.POReceiptType;
                newSplit.POReceiptNbr = split.POReceiptNbr;

                newSplit = (FSSODetSplit)docgraph.Splits.Cache.CreateCopy(docgraph.Splits.Insert(newSplit));

                newSplit.PlanType = split.PlanType;
                newSplit.IsAllocated = false;
                newSplit.ShipmentNbr = null;
                newSplit.LotSerialNbr = null;
                newSplit.VendorID = null;

                newSplit.BaseReceivedQty = 0m;
                newSplit.ReceivedQty = 0m;
                newSplit.BaseShippedQty = 0m;
                newSplit.ShippedQty = 0m;

                newSplit.BaseQty = baseNewSplitQty;
                newSplit.Qty = INUnitAttribute.ConvertFromBase(splitsCache, newSplit.InventoryID, newSplit.UOM, (decimal)newSplit.BaseQty, INPrecision.QUANTITY);

                docgraph.Splits.Update(newSplit);
            }


            docgraph.RowUpdating.RemoveHandler<FSSODet>(cancel_handler);
            ConfirmSingleLine(docgraph, split, line);

            return baseDeallocationQty;
        }

        public virtual void ConfirmSingleLine(ServiceOrderEntry docgraph, FSSODetSplit splitLine, FSSODet line)
        {
            FSSODet newLine = null;

            docgraph.lsFSSODetSelect.SuppressedMode = true;

            PXCache cache = docgraph.ServiceOrderDetails.Cache;

            newLine = (FSSODet)cache.CreateCopy(line);

            if (newLine.BaseShippedQty < newLine.BaseOrderQty && newLine.IsFree == false)
            {
                newLine.BaseShippedQty += splitLine.BaseShippedQty;
                newLine.ShippedQty += splitLine.ShippedQty;
                newLine.OpenQty = newLine.OrderQty - newLine.ShippedQty;
                newLine.BaseOpenQty = newLine.BaseOrderQty - newLine.BaseShippedQty;
                newLine.ClosedQty = newLine.ShippedQty;
                newLine.BaseClosedQty = newLine.BaseShippedQty;

                newLine.Completed = newLine.OpenQty == 0;

                cache.Update(newLine);
            }
            else
            {
                newLine.OpenQty = 0m;
                newLine.ClosedQty = newLine.OrderQty;
                newLine.ShippedQty += splitLine.ShippedQty;
                newLine.BaseOpenQty = newLine.BaseOrderQty - newLine.BaseShippedQty;
                newLine.BaseClosedQty = newLine.BaseOrderQty;
                newLine.Completed = true;

                cache.Update(newLine);
                docgraph.lsFSSODetSelect.CompleteSchedules(cache, line);
            }

            docgraph.lsFSSODetSelect.SuppressedMode = false;
        }

        public virtual void UpdateCurrentLineBasedOnSplit(ServiceOrderEntry graph)
        {
            FSSODet line = graph.ServiceOrderDetails.Current;

            if (line == null)
            {
                throw new ArgumentNullException();
            }

            line.OpenQty = line.OrderQty;
            line.BaseOpenQty = line.BaseOrderQty;
            line.ShippedQty = 0m;
            line.BaseShippedQty = 0m;
            line.Completed = false;

            foreach (FSSODetSplit split in graph.Splits.Select())
            {
                if (split.POCreate == true || split.Completed == false)
                {
                    continue;
                }

                line.OpenQty -= split.ShippedQty;
                line.BaseOpenQty -= split.BaseShippedQty;
                line.ShippedQty += split.ShippedQty;
                line.BaseShippedQty += split.BaseShippedQty;
            }

            line.Completed = line.OpenQty == 0;

            graph.ServiceOrderDetails.Update(line);
        }

        public class SplitKey : IEquatable<SplitKey>
        {
            public enum PlanStates
            {
                Null,
                Created
            }

            public DateTime? ShipDate;
            public DateTime? ExpireDate;
            public bool? IsAllocated;
            public bool? Completed;
            public int? InventoryID;
            public string UOM;
            public int? SiteID;
            public int? LocationID;
            public string LotSerialNbr;
            public Guid? RefNoteID;
            public string POReceiptType;
            public string POReceiptNbr;

            public PlanStates PlanState;

            protected StringBuilder KeyBuilder;
            protected string Key;

            public SplitKey(FSSODetSplit split)
            {
                KeyBuilder = new StringBuilder();

                ShipDate = split.ShipDate;
                AddKeyPart(ShipDate);

                ExpireDate = split.ExpireDate;
                AddKeyPart(ExpireDate);

                IsAllocated = split.IsAllocated;
                AddKeyPart(IsAllocated);

                Completed = split.Completed;
                AddKeyPart(Completed);

                InventoryID = split.InventoryID;
                AddKeyPart(InventoryID);

                UOM = split.UOM;
                AddKeyPart(UOM);

                SiteID = split.SiteID;
                AddKeyPart(SiteID);

                LocationID = split.LocationID;
                AddKeyPart(LocationID);

                LotSerialNbr = string.IsNullOrEmpty(split.LotSerialNbr) ? null : split.LotSerialNbr;
                AddKeyPart(LotSerialNbr);

                RefNoteID = split.RefNoteID;
                AddKeyPart(RefNoteID);

                POReceiptType = split.POReceiptType;
                AddKeyPart(POReceiptType);

                POReceiptNbr = split.POReceiptNbr;
                AddKeyPart(POReceiptNbr);

                if (split.PlanID == null)
                {
                    PlanState = PlanStates.Null;
                }
                else
                {
                    PlanState = PlanStates.Created;
                }
                AddKeyPart(PlanState);

                Key = KeyBuilder.ToString();
            }

            public bool Equals(SplitKey other)
            {
                if (ReferenceEquals(null, other))
                    return false;
                if (ReferenceEquals(this, other))
                    return true;

                if (this.GetHashCode() == other.GetHashCode())
                    return true;

                return false;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return Key.GetHashCode();
                }
            }

            protected virtual void AddKeyPart(object part)
            {
                if (part != null)
                {
                    KeyBuilder.Append(part.ToString());
                }

                KeyBuilder.Append("|");
            }
        }

        public virtual void MergeEqualSplitsForCurrentLine(ServiceOrderEntry graph, Func<FSSODetSplit, bool> filter)
        {
            List<FSSODetSplit> splits = graph.Splits.Select().
                    RowCast<FSSODetSplit>().
                    Where(s => s.POCreate == false
                            && filter.Invoke(s) == true).
                    OrderBy(s => s.SplitLineNbr).ToList();

            IEnumerable<IGrouping<SplitKey, FSSODetSplit>> splitGroups = splits.GroupBy(s => new SplitKey(s));

            PXCache planCache = graph.Caches[typeof(INItemPlan)];

            foreach (IGrouping<SplitKey, FSSODetSplit> splitGroup in splitGroups)
            {
                if (splitGroup.Count() == 1)
                {
                    continue;
                }

                FSSODetSplit sumSplit = splitGroup.First();

                foreach (FSSODetSplit beingDeleted in splitGroup)
                {
                    if (beingDeleted.SplitLineNbr == sumSplit.SplitLineNbr)
                    {
                        continue;
                    }

                    if (beingDeleted.PlanType != sumSplit.PlanType)
                    {
                        throw new PXInvalidOperationException();
                    }

                    if (beingDeleted.SplitLineNbr == sumSplit.ParentSplitLineNbr)
                    {
                        sumSplit.ParentSplitLineNbr = beingDeleted.ParentSplitLineNbr;
                    }

                    sumSplit.Qty += beingDeleted.Qty;
                    sumSplit.BaseQty += beingDeleted.BaseQty;

                    sumSplit.OpenQty += beingDeleted.OpenQty;
                    sumSplit.BaseOpenQty += beingDeleted.BaseOpenQty;

                    sumSplit.ShippedQty += beingDeleted.ShippedQty;
                    sumSplit.BaseShippedQty += beingDeleted.BaseShippedQty;

                    if (beingDeleted.PlanID != null)
                    {
                        DeletePlan(planCache, beingDeleted);
                    }

                    graph.Splits.Delete(beingDeleted);
                }

                sumSplit = graph.Splits.Update(sumSplit);

                if (sumSplit.PlanID != null)
                {
                    long? oldPlanID = sumSplit.PlanID;

                    DeletePlan(planCache, sumSplit);
                    CreateAndAssignPlan(graph.Splits.Cache, sumSplit);

                    long? newPlanID = sumSplit.PlanID;

                    if (newPlanID == oldPlanID)
                    {
                        throw new PXInvalidOperationException();
                    }

                    sumSplit = graph.Splits.Update(sumSplit);

                    if (sumSplit.PlanID != newPlanID)
                    {
                        throw new PXInvalidOperationException();
                    }
                }
            }
        }

        public virtual void CreateAndAssignPlan(PXCache splitCache, FSSODetSplit openSplit)
        {
            if (openSplit.PlanID != null)
            {
                throw new PXInvalidOperationException();
            }

            INItemPlan plan = INItemPlanIDAttribute.DefaultValues(splitCache, openSplit);

            if (plan != null)
            {
                plan = (INItemPlan)splitCache.Graph.Caches[typeof(INItemPlan)].Insert(plan);
                openSplit.PlanID = plan.PlanID;
            }
        }

        public virtual void DeletePlan(PXCache planCache, FSSODetSplit split)
        {
            var plan = PXSelect<INItemPlan,
                    Where<INItemPlan.planID, Equal<Required<INItemPlan.planID>>>>.
                    Select(planCache.Graph, split.PlanID);

            if (plan == null)
            {
                throw new PXException(TX.Error.RECORD_X_NOT_FOUND, DACHelper.GetDisplayName(typeof(INItemPlan)));
            }

            planCache.Delete(plan);

            split.PlanID = null;
        }
    }
}
