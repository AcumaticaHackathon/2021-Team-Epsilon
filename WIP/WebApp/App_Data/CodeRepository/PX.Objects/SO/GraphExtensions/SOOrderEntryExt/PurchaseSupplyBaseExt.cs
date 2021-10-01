using PX.Common;
using PX.Data;
using PX.Objects.Common.DAC;
using PX.Objects.CS;
using PX.Objects.IN;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.SO.GraphExtensions.SOOrderEntryExt
{
	public class PurchaseSupplyBaseExt : PXGraphExtension<SOOrderEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.sOToPOLink>()
				|| PXAccess.FeatureInstalled<FeaturesSet.dropShipments>()
				|| PXAccess.FeatureInstalled<FeaturesSet.purchaseRequisitions>();
		}

		[PXCopyPasteHiddenView()]
		public PXSelect<DropShipLink,
			Where<DropShipLink.sOOrderType, Equal<Required<SOLine.orderType>>,
				And<DropShipLink.sOOrderNbr, Equal<Required<SOLine.orderNbr>>,
				And<DropShipLink.sOLineNbr, Equal<Required<SOLine.lineNbr>>>>>> DropShipLinks;

		public virtual IEnumerable dropShipLinks()
		{
			var parameters = PXView.Parameters;

			if (parameters.Length >= 3)
			{
				var key = new DropShipLink();
				key.SOOrderType = (string)parameters[0];
				key.SOOrderNbr = (string)parameters[1];
				key.SOLineNbr = (int?)parameters[2];

				DropShipLink cached = (DropShipLink)DropShipLinks.Cache.Locate(key);
				if (cached != null && DropShipLinks.Cache.GetStatus(cached).IsNotIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted))
				{
					yield return cached;
					yield break;
				}

				cached = DropShipLink.UK.BySOLine.FindDirty(Base, key.SOOrderType, key.SOOrderNbr, key.SOLineNbr);
				yield return cached;
			}
			yield break;
		}

		[PXCopyPasteHiddenView()]
		public PXSelect<SupplyPOOrder,
			Where<SupplyPOOrder.orderType, Equal<Required<SupplyPOOrder.orderType>>,
				And<SupplyPOOrder.orderNbr, Equal<Required<SupplyPOOrder.orderNbr>>>>> SupplyPOOrders;

		public virtual IEnumerable supplyPOOrders()
		{
			var parameters = PXView.Parameters;

			if (parameters.Length >= 2)
			{
				var key = new SupplyPOOrder();
				key.OrderType = (string)parameters[0];
				key.OrderNbr = (string)parameters[1];

				SupplyPOOrder cached = (SupplyPOOrder)SupplyPOOrders.Cache.Locate(key);
				if (cached != null && SupplyPOOrders.Cache.GetStatus(cached).IsNotIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted))
				{
					yield return cached;
					yield break;
				}

				cached = SupplyPOOrder.PK.FindDirty(Base, key.OrderType, key.OrderNbr);
				yield return cached;
			}
			yield break;
		}

		#region Events

		public virtual void _(Events.RowSelected<SOOrder> e)
		{
			if (e.Row == null)
				return;

			PXUIFieldAttribute.SetVisible<SOLine.pOCreate>(Base.Transactions.Cache, null, Base.soordertype.Current.RequireShipping ?? false);
			PXUIFieldAttribute.SetVisible<SOLineSplit.pOCreate>(Base.splits.Cache, null, Base.soordertype.Current.RequireShipping ?? false);
			PXUIFieldAttribute.SetVisible<SOLine.pOSource>(Base.Transactions.Cache, null, Base.soordertype.Current.RequireShipping ?? false);
			PXUIFieldAttribute.SetVisible<SOLineSplit.pOSource>(Base.splits.Cache, null, Base.soordertype.Current.RequireShipping ?? false);
		}

		public virtual void _(Events.RowSelected<SOLine> e)
		{
			if (e.Row == null)
				return;

			POCreateVerifyValue(e.Cache, e.Row, e.Row.POCreate);

			if (e.Row.POSource == null)
			{
				PXUIFieldAttribute.SetEnabled<SOLine.pOCreate>(e.Cache, e.Row, IsPOCreateEnabled(e.Row) );
				PXUIFieldAttribute.SetEnabled<SOLine.pOSource>(e.Cache, e.Row, IsPOCreateEnabled(e.Row) && e.Row.POCreate == true && !IsDropshipReturn(e.Row));
			}
			else if (IsPoToSoOrBlanket(e.Row.POSource) || e.Row.IsLegacyDropShip == true)
			{
				bool enabled = (Base.soordertype.Current.RequireShipping == true && e.Row.TranType != INDocType.Undefined && e.Row.Operation == SOOperation.Issue);
				if (enabled == false)
				{
					// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs [Leaving legacy approach for POtoSO and legacy drop-ships. Should be reworked on legacy drop-ship code deletion.]
					e.Row.POCreate = false;
					// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs [Leaving legacy approach for POtoSO and legacy drop-ships. Should be reworked on legacy drop-ship code deletion.]
					e.Row.POSource = null;
				}

				PXUIFieldAttribute.SetEnabled<SOLine.pOCreate>(e.Cache, e.Row, enabled);
				PXUIFieldAttribute.SetEnabled<SOLine.pOSource>(e.Cache, e.Row, enabled && e.Row.POCreate == true && e.Row.POCreated != true && (e.Row.ShippedQty == 0m || e.Row.IsLegacyDropShip == true));
			}
			else if (e.Row.POSource == INReplenishmentSource.DropShipToOrder) // && e.Row.IsLegacyDropShip != true
			{
				DropShipLink link = GetDropShipLink(e.Row);
				bool anyQtyReceived = link != null && link.BaseReceivedQty > 0m;
				PXUIFieldAttribute.SetEnabled<SOLine.pOCreate>(e.Cache, e.Row, IsPOCreateEnabled(e.Row) && !anyQtyReceived);
				PXUIFieldAttribute.SetEnabled<SOLine.pOSource>(e.Cache, e.Row, IsPOCreateEnabled(e.Row) && e.Row.POCreate == true && link == null && !IsDropshipReturn(e.Row));
			}
		}

		protected virtual void _(Events.RowPersisting<SOLine> e)
		{
			if (e.Operation.Command().IsNotIn(PXDBOperation.Insert, PXDBOperation.Update))
				return;

			PXDefaultAttribute.SetPersistingCheck<SOLine.pOSource>(e.Cache, e.Row, e.Row.POCreate == true ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
		}

		public virtual void _(Events.FieldVerifying<SOLine, SOLine.pOCreate> e)
		{
			if (e.Row == null)
				return;

			bool? newVal = (bool?)e.NewValue;
			POCreateVerifyValue(e.Cache, e.Row, newVal);
		}

		public virtual void _(Events.FieldDefaulting<SOLine, SOLine.pOCreate> e)
		{
			if (e.Row == null)
				return;

			if (!IsPOCreateEnabled(e.Row))
			{
				e.NewValue = false;
				e.Cancel = true;
				return;
			}

			if (e.Row.InventoryID != null && e.Row.SiteID != null)
			{
				bool dropShipmentsEnabled = PXAccess.FeatureInstalled<FeaturesSet.dropShipments>();
				bool soToPOLinkEnabled = PXAccess.FeatureInstalled<FeaturesSet.sOToPOLink>();
				INItemSiteSettings itemSettings = Base.initemsettings.SelectSingle(e.Row.InventoryID, e.Row.SiteID);

				if (itemSettings.ReplenishmentSource == INReplenishmentSource.DropShipToOrder && dropShipmentsEnabled && !IsIssueWithARNoUpdate(e.Row)
					|| itemSettings.ReplenishmentSource == INReplenishmentSource.PurchaseToOrder && soToPOLinkEnabled)
				{
					e.NewValue = true;
					e.Cancel = true;
					return;
				}
			}
		}

		public virtual void _(Events.FieldDefaulting<SOLine, SOLine.pOSiteID> e)
		{
			if (e.Row == null)
				return;

			INItemSiteSettings itemSettings = Base.initemsettings.SelectSingle(e.Row.InventoryID, e.Row.SiteID);

			object newVal = null;
			if (itemSettings != null)
			{
				if (itemSettings.ReplenishmentSource == INReplenishmentSource.Purchased ||
					itemSettings.ReplenishmentSource == INReplenishmentSource.PurchaseToOrder ||
					itemSettings.ReplenishmentSource == INReplenishmentSource.DropShipToOrder)
					newVal = itemSettings.ReplenishmentSourceSiteID;
			}

			if (newVal == null)
				newVal = e.Row.SiteID;

			e.NewValue = newVal;
			e.Cancel = true;
		}

		public virtual void _(Events.FieldDefaulting<SOLine, SOLine.pOSource> e)
		{
			if (e.Row == null)
				return;

			bool dropShipmentsEnabled = PXAccess.FeatureInstalled<FeaturesSet.dropShipments>();
			bool soToPOLinkEnabled = PXAccess.FeatureInstalled<FeaturesSet.sOToPOLink>();

			if (e.Row.POCreate != true)
			{
				e.NewValue = null;
				e.Cancel = true;
				return;
			}

			InventoryItem item;
			if (dropShipmentsEnabled && (IsDropshipReturn(e.Row)
				|| !IsIssueWithARNoUpdate(e.Row) && (item = InventoryItem.PK.Find(Base, e.Row.InventoryID)) != null
				&& item.StkItem != true && item.NonStockReceipt == true && item.NonStockShip == true))
			{
				e.NewValue = INReplenishmentSource.DropShipToOrder;
				e.Cancel = true;
				return;
			}

			INItemSiteSettings itemSettings = Base.initemsettings.SelectSingle(e.Row.InventoryID, e.Row.SiteID);
			if (itemSettings?.POSource == INReplenishmentSource.PurchaseToOrder && soToPOLinkEnabled
				|| itemSettings?.POSource == INReplenishmentSource.DropShipToOrder && dropShipmentsEnabled && !IsIssueWithARNoUpdate(e.Row))
			{
				e.NewValue = itemSettings.POSource;
				e.Cancel = true;
				return;
			}
		}

		public virtual void _(Events.FieldUpdating<SOLine, SOLine.pOSource> e)
		{
			if (e.Row == null)
				return;

			if (e.Row.POCreate != true && e.Row.POSource != null)
			{
				e.Row.POSource = null;
			}
		}

		public virtual void _(Events.FieldVerifying<SOLine, SOLine.pOSource> e)
		{
			if (e.Row == null || e.NewValue == null)
				return;

			string newValue = (string)e.NewValue;
			InventoryItem item = InventoryItem.PK.Find(Base, e.Row.InventoryID);
			if (item != null && item.StkItem != true && newValue == INReplenishmentSource.DropShipToOrder)
			{
				if (item.NonStockReceipt != true || item.NonStockShip != true || e.Row.LineType == SOLineType.MiscCharge)
				{
					throw new PXSetPropertyException<SOLine.pOSource>(Messages.ReceiptShipmentRequiredForDropshipNonstock);
				}
			}

			if (IsIssueWithARNoUpdate(e.Row) && newValue.IsIn(INReplenishmentSource.DropShipToOrder, INReplenishmentSource.BlanketDropShipToOrder))
			{
				throw new PXSetPropertyException<SOLine.pOSource>(Messages.DropshipmentNotAllowedForOrderType, e.Row.OrderType);
			}

			if (newValue.IsIn(INReplenishmentSource.DropShipToOrder, INReplenishmentSource.BlanketDropShipToOrder)
				&& !Base.lsselect.IsLotSerialsAllowedForDropShipLine(Base.Transactions.Cache, e.Row) && Base.lsselect.HasMultipleSplitsOrAllocation(Base.splits.Cache, e.Row))
			{
				throw new PXSetPropertyException<SOLine.pOSource>(Messages.DropShipSOLineCantHaveMultipleSplitsOrAllocation, e.Row.OrderType);
			}
		}

		protected virtual void _(Events.FieldUpdated<SOLine, SOLine.tranType> e)
		{
			if (e.Row == null || (string)e.OldValue == (string)e.NewValue)
				return;

			e.Cache.SetDefaultExt<SOLine.pOCreate>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<SOLine, SOLine.operation> e)
		{
			if (e.Row == null)
				return;

			e.Cache.SetDefaultExt<SOLine.pOCreate>(e.Row);
			e.Cache.SetDefaultExt<SOLine.pOSource>(e.Row);
		}

		public virtual void _(Events.FieldUpdated<SOLine, SOLine.siteID> e)
		{
			if (e.Row == null || e.Row.POSource == INReplenishmentSource.DropShipToOrder && e.Row.IsLegacyDropShip != true)
				return;

			e.Cache.SetDefaultExt<SOLine.pOCreate>(e.Row);
			e.Cache.SetDefaultExt<SOLine.pOSource>(e.Row);
		}

		public virtual void _(Events.FieldUpdated<SOLine, SOLine.pOCreate> e)
		{
			if (e.Row.POCreated != true)
			{
				if (e.Row.POCreate == true)
					e.Cache.SetDefaultExt<SOLine.pOSource>(e.Row);
				else
					e.Cache.SetValueExt<SOLine.pOSource>(e.Row, null);
			}

			LSSOLine.ResetAvailabilityCounters(e.Row);
		}

		public virtual void _(Events.FieldUpdated<SOLineSplit, SOLineSplit.pOCreate> e)
		{
			if (e.Row.POCreate == true)
			{
				e.Cache.SetDefaultExt<SOLineSplit.pOSource>(e.Row);
			}
			else
			{
				e.Cache.SetValueExt<SOLineSplit.pOSource>(e.Row, null);
			}
		}

		public virtual void _(Events.FieldDefaulting<SOLineSplit, SOLineSplit.pOSource> e)
		{
			if (e.Row != null && e.Row.POCreate != true)
			{
				e.NewValue = null;
				e.Cancel = true;
			}
		}

		#endregion Events

		/// <summary>
		/// Overrides <see cref="SOOrderEntry.Persist"/>
		/// </summary>
		[PXOverride]
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2021R2)]
		public virtual void Persist(Action baseMethod)
		{
			baseMethod();
		}

		public virtual void POCreateVerifyValue(PXCache sender, SOLine row, bool? value)
		{
			if (row == null || row.InventoryID == null || value != true)
				return;

			InventoryItem item = InventoryItem.PK.Find(Base, row.InventoryID);
			if (item != null && item.StkItem != true)
			{
				if (item.KitItem == true)
				{
					sender.RaiseExceptionHandling<SOLine.pOCreate>(row, value, new PXSetPropertyException(Messages.SOPOLinkNotForNonStockKit, PXErrorLevel.Error));
				}
				else if ((item.NonStockShip != true || item.NonStockReceipt != true) && PXAccess.FeatureInstalled<FeaturesSet.sOToPOLink>())
				{
					sender.RaiseExceptionHandling<SOLine.pOCreate>(row, value, new PXSetPropertyException(Messages.NonStockShipReceiptIsOff, PXErrorLevel.Warning));
				}
			}
		}

		public virtual bool IsPOCreateEnabled(SOLine row) => Base.soordertype.Current.RequireShipping == true && row.TranType != INDocType.Undefined
			&& (Base.soordertype.Current.ARDocType != AR.ARDocType.NoUpdate || PXAccess.FeatureInstalled<FeaturesSet.sOToPOLink>())
			&& (row.Operation == SOOperation.Issue || IsDropshipReturn(row) && PXAccess.FeatureInstalled<FeaturesSet.dropShipments>());

		public virtual bool IsIssueWithARNoUpdate(SOLine row) => row.Operation == SOOperation.Issue && Base.soordertype.Current.ARDocType == AR.ARDocType.NoUpdate;

		public virtual bool IsDropshipReturn(SOLine row) => row.Operation == SOOperation.Receipt && row.Behavior == SOBehavior.RM
			&& row.OrigShipmentType == SOShipmentType.DropShip && Base.soordertype.Current.ARDocType != AR.ARDocType.NoUpdate;

		public virtual bool IsPoToSoOrBlanket(string poSource) => poSource != null && poSource.IsIn(INReplenishmentSource.PurchaseToOrder,
			INReplenishmentSource.BlanketDropShipToOrder, INReplenishmentSource.BlanketPurchaseToOrder);

		public virtual bool IsOriginalDSLinkVisible(SOLine row) => row?.POCreate == true && row.Operation == SOOperation.Receipt
			&& row.OrigShipmentType == SOShipmentType.DropShip;

		#region LSSOLine

		public virtual void FillInsertingSchedule(PXCache sender, SOLineSplit split)
		{
			SOLine soLine = split != null ? PXParentAttribute.SelectParent<SOLine>(sender, split) : null;
			if (split == null || split.POLineNbr != null || soLine == null
				|| soLine.POSource != INReplenishmentSource.DropShipToOrder || soLine.IsLegacyDropShip == true)
				return;

			DropShipLink link = GetDropShipLink(soLine);
			if (link == null)
				return;

			split.POType = link.POOrderType;
			split.PONbr = link.POOrderNbr;
			split.POLineNbr = link.POLineNbr;
		}

		#endregion

		protected bool prefetched = false;

		[PXOverride]
		public virtual void PrefetchWithDetails()
		{
			if (Base.Document.Current == null || prefetched || DropShipLinks.Cache.IsDirty)
				return;

			SOOrderTypeOperation receiptOperation = PXSelect<SOOrderTypeOperation,
				Where<SOOrderTypeOperation.orderType, Equal<Current<SOOrderType.orderType>>,
					And<SOOrderTypeOperation.operation, Equal<SOOperation.receipt>,
					And<SOOrderTypeOperation.active, Equal<True>>>>>
				.Select(Base);
			if (receiptOperation != null)
			{
				var receiptDetailsWithLinksQuery = new PXSelectReadonly2<SOLine,
					LeftJoin<DropShipLink,
						On<DropShipLink.sOOrderType, Equal<SOLine.origOrderType>,
							And<DropShipLink.sOOrderNbr, Equal<SOLine.origOrderNbr>,
							And<DropShipLink.sOLineNbr, Equal<SOLine.origLineNbr>>>>,
					LeftJoin<SupplyPOOrder, On<DropShipLink.FK.SupplyPOOrder>>>,
					Where<SOLine.orderType, Equal<Current<SOOrder.orderType>>,
						And<SOLine.orderNbr, Equal<Current<SOOrder.orderNbr>>,
						And<SOLine.operation, Equal<SOOperation.receipt>,
						And<SOLine.origShipmentType, Equal<SOShipmentType.dropShip>>>>>>(Base);
				DoPrefetch(receiptDetailsWithLinksQuery);
			}

			var issueDetailsWithLinksQuery = new PXSelectReadonly2<SOLine,
				LeftJoin<DropShipLink, On<DropShipLink.FK.SOLine>,
				LeftJoin<SupplyPOOrder, On<DropShipLink.FK.SupplyPOOrder>>>,
				Where<SOLine.orderType, Equal<Current<SOOrder.orderType>>,
					And<SOLine.orderNbr, Equal<Current<SOOrder.orderNbr>>,
					And<SOLine.operation, Equal<SOOperation.issue>>>>>(Base);
			DoPrefetch(issueDetailsWithLinksQuery);

			prefetched = true;
		}

		protected virtual void DoPrefetch(PXSelectBase<SOLine> detailsWithLinksQuery)
		{
			var fieldsAndTables = new[]
			{
				typeof(SOLine.orderType), typeof(SOLine.orderNbr), typeof(SOLine.lineNbr), typeof(DropShipLink), typeof(SupplyPOOrder)
			};
			using (new PXFieldScope(detailsWithLinksQuery.View, fieldsAndTables))
			{
				int startRow = PXView.StartRow;
				int totalRows = 0;
				foreach (PXResult<SOLine, DropShipLink, SupplyPOOrder> record in detailsWithLinksQuery.View.Select(
					PXView.Currents, PXView.Parameters, PXView.Searches, PXView.SortColumns,
					PXView.Descendings, PXView.Filters, ref startRow, PXView.MaximumRows, ref totalRows))
				{
					SOLine line = record;
					DropShipLink link = record;
					SupplyPOOrder supplyOrder = record;

					DropShipLinkStoreCached(link, line);

					if (supplyOrder?.OrderNbr != null)
					{
						SupplyOrderStoreCached(supplyOrder);
					}
				}
			}
		}

		public virtual void DropShipLinkStoreCached(DropShipLink link, SOLine line)
		{
			//DropShipLinks.StoreCached(
			//	new PXCommandKey(new object[] { line.OrderType, line.OrderNbr, line.LineNbr }, singleRow: true),
			//	link.POLineNbr != null ? new List<object> { link } : new List<object>());
			if (!(DropShipLinks.Cache.Locate(link) is DropShipLink cached) || DropShipLinks.Cache.GetStatus(cached) == PXEntryStatus.Notchanged)
			{
				DropShipLinks.Cache.Hold(link);
			}
		}

		public virtual DropShipLink GetDropShipLink(SOLine line)
		{
			if (line == null)
				return null;

			return DropShipLinks.SelectWindowed(0, 1, line.OrderType, line.OrderNbr, line.LineNbr);
		}

		public virtual DropShipLink GetOriginalDropShipLink(SOLine line)
		{
			if (line == null)
				return null;

			return DropShipLinks.SelectWindowed(0, 1, line.OrigOrderType, line.OrigOrderNbr, line.OrigLineNbr);
		}

		public virtual void SupplyOrderStoreCached(SupplyPOOrder order)
		{
			if (!(SupplyPOOrders.Cache.Locate(order) is SupplyPOOrder cached) || SupplyPOOrders.Cache.GetStatus(cached) == PXEntryStatus.Notchanged)
			{
				SupplyPOOrders.Cache.Hold(order);
			}
		}

		public virtual SupplyPOOrder GetSupplyOrder(SOLine line)
		{
			if (line == null || line.POSource != INReplenishmentSource.DropShipToOrder || line.IsLegacyDropShip == true)
				return null;

			DropShipLink link = GetDropShipLink(line);
			if (link == null)
				return null;

			return SupplyPOOrders.SelectWindowed(0, 1, link.POOrderType, link.POOrderNbr);
		}

		public virtual SupplyPOOrder GetOriginalSupplyOrder(SOLine line)
		{
			if (line == null || line.POSource != INReplenishmentSource.DropShipToOrder || line.IsLegacyDropShip == true)
				return null;

			DropShipLink link = GetOriginalDropShipLink(line);
			if (link == null)
				return null;

			return SupplyPOOrders.SelectWindowed(0, 1, link.POOrderType, link.POOrderNbr);
		}
	}
}
