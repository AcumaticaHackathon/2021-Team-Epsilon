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
using PX.Objects.PO;

namespace PX.Objects.SO.GraphExtensions.SOOrderEntryExt
{
	public class DropshipReturn : PXGraphExtension<SOOrderEntry>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.dropShipments>();

		public PXAction<SOOrder> createVendorReturn;
		[PXUIField(DisplayName = "Create Vendor Return", MapEnableRights = PXCacheRights.Select)]
		[PXButton(CommitChanges = true)]
		protected virtual IEnumerable CreateVendorReturn(PXAdapter adapter)
		{
			List<SOOrder> list = adapter.Get<SOOrder>().ToList();
			Base.Save.Press();

			PXLongOperation.StartOperation(Base, () =>
			{
				var receiptGraph = PXGraph.CreateInstance<POReceiptEntry>();
				var receiptList = new DocumentList<POReceipt>(receiptGraph);

				CreatePOReturn(receiptGraph, receiptList);

				if (receiptList.Count == 0)
				{
					throw new PXException(PO.Messages.NoLinesForVendorReturn);
				}
			});

			return list;
		}

		protected virtual void CreatePOReturn(POReceiptEntry receiptGraph, DocumentList<POReceipt> receiptList)
		{
			var linesToReturn = SelectFrom<SOLine>
				.InnerJoin<SOOrder>.On<SOLine.FK.Order>
				.InnerJoin<ARTran>.On<SOLine.FK.InvoiceLine>
				.InnerJoin<POReceipt>.On<POReceipt.receiptNbr.IsEqual<ARTran.sOShipmentNbr>>
				.LeftJoin<POReceiptLineReturn>
					.On<POReceiptLineReturn.receiptNbr.IsEqual<ARTran.sOShipmentNbr>
					.And<POReceiptLineReturn.lineNbr.IsEqual<ARTran.sOShipmentLineNbr>>>
				.LeftJoin<POReceiptLine>.On<POReceiptLine.FK.SOLine>
				.Where<SOLine.FK.Order.SameAsCurrent
					.And<SOLine.operation.IsEqual<SOOperation.receipt>>
					.And<SOLine.origShipmentType.IsEqual<SOShipmentType.dropShip>>
					.And<SOLine.pOCreate.IsEqual<True>>
					.And<SOLine.pOSource.IsEqual<INReplenishmentSource.dropShipToOrder>>
					.And<SOLine.completed.IsEqual<False>>
					.And<POReceiptLine.receiptNbr.IsNull>>
				.OrderBy<Asc<POReceipt.receiptNbr>>
				.View.Select(Base)
				.Cast<PXResult<SOLine, SOOrder, ARTran, POReceipt, POReceiptLineReturn>>()
				.ToList();

			for (int i = 0; i < linesToReturn.Count; i++)
			{
				PXResult<SOLine, SOOrder, ARTran, POReceipt, POReceiptLineReturn> res = linesToReturn[i];

				SOLine line = res;
				SOOrder order = res;
				POReceipt receipt = res;
				POReceiptLineReturn receiptLine = res;
				if (string.IsNullOrEmpty(receiptLine.ReceiptNbr))
				{
					throw new PXInvalidOperationException(PO.Messages.OrigReceiptLineNotFound,
						PXForeignSelectorAttribute.GetValueExt<SOLine.inventoryID>(Base.Caches[typeof(SOLine)], line));
				}
				receiptGraph.PopulateReturnedQty(receiptLine);

				if (receiptGraph.Document.Current == null)
				{
					receiptGraph.Document.Insert(new POReceipt()
					{
						ReceiptType = POReceiptType.POReturn,
						BranchID = receipt.BranchID,
						VendorID = receipt.VendorID,
						VendorLocationID = receipt.VendorLocationID,
						ProjectID = receiptLine.ProjectID,
						CuryID = receipt.CuryID,
						AutoCreateInvoice = false,
						ReturnInventoryCostMode = ReturnCostMode.OriginalCost,

						SOOrderType = line.OrderType,
						SOOrderNbr = line.OrderNbr,
					});

					receiptGraph.CopyReceiptCurrencyInfoToReturn(receipt.CuryInfoID, receiptGraph.Document.Current?.CuryInfoID, receiptGraph.Document.Current?.ReturnInventoryCostMode == ReturnCostMode.OriginalCost);
				}

				POReceiptLine returnLine = AddReturnLine(receiptGraph, receiptLine, line);

				SOLine4 lineAlias = PropertyTransfer.Transfer(line, new SOLine4());
				decimal? actualDiscUnitPrice = receiptGraph.GetActualDiscUnitPrice(lineAlias);
				decimal? lineAmt = PXCurrencyAttribute.BaseRound(receiptGraph, line.OrderQty * actualDiscUnitPrice);
				var oshipment = receiptGraph.CreateUpdateOrderShipment(order, returnLine, null, false, line.BaseOrderQty, lineAmt);

				if (i + 1 >= linesToReturn.Count
					|| !receiptGraph.Document.Cache.ObjectsEqual<POReceipt.receiptNbr>(receipt, (POReceipt)linesToReturn[i + 1]))
				{
					receiptGraph.Save.Press();
					receiptList.Add(receiptGraph.Document.Current);
					receiptGraph.Clear();
				}
			}
		}

		protected virtual POReceiptLine AddReturnLine(POReceiptEntry receiptGraph, POReceiptLineReturn receiptLine, SOLine line)
		{
			bool requireLotSerial = IsLotSerialRequired(receiptLine.InventoryID);
			var newLine = receiptGraph.transactions.Insert(new POReceiptLine
			{
				IsLSEntryBlocked = requireLotSerial,
			});

			receiptGraph.CopyFromOrigReceiptLine(newLine, receiptLine, preserveLineType: true, returnOrigCost: receiptGraph.Document.Current?.ReturnInventoryCostMode == ReturnCostMode.OriginalCost);
			newLine = receiptGraph.transactions.Update(newLine);

			newLine.SOOrderType = line.OrderType;
			newLine.SOOrderNbr = line.OrderNbr;
			newLine.SOOrderLineNbr = line.LineNbr;
			newLine.UOM = line.UOM;
			newLine.ReceiptQty = line.OrderQty;
			newLine.BaseReceiptQty = line.BaseOrderQty;
			newLine = receiptGraph.transactions.Update(newLine);

			if (requireLotSerial)
			{
				newLine.IsLSEntryBlocked = false;
				newLine = receiptGraph.transactions.Update(newLine);
				if (receiptLine.LocationID != null)
					receiptGraph.transactions.Cache.SetValueExt<POReceiptLine.locationID>(newLine, receiptLine.LocationID);
				else
					receiptGraph.transactions.Cache.SetDefaultExt<POReceiptLine.locationID>(newLine);

				List<SOLineSplit> lineSplits = SelectFrom<SOLineSplit>
					.Where<SOLineSplit.FK.OrderLine.SameAsCurrent>
					.View.SelectMultiBound(Base, new[] { line })
					.RowCast<SOLineSplit>().ToList();
				foreach (SOLineSplit lineSplit in lineSplits)
				{
					var split = new POReceiptLineSplit
					{
						InventoryID = lineSplit.InventoryID,
						SubItemID = lineSplit.SubItemID,
						LotSerialNbr = lineSplit.LotSerialNbr,
						ExpireDate = lineSplit.ExpireDate,
					};
					split = PXCache<POReceiptLineSplit>.CreateCopy(receiptGraph.splits.Insert(split));
					split.Qty = lineSplit.Qty;
					split = receiptGraph.splits.Update(split);
				}
			}

			if (!string.IsNullOrEmpty(newLine.POType) && !string.IsNullOrEmpty(newLine.PONbr))
			{
				receiptGraph.AddPOOrderReceipt(newLine.POType, newLine.PONbr);
			}
			return newLine;
		}

		protected virtual bool IsLotSerialRequired(int? inventoryID)
		{
			var item = InventoryItem.PK.Find(Base, inventoryID);
			if (item?.StkItem != true)
				return false;
			var lotSerClass = INLotSerClass.PK.Find(Base, item.LotSerClassID);
			return lotSerClass?.RequiredForDropship == true;
		}
	}
}
