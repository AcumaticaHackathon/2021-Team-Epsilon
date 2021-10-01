using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.SO.GraphExtensions.SOOrderEntryExt
{
	[Obsolete(Common.Messages.ClassIsObsolete + " Use " + nameof(DropShipLinkDialog) + " instead.")]
	public class DropShipLegacyDialog : PXGraphExtension<POLinkDialog, PurchaseSupplyBaseExt, SOOrderEntry>
	{
		private struct POSupplyResult
		{
			public string POOrderType;
			public string POOrderNbr;
			public int? POLineNbr;
			public POLine3 POLine;
			public POOrder POOrder;
			public List<SOLineSplit> CurrentSOLineSplits;
			public List<SOLineSplit> ForeignSOLineSplits;
		}

		public static bool IsActive()
		{
			// Legacy PO Link dialog implementation is required for processing legacy drop-ships only.
			return PXAccess.FeatureInstalled<CS.FeaturesSet.dropShipments>();
		}

		public PXSelect<SOLine,
			Where<SOLine.orderType, Equal<Optional<SOLine.orderType>>,
				And<SOLine.orderNbr, Equal<Optional<SOLine.orderNbr>>,
				And<SOLine.lineNbr, Equal<Optional<SOLine.lineNbr>>>>>> currentposupply;
		
		[PXCopyPasteHiddenView()]
		public PXSelect<POLine3> posupply;

		[PXCopyPasteHiddenView()]
		public PXSelect<POOrder> poorderlink;

		public virtual IEnumerable POSupply()
		{
			SOLine currentSOLine = (SOLine)currentposupply.Select();

			List<POLine3> ret = new List<POLine3>();
			if (currentSOLine == null || currentSOLine.POSource != INReplenishmentSource.DropShipToOrder
				|| currentSOLine.IsLegacyDropShip != true)
				return ret;

			List<POSupplyResult> mergedResults = new List<POSupplyResult>();

			foreach (PXResult<POLine3, POOrder, SOLineSplit> res in
				PXSelectReadonly2<POLine3,
				InnerJoin<POOrder,
					On<POOrder.orderNbr, Equal<POLine3.orderNbr>, And<POOrder.orderType, Equal<POLine3.orderType>>>,
				LeftJoin<SOLineSplit,
					On<SOLineSplit.pOType, Equal<POLine3.orderType>,
				And<SOLineSplit.pONbr, Equal<POLine3.orderNbr>,
				And<SOLineSplit.pOLineNbr, Equal<POLine3.lineNbr>>>>>>,
				Where2<
					Where<Current<SOLine.pOSource>, Equal<INReplenishmentSource.purchaseToOrder>,
								And<POLine3.orderType, In3<POOrderType.regularOrder, POOrderType.blanket>,
							Or<Current<SOLine.pOSource>, Equal<INReplenishmentSource.dropShipToOrder>,
								And<Where<POLine3.orderType, Equal<POOrderType.dropShip>,
									And2<Where<Current<SOLine.customerID>, Equal<POOrder.shipToBAccountID>,
										Or<POOrder.shipDestType, NotEqual<POShippingDestination.customer>>>,
									Or<POLine3.orderType, Equal<POOrderType.blanket>>>>>>>>,
				And<POLine3.lineType, In3<POLineType.goodsForInventory,
					POLineType.nonStock,
					POLineType.goodsForDropShip,
					POLineType.nonStockForDropShip,
					POLineType.goodsForSalesOrder,
					POLineType.goodsForServiceOrder,
					POLineType.nonStockForSalesOrder,
					POLineType.nonStockForServiceOrder,
					POLineType.goodsForReplenishment>,
				And<POLine3.inventoryID, Equal<Current<SOLine.inventoryID>>,
				And2<Where<Current<SOLine.subItemID>, IsNull,
						Or<POLine3.subItemID, Equal<Current<SOLine.subItemID>>>>,
				And<POLine3.siteID, Equal<Current<SOLine.pOSiteID>>,
				And2<Where<Current<SOLine.vendorID>, IsNull,
					Or<POLine3.vendorID, Equal<Current<SOLine.vendorID>>>>,
				And<Current2<SOLine.pOSource>, IsNotNull,
				And<POOrder.isLegacyDropShip, Equal<boolTrue>>>>>>>>>>.SelectMultiBound(Base, new object[] { currentSOLine }))
			{
				POLine3 supply = PXCache<POLine3>.CreateCopy(res);
				POOrder poorder = (POOrder)Base.Caches[typeof(POOrder)].CreateCopy(Base.Caches[typeof(POOrder)].Locate((POOrder)res)) ?? res;
				SOLineSplit split = PXCache<SOLineSplit>.CreateCopy(res);
				SOLineSplit foreignsplit = new SOLineSplit();

				SOLineSplit selectedSplitCached = (SOLineSplit)Base.Caches[typeof(SOLineSplit)].Locate((SOLineSplit)res);

				if (selectedSplitCached != null)
				{
					if (selectedSplitCached.PONbr == null || Base.Caches[typeof(SOLineSplit)].GetStatus(selectedSplitCached) == PXEntryStatus.Deleted
						|| selectedSplitCached.POType != supply.OrderType || selectedSplitCached.PONbr != supply.OrderNbr || selectedSplitCached.POLineNbr != supply.LineNbr)
					{
						// Selected split found in cache, but was deleted or linked to another POLine.
						split = new SOLineSplit();
					}
					else
					{
						// Selected split found in cache, replace selected plan with cached plan.
						split = (SOLineSplit)Base.Caches[typeof(SOLineSplit)].CreateCopy(selectedSplitCached);
					}
				}

				if (split.PONbr == null)
				{
					split = new SOLineSplit();
				}
				else if (split.OrderType != currentSOLine.OrderType || split.OrderNbr != currentSOLine.OrderNbr || split.LineNbr != currentSOLine.LineNbr)
				{
					foreignsplit = (SOLineSplit)Base.Caches[typeof(SOLineSplit)].CreateCopy(split);
					split = new SOLineSplit();
				}

				POSupplyResult result = new POSupplyResult
				{
					POOrderType = supply.OrderType,
					POOrderNbr = supply.OrderNbr,
					POLineNbr = supply.LineNbr,
					POLine = supply,
					POOrder = poorder,
					CurrentSOLineSplits = split.SplitLineNbr != null ? new List<SOLineSplit> { split } : new List<SOLineSplit> { },
					ForeignSOLineSplits = foreignsplit.SplitLineNbr != null ? new List<SOLineSplit> { foreignsplit } : new List<SOLineSplit> { }
				};

				POSupplyResult existingResult = mergedResults.FirstOrDefault(x => x.POOrderType == result.POOrderType && x.POOrderNbr == result.POOrderNbr && x.POLineNbr == result.POLineNbr);
				if (existingResult.POOrderNbr != null)
				{
					if (!existingResult.CurrentSOLineSplits.Any(x => x.OrderType == split.OrderType && x.OrderNbr == split.OrderNbr && x.LineNbr == split.LineNbr && x.SplitLineNbr == split.SplitLineNbr))
					{
						existingResult.CurrentSOLineSplits.Add(split);
					}
					if (!existingResult.ForeignSOLineSplits.Any(x => x.OrderType == foreignsplit.OrderType && x.OrderNbr == foreignsplit.OrderNbr && x.LineNbr == foreignsplit.LineNbr && x.SplitLineNbr == foreignsplit.SplitLineNbr))
					{
						existingResult.ForeignSOLineSplits.Add(foreignsplit);
					}
				}
				else
				{
					mergedResults.Add(result);
				}
			}

			bool allSplitsCompleted = true;
			foreach (SOLineSplit split in Base.splits.Select())
			{
				if (split.Completed != true)
				{
					allSplitsCompleted = false;
					break;
				}
			}
			//searching for other matching splits in cache and checking if all splits completed
			foreach (SOLineSplit splitFromCache in PXSelect<SOLineSplit,
														Where<SOLineSplit.orderType, Equal<Required<SOLineSplit.orderType>>,
															And<SOLineSplit.orderNbr, Equal<Required<SOLineSplit.orderNbr>>>>>
														.Select(Base, currentSOLine.OrderType, currentSOLine.OrderNbr)
														.Where(x => ((SOLineSplit)x).PONbr != null))
			{
				POSupplyResult existingResult = mergedResults.FirstOrDefault(
					x => x.POOrderType == splitFromCache.POType && x.POOrderNbr == splitFromCache.PONbr && x.POLineNbr == splitFromCache.POLineNbr);
				if (existingResult.POOrderNbr != null)
				{
					if (splitFromCache.LineNbr == currentSOLine.LineNbr)
					{
						//matching splits for current SOLine
						if (!existingResult.CurrentSOLineSplits.Any(x => x.SplitLineNbr == splitFromCache.SplitLineNbr))
						{
							existingResult.CurrentSOLineSplits.Add((SOLineSplit)Base.Caches[typeof(SOLineSplit)].CreateCopy(splitFromCache));
						}
					}
					else
					{
						//matching splits for other SOLines
						if (!existingResult.ForeignSOLineSplits.Any(x => x.LineNbr == splitFromCache.LineNbr && x.SplitLineNbr == splitFromCache.SplitLineNbr))
						{
							existingResult.ForeignSOLineSplits.Add((SOLineSplit)Base.Caches[typeof(SOLineSplit)].CreateCopy(splitFromCache));
						}
					}
				}
			}

			foreach (POSupplyResult res in mergedResults)
			{
				POLine3 supply = PXCache<POLine3>.CreateCopy(res.POLine);
				POOrder poorder = res.POOrder;

				decimal demandQty = 0m;
				SOLineSplit linkWithCurrentSOLine = null;
				foreach (SOLineSplit split in res.CurrentSOLineSplits)
				{
					if (split.PONbr != null)
					{
						if (split.PlanID != null && split.POCompleted != true)
							demandQty += (split.BaseQty ?? 0m) - (split.BaseReceivedQty ?? 0m);
						if (linkWithCurrentSOLine == null)
							linkWithCurrentSOLine = split;
					}
				}
				bool linkedWithCurrentSOLine = linkWithCurrentSOLine != null;
				bool linkedWithForeignSOLines = false;

				foreach (SOLineSplit foreignsplit in res.ForeignSOLineSplits)
				{
					if (foreignsplit.PONbr != null)
					{
						if (foreignsplit.PlanID != null && foreignsplit.POCompleted != true)
							demandQty += (foreignsplit.BaseQty ?? 0m) - (foreignsplit.BaseReceivedQty ?? 0m);
						linkedWithForeignSOLines = true;
					}
				}

				if (currentSOLine.POSource == INReplenishmentSource.DropShipToOrder &&
					  supply.OrderType == PO.POOrderType.RegularOrder) continue;

				if (!linkedWithCurrentSOLine
					&& (poorder.Hold == true && !Base.SOPOLinkShowDocumentsOnHold || (allSplitsCompleted || currentSOLine.Completed == true)))
					continue;

				if (linkedWithCurrentSOLine
					|| supply.OrderType != PO.POOrderType.DropShip && supply.Completed == false && supply.Cancelled == false && supply.BaseOpenQty - demandQty > 0m
					|| supply.OrderType == PO.POOrderType.DropShip && !linkedWithForeignSOLines &&
						(supply.Completed == false && supply.Cancelled == false && supply.BaseOrderQty >= 0m || supply.BaseReceivedQty > 0m))
				{
					// Records may be marked updated from UI by checking "selected" checkbox.
					POLine3 cachedSupply = posupply.Locate(supply);
					if (cachedSupply == null)
					{
						// New record should be stored to cache to preserve unbound fields.
						posupply.Cache.Hold(supply);
						cachedSupply = supply;
					}

					if (cachedSupply.SOOrderType != currentSOLine.OrderType
						|| cachedSupply.SOOrderNbr != currentSOLine.OrderNbr
						|| cachedSupply.SOOrderLineNbr != currentSOLine.LineNbr)
					{
						// Preserve user input on Grid refresh/filter update.
						cachedSupply.Selected = linkedWithCurrentSOLine;
					}

					cachedSupply.SOOrderType = currentSOLine.OrderType;
					cachedSupply.SOOrderNbr = currentSOLine.OrderNbr;
					cachedSupply.SOOrderLineNbr = currentSOLine.LineNbr;
					cachedSupply.SOOrderSplitLineNbr = linkedWithCurrentSOLine ? linkWithCurrentSOLine.SplitLineNbr : null;
					cachedSupply.LinkedToCurrentSOLine = linkedWithCurrentSOLine;
					cachedSupply.VendorRefNbr = poorder.VendorRefNbr;
					cachedSupply.DemandQty = demandQty;

					ret.Add(cachedSupply);
				}
			}
			return ret;
		}

		protected virtual void SOLine_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			SOLine row = (SOLine)e.Row;
			if (row == null)
				return;

			// Switching between new and legacy grids in POLink dialog.
			Base2.SupplyPOLines.AllowSelect = row.IsLegacyDropShip != true;
			posupply.AllowSelect = row.IsLegacyDropShip == true;

			if (row.POSource != INReplenishmentSource.DropShipToOrder || row.IsLegacyDropShip != true)
				return;

			bool editable = false;
			if (Base1.IsPOCreateEnabled(row) && row.POCreate == true)
			{
				ARTran tran = PXSelect<ARTran,
					Where<ARTran.sOOrderType, Equal<Required<ARTran.sOOrderType>>,
						And<ARTran.sOOrderNbr, Equal<Required<ARTran.sOOrderNbr>>,
						And<ARTran.sOOrderLineNbr, Equal<Required<ARTran.sOOrderLineNbr>>>>>>
					.SelectWindowed(Base, 0, 1, row.OrderType, row.OrderNbr, row.LineNbr);

				editable = (tran == null);
			}

			PXUIFieldAttribute.SetEnabled<POLine3.selected>(posupply.Cache, null, editable);
		}

		protected virtual void POLine3_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			//TODO: change to warnings and disable selection
			//if (supply.OrderType != PO.POOrderType.DropShip &&
			//        (supply.BaseOpenQty - demand.PlanQty > 0m && soline.ShipComplete != SOShipComplete.ShipComplete || supply.BaseOpenQty - demand.PlanQty >= soline.BaseOrderQty * soline.CompleteQtyMin / 100m) &&
			//        supply.Completed == false && supply.Cancelled == false ||
			//    supply.OrderType == PO.POOrderType.DropShip && IsLinkedToSO == false && (supply.Completed == false &&
			//        (supply.BaseOrderQty >= soline.BaseOrderQty * soline.CompleteQtyMin / 100m || soline.ShipComplete != SOShipComplete.ShipComplete) && supply.BaseOrderQty <= soline.BaseOrderQty * soline.CompleteQtyMax / 100m ||
			//        (supply.BaseReceivedQty >= soline.BaseOrderQty * soline.CompleteQtyMin / 100m || soline.ShipComplete == SOShipComplete.CancelRemainder) && supply.BaseReceivedQty <= soline.BaseOrderQty * soline.CompleteQtyMax / 100m) ||
			//    link != null)
			POLine3 supply = e.Row as POLine3;
			if (supply == null)
				return;

			SOLine soline = PXSelect<SOLine,
				Where<SOLine.orderType, Equal<Current<POLine3.sOOrderType>>,
					And<SOLine.orderNbr, Equal<Current<POLine3.sOOrderNbr>>,
					And<SOLine.lineNbr, Equal<Current<POLine3.sOOrderLineNbr>>>>>>
				.Select(Base);

			bool RegularOrderMinMax = supply != null && soline != null && supply.OrderType != PO.POOrderType.DropShip &&
					(supply.BaseOpenQty - supply.DemandQty > 0m && soline.ShipComplete != SOShipComplete.ShipComplete || supply.BaseOpenQty - supply.DemandQty >= soline.BaseOrderQty * soline.CompleteQtyMin / 100m);

			bool DropShipOrderMinMax = supply != null && soline != null && supply.OrderType == PO.POOrderType.DropShip &&
					(supply.Completed == false && (supply.BaseOrderQty >= soline.BaseOpenQty * soline.CompleteQtyMin / 100m || soline.ShipComplete != SOShipComplete.ShipComplete) && supply.BaseOrderQty <= soline.BaseOpenQty * soline.CompleteQtyMax / 100m ||
					(supply.BaseReceivedQty >= soline.BaseOpenQty * soline.CompleteQtyMin / 100m || soline.ShipComplete == SOShipComplete.CancelRemainder) && supply.BaseReceivedQty <= soline.BaseOpenQty * soline.CompleteQtyMax / 100m);

			bool PartiallyReceipted = supply != null && soline != null && supply.Selected == true && supply.BaseOrderQty - supply.BaseOpenQty > 0 && supply.LinkedToCurrentSOLine == true;

			PXUIFieldAttribute.SetEnabled<POLine3.selected>(sender, e.Row, (RegularOrderMinMax || DropShipOrderMinMax || supply.LinkedToCurrentSOLine == true) && !PartiallyReceipted);

			if (PartiallyReceipted)
				PXUIFieldAttribute.SetWarning<POLine3.selected>(sender, e.Row, Messages.PurchaseOrderCannotBeDeselected);
		}

		[PXOverride]
		public virtual void POSupplyDialogInitializer(PXGraph graph, string viewName)
		{
			foreach (POLine3 supply in posupply.Cache.Updated)
			{
				// We should not preserve user input if dialog was closed without saving.
				supply.Selected = false;
				supply.SOOrderType = null;
				supply.SOOrderNbr = null;
				supply.SOOrderLineNbr = null;
			}
		}

		[PXOverride]
		public virtual void LinkPOSupply(SOLine currentSOLine)
		{
			if (currentSOLine.POSource != INReplenishmentSource.DropShipToOrder || currentSOLine.IsLegacyDropShip != true)
				return;

			LinkSupplyDemand();
		}

		public virtual void LinkSupplyDemand()
		{
			List<SOLineSplit> splits = Base.splits.Select().RowCast<SOLineSplit>().ToList();

			//unlink first
			bool removedLink = false;
			foreach (POLine3 supply in posupply.Cache.Updated)
			{
				SOLine line = (SOLine)currentposupply.Select(supply.SOOrderType, supply.SOOrderNbr, supply.SOOrderLineNbr);

				line = PXCache<SOLine>.CreateCopy(line);

				if (supply.Selected == false && supply.LinkedToCurrentSOLine == true)
				{
					foreach (SOLineSplit split in splits)
					{
						if (Base.splits.Cache.GetStatus(split) == PXEntryStatus.Deleted || Base.splits.Cache.GetStatus(split) == PXEntryStatus.InsertedDeleted)
							continue;

						if (split.POType == supply.OrderType && split.PONbr == supply.OrderNbr && split.POLineNbr == supply.LineNbr &&
							split.POCompleted == false && split.Completed == false || supply.OrderType == POOrderType.DropShip)
						{
							if (split.POType != null && split.PONbr != null && split.POType == supply.OrderType && split.PONbr == supply.OrderNbr)
							{
								POOrder poorder = PXSelect<POOrder, Where<POOrder.orderType, Equal<Required<POOrder.orderType>>,
										And<POOrder.orderNbr, Equal<Required<POOrder.orderNbr>>>>>.Select(Base, supply.OrderType, supply.OrderNbr);

								if (poorder != null)
								{
									if (split.RefNoteID == poorder.NoteID)
										split.RefNoteID = null;
									if (poorder.SOOrderType == split.OrderType && poorder.SOOrderNbr == split.OrderNbr)
									{
										poorder.SOOrderType = null;
										poorder.SOOrderNbr = null;
										poorderlink.Update(poorder);
									}
								}
							}
							if (split.PONbr != null)
							{
								split.ClearPOReferences();
								split.POCompleted = false;
								removedLink = true;
							}

							split.ReceivedQty = 0m;
							split.ShippedQty = 0m;
							split.Completed = false;

							Base.splits.Update(split);

							if (supply.OrderType == PO.POOrderType.DropShip)
							{
								line.ShippedQty = 0m;
								line.UnbilledQty = line.OrderQty;
								line.OpenQty = line.OrderQty;
								line.ClosedQty = 0m;
								line.Completed = false;

								Base.Transactions.Update(line);
							}
						}
					}
					supply.SOOrderSplitLineNbr = null;
					supply.LinkedToCurrentSOLine = false;
				}
			}

			//then link
			bool addedLink = false;
			bool poLineCompleted = false;
			foreach (POLine3 supply in posupply.Cache.Updated)
			{
				SOLine line = (SOLine)currentposupply.Select(supply.SOOrderType, supply.SOOrderNbr, supply.SOOrderLineNbr);

				line = PXCache<SOLine>.CreateCopy(line);

				if (supply.Selected == true && supply.LinkedToCurrentSOLine != true)
				{
					LinkToSuplyLine(line, splits, supply, ref addedLink, ref poLineCompleted);
				}
			}

			var soLine = Base.Transactions.Current;
			if (addedLink)
			{
				if (soLine.POCreated != true)
					Base.Transactions.Cache.SetValue<SOLine.pOCreated>(soLine, true);

				if (poLineCompleted)
					Base.lsselect.CompleteSchedules(Base.lsselect.Cache, soLine);
			}
			else if (removedLink)
			{
				if (soLine.POCreated == true)
				{
					var linked = splits.Any(x => x.POCreate == true && x.PONbr != null);
					if (!linked)
						Base.Transactions.Cache.SetValue<SOLine.pOCreated>(soLine, linked);
				}
			}
		}

		public virtual void LinkToSuplyLine(SOLine line, List<SOLineSplit> splits, POLine3 supply, ref bool addedLink, ref bool poLineCompleted)
		{
			decimal? BaseOpenQty = supply.BaseOrderQty - supply.DemandQty;

			for (int i = 0; i < splits.Count; i++)
			{
				SOLineSplit split = PXCache<SOLineSplit>.CreateCopy(splits[i]);

				//TODO: it should not be possible to unallocate TR schedules
				if (string.IsNullOrEmpty(split.SOOrderNbr) && string.IsNullOrEmpty(split.PONbr) && split.IsAllocated == false && split.Completed == false && split.BaseQty > 0m)
				{
					if (supply.OrderType != POOrderType.Blanket)
					{
						supply.LineType =
							(line.POSource == INReplenishmentSource.DropShipToOrder) ?
							(line.LineType == SOLineType.Inventory ? POLineType.GoodsForDropShip : POLineType.NonStockForDropShip) :
							(line.LineType == SOLineType.Inventory ? POLineType.GoodsForSalesOrder : POLineType.NonStockForSalesOrder);
					}

					supply.SOOrderSplitLineNbr = split.SplitLineNbr;
					supply.LinkedToCurrentSOLine = true;

					INItemPlan plan;
					if (supply.Completed == false)
					{
						plan = PXSelect<INItemPlan, Where<INItemPlan.planID, Equal<Required<INItemPlan.planID>>>>.Select(Base, supply.PlanID);
						if (plan == null) continue;
						if (supply.OrderType != PX.Objects.PO.POOrderType.Blanket)
						{
							plan = PXCache<INItemPlan>.CreateCopy(plan);
							plan.PlanType = (line.POSource == INReplenishmentSource.DropShipToOrder ? INPlanConstants.Plan74 : INPlanConstants.Plan76);
							Base.Caches[typeof(INItemPlan)].Update(plan);
						}

						if (supply.OrderType == PO.POOrderType.DropShip)
						{
							POReceiptLine receipted = PXSelectJoinGroupBy<POReceiptLine,
								InnerJoin<POReceipt, On<POReceipt.receiptType, Equal<POReceiptLine.receiptType>, And<POReceipt.receiptNbr, Equal<POReceiptLine.receiptNbr>>>>,
								Where<POReceiptLine.pOType, Equal<Current<POLine3.orderType>>,
								And<POReceiptLine.pONbr, Equal<Current<POLine3.orderNbr>>,
								And<POReceiptLine.pOLineNbr, Equal<Current<POLine3.lineNbr>>,
								And<POReceipt.released, Equal<True>>>>>,
								Aggregate<Sum<POReceiptLine.baseReceiptQty>>>.SelectSingleBound(Base, new object[] { supply });

							split.BaseShippedQty = receipted.BaseReceiptQty ?? 0m;
							PXDBQuantityAttribute.CalcTranQty<SOLineSplit.shippedQty>(Base.splits.Cache, split);
							split.OpenQty = (split.Qty - split.ShippedQty);

							line.BaseShippedQty += split.BaseShippedQty;
							PXDBQuantityAttribute.CalcTranQty<SOLine.shippedQty>(Base.Transactions.Cache, line);
							line.OpenQty = (line.OrderQty - line.ShippedQty);
							line.ClosedQty = line.ShippedQty;

							Base.Transactions.Update(line);
						}
					}

					plan = PXSelect<INItemPlan, Where<INItemPlan.planID, Equal<Required<INItemPlan.planID>>>>.Select(Base, split.PlanID);

					if (supply.OrderType == PO.POOrderType.DropShip)
					{
						foreach (PXResult<POReceiptLine, POReceipt> res in PXSelectJoin<POReceiptLine,
							InnerJoin<POReceipt,
								On<POReceipt.receiptType, Equal<POReceiptLine.receiptType>,
								And<POReceipt.receiptNbr, Equal<POReceiptLine.receiptNbr>,
								And<POReceipt.released, Equal<True>>>>>,
							Where<POReceiptLine.pOType, Equal<Required<POReceiptLine.pOType>>,
								And<POReceiptLine.pONbr, Equal<Required<POReceiptLine.pONbr>>,
								And<POReceiptLine.pOLineNbr, Equal<Required<POReceiptLine.pOLineNbr>>>>>>.Select(Base, supply.OrderType, supply.OrderNbr, supply.LineNbr))
						{
							POReceiptLine porl = res;
							SOOrderShipment os = Base.shipmentlist.Select().Where(s => ((SOOrderShipment)s).ShipmentNbr == porl.ReceiptNbr).FirstOrDefault();
							if (os == null)
							{
								os = new SOOrderShipment();
								os.OrderType = Base.Document.Current.OrderType;
								os.OrderNbr = Base.Document.Current.OrderNbr;
								os.ShipAddressID = Base.Document.Current.ShipAddressID;
								os.ShipContactID = Base.Document.Current.ShipContactID;
								os.ShipmentType = INDocType.DropShip;
								os.ShipmentNbr = porl.ReceiptNbr;
								os.ShippingRefNoteID = ((POReceipt)res).NoteID;
								os.Operation = SOOperation.Issue;
								os.ShipDate = porl.ReceiptDate;
								os.CustomerID = Base.Document.Current.CustomerID;
								os.CustomerLocationID = Base.Document.Current.CustomerLocationID;
								os.SiteID = null;
								os.ShipmentWeight = porl.ExtWeight;
								os.ShipmentVolume = porl.ExtVolume;
								os.ShipmentQty = porl.ReceiptQty;
								os.LineTotal = 0m;
								os.Confirmed = true;
								os.CreateINDoc = true;

								os.OrderType = Base.Document.Current.OrderType;
								os.OrderNbr = Base.Document.Current.OrderNbr;
								os.OrderNoteID = Base.Document.Current.NoteID;
								Base.shipmentlist.Insert(os);

								Base.Document.Current.ShipmentCntr++;
							}
							else
							{
								os.ShipmentWeight += porl.ExtWeight;
								os.ShipmentVolume += porl.ExtVolume;
								os.ShipmentQty += porl.ReceiptQty;
								Base.shipmentlist.Update(os);
							}
						}


						if (supply.Completed == true)
						{
							Base.Caches[typeof(INItemPlan)].Delete(plan);
							plan = null;

							split.BaseShippedQty = supply.BaseReceivedQty;
							PXDBQuantityAttribute.CalcTranQty<SOLineSplit.shippedQty>(Base.splits.Cache, split);
							split.Completed = true;
							split.PlanID = null;

							line.BaseShippedQty += split.BaseShippedQty;
							PXDBQuantityAttribute.CalcTranQty<SOLine.shippedQty>(Base.Transactions.Cache, line);
							line.UnbilledQty -= (line.OrderQty - line.ShippedQty);
							line.OpenQty = 0m;
							line.ClosedQty = line.OrderQty;
							line.Completed = true;
							poLineCompleted = true;

							using (Base.lsselect.SuppressedModeScope(true))
								Base.Transactions.Update(line);
						}

						if (plan != null && plan.SupplyPlanID == null)
						{
							plan = PXCache<INItemPlan>.CreateCopy(plan);
							plan.SupplyPlanID = supply.PlanID;
							Base.Caches[typeof(INItemPlan)].Update(plan);
						}
					}
					else
					{
						plan = PXCache<INItemPlan>.CreateCopy(plan);

						plan.PlanType = (supply.OrderType == PO.POOrderType.Blanket) ?
							(line.POSource == INReplenishmentSource.PurchaseToOrder ? INPlanConstants.Plan6B : INPlanConstants.Plan6E) :
							(line.POSource == INReplenishmentSource.DropShipToOrder ? INPlanConstants.Plan6D : INPlanConstants.Plan66);

						plan.FixedSource = INReplenishmentSource.Purchased;
						plan.SupplyPlanID = supply.PlanID;

						POOrder poorder = PXSelect<POOrder, Where<POOrder.orderType, Equal<Required<POOrder.orderType>>,
								And<POOrder.orderNbr, Equal<Required<POOrder.orderNbr>>>>>.Select(Base, supply.OrderType, supply.OrderNbr);

						if (poorder != null)
						{
							plan.VendorID = poorder.VendorID;
							plan.VendorLocationID = poorder.VendorLocationID;
						}
						Base.Caches[typeof(INItemPlan)].Update(plan);
					}

					split.POCreate = true;
					split.VendorID = supply.VendorID;
					split.POType = supply.OrderType;
					split.PONbr = supply.OrderNbr;
					split.POLineNbr = supply.LineNbr;
					addedLink = true;

					if (split.BaseQty <= BaseOpenQty)
					{
						BaseOpenQty -= split.BaseQty;

						split = Base.splits.Update(split);
					}
					else
					{
						SOLineSplit copy = PXCache<SOLineSplit>.CreateCopy(split);

						copy.SplitLineNbr = null;
						copy.IsAllocated = false;

						copy.ClearPOFlags();
						copy.ClearPOReferences();
						copy.VendorID = null;
						copy.POCreate = true;

						copy.BaseQty = copy.BaseQty - BaseOpenQty;
						copy.Qty = INUnitAttribute.ConvertFromBase(Base.splits.Cache, copy.InventoryID, copy.UOM, (decimal)copy.BaseQty, INPrecision.QUANTITY);
						copy.ShippedQty = 0m;
						copy.ReceivedQty = 0m;
						copy.UnreceivedQty = copy.BaseQty;
						copy.PlanID = null;
						copy.Completed = false;

						split.BaseQty = BaseOpenQty;
						split.Qty = INUnitAttribute.ConvertFromBase(Base.splits.Cache, split.InventoryID, split.UOM, (decimal)split.BaseQty, INPrecision.QUANTITY);
						BaseOpenQty = 0m;
						split = Base.splits.Update(split);

						if ((copy = Base.splits.Insert(copy)) != null)
						{
							splits.Insert(i + 1, copy);
						}
					}
					splits[i] = split;
				}

				if (BaseOpenQty <= 0m) break;
			}
		}
	}
}

namespace PX.Objects.SO
{
	[Serializable()]
	[PXProjection(typeof(Select<POLine>), Persistent = true)]
	[Obsolete(Common.Messages.ClassIsObsolete + " Use " + nameof(SupplyPOLine) + " instead.")]
	public partial class POLine3 : PX.Data.IBqlTable, ISortOrder
	{
		#region Selected
		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }
		protected bool? _Selected = false;
		[PXBool]
		[PXDefault(false)]
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
		#region OrderType
		public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }
		protected String _OrderType;
		[PXDBString(2, IsKey = true, IsFixed = true, BqlField = typeof(POLine.orderType))]
		[PXDefault()]
		[PXUIField(DisplayName = "PO Type", Enabled = false)]
		[PO.POOrderType.List()]
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
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "", BqlField = typeof(POLine.orderNbr))]
		[PXDefault()]
		[PXUIField(DisplayName = "PO Nbr.", Enabled = false)]
		[PXSelector(typeof(Search<POOrder.orderNbr, Where<POOrder.orderType, Equal<Current<POLine3.orderType>>>>), DescriptionField = typeof(POOrder.orderDesc))]
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
		#region VendorRefNbr
		public abstract class vendorRefNbr : PX.Data.BQL.BqlString.Field<vendorRefNbr> { }
		protected String _VendorRefNbr;
		[PXString(40)]
		[PXUIField(DisplayName = "Vendor Ref.", Enabled = false)]
		public virtual String VendorRefNbr
		{
			get
			{
				return this._VendorRefNbr;
			}
			set
			{
				this._VendorRefNbr = value;
			}
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		protected Int32? _LineNbr;
		[PXDBInt(IsKey = true, BqlField = typeof(POLine.lineNbr))]
		[PXDefault()]
		[PXUIField(DisplayName = "PO Line Nbr.", Visible = false)]
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
		#region SortOrder
		public abstract class sortOrder : PX.Data.BQL.BqlInt.Field<sortOrder> { }
		protected Int32? _SortOrder;
		[PXDBInt(BqlField = typeof(POLine.sortOrder))]
		public virtual Int32? SortOrder
		{
			get
			{
				return this._SortOrder;
			}
			set
			{
				this._SortOrder = value;
			}
		}
		#endregion
		#region LineType
		public abstract class lineType : PX.Data.BQL.BqlString.Field<lineType> { }
		protected String _LineType;
		[PXDBString(2, IsFixed = true, BqlField = typeof(POLine.lineType))]
		[PO.POLineType.List()]
		[PXUIField(DisplayName = "Line Type", Enabled = false)]
		public virtual String LineType
		{
			get
			{
				return this._LineType;
			}
			set
			{
				this._LineType = value;
			}
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		protected Int32? _InventoryID;
		[Inventory(Filterable = true, BqlField = typeof(POLine.inventoryID), Enabled = false)]
		public virtual Int32? InventoryID
		{
			get
			{
				return this._InventoryID;
			}
			set
			{
				this._InventoryID = value;
			}
		}
		#endregion
		#region SubItemID
		public abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
		protected Int32? _SubItemID;
		[SubItem(BqlField = typeof(POLine.subItemID), Enabled = false)]
		public virtual Int32? SubItemID
		{
			get
			{
				return this._SubItemID;
			}
			set
			{
				this._SubItemID = value;
			}
		}
		#endregion
		#region PlanID
		public abstract class planID : PX.Data.BQL.BqlLong.Field<planID> { }
		protected Int64? _PlanID;
		[PXDBLong(BqlField = typeof(POLine.planID))]
		[PXUIField(Visible = false, Enabled = false)]
		public virtual Int64? PlanID
		{
			get
			{
				return this._PlanID;
			}
			set
			{
				this._PlanID = value;
			}
		}
		#endregion
		#region VendorID
		public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		protected Int32? _VendorID;
		[AP.Vendor(typeof(Search<BAccountR.bAccountID,
			Where<Vendor.type, NotEqual<BAccountType.employeeType>>>),
			BqlField = typeof(POLine.vendorID), Enabled = false)]
		public virtual Int32? VendorID
		{
			get
			{
				return this._VendorID;
			}
			set
			{
				this._VendorID = value;
			}
		}
		#endregion
		#region OrderDate
		public abstract class orderDate : PX.Data.BQL.BqlDateTime.Field<orderDate> { }
		protected DateTime? _OrderDate;
		[PXDBDate(BqlField = typeof(POLine.orderDate))]
		[PXUIField(DisplayName = "Order Date", Enabled = false)]
		public virtual DateTime? OrderDate
		{
			get
			{
				return this._OrderDate;
			}
			set
			{
				this._OrderDate = value;
			}
		}
		#endregion
		#region PromisedDate
		public abstract class promisedDate : PX.Data.BQL.BqlDateTime.Field<promisedDate> { }
		protected DateTime? _PromisedDate;
		[PXDBDate(BqlField = typeof(POLine.promisedDate))]
		[PXUIField(DisplayName = "Promised", Enabled = false)]
		public virtual DateTime? PromisedDate
		{
			get
			{
				return this._PromisedDate;
			}
			set
			{
				this._PromisedDate = value;
			}
		}
		#endregion
		#region Cancelled
		public abstract class cancelled : PX.Data.BQL.BqlBool.Field<cancelled> { }
		protected Boolean? _Cancelled;
		[PXDBBool(BqlField = typeof(POLine.cancelled))]
		public virtual Boolean? Cancelled
		{
			get
			{
				return this._Cancelled;
			}
			set
			{
				this._Cancelled = value;
			}
		}
		#endregion
		#region Completed
		public abstract class completed : PX.Data.BQL.BqlBool.Field<completed> { }
		[PXDBBool(BqlField = typeof(POLine.completed))]
		public virtual bool? Completed
		{
			get;
			set;
		}
		#endregion
		#region Closed
		public abstract class closed : PX.Data.BQL.BqlBool.Field<closed> { }
		[PXDBBool(BqlField = typeof(POLine.closed))]
		public virtual bool? Closed
		{
			get;
			set;
		}
		#endregion
		#region SiteID
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		protected Int32? _SiteID;
		[PXDBInt(BqlField = typeof(POLine.siteID))]
		public virtual Int32? SiteID
		{
			get
			{
				return this._SiteID;
			}
			set
			{
				this._SiteID = value;
			}
		}
		#endregion
		#region UOM
		public abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }
		protected String _UOM;
		[PXDBString(6, IsUnicode = true, BqlField = typeof(POLine.uOM))]
		[PXUIField(DisplayName = "UOM", Enabled = false)]
		public virtual String UOM
		{
			get
			{
				return this._UOM;
			}
			set
			{
				this._UOM = value;
			}
		}
		#endregion
		#region OrderQty
		public abstract class orderQty : PX.Data.BQL.BqlDecimal.Field<orderQty> { }
		protected Decimal? _OrderQty;
		[PXDBQuantity(BqlField = typeof(POLine.orderQty))]
		[PXUIField(DisplayName = "Order Qty.", Enabled = false)]
		public virtual Decimal? OrderQty
		{
			get
			{
				return this._OrderQty;
			}
			set
			{
				this._OrderQty = value;
			}
		}
		#endregion
		#region BaseOrderQty
		public abstract class baseOrderQty : PX.Data.BQL.BqlDecimal.Field<baseOrderQty> { }
		protected Decimal? _BaseOrderQty;
		[PXDBQuantity(BqlField = typeof(POLine.baseOrderQty))]
		public virtual Decimal? BaseOrderQty
		{
			get
			{
				return this._BaseOrderQty;
			}
			set
			{
				this._BaseOrderQty = value;
			}
		}
		#endregion
		#region OpenQty
		public abstract class openQty : PX.Data.BQL.BqlDecimal.Field<openQty> { }
		protected Decimal? _OpenQty;
		[PXDBQuantity(BqlField = typeof(POLine.openQty))]
		[PXUIField(DisplayName = "Open Qty.", Enabled = false)]
		public virtual Decimal? OpenQty
		{
			get
			{
				return this._OpenQty;
			}
			set
			{
				this._OpenQty = value;
			}
		}
		#endregion
		#region BaseOpenQty
		public abstract class baseOpenQty : PX.Data.BQL.BqlDecimal.Field<baseOpenQty> { }
		protected Decimal? _BaseOpenQty;
		[PXDBDecimal(6, BqlField = typeof(POLine.baseOpenQty))]
		public virtual Decimal? BaseOpenQty
		{
			get
			{
				return this._BaseOpenQty;
			}
			set
			{
				this._BaseOpenQty = value;
			}
		}
		#endregion
		#region ReceivedQty
		public abstract class receivedQty : PX.Data.BQL.BqlDecimal.Field<receivedQty> { }
		protected Decimal? _ReceivedQty;
		[PXDBDecimal(6, BqlField = typeof(POLine.receivedQty))]
		public virtual Decimal? ReceivedQty
		{
			get
			{
				return this._ReceivedQty;
			}
			set
			{
				this._ReceivedQty = value;
			}
		}
		#endregion
		#region BaseReceivedQty
		public abstract class baseReceivedQty : PX.Data.BQL.BqlDecimal.Field<baseReceivedQty> { }
		protected Decimal? _BaseReceivedQty;
		[PXDBDecimal(6, BqlField = typeof(POLine.baseReceivedQty))]
		public virtual Decimal? BaseReceivedQty
		{
			get
			{
				return this._BaseReceivedQty;
			}
			set
			{
				this._BaseReceivedQty = value;
			}
		}
		#endregion
		#region TranDesc
		public abstract class tranDesc : PX.Data.BQL.BqlString.Field<tranDesc> { }
		protected String _TranDesc;
		[PXDBString(256, IsUnicode = true, BqlField = typeof(POLine.tranDesc))]
		[PXUIField(DisplayName = "Line Description", Enabled = false)]
		public virtual String TranDesc
		{
			get
			{
				return this._TranDesc;
			}
			set
			{
				this._TranDesc = value;
			}
		}
		#endregion
		#region ReceiptStatus
		public abstract class receiptStatus : PX.Data.BQL.BqlString.Field<receiptStatus> { }
		protected String _ReceiptStatus;
		[PXDBString(1, IsFixed = true, BqlField = typeof(POLine.receiptStatus))]
		public virtual String ReceiptStatus
		{
			get
			{
				return this._ReceiptStatus;
			}
			set
			{
				this._ReceiptStatus = value;
			}
		}
		#endregion
		#region SOOrderType
		public abstract class sOOrderType : PX.Data.BQL.BqlString.Field<sOOrderType> { }
		protected String _SOOrderType;
		[PXString(2, IsFixed = true)]
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
		[PXString(15, IsUnicode = true)]
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
		#region SOOrderLineNbr
		public abstract class sOOrderLineNbr : PX.Data.BQL.BqlInt.Field<sOOrderLineNbr> { }
		protected Int32? _SOOrderLineNbr;
		[PXInt()]
		public virtual Int32? SOOrderLineNbr
		{
			get
			{
				return this._SOOrderLineNbr;
			}
			set
			{
				this._SOOrderLineNbr = value;
			}
		}
		#endregion
		#region LinkedToCurrentSOLine
		public abstract class linkedToCurrentSOLine : PX.Data.BQL.BqlBool.Field<linkedToCurrentSOLine> { }

		[PXBool()]
		public virtual bool? LinkedToCurrentSOLine
		{
			get;
			set;
		}
		#endregion
		#region SOOrderSplitLineNbr
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2020R2 + "LinkedToCurrentSOLine field should be used instead.")]
		public abstract class sOOrderSplitLineNbr : PX.Data.BQL.BqlInt.Field<sOOrderSplitLineNbr> { }
		protected Int32? _SOOrderSplitLineNbr;
		[PXInt()]
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2020R2 + "LinkedToCurrentSOLine field should be used instead.")]
		public virtual Int32? SOOrderSplitLineNbr
		{
			get
			{
				return this._SOOrderSplitLineNbr;
			}
			set
			{
				this._SOOrderSplitLineNbr = value;
			}
		}
		#endregion
		#region DemandQty
		public abstract class demandQty : PX.Data.BQL.BqlDecimal.Field<demandQty> { }
		protected Decimal? _DemandQty;
		[PXDecimal(6)]
		public virtual Decimal? DemandQty
		{
			get
			{
				return this._DemandQty;
			}
			set
			{
				this._DemandQty = value;
			}
		}
		#endregion

		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		[PXDBTimestamp(BqlField = typeof(POLine.Tstamp))]
		public virtual Byte[] tstamp
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID(BqlField = typeof(POLine.lastModifiedByID))]
		public virtual Guid? LastModifiedByID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID(BqlField = typeof(POLine.lastModifiedByScreenID))]
		public virtual String LastModifiedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime(BqlField = typeof(POLine.lastModifiedDateTime))]
		public virtual DateTime? LastModifiedDateTime
		{
			get;
			set;
		}
		#endregion
	}
}