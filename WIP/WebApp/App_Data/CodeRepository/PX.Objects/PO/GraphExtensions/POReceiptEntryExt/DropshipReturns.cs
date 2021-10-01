using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.SO;

namespace PX.Objects.PO.GraphExtensions.POReceiptEntryExt
{
	public class DropshipReturn : PXGraphExtension<POReceiptEntry>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.dropShipments>();

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDBDefault(typeof(POReceipt.receiptNbr), DefaultForUpdate = false)]
		[PXParent(typeof(Select<POReceipt, Where<POReceipt.receiptNbr, Equal<Current<SOOrderShipment.shipmentNbr>>, And<Current<SOOrderShipment.shipmentType>, Equal<SOShipmentType.dropShip>>>>))]
		public virtual void _(Events.CacheAttached<SOOrderShipment.shipmentNbr> e)
		{
		}

		public virtual void _(Events.RowSelected<POReceipt> e)
		{
			PXUIFieldAttribute.SetDisplayName<POReceiptLine.sOOrderType>(Base.transactions.Cache,
				(e.Row?.ReceiptType == POReceiptType.POReturn) ? Messages.SOReturnType : Messages.TransferOrderType);
			PXUIFieldAttribute.SetDisplayName<POReceiptLine.sOOrderNbr>(Base.transactions.Cache,
				(e.Row?.ReceiptType == POReceiptType.POReturn) ? Messages.SOReturn : Messages.TransferOrderNbr);
			PXUIFieldAttribute.SetDisplayName<POReceiptLine.sOOrderLineNbr>(Base.transactions.Cache,
				(e.Row?.ReceiptType == POReceiptType.POReturn) ? Messages.SOReturnLine : Messages.TransferLineNbr);

			if (e.Row == null) return;

			bool dropshipReturn = !string.IsNullOrEmpty(e.Row.SOOrderNbr);
			PXUIFieldAttribute.SetVisible<POReceipt.sOOrderNbr>(e.Cache, e.Row, dropshipReturn);
			if (dropshipReturn)
			{
				Base.transactions.Cache.AllowInsert =
					Base.transactions.Cache.AllowUpdate =
					Base.transactions.Cache.AllowDelete = false;
				Base.addPOReceiptReturn.SetEnabled(false);
				Base.addPOReceiptLineReturn.SetEnabled(false);
				PXUIFieldAttribute.SetEnabled<POReceipt.returnInventoryCostMode>(e.Cache, e.Row, false);
			}
		}

		[PXOverride]
		public virtual void UpdateReturnOrdersMarkedForDropship(POReceiptLine line, POLineUOpen poLine,
			Action<POReceiptLine, POLineUOpen> baseMethod)
		{
			baseMethod?.Invoke(line, poLine);

			if (!POLineType.IsDropShip(line.LineType))
				return;

			SOLine4 soLine = SelectFrom<SOLine4>
				.Where<SOLine4.orderType.IsEqual<POReceiptLine.sOOrderType.FromCurrent>
				.And<SOLine4.orderNbr.IsEqual<POReceiptLine.sOOrderNbr.FromCurrent>>
				.And<SOLine4.lineNbr.IsEqual<POReceiptLine.sOOrderLineNbr.FromCurrent>>>
				.View.SelectMultiBound(Base, new object[] { line });

			if (soLine == null
				|| soLine.UOM != line.UOM || soLine.OrderQty != line.ReceiptQty || soLine.OpenQty != line.ReceiptQty
				|| soLine.Operation != SOOperation.Receipt
				|| soLine.POCreate != true || soLine.POSource != INReplenishmentSource.DropShipToOrder
				|| soLine.OpenLine != true || soLine.Cancelled == true)
			{
				throw new PXException(Messages.RMALineModifiedVendorReturnCantRelease);
			}

			soLine = PXCache<SOLine4>.CreateCopy(soLine);
			soLine.ShippedQty = soLine.OrderQty;
			soLine.BaseShippedQty = soLine.BaseOrderQty;
			soLine.OpenQty = 0m;
			soLine.CuryOpenAmt = 0m;
			soLine.Cancelled = true;
			soLine.OpenLine = false;
			soLine = Base.solineselect.Update(soLine);

			foreach (var soSplitRes in SelectFrom<SOLineSplit>
				.Where<SOLineSplit.orderType.IsEqual<POReceiptLine.sOOrderType.FromCurrent>
				.And<SOLineSplit.orderNbr.IsEqual<POReceiptLine.sOOrderNbr.FromCurrent>>
				.And<SOLineSplit.lineNbr.IsEqual<POReceiptLine.sOOrderLineNbr.FromCurrent>>>
				.View.SelectMultiBound(Base, new object[] { line }))
			{
				SOLineSplit soSplit = PXCache<SOLineSplit>.CreateCopy(soSplitRes);
				soSplit.ReceivedQty = soSplit.Qty;
				soSplit.ShippedQty = soSplit.Qty;
				soSplit.Completed = true;
				soSplit.POReceiptType = line.ReceiptType;
				soSplit.POReceiptNbr = line.ReceiptNbr;
				soSplit.POCompleted = true;
				soSplit.PlanID = null;
				soSplit = Base.solinesplitselect.Update(soSplit);
			}

			var order = (SOOrder)PXParentAttribute.SelectParent(Base.solineselect.Cache, soLine);
			if (order.Approved != true)
			{
				var ownerName = Base.soorderselect.Cache.GetValueExt<SOOrder.ownerID>(order);

				throw new PXException(Messages.SalesOrderRelatedToDropShipReceiptIsNotApproved,
					order.OrderType, order.OrderNbr, ownerName);
			}
			order.OpenLineCntr--;

			decimal? actualDiscUnitPrice = Base.GetActualDiscUnitPrice(soLine);
			decimal? lineAmt = PXCurrencyAttribute.BaseRound(Base, soLine.OrderQty * actualDiscUnitPrice);
			var oshipment = Base.CreateUpdateOrderShipment(order, line, null, true, soLine.BaseOrderQty, lineAmt);

			if (order.OpenShipmentCntr == 0 && order.OpenLineCntr == 0)
			{
				order.MarkCompleted();
			}
		}
	}
}
