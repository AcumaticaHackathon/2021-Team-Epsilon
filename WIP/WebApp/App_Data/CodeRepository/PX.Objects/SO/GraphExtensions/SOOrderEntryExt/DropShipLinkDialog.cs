using PX.Common;
using PX.Data;
using PX.Objects.Common.DAC;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PO;
using PX.Objects.SO.Services;
using PX.Objects.Common.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.SO.GraphExtensions.SOOrderEntryExt
{
	public class DropShipLinkDialog : PXGraphExtension<POLinkDialog, PurchaseSupplyBaseExt, SOOrderEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.dropShipments>();
		}

		public virtual void _(Events.FieldUpdated<SupplyPOLine.selected> e)
		{
			SupplyPOLine updatedSupplyLine = (SupplyPOLine)e.Row;
			SOLine currentSOLine = Base2.SOLineDemand.Select();
			if (updatedSupplyLine == null || (bool?)e.OldValue == updatedSupplyLine.Selected
				|| currentSOLine.POSource != INReplenishmentSource.DropShipToOrder || currentSOLine.IsLegacyDropShip == true)
				return;

			if (updatedSupplyLine.Selected == true)
				updatedSupplyLine.SelectedSOLines = updatedSupplyLine.SelectedSOLines.SparseArrayAddDistinct(currentSOLine.LineNbr.Value);
			else
				updatedSupplyLine.SelectedSOLines.SparseArrayRemove(currentSOLine.LineNbr.Value);
		}

		public virtual void _(Events.RowSelected<SupplyPOLine> e)
		{
			SOLine currentSOLine = Base.Transactions.Current;
			if (currentSOLine == null || e.Row == null
				|| currentSOLine.POSource != INReplenishmentSource.DropShipToOrder || currentSOLine.IsLegacyDropShip == true)
				return;

			DropShipLink link = Base1.GetDropShipLink(currentSOLine);
			bool anyQtyReceived = link != null && link.BaseReceivedQty > 0;
			PXUIFieldAttribute.SetEnabled<SupplyPOLine.selected>(e.Cache, e.Row, !anyQtyReceived && link?.InReceipt != true);

			if (e.Row.Selected == true && anyQtyReceived)
				PXUIFieldAttribute.SetWarning<SupplyPOLine.selected>(e.Cache, e.Row, Messages.PurchaseOrderCannotBeDeselected);
		}

		public virtual void _(Events.RowUpdated<SupplyPOLine> e)
		{
			SOLine currentSOLine = Base2.SOLineDemand.Select();
			if (e.OldRow?.Selected == e.Row.Selected || currentSOLine.POSource != INReplenishmentSource.DropShipToOrder
				|| currentSOLine.IsLegacyDropShip == true)
				return;

			bool updatedSupplyLineSelected = e.Row.Selected == true || e.Row.SelectedSOLines.Contains(currentSOLine.LineNbr.Value);
			if (!updatedSupplyLineSelected)
				return;

			foreach (SupplyPOLine supplyLine in Base2.SupplyPOLines.Select())
			{
				bool selected = supplyLine.Selected == true || supplyLine.SelectedSOLines.Contains(currentSOLine.LineNbr.Value);
				// It should be possible to select the one line only in case of Drop-Ships.
				if (selected && (e.Row.OrderType != supplyLine.OrderType
					|| e.Row.OrderNbr != supplyLine.OrderNbr || e.Row.LineNbr != supplyLine.LineNbr))
				{
					supplyLine.Selected = false;
					supplyLine.SelectedSOLines.SparseArrayRemove(currentSOLine.LineNbr.Value);
					Base2.SupplyPOLines.Update(supplyLine);
				}
			}

			Base2.SupplyPOLines.Current = e.Row;
			Base2.SupplyPOLines.View.RequestRefresh();
		}

		/// <summary>
		/// Overrides <see cref="POLinkDialog.CollectSupplyPOLines(SOLine, ICollection{SupplyPOLine})"/>
		/// </summary>
		[PXOverride]
		public virtual void CollectSupplyPOLines(SOLine currentSOLine, ICollection<SupplyPOLine> supplyPOLines)
		{
			if (currentSOLine.POSource != INReplenishmentSource.DropShipToOrder || currentSOLine.IsLegacyDropShip == true)
				return;

			var linksToDisplayQuery = new PXSelectJoin<SupplyPOLine,
				InnerJoin<SupplyPOOrder, On<SupplyPOLine.FK.SupplyOrder>,
				LeftJoin<DropShipLink, On<DropShipLink.FK.SupplyPOLine>>>,
				Where<SupplyPOLine.lineType, In3<POLineType.goodsForDropShip, POLineType.nonStockForDropShip>,
					And2<Where<SupplyPOLine.vendorID, Equal<Current<SOLine.vendorID>>,
						Or<Current<SOLine.vendorID>, IsNull>>,
					And<SupplyPOOrder.orderType, Equal<POOrderType.dropShip>,
					And2<Where<SupplyPOOrder.shipDestType, NotEqual<POShippingDestination.customer>,
						Or<SupplyPOOrder.shipToBAccountID, Equal<Current<SOLine.customerID>>>>,
					And<Where<DropShipLink.sOOrderType, Equal<Current<SOLine.orderType>>,
						And<DropShipLink.sOOrderNbr, Equal<Current<SOLine.orderNbr>>,
						Or<DropShipLink.sOOrderNbr, IsNull,
							And<SupplyPOLine.inventoryID, Equal<Current<SOLine.inventoryID>>,
							And2<Where<SupplyPOLine.subItemID, Equal<Current<SOLine.subItemID>>,
								Or<Current<SOLine.subItemID>, IsNull>>, // Remove if we absolutely sure that line with non-stock item will never have null SubItemID.
							And<SupplyPOLine.baseOrderQty, Equal<Current<SOLine.baseOrderQty>>,
							And<SupplyPOLine.siteID, Equal<Current<SOLine.pOSiteID>>,
							And<SupplyPOLine.completed, Equal<False>,
							And<SupplyPOLine.cancelled, Equal<False>>>>>>>>>>>>>>>>(Base);

			var allLines = new List<SupplyPOLine>();
			SupplyPOLine receiptedLine = null;
			foreach (PXResult<SupplyPOLine, SupplyPOOrder, DropShipLink> row in linksToDisplayQuery.Select())
			{
				SupplyPOLine supplyLine = row;
				SupplyPOOrder supplyOrder = Base1.SupplyPOOrders.Locate(row) ?? row;
				DropShipLink link = LocateActualLinkForSupplyLine((DropShipLink)row, supplyLine);
				if (link != null && (link.SOOrderType != currentSOLine.OrderType || link.SOOrderNbr != currentSOLine.OrderNbr
					|| link.SOLineNbr != currentSOLine.LineNbr))
					continue;

				string[] statusesBeforeAwaitingLink = new[] { POOrderStatus.Hold, POOrderStatus.PendingApproval, POOrderStatus.PendingPrint, POOrderStatus.PendingEmail };
				bool linkingInRequisitions = Base.SOPOLinkShowDocumentsOnHold && supplyOrder.Status.IsIn(statusesBeforeAwaitingLink);
				bool supplyOrderStatusValid = supplyOrder.SOOrderNbr == null && (linkingInRequisitions || supplyOrder.Status == POOrderStatus.AwaitingLink)
					|| supplyOrder.SOOrderNbr == currentSOLine.OrderNbr && supplyOrder.SOOrderType == currentSOLine.OrderType;
				if (!supplyOrderStatusValid)
					continue;

				if (Base2.SupplyPOLines.Cache.GetStatus(supplyLine) == PXEntryStatus.Notchanged)
				{
					supplyLine.SelectedSOLines = link != null ? new int?[] { link.SOLineNbr.Value } : new int?[0];
					supplyLine.LinkedSOLines = link != null ? new int?[] { link.SOLineNbr.Value } : new int?[0];
				}
				else
				{
					if (link != null)
						supplyLine.LinkedSOLines = new int?[] { link.SOLineNbr.Value };
					else
						supplyLine.LinkedSOLines.SparseArrayClear();
				}

				supplyLine.Selected = supplyLine.SelectedSOLines.Contains(currentSOLine.LineNbr.Value);
				Base2.SupplyPOLines.Cache.Hold(supplyLine);

				if (supplyLine.Selected == true && link?.BaseReceivedQty > 0)
				{
					receiptedLine = supplyLine;
					break;
				}

				allLines.Add(supplyLine);
			}

			if (receiptedLine != null)
			{
				supplyPOLines.Add(receiptedLine);
			}

			foreach (var line in allLines)
			{
				supplyPOLines.Add(line);
			}
		}

		protected virtual DropShipLink LocateActualLinkForSupplyLine(DropShipLink link, SupplyPOLine supplyLine)
		{
			var newLink = Base1.DropShipLinks.Cache.Inserted.Cast<DropShipLink>().FirstOrDefault(l => l.POOrderType == supplyLine.OrderType
					&& l.POOrderNbr == supplyLine.OrderNbr && l.POLineNbr == supplyLine.LineNbr);

			if (newLink != null)
				return newLink;

			if (link?.SOLineNbr == null || Base1.DropShipLinks.Cache.GetStatus(link).IsIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted))
				return null;

			return Base1.DropShipLinks.Locate(link) ?? link;
		}

		[PXOverride]
		public virtual void LinkPOSupply(SOLine currentSOLine)
		{
			if (currentSOLine.POSource != INReplenishmentSource.DropShipToOrder || currentSOLine.IsLegacyDropShip == true)
				return;

			List<SOLineSplit> splits = Base.splits.Select().RowCast<SOLineSplit>().ToList();
			SOLineSplit currentSOSplit = splits.FirstOrDefault();
			if (currentSOSplit == null)
				return;

			if (splits.Count > 1 || currentSOSplit.IsAllocated == true)
				throw new PXException(Messages.DropShipSOLineCantHaveMultipleSplitsOrAllocation);

			// Unlink.
			bool removedLink = false;
			foreach (SupplyPOLine supply in Base2.SupplyPOLines.Cache.Updated)
			{
				bool selected = supply.SelectedSOLines.Contains(currentSOLine.LineNbr.Value);
				DropShipLink link = Base1.GetDropShipLink(currentSOLine);
				bool linkedToCurrentLine = link != null && link.POOrderType == supply.OrderType && link.POOrderNbr == supply.OrderNbr && link.POLineNbr == supply.LineNbr;

				if (!selected && linkedToCurrentLine)
				{
					UnlinkFromSupplyLine(supply, link, splits, currentSOLine.POCreate);
					removedLink = true;
				}
			}

			// Link.
			bool addedLink = false;
			foreach (SupplyPOLine supply in Base2.SupplyPOLines.Cache.Updated)
			{
				bool selected = supply.SelectedSOLines.Contains(currentSOLine.LineNbr.Value);
				DropShipLink link = Base1.GetDropShipLink(currentSOLine);

				if (selected && link == null && currentSOSplit.IsAllocated == false && currentSOSplit.Completed == false && currentSOSplit.BaseQty > 0m
					&& string.IsNullOrEmpty(currentSOSplit.SOOrderNbr) && string.IsNullOrEmpty(currentSOSplit.PONbr) && currentSOSplit.BaseQty == currentSOLine.BaseQty)
				{
					INItemPlan supplyPlan = PXSelect<INItemPlan, Where<INItemPlan.planID, Equal<Required<INItemPlan.planID>>>>.Select(Base, supply.PlanID);
					if (supplyPlan == null)
						continue;

					LinkToSupplyLine(currentSOSplit, supply);

					addedLink = true;
					supply.LinkedSOLines = supply.LinkedSOLines.SparseArrayAddDistinct(currentSOLine.LineNbr.Value);
				}
			}

			var soLine = Base.Transactions.Current;
			if (addedLink)
			{
				if (soLine.POCreated != true)
					Base.Transactions.Cache.SetValue<SOLine.pOCreated>(soLine, true);
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

		public virtual void LinkToSupplyLine(SOLineSplit split, SupplyPOLine supply, bool linkActive = true)
		{
			INItemPlan demandPlan = PXSelect<INItemPlan, Where<INItemPlan.planID, Equal<Required<INItemPlan.planID>>>>.Select(Base, split.PlanID);
			if (demandPlan != null && demandPlan.SupplyPlanID == null)
			{
				demandPlan.SupplyPlanID = supply.PlanID;
				Base.Caches[typeof(INItemPlan)].Update(demandPlan);
			}

			split.POCreate = true;
			split.VendorID = supply.VendorID;
			split.POType = supply.OrderType;
			split.PONbr = supply.OrderNbr;
			split.POLineNbr = supply.LineNbr;

			Base.splits.Update(split);

			DropShipLink link = new DropShipLink
			{
				SOOrderType = split.OrderType,
				SOOrderNbr = split.OrderNbr,
				SOLineNbr = split.LineNbr,
				POOrderType = supply.OrderType,
				POOrderNbr = supply.OrderNbr,
				POLineNbr = supply.LineNbr,
				Active = linkActive,
				SOInventoryID = supply.InventoryID,
				SOSiteID = supply.SiteID,
				SOBaseOrderQty = supply.BaseOrderQty,
				POInventoryID = supply.InventoryID,
				POSiteID = supply.SiteID,
				POBaseOrderQty = supply.BaseOrderQty
			};

			Base1.DropShipLinks.Insert(link);

			SupplyPOOrder supplyOrder = SupplyPOLine.FK.SupplyOrder.FindParent(Base, supply);
			supplyOrder = Base1.SupplyPOOrders.Locate(supplyOrder) ?? supplyOrder;
			if (supplyOrder.SOOrderNbr == null)
			{
				supplyOrder.SOOrderType = split.OrderType;
				supplyOrder.SOOrderNbr = split.OrderNbr;
				Base1.SupplyPOOrders.Update(supplyOrder);
			}
		}

		protected virtual void UnlinkFromSupplyLine(SupplyPOLine supply, DropShipLink link, List<SOLineSplit> splits, bool? poCreate)
		{
			if (splits.Count == 0)
				return;

			SupplyPOOrder supplyOrder = SupplyPOLine.FK.SupplyOrder.FindParent(Base, supply);
			foreach (var split in splits)
			{
				if (split.POCompleted != true && split.Completed != true
					&& split.POType == supply.OrderType && split.PONbr == supply.OrderNbr && split.POLineNbr == supply.LineNbr)
				{
					if (supplyOrder != null && split.RefNoteID == supplyOrder.NoteID)
						split.RefNoteID = null;

					split.ClearPOReferences();
					split.POCreate = poCreate;
					split.POCompleted = false;
					split.ReceivedQty = 0m;
					split.ShippedQty = 0m;
					split.Completed = false;

					Base.splits.Update(split);
				}
			}

			Base1.DropShipLinks.Delete(link);
			supply.LinkedSOLines?.SparseArrayRemove(splits.First().LineNbr.Value);

			supplyOrder = Base1.SupplyPOOrders.Locate(supplyOrder) ?? supplyOrder;
			if (supplyOrder.DropShipLinkedLinesCount == 0)
			{
				supplyOrder.SOOrderType = null;
				supplyOrder.SOOrderNbr = null;
				Base1.SupplyPOOrders.Update(supplyOrder);
			}
		}

		public virtual void UnlinkSOLineFromSupplyLine(DropShipLink link, SOLine soLine)
		{
			SupplyPOLine supply = DropShipLink.FK.SupplyPOLine.FindParent(Base, link);
			supply = Base2.SupplyPOLines.Locate(supply) ?? supply;
			if (link == null || supply == null)
				return;

			if (soLine == null)
				throw new ArgumentNullException(nameof(soLine));

			List<SOLineSplit> splits = PXSelect<SOLineSplit,
				Where<SOLineSplit.orderType, Equal<Required<DropShipLink.sOOrderType>>,
					And<SOLineSplit.orderNbr, Equal<Required<DropShipLink.sOOrderNbr>>,
					And<SOLineSplit.lineNbr, Equal<Required<DropShipLink.sOLineNbr>>>>>>
				.Select(Base, link.SOOrderType, link.SOOrderNbr, link.SOLineNbr)
				.RowCast<SOLineSplit>().ToList();

			soLine = Base.Transactions.Locate(soLine) ?? soLine;

			UnlinkFromSupplyLine(supply, link, splits, soLine.POCreate);
			supply.SelectedSOLines?.SparseArrayRemove(link.SOLineNbr.Value);

			if (soLine.POCreate != true)
				Base.Transactions.Cache.SetValueExt<SOLine.pOSource>(soLine, null);
			if (soLine.POCreated == true)
				Base.Transactions.Cache.SetValueExt<SOLine.pOCreated>(soLine, false);
		}
	}
}
