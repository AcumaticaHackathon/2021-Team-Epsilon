using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.AR.GraphExtensions;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.PO;
using GenerateBillParameters = PX.Objects.AR.GraphExtensions.GenerateIntercompanyBillExtension.GenerateBillParameters;

namespace PX.Objects.SO.GraphExtensions.ARInvoiceEntryExt
{
	public class Intercompany : PXGraphExtension<GenerateIntercompanyBillExtension, ARInvoiceEntry.CostAccrual, ARInvoiceEntry>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.interBranch>()
			&& PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

		private Dictionary<ARTran, IAPTranSource> _poSources;

		[PXOverride]
		public virtual APTran GenerateIntercompanyAPTran(APInvoiceEntry apInvoiceEntryGraph, ARTran arTran,
			Func<APInvoiceEntry, ARTran, APTran> baseFunc)
		{
			APTran newTran = apInvoiceEntryGraph.Transactions.Insert();

			IAPTranSource poSource = GetAPTranSource(arTran);
			if (poSource != null)
			{
				poSource.SetReferenceKeyTo(newTran);
			}

			Base2.SetAPTranFields(apInvoiceEntryGraph.Transactions.Cache, newTran, arTran);

			if (poSource != null)
			{
				newTran.LineType = poSource.LineType;
				newTran.AccountID = poSource.POAccrualAcctID ?? poSource.ExpenseAcctID;
				newTran.SubID = poSource.POAccrualSubID ?? poSource.ExpenseSubID;
				newTran.SiteID = poSource.SiteID;
			}

			newTran = apInvoiceEntryGraph.Transactions.Update(newTran);
			return newTran;
		}

		[PXOverride]
		public virtual int? GetAPTranProjectID(GenerateBillParameters parameters, ARInvoice arInvoice, ARTran arTran,
			Func<GenerateBillParameters, ARInvoice, ARTran, int?> baseFunc)
		{
			var source = GetAPTranSource(arTran);
			return (source != null)
				? source.ProjectID
				: baseFunc(parameters, arInvoice, arTran);
		}

		[PXOverride]
		public virtual int? GetAPTranTaskID(GenerateBillParameters parameters, ARInvoice arInvoice, ARTran arTran,
			Func<GenerateBillParameters, ARInvoice, ARTran, int?> baseFunc)
		{
			var source = GetAPTranSource(arTran);
			return (source != null)
				? source.TaskID
				: baseFunc(parameters, arInvoice, arTran);
		}

		[PXOverride]
		public virtual int? GetAPTranCostCodeID(GenerateBillParameters parameters, ARInvoice arInvoice, ARTran arTran,
			Func<GenerateBillParameters, ARInvoice, ARTran, int?> baseFunc)
		{
			var source = GetAPTranSource(arTran);
			return (source != null)
				? source.CostCodeID
				: baseFunc(parameters, arInvoice, arTran);
		}

		public virtual IAPTranSource GetAPTranSource(ARTran arTran)
		{
			IAPTranSource poSource = null;
			if (_poSources?.TryGetValue(arTran, out poSource) == true)
				return poSource;
			(POReceipt receipt, POReceiptLine receiptLine) = GetReceiptData(arTran);
			if (receiptLine?.Released == true)
			{
				poSource = POReceiptLineS.PK.Find(Base, receiptLine.ReceiptNbr, receiptLine.LineNbr);
			}
			else
			{
				// PO return can't have order based accrual, so the following query makes sense only for issues
				if (arTran.SOOrderLineOperation == SOOperation.Issue)
				{
					(POOrder poOrder, POLine poLine, SOOrder soOrder, SOLine soLine) = GetOrderData(arTran);
					if (poLine?.POAccrualType == POAccrualType.Order)
					{
						poSource = POLineS.PK.Find(Base, poLine.OrderType, poLine.OrderNbr, poLine.LineNbr);
					}
				}
			}
			if (_poSources==null)
			{
				_poSources = new Dictionary<ARTran, IAPTranSource>(Base.Transactions.Cache.GetComparer());
			}
			_poSources.Add(arTran, poSource);
			return poSource;
		}

		protected virtual (POOrder, POLine, SOOrder, SOLine) GetOrderData(ARTran arTran)
		{
			if (string.IsNullOrEmpty(arTran.SOOrderNbr))
				return (null, null, null, null);
			PXResult<SOLine> resOrder = SelectFrom<SOLine>
				.InnerJoin<SOOrder>.On<SOLine.FK.Order>
				.LeftJoin<POOrder>.On<SOOrder.FK.IntercompanyPOOrder>
				.LeftJoin<POLine>.On<POLine.FK.Order
					.And<SOLine.intercompanyPOLineNbr.IsEqual<POLine.lineNbr>>>
				.Where<SOLine.orderType.IsEqual<ARTran.sOOrderType.FromCurrent>
					.And<SOLine.orderNbr.IsEqual<ARTran.sOOrderNbr.FromCurrent>
					.And<SOLine.lineNbr.IsEqual<ARTran.sOOrderLineNbr.FromCurrent>>>>
				.View.SelectSingleBound(Base, new[] { arTran });
			return (PXResult.Unwrap<POOrder>(resOrder),
				PXResult.Unwrap<POLine>(resOrder),
				PXResult.Unwrap<SOOrder>(resOrder),
				resOrder);
		}

		protected virtual (POReceipt, POReceiptLine) GetReceiptData(ARTran arTran)
		{
			if (string.IsNullOrEmpty(arTran.SOOrderNbr))
				return (null, null);
			PXResult res;
			if (arTran.SOOrderLineOperation == SOOperation.Issue)
			{
				res = SelectFrom<SOShipment>
					.LeftJoin<SOShipLine>.On<SOShipLine.FK.Shipment
						.And<SOShipLine.lineNbr.IsEqual<ARTran.sOShipmentLineNbr.FromCurrent>>>
					.LeftJoin<POReceipt>.On<POReceipt.FK.IntercompanyShipment>
					.LeftJoin<POReceiptLine>.On<POReceiptLine.FK.Receipt
						.And<POReceiptLine.intercompanyShipmentLineNbr.IsEqual<SOShipLine.lineNbr>>>
					.Where<SOShipment.shipmentNbr.IsEqual<ARTran.sOShipmentNbr.FromCurrent>>
					.View.SelectSingleBound(Base, new[] { arTran });
			}
			else
			{
				res = SelectFrom<SOLine>
					.InnerJoin<SOOrder>.On<SOLine.FK.Order>
					.LeftJoin<POReceipt>.On<POReceipt.receiptType.IsEqual<POReceiptType.poreturn>
						.And<POReceipt.receiptNbr.IsEqual<SOOrder.intercompanyPOReturnNbr>>>
					.LeftJoin<POReceiptLine>.On<POReceiptLine.FK.Receipt
						.And<POReceiptLine.lineNbr.IsEqual<SOLine.intercompanyPOLineNbr>>>
					.Where<SOLine.orderType.IsEqual<ARTran.sOOrderType.FromCurrent>
						.And<SOLine.orderNbr.IsEqual<ARTran.sOOrderNbr.FromCurrent>>
						.And<SOLine.lineNbr.IsEqual<ARTran.sOOrderLineNbr.FromCurrent>>>
					.View.SelectSingleBound(Base, new[] { arTran });
			}
			return (PXResult.Unwrap<POReceipt>(res), PXResult.Unwrap<POReceiptLine>(res));
		}

		protected virtual void ARTran_ExpenseAccountID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e,
			PXFieldDefaulting baseFunc)
		{
			ARTran tran = (ARTran)e.Row;

			if (tran != null && tran.IsStockItem != true && tran.AccrueCost == true &&
				Base.customer.Current?.IsBranch == true && tran.SOOrderNbr != null)
			{
				SOOrderType ordertype = SOOrderType.PK.Find(sender.Graph, tran.SOOrderType);

				if (ordertype != null)
				{
					switch (ordertype.IntercompanyCOGSAcctDefault)
					{
						case SOIntercompanyAcctDefault.MaskItem:
							InventoryItem item = IN.InventoryItem.PK.Find(sender.Graph, tran.InventoryID);
							if (item == null) break;
							e.NewValue = Base.GetValue<InventoryItem.cOGSAcctID>(item);
							e.Cancel = true;
							break;
						case SOIntercompanyAcctDefault.MaskLocation:
							if (Base.customer.Current == null) break;
							e.NewValue = Base.GetValue<Customer.cOGSAcctID>(Base.customer.Current);
							e.Cancel = true;
							break;
					}
				}
			}
			else
			{
				baseFunc(sender, e);
			}
		}
	}
}
