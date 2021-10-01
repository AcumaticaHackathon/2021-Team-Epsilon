using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.Common.DAC;
using PX.Objects.CS;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.SO.GraphExtensions.SOOrderEntryExt
{
	public class DropShipLinksExt : PXGraphExtension<PurchaseSupplyBaseExt, SOOrderEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.dropShipments>();
		}

		#region CacheAttached

		[PXCustomizeBaseAttribute(typeof(PXDBStringAttribute), nameof(PXDBFieldAttribute.IsKey), true)]
		public virtual void _(Events.CacheAttached<DropShipLink.sOOrderType> e)
		{
		}

		[PXCustomizeBaseAttribute(typeof(PXDBStringAttribute), nameof(PXDBFieldAttribute.IsKey), true)]
		public virtual void _(Events.CacheAttached<DropShipLink.sOOrderNbr> e)
		{
		}

		[PXCustomizeBaseAttribute(typeof(PXDBIntAttribute), nameof(PXDBFieldAttribute.IsKey), true)]
		public virtual void _(Events.CacheAttached<DropShipLink.sOLineNbr> e)
		{
		}

		[PXCustomizeBaseAttribute(typeof(PXDBStringAttribute), nameof(PXDBFieldAttribute.IsKey), false)]
		public virtual void _(Events.CacheAttached<DropShipLink.pOOrderType> e)
		{
		}

		[PXParent(typeof(DropShipLink.FK.SupplyPOOrder))]
		[PXFormula(null, typeof(CountCalc<SupplyPOOrder.dropShipLinkedLinesCount>))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXDBStringAttribute), nameof(PXDBFieldAttribute.IsKey), false)]
		public virtual void _(Events.CacheAttached<DropShipLink.pOOrderNbr> e)
		{
		}

		[PXCustomizeBaseAttribute(typeof(PXDBIntAttribute), nameof(PXDBFieldAttribute.IsKey), false)]
		public virtual void _(Events.CacheAttached<DropShipLink.pOLineNbr> e)
		{
		}


		[PXUnboundFormula(typeof(Switch<Case<Where<DropShipLink.active, Equal<True>, And<DropShipLink.poCompleted, Equal<False>>>, int1>, int0>), typeof(AddCalc<SupplyPOOrder.dropShipActiveLinksCount>))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		public virtual void _(Events.CacheAttached<DropShipLink.active> e)
		{
		}

		#endregion CacheAttached

		#region SOLine events

		public virtual void _(Events.FieldSelecting<SOLine, SOLine.pOOrderStatus> e)
		{
			SupplyPOOrder supplyOrder = Base1.IsOriginalDSLinkVisible(e.Row) ? Base1.GetOriginalSupplyOrder(e.Row)
				: Base1.GetSupplyOrder(e.Row);

			if (e.Row != null && supplyOrder != null && e.Row.POOrderStatus != supplyOrder.Status)
			{
				e.Row.POOrderStatus = supplyOrder.Status;
			}

			e.ReturnState = supplyOrder?.Status;
		}

		public virtual void _(Events.FieldSelecting<SOLine, SOLine.pOOrderNbr> e)
		{
			DropShipLink link = Base1.IsOriginalDSLinkVisible(e.Row) ? Base1.GetOriginalDropShipLink(e.Row)
				: Base1.GetDropShipLink(e.Row);

			if (e.Row != null && link != null && e.Row.POOrderNbr != link.POOrderNbr)
			{
				e.Row.POOrderType = link.POOrderType;
				e.Row.POOrderNbr = link.POOrderNbr;
			}

			e.ReturnState = link?.POOrderNbr;
		}

		public virtual void _(Events.FieldSelecting<SOLine, SOLine.pOLineNbr> e)
		{
			DropShipLink link = Base1.IsOriginalDSLinkVisible(e.Row) ? Base1.GetOriginalDropShipLink(e.Row)
				: Base1.GetDropShipLink(e.Row);

			if (e.Row != null && link != null && e.Row.POLineNbr != link.POLineNbr)
			{
				e.Row.POLineNbr = link.POLineNbr;
			}

			e.ReturnState = link?.POLineNbr;
		}

		public virtual void _(Events.FieldSelecting<SOLine, SOLine.pOLinkActive> e)
		{
			DropShipLink link = Base1.IsOriginalDSLinkVisible(e.Row) ? Base1.GetOriginalDropShipLink(e.Row)
				: Base1.GetDropShipLink(e.Row);

			if (e.Row != null && link != null && e.Row.POLinkActive != link.Active)
			{
				e.Row.POLinkActive = link.Active;
			}

			e.ReturnState = link?.Active;
		}

		public virtual void _(Events.FieldDefaulting<SOLine, SOLine.pOSiteID> e)
		{
			if (e.Row == null || e.Row.POSource != INReplenishmentSource.DropShipToOrder || e.Row.IsLegacyDropShip == true)
				return;

			e.NewValue = e.Row.SiteID;
			e.Cancel = true;
		}

		public virtual void _(Events.FieldUpdated<SOLine, SOLine.pOSource> e)
		{
			if (e.Row == null || e.Row.POSource != INReplenishmentSource.DropShipToOrder || e.Row.IsLegacyDropShip == true)
				return;

			e.Cache.SetValue<SOLine.pOSiteID>(e.Row, e.Row.SiteID);
		}

		public virtual void _(Events.FieldUpdated<SOLine, SOLine.siteID> e)
		{
			if (e.Row == null || e.Row.POSource != INReplenishmentSource.DropShipToOrder || e.Row.IsLegacyDropShip == true)
				return;

			e.Cache.SetValue<SOLine.pOSiteID>(e.Row, e.Row.SiteID);
		}

		public virtual void _(Events.FieldVerifying<SOLine, SOLine.pOCreate> e)
		{
			if (e.Row == null || e.Row.POSource != INReplenishmentSource.DropShipToOrder || e.Row.IsLegacyDropShip == true)
				return;

			DropShipLink link = Base1.GetDropShipLink(e.Row);
			if (link?.Active == true && (bool?)e.NewValue != true && e.Row.POCreate == true)
			{
				throw new PXSetPropertyException(Messages.DropShipSOLineHasActiveLink, link.POOrderNbr);
			}
		}

		public virtual void _(Events.FieldVerifying<SOLine, SOLine.inventoryID> e)
		{
			if (e.Row == null || e.Row.POSource != INReplenishmentSource.DropShipToOrder || e.Row.IsLegacyDropShip == true)
				return;

			DropShipLink link = Base1.GetDropShipLink(e.Row);
			if (link != null && link.Active == true)
			{
				InventoryItem item = InventoryItem.PK.Find(Base, (int?)e.NewValue);
				e.NewValue = item?.InventoryCD;
				throw new PXSetPropertyException(Messages.DropShipSOLineHasActiveLink, link.POOrderNbr);
			}

			var newItem = InventoryItem.PK.Find(Base, (int?)e.NewValue);
			if (newItem.StkItem != true && (newItem.NonStockReceipt != true || newItem.NonStockShip != true))
			{
				InventoryItem item = InventoryItem.PK.Find(Base, (int?)e.NewValue);
				e.NewValue = item?.InventoryCD;
				throw new PXSetPropertyException(Messages.ReceiptShipmentRequiredForDropshipNonstock);
			}
		}

		public virtual void _(Events.FieldVerifying<SOLine, SOLine.siteID> e)
		{
			if (e.Row == null || e.Row.POSource != INReplenishmentSource.DropShipToOrder || e.Row.IsLegacyDropShip == true)
				return;

			DropShipLink link = Base1.GetDropShipLink(e.Row);
			if (link != null && link.Active == true)
			{
				INSite newSite = INSite.PK.Find(Base, (int?)e.NewValue);
				e.NewValue = newSite?.SiteCD;
				throw new PXSetPropertyException(Messages.DropShipSOLineHasActiveLink, link.POOrderNbr);
			}
		}

		public virtual void _(Events.FieldVerifying<SOLine, SOLine.orderQty> e)
		{
			if (e.Row == null || e.Row.POSource != INReplenishmentSource.DropShipToOrder || e.Row.IsLegacyDropShip == true)
				return;

			DropShipLink link = Base1.GetDropShipLink(e.Row);
			if (link == null)
				return;

			if (link.Active == true)
			{
				throw new PXSetPropertyException(Messages.DropShipSOLineHasActiveLink, link.POOrderNbr);
			}

			if (link.BaseReceivedQty > 0)
			{
				decimal? newBaseOrderQty = 0;
				if ((decimal?)e.NewValue > 0m)
					newBaseOrderQty = INUnitAttribute.ConvertToBase<SOLine.inventoryID, SOLine.uOM>(e.Cache, e.Row, (decimal)e.NewValue, INPrecision.QUANTITY);

				if (link.Active != true && newBaseOrderQty < link.BaseReceivedQty)
				{
					decimal minOrderQty = INUnitAttribute.ConvertFromBase<SOLine.inventoryID, SOLine.uOM>(e.Cache, e.Row, (decimal)link.BaseReceivedQty, INPrecision.QUANTITY);
					throw new PXSetPropertyException(CS.Messages.Entry_GE, minOrderQty);
				}
			}
		}

		public virtual void _(Events.FieldVerifying<SOLine, SOLine.uOM> e)
		{
			if (e.Row == null || e.Row.POSource != INReplenishmentSource.DropShipToOrder || e.Row.IsLegacyDropShip == true)
				return;

			DropShipLink link = Base1.GetDropShipLink(e.Row);
			if (link != null && link.Active == true)
			{
				throw new PXSetPropertyException(Messages.DropShipSOLineHasActiveLink, link.POOrderNbr);
			}
		}

		public virtual void _(Events.FieldVerifying<SOLine, SOLine.pOLinkActive> e)
		{
			if (e.Row == null || e.Row.POSource != INReplenishmentSource.DropShipToOrder || e.Row.IsLegacyDropShip == true)
				return;

			DropShipLink link = Base1.GetDropShipLink(e.Row);
			if (link == null)
				return;

			if (link.Active == true && link.InReceipt == true && (bool?)e.NewValue != true)
			{
				throw new PXSetPropertyException(Messages.DropShipSOLineUnreleasedReceiptExists, link.POOrderNbr);
			}
			else if (link.Active != true && (bool?)e.NewValue == true)
			{
				string firstMismatch = null;
				if (e.Row.InventoryID != link.POInventoryID)
					firstMismatch = PXUIFieldAttribute.GetDisplayName<SOLine.inventoryID>(e.Cache);
				else if (e.Row.SiteID != link.POSiteID)
					firstMismatch = PXUIFieldAttribute.GetDisplayName<SOLine.siteID>(e.Cache);
				else if (e.Row.BaseOrderQty != link.POBaseOrderQty)
					firstMismatch = PXUIFieldAttribute.GetDisplayName<SOLine.orderQty>(e.Cache);

				if (firstMismatch != null)
				{
					throw new PXSetPropertyException(Messages.DropShipSOLineValidationFailed, firstMismatch, link.POOrderNbr);
				}
			}
		}

		public virtual void _(Events.RowUpdated<SOLine> e)
		{
			if (e.Row.POCreate != true && e.OldRow.POCreate == true
				&& e.OldRow.POSource == INReplenishmentSource.DropShipToOrder && e.OldRow.IsLegacyDropShip != true)
			{
				DropShipLink link = Base1.GetDropShipLink(e.Row);
				if (link != null)
				{
					DropShipLinkDialog poLinkDialog = Base.GetExtension<DropShipLinkDialog>();
					poLinkDialog.UnlinkSOLineFromSupplyLine(link, e.Row);
				}
			}
			else if (e.Row.POSource == INReplenishmentSource.DropShipToOrder && e.Row.IsLegacyDropShip != true)
			{
				DropShipLink link = Base1.GetDropShipLink(e.Row);
				if (link != null && !e.Cache.ObjectsEqual<SOLine.pOLinkActive, SOLine.inventoryID, SOLine.siteID, SOLine.baseOrderQty, SOLine.completed>(e.Row, e.OldRow))
				{
					if (e.Row.POLinkActive != e.OldRow.POLinkActive) // Otherwise may be updated from SOShipmentEntry with the null value.
						link.Active = e.Row.POLinkActive;

					link.SOInventoryID = e.Row.InventoryID;
					link.SOSiteID = e.Row.SiteID;
					link.SOBaseOrderQty = e.Row.BaseOrderQty;
					link.SOCompleted = e.Row.Completed;
					Base1.DropShipLinks.Update(link);

					if (e.Row.POLinkActive != e.OldRow.POLinkActive)
					{
						// We should cleanup warings.
						Base.Transactions.View.RequestRefresh();
					}
				}
			}
		}

		public virtual void _(Events.RowDeleting<SOLine> e)
		{
			SOOrder document = Base.Document.Current;
			if (e.Row == null || e.Row.POSource != INReplenishmentSource.DropShipToOrder || e.Row.IsLegacyDropShip == true)
				return;

			DropShipLink link = Base1.GetDropShipLink(e.Row);
			if (link?.Active == true && Base.Document.Cache.GetStatus(document).IsNotIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted))
			{
				e.Cancel = true;
				throw new PXException(Messages.DropShipSOLineHasLinkAndCantBeDeleted);
			}
		}

		public virtual void _(Events.RowSelected<SOLine> e)
		{
			if (e.Row == null || e.Row.POSource != INReplenishmentSource.DropShipToOrder || e.Row.IsLegacyDropShip == true)
				return;

			DropShipLink link = Base1.GetDropShipLink(e.Row);
			bool fullQtyReceived = link != null && link.Active == true && link.BaseReceivedQty == link.POBaseOrderQty;
			PXUIFieldAttribute.SetEnabled<SOLine.pOSiteID>(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<SOLine.pOLinkActive>(e.Cache, e.Row, e.Row.Operation == SOOperation.Issue &&
				Base1.IsPOCreateEnabled(e.Row) && e.Row.POCreate == true && link != null && !fullQtyReceived);

			if (PXUIFieldAttribute.GetErrorOnly<SOLine.pOLinkActive>(e.Cache, e.Row) != null)
				return;

			if (e.Row.POCreate == true && link != null && link.POCompleted == true && e.Row.Completed != true && link.BaseReceivedQty < link.POBaseOrderQty)
			{
				e.Cache.RaiseExceptionHandling<SOLine.pOLinkActive>(e.Row, e.Row.POLinkActive,
					new PXSetPropertyException<SOLine.pOLinkActive>(Messages.DropShipLinkedPOLineCompleted, PXErrorLevel.Warning, link.POOrderNbr));
			}
			else if (e.Row.POCreate == true && link?.Active != true && e.Row.Completed != true && !Base1.IsDropshipReturn(e.Row))
			{
				e.Cache.RaiseExceptionHandling<SOLine.pOLinkActive>(e.Row, e.Row.POLinkActive,
					new PXSetPropertyException<SOLine.pOLinkActive>(Messages.DropShipSOLineNoLink, PXErrorLevel.Warning));
			}
			else
			{
				e.Cache.RaiseExceptionHandling<SOLine.pOLinkActive>(e.Row, e.Row.POLinkActive, null);
			}
		}

		#endregion SOLine events

		#region Other events

		/// <summary>
		/// Overrides <see cref="SOOrderEntry.SOOrder_RowDeleting(PXCache, PXRowDeletingEventArgs)"/>
		/// </summary>
		[PXOverride]
		public virtual void SOOrder_RowDeleting(PXCache sender, PXRowDeletingEventArgs e, PXRowDeleting baseMethod)
		{
			baseMethod(sender, e);

			SOOrder order = (SOOrder)e.Row;
			/// Skip cache merging with prefetched links <see cref="PurchaseSupplyBaseExt.PrefetchWithDetails"/>
			DropShipLink link = PXSelectReadonly<DropShipLink,
				Where<DropShipLink.sOOrderType, Equal<Required<SOLine.orderType>>,
					And<DropShipLink.sOOrderNbr, Equal<Required<SOLine.orderNbr>>>>,
				OrderBy<Desc<DropShipLink.inReceipt>>>
				.SelectWindowed(Base, 0, 1, order.OrderType, order.OrderNbr);

			if (link == null)
				link = Base.Caches[typeof(DropShipLink)].Inserted.Cast<DropShipLink>()
						.FirstOrDefault(l => l.SOOrderType == order.OrderType && l.SOOrderNbr == order.OrderNbr);

			if (link == null)
				return;

			if (link.InReceipt == true)
				throw new PXException(Messages.DropShipSOOrderDeletionReceiptExists, order.OrderNbr, link.POOrderNbr);

			string message = PXMessages.LocalizeFormatNoPrefixNLA(Messages.DropShipSOOrderDeletionConfirmation, order.OrderNbr);
			if (Base.Document.View.Ask(message, MessageButtons.OKCancel) == WebDialogResult.Cancel)
			{
				e.Cancel = true;
				return;
			}
		}

		protected virtual void _(Events.RowUpdated<SOOrder> e)
		{
			if (e.Row.Cancelled == true && e.OldRow.Cancelled != true)
			{
				var dsLines = PXSelect<SOLine,
					Where<SOLine.orderType, Equal<Required<SOOrder.orderType>>,
						And<SOLine.orderNbr, Equal<Required<SOOrder.orderNbr>>,
						And<SOLine.pOCreate, Equal<True>,
						And<SOLine.pOSource, Equal<INReplenishmentSource.dropShipToOrder>>>>>>
						.Select(Base, e.Row.OrderType, e.Row.OrderNbr);

				DropShipLinkDialog poLinkDialog = Base.GetExtension<DropShipLinkDialog>();
				foreach (SOLine line in dsLines)
				{
					var link = Base1.GetDropShipLink(line);
					poLinkDialog.UnlinkSOLineFromSupplyLine(link, line);
				}
			}
		}

		/// <summary>
		/// Overrides <see cref="SOOrderEntry.SOOrder_Cancelled_FieldVerifying(PXCache, PXFieldVerifyingEventArgs)"/>
		/// </summary>
		[PXOverride]
		public virtual void SOOrder_Cancelled_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e, PXFieldVerifying baseMethod)
		{
			baseMethod(sender, e);

			SOOrder order = (SOOrder)e.Row;
			if (e.Row == null || (bool?)e.NewValue != true)
				return;

			/// Skip cache merging with prefetched links <see cref="PurchaseSupplyBaseExt.PrefetchWithDetails"/>
			DropShipLink link = PXSelectReadonly<DropShipLink,
				Where<DropShipLink.sOOrderType, Equal<Required<SOOrder.orderType>>,
					And<DropShipLink.sOOrderNbr, Equal<Required<SOOrder.orderNbr>>>>,
				OrderBy<Desc<DropShipLink.inReceipt>>>
				.SelectWindowed(Base, 0, 1, order.OrderType, order.OrderNbr);
			if (link == null)
				link = Base.Caches[typeof(DropShipLink)].Inserted.Cast<DropShipLink>()
						.FirstOrDefault(l => l.SOOrderType == order.OrderType && l.SOOrderNbr == order.OrderNbr);
			if (link == null)
				return;

			if (link.InReceipt == true)
				throw new PXException(Messages.DropShipSOOrderCancelationReceiptExists, order.OrderNbr, link.POOrderNbr);

			string message = PXMessages.LocalizeFormatNoPrefixNLA(Messages.DropShipSOOrderCancelationConfirmation, order.OrderNbr);
			if (Base.Document.View.Ask(message, MessageButtons.OKCancel) == WebDialogResult.Cancel)
			{
				e.Cancel = true;
				return;
			}
		}

		public virtual void _(Events.FieldUpdated<SupplyPOOrder, SupplyPOOrder.dropShipLinkedLinesCount> e)
		{
			int? oldValue = (int?)e.OldValue;
			if (e.Row == null || e.Row.DropShipLinkedLinesCount == oldValue)
				return;

			if (e.Row.DropShipLinkedLinesCount == 0)
			{
				Base1.SupplyPOOrders.Cache.SetValue<SupplyPOOrder.sOOrderType>(e.Row, null);
				Base1.SupplyPOOrders.Cache.SetValue<SupplyPOOrder.sOOrderNbr>(e.Row, null);
			}
		}

		#endregion Other events
	}
}
