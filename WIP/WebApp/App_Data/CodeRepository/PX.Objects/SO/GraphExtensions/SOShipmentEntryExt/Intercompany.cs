using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.PO;

namespace PX.Objects.SO.GraphExtensions.SOShipmentEntryExt
{
	public class Intercompany : PXGraphExtension<SOShipmentEntry>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.interBranch>()
			&& PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

		public PXAction<SOShipment> generatePOReceipt;
		[PXUIField(DisplayName = "Generate PO Receipt", MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Select)]
		[PXButton]
		protected virtual IEnumerable GeneratePOReceipt(PXAdapter adapter)
		{
			Base.Save.Press();

			SOShipment shipment = Base.Document.Current;
			List<PXResult<SOShipLine, SOLine>> shipLines =
				SelectFrom<SOShipLine>
					.InnerJoin<SOLine>.On<SOShipLine.FK.OrderLine>
					.Where<SOShipLine.FK.Shipment.SameAsCurrent>
					.View.Select(Base)
					.Cast<PXResult<SOShipLine, SOLine>>()
					.ToList();

			PXLongOperation.StartOperation(Base, () =>
			{
				var baseGraph = PXGraph.CreateInstance<SOShipmentEntry>();
				var ext = baseGraph.GetExtension<Intercompany>();
				ext.GenerateIntercompanyPOReceipt(shipment, shipLines, null, null);
			});

			yield return shipment;
		}

		public virtual POReceipt GenerateIntercompanyPOReceipt(
			SOShipment shipment,
			List<PXResult<SOShipLine, SOLine>> shipLines,
			bool? holdValue,
			DateTime? receiptDate)
		{
			if (!string.IsNullOrEmpty(shipment.IntercompanyPOReceiptNbr)
				|| shipment.ShipmentType != SOShipmentType.Issue
				|| shipment.Operation != SOOperation.Issue)
			{
				throw new PXInvalidOperationException();
			}
			SOOrder so = SelectFrom<SOOrder>
				.InnerJoin<SOOrderShipment>.On<SOOrderShipment.FK.Order>
				.Where<SOOrderShipment.FK.Shipment.SameAsCurrent>
				.View.SelectSingleBound(Base, new[] { shipment });
			Branch vendorBranch = Branch.PK.Find(Base, so.BranchID);
			Vendor vendor = Vendor.PK.Find(Base, vendorBranch?.BAccountID);
			if (vendor == null)
			{
				throw new PXException(Messages.BranchIsNotExtendedToVendor, vendorBranch?.BranchCD.TrimEnd());
			}
			var customerBranch = PXAccess.GetBranchByBAccountID(so.CustomerID);

			POOrder po = null;
			if (so.Behavior == SOBehavior.SO)
			{
				po = POOrder.PK.Find(Base, so.IntercompanyPOType, so.IntercompanyPONbr);
			}
			else if (so.Behavior == SOBehavior.RM)
			{
				po = SelectFrom<POOrder>
					.InnerJoin<POOrderReceipt>.On<POOrderReceipt.FK.Order>
					.Where<POOrderReceipt.receiptNbr.IsEqual<@P.AsString>>
					.View.ReadOnly
					.Select(Base, so.IntercompanyPOReturnNbr);
			}

			if (po?.Cancelled == true)
			{
				throw new PXException(Messages.POCancelledPRCannotBeCreated, po.OrderNbr);
			}

			var graph = PXGraph.CreateInstance<POReceiptEntry>();
			var doc = new POReceipt
			{
				ReceiptType = POReceiptType.POReceipt,
				ReceiptDate = receiptDate,
			};
			doc = PXCache<POReceipt>.CreateCopy(graph.Document.Insert(doc));
			doc.VendorID = vendor.BAccountID;
			doc = PXCache<POReceipt>.CreateCopy(graph.Document.Update(doc));
			doc.BranchID = customerBranch.BranchID;
			doc = PXCache<POReceipt>.CreateCopy(graph.Document.Update(doc));

			doc.CuryID = po?.CuryID ?? so.CuryID;
			doc = PXCache<POReceipt>.CreateCopy(graph.Document.Update(doc));
			CurrencyInfo origCuryInfo = CurrencyInfo.PK.Find(graph, po?.CuryInfoID ?? so.CuryInfoID);
			CurrencyInfo curyInfo = graph.currencyinfo.Search<CurrencyInfo.curyInfoID>(doc.CuryInfoID);
			curyInfo.CuryRateTypeID = origCuryInfo.CuryRateTypeID;
			curyInfo = graph.currencyinfo.Update(curyInfo);

			doc.ProjectID = po?.ProjectID ?? so.ProjectID;
			doc.AutoCreateInvoice = false;
			doc.InvoiceNbr = shipment.ShipmentNbr;
			doc.IntercompanyShipmentNbr = shipment.ShipmentNbr;
			doc = graph.Document.Update(doc);

			foreach (PXResult<SOShipLine, SOLine> shipLine in shipLines)
			{
				GeneratePOReceiptLine(graph, po, shipLine, so, shipLine);
			}

			var uniquenessChecker = new UniquenessChecker<
				SelectFrom<POReceipt>
				.Where<POReceipt.FK.IntercompanyShipment.SameAsCurrent>>(shipment);
			graph.OnBeforeCommit += uniquenessChecker.OnBeforeCommitImpl;
			try
			{
				graph.Save.Press();

				if (holdValue != null)
				{
					if (holdValue == true)
						graph.putOnHold.Press();
					else
						graph.releaseFromHold.Press();
				}
			}
			finally
			{
				graph.OnBeforeCommit -= uniquenessChecker.OnBeforeCommitImpl;
			}

			return graph.Document.Current;
		}

		protected virtual POReceiptLine GeneratePOReceiptLine(
			POReceiptEntry graph,
			POOrder po,
			SOShipLine shipLine,
			SOOrder so,
			SOLine soLine)
		{
			POReceiptLine line;
			POLine poLine = null;
			if (po != null)
			{
				if (so.Behavior == SOBehavior.SO)
				{
					poLine = POLine.PK.Find(graph, po?.OrderType, po?.OrderNbr, soLine.IntercompanyPOLineNbr);
				}
				else if (so.Behavior == SOBehavior.RM)
				{
					SOLine receiptSOLine = SOLine.FK.OriginalOrderLine.FindParent(graph, soLine);
					if (receiptSOLine != null)
					{
						poLine = SelectFrom<POLine>
							.InnerJoin<POReceiptLine>.On<POReceiptLine.FK.OrderLine>
							.Where<POReceiptLine.receiptNbr.IsEqual<@P.AsString>
								.And<POReceiptLine.lineNbr.IsEqual<@P.AsInt>>>
							.View.ReadOnly
							.Select(graph, so.IntercompanyPOReturnNbr, receiptSOLine.IntercompanyPOLineNbr);
					}
				}
			}
			if (poLine != null)
			{
				line = graph.AddPOLine(poLine, true);
				line.IsLSEntryBlocked = true;
				graph.AddPOOrderReceipt(poLine.OrderType, poLine.OrderNbr);
			}
			else
			{
				line = new POReceiptLine
				{
					InventoryID = shipLine.InventoryID,
					SubItemID = shipLine.SubItemID,
					IsLSEntryBlocked = true,
				};
				line = PXCache<POReceiptLine>.CreateCopy(graph.transactions.Insert(line));
				line.ProjectID = soLine.ProjectID;
				line.TaskID = soLine.TaskID;
				line.CostCodeID = soLine.CostCodeID;
				line = graph.transactions.Update(line);
			}
			line = PXCache<POReceiptLine>.CreateCopy(line);
			line.UOM = shipLine.UOM;
			line.TranDesc = shipLine.TranDesc;
			line = PXCache<POReceiptLine>.CreateCopy(graph.transactions.Update(line));
			line.Qty = shipLine.ShippedQty;
			line = PXCache<POReceiptLine>.CreateCopy(graph.transactions.Update(line));

			line.CuryUnitCost = soLine.CuryUnitPrice;
			line.ManualPrice = true;
			line = PXCache<POReceiptLine>.CreateCopy(graph.transactions.Update(line));
			decimal? ratio = soLine.BaseQty == 0m ? 1m : shipLine.BaseQty / soLine.BaseQty;
			line.CuryExtCost = PXCurrencyAttribute.Round(graph.transactions.Cache, line, (soLine.CuryExtPrice * ratio) ?? 0m, CMPrecision.TRANCURY);
			line = PXCache<POReceiptLine>.CreateCopy(graph.transactions.Update(line));
			line.DiscPct = soLine.DiscPct;
			line = PXCache<POReceiptLine>.CreateCopy(graph.transactions.Update(line));
			line.CuryDiscAmt = PXCurrencyAttribute.Round(graph.transactions.Cache, line, (soLine.CuryDiscAmt * ratio) ?? 0m, CMPrecision.TRANCURY);
			line = PXCache<POReceiptLine>.CreateCopy(graph.transactions.Update(line));

			line.IsLSEntryBlocked = false;
			line.IntercompanyShipmentLineNbr = shipLine.LineNbr;
			line = graph.transactions.Update(line);
			graph.transactions.Cache.SetDefaultExt<POReceiptLine.locationID>(line);

			if (line.IsStkItem == true)
			{
				List<SOShipLineSplit> shipLineSplits = SelectFrom<SOShipLineSplit>
					.Where<SOShipLineSplit.FK.ShipmentLine.SameAsCurrent>
					.View.SelectMultiBound(graph, new[] { shipLine })
					.RowCast<SOShipLineSplit>().ToList();
				foreach (SOShipLineSplit shipLineSplit in shipLineSplits)
				{
					var split = new POReceiptLineSplit
					{
						InventoryID = shipLineSplit.InventoryID,
						SubItemID = shipLineSplit.SubItemID,
						LotSerialNbr = shipLineSplit.LotSerialNbr,
						ExpireDate = shipLineSplit.ExpireDate,
					};
					split = PXCache<POReceiptLineSplit>.CreateCopy(graph.splits.Insert(split));
					split.Qty = shipLineSplit.Qty;
					split = graph.splits.Update(split);
				}
			}

			return line;
		}

		protected virtual void _(Events.RowSelecting<SOShipment> eventArgs)
		{
			if (eventArgs.Row == null
				|| eventArgs.Row.ShipmentType != SOShipmentType.Issue
				|| eventArgs.Row.Operation != SOOperation.Issue)
				return;

			if (eventArgs.Row.IsIntercompany == true)
			{
				using (new PXConnectionScope())
				using (new PXReadBranchRestrictedScope())
				{
					POReceipt intercompanyPOReceipt =
						SelectFrom<POReceipt>
							.Where<POReceipt.FK.IntercompanyShipment.SameAsCurrent>
						.View.SelectSingleBound(Base, new[] { eventArgs.Row });
					eventArgs.Row.IntercompanyPOReceiptNbr = intercompanyPOReceipt?.ReceiptNbr;
				}
			}
		}

		protected virtual void _(Events.RowSelected<SOShipment> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			bool isIntercompanyIssue =
				eventArgs.Row.ShipmentType == SOShipmentType.Issue
				&& eventArgs.Row.IsIntercompany == true;
			eventArgs.Cache.Adjust<PXUIFieldAttribute>(eventArgs.Row)
				.For<SOShipment.intercompanyPOReceiptNbr>(a =>
				{
					a.Visible = isIntercompanyIssue;
					a.Enabled = false;
				})
				.For<SOShipment.excludeFromIntercompanyProc>(a =>
				{
					a.Visible = isIntercompanyIssue;
					a.Enabled = true;
				});

			if (isIntercompanyIssue)
			{
				eventArgs.Cache.AllowUpdate = true; // needed to enable ExcludeFromIntercompanyProc
				PXUIFieldAttribute.SetEnabled<SOShipment.shipmentNbr>(eventArgs.Cache, eventArgs.Row);

				Base.addSO.SetEnabled(false);
			}
		}
	}
}
