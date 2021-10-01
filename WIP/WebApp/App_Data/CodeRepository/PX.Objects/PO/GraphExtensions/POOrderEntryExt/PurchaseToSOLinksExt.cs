using PX.Common;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.SO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SOLineSplit3 = PX.Objects.PO.POOrderEntry.SOLineSplit3;

namespace PX.Objects.PO.GraphExtensions.POOrderEntryExt
{
	// Code here is shared between legacy Drop-Ship feature & PO to SO.
	// If you want to change something here that may affect legacy Drop-Ship functionality,
	// please move legacy implementation to DropShipLegacyLinksExt (should be created if does not extist).
	public class PurchaseToSOLinksExt : PXGraphExtension<POOrderEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<CS.FeaturesSet.dropShipments>() || PXAccess.FeatureInstalled<CS.FeaturesSet.sOToPOLink>();
		}

		public PXAction<POOrder> viewDemand;

		[PXUIField(DisplayName = Messages.ViewDemand, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton(VisibleOnDataSource = false)]
		public virtual IEnumerable ViewDemand(PXAdapter adapter)
		{
			INReplenishmentLine line = Base.ReplenishmentLines.SelectWindowed(0, 1);
			if (line != null)
				Base.ReplenishmentLines.AskExt();
			else
				Base.FixedDemand.AskExt();
			return adapter.Get();
		}

		protected virtual void POOrder_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			POOrder doc = e.Row as POOrder;
			if (e.Row == null || doc.OrderType == POOrderType.DropShip)
				return;

			PXUIFieldAttribute.SetEnabled<POOrder.sOOrderType>(cache, doc, false);
			PXUIFieldAttribute.SetEnabled<POOrder.sOOrderNbr>(cache, doc, false);
		}

		protected virtual void POLine_InventoryID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			POOrder doc = Base.Document.Current;
			POLine row = (POLine)e.Row;
			if (doc == null || (doc.OrderType == POOrderType.DropShip && doc.IsLegacyDropShip != true) || row?.InventoryID == null)
				return;

			SOLineSplit link = PXSelect<SOLineSplit,
				Where<SOLineSplit.pOType, Equal<Required<SOLineSplit.pOType>>,
					And<SOLineSplit.pONbr, Equal<Required<SOLineSplit.pONbr>>,
					And<SOLineSplit.pOLineNbr, Equal<Required<SOLineSplit.pOLineNbr>>>>>>
				.SelectWindowed(Base, 0, 1, row.OrderType, row.OrderNbr, row.LineNbr);
			if (link != null && link.InventoryID != (int?)e.NewValue)
			{
				InventoryItem item = InventoryItem.PK.Find(Base, (int?)e.NewValue);

				var ex = new PXSetPropertyException<POLine.inventoryID>(Messages.ChangingInventoryForLinkedRecord);
				ex.ErrorValue = item?.InventoryCD;

				throw ex;
			}
		}

		protected virtual void POOrder_RowDeleting(PXCache sender, PXRowDeletingEventArgs e)
		{
			POOrder doc = (POOrder)e.Row;
			if (doc.OrderType == POOrderType.DropShip && doc.IsLegacyDropShip != true)
				return;

			Base.Transactions.View.SetAnswer("POLineLinkedToSOLine", WebDialogResult.OK);
		}

		protected virtual void POLine_RowDeleting(PXCache sender, PXRowDeletingEventArgs e)
		{
			POLine row = (POLine)e.Row;
			if ((Base.Document.Current.OrderType != POOrderType.DropShip || Base.Document.Current.IsLegacyDropShip == true)
				&& row.LineType.IsIn(POLineType.GoodsForSalesOrder, POLineType.GoodsForDropShip, POLineType.NonStockForSalesOrder,
					POLineType.NonStockForDropShip))
			{
				SOLineSplit first;
				using (new PXFieldScope(Base.RelatedSOLineSplit.View,
					typeof(SOLineSplit.orderType),
					typeof(SOLineSplit.orderNbr),
					typeof(SOLineSplit.lineNbr),
					typeof(SOLineSplit.splitLineNbr)))
				{
					first = (SOLineSplit)Base.RelatedSOLineSplit.View.SelectMultiBound(new object[] { e.Row }).FirstOrDefault();
				}

				if (first == null)
					return;

				string message = PXMessages.LocalizeFormatNoPrefixNLA(Messages.POLineLinkedToSOLine, first.OrderNbr);
				if (Base.Transactions.View.Ask("POLineLinkedToSOLine", message, MessageButtons.OKCancel) == WebDialogResult.Cancel)
				{
					e.Cancel = true;
				}
			}
		}

		protected virtual void POLine_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			if ((Base.Document.Current.OrderType == POOrderType.DropShip && Base.Document.Current.IsLegacyDropShip != true))
				return;

			POLine row = (POLine)e.Row;
			using (new PXFieldScope(Base.RelatedSOLineSplit.View,
				typeof(SOLineSplit.orderType),
				typeof(SOLineSplit.orderNbr),
				typeof(SOLineSplit.lineNbr),
				typeof(SOLineSplit.splitLineNbr)))
			{
				foreach (SOLineSplit r in Base.RelatedSOLineSplit.View.SelectMultiBound(new object[] { e.Row }))
				{
					var upd = (SOLineSplit3)PXSelect<SOLineSplit3,
						Where<SOLineSplit3.orderType, Equal<Required<SOLineSplit3.orderType>>,
							And<SOLineSplit3.orderNbr, Equal<Required<SOLineSplit3.orderNbr>>,
							And<SOLineSplit3.lineNbr, Equal<Required<SOLineSplit3.lineNbr>>,
							And<SOLineSplit3.splitLineNbr, Equal<Required<SOLineSplit3.splitLineNbr>>>>>>>
						.Select(Base, r.OrderType, r.OrderNbr, r.LineNbr, r.SplitLineNbr);

					upd.POType = null;
					upd.PONbr = null;
					upd.POLineNbr = null;
					upd.RefNoteID = null;

					bool poCreated = false;
					if (upd.POCreated == true)
					{
						bool anyLinked = (SOLineSplit3)PXSelect<SOLineSplit3,
							Where<SOLineSplit3.orderType, Equal<Required<SOLineSplit3.orderType>>,
							And<SOLineSplit3.orderNbr, Equal<Required<SOLineSplit3.orderNbr>>,
							And<SOLineSplit3.lineNbr, Equal<Required<SOLineSplit3.lineNbr>>,
							And<SOLineSplit3.pONbr, IsNotNull,
							And<SOLineSplit3.splitLineNbr, NotEqual<Required<SOLineSplit3.splitLineNbr>>>>>>>>
							.SelectWindowed(Base, 0, 1, upd.OrderType, upd.OrderNbr, upd.LineNbr, upd.SplitLineNbr) != null;
						poCreated = anyLinked;
					}
					Base.UpdateSOLine(upd, upd.VendorID, poCreated);

					Base.FixedDemand.Update(upd);
					INItemPlan plan = INItemPlan.PK.Find(Base, upd.PlanID);
					if (plan?.PlanType != null && plan.SupplyPlanID == row.PlanID)
					{
						if (upd.POCreate == false)
						{
							var op = SOOrderTypeOperation.PK.Find(Base, upd.OrderType, upd.Operation);
							if (op != null && op.OrderPlanType != null)
								plan.PlanType = op.OrderPlanType;
						}
						plan.SupplyPlanID = null;
						sender.Graph.Caches[typeof(INItemPlan)].Update(plan);
					}
				}
			}
		}
	}
}
