using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.SO;

namespace PX.Objects.PO.GraphExtensions.POReceiptEntryExt
{
	public class Intercompany : PXGraphExtension<POReceiptEntry>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.interBranch>()
			&& PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

		public PXAction<POReceipt> generateSalesReturn;
		[PXUIField(DisplayName = "Generate Sales Return", MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Select)]
		[PXButton]
		protected virtual IEnumerable GenerateSalesReturn(PXAdapter adapter)
		{
			Base.Save.Press();

			POReceipt poReturn = Base.Document.Current;

			PXLongOperation.StartOperation(Base, () =>
			{
				var baseGraph = PXGraph.CreateInstance<POReceiptEntry>();
				var ext = baseGraph.GetExtension<Intercompany>();
				ext.GenerateIntercompanySOReturn(poReturn, null);
			});

			yield return poReturn;
		}

		public virtual SOOrder GenerateIntercompanySOReturn(POReceipt poReturn, string orderType)
		{
			if (!string.IsNullOrEmpty(poReturn.IntercompanySONbr)
				|| poReturn.ReceiptType != POReceiptType.POReturn)
			{
				throw new PXInvalidOperationException();
			}
			Branch customerBranch = Branch.PK.Find(Base, poReturn.BranchID);
			Customer customer = Customer.PK.Find(Base, customerBranch?.BAccountID);
			if (customer == null)
			{
				throw new PXException(Messages.BranchIsNotExtendedToCustomer, customerBranch?.BranchCD.TrimEnd());
			}
			var vendorBranch = PXAccess.GetBranchByBAccountID(poReturn.VendorID);

			var linesForSOReturn = GetLinesForSOReturn(poReturn);
			int? projectFromSO = linesForSOReturn
				.Select(res => PXResult.Unwrap<ARTran>(res))
				.FirstOrDefault(t => t?.ProjectID != null)?.ProjectID;
			int? projectID = projectFromSO ?? ProjectDefaultAttribute.NonProject();

			var graph = PXGraph.CreateInstance<SOOrderEntry>();
			string sOOrderType = orderType ?? graph.sosetup.Current.DfltIntercompanyRMAType;
			bool hold = false;
			if (PXAccess.FeatureInstalled<FeaturesSet.approvalWorkflow>())
			{
				SOSetupApproval setupApproval = graph.SetupApproval.Select(sOOrderType);
				hold = (setupApproval?.IsActive == true);
			}
			var doc = new SOOrder
			{
				OrderType = sOOrderType,
				BranchID = vendorBranch.BranchID,
				Hold = hold,
			};
			doc = PXCache<SOOrder>.CreateCopy(graph.Document.Insert(doc));
			doc.CustomerID = customer.BAccountID;
			doc.ProjectID = projectID;
			doc.IntercompanyPOReturnNbr = poReturn.ReceiptNbr;
			doc.ShipSeparately = true;
			doc = PXCache<SOOrder>.CreateCopy(graph.Document.Update(doc));
			doc.OrderDate = poReturn.ReceiptDate;
			doc.CustomerOrderNbr = poReturn.ReceiptNbr;
			doc = PXCache<SOOrder>.CreateCopy(graph.Document.Update(doc));
			doc.DisableAutomaticDiscountCalculation = true;
			doc = PXCache<SOOrder>.CreateCopy(graph.Document.Update(doc));

			doc.CuryID = poReturn.CuryID;
			doc = PXCache<SOOrder>.CreateCopy(graph.Document.Update(doc));
			CurrencyInfo origCuryInfo = CurrencyInfo.PK.Find(graph, poReturn.CuryInfoID);
			CurrencyInfo curyInfo = graph.currencyinfo.Select();
			PXCache<CurrencyInfo>.RestoreCopy(curyInfo, origCuryInfo);
			curyInfo.CuryInfoID = doc.CuryInfoID;

			AddSOReturnLines(graph, linesForSOReturn);

			graph.RowPersisted.AddHandler<SOOrder>(UpdatePOReceiptOnSOOrderRowPersisted);
			var uniquenessChecker = new UniquenessChecker<
				SelectFrom<SOOrder>
				.Where<SOOrder.FK.IntercompanyPOReturn.SameAsCurrent>>(poReturn);
			graph.OnBeforeCommit += uniquenessChecker.OnBeforeCommitImpl;
			try
			{
				graph.Save.Press();
			}
			finally
			{
				graph.RowPersisted.RemoveHandler<SOOrder>(UpdatePOReceiptOnSOOrderRowPersisted);
				graph.OnBeforeCommit -= uniquenessChecker.OnBeforeCommitImpl;
			}

			return graph.Document.Current;
		}

		protected virtual void UpdatePOReceiptOnSOOrderRowPersisted(PXCache sender, PXRowPersistedEventArgs e)
		{
			SOOrder doc = (SOOrder)e.Row;
			if (!string.IsNullOrEmpty(doc?.IntercompanyPOReturnNbr)
				&& e.Operation == PXDBOperation.Insert && e.TranStatus == PXTranStatus.Open)
			{
				PXDatabase.Update<POReceipt>(
					new PXDataFieldAssign<POReceipt.isIntercompanySOCreated>(PXDbType.Bit, true),
					new PXDataFieldRestrict<POReceipt.receiptType>(PXDbType.Char, POReceiptType.POReturn),
					new PXDataFieldRestrict<POReceipt.receiptNbr>(PXDbType.NVarChar, doc.IntercompanyPOReturnNbr));
			}
		}

		protected virtual void AddSOReturnLines(SOOrderEntry graph, List<PXResult<POReceiptLine>> linesForSOReturn)
		{
			foreach (PXResult<POReceiptLine> res in linesForSOReturn)
			{
				POReceiptLine line = res;
				ARTran tran = PXResult.Unwrap<ARTran>(res);
				var splits = SelectFrom<POReceiptLineSplit>
					.Where<POReceiptLineSplit.FK.ReceiptLine.SameAsCurrent>
					.View.ReadOnly.SelectMultiBound(graph, new[] { line })
					.RowCast<POReceiptLineSplit>().ToList();

				var soLine = new SOLine
				{
					Operation = SOOperation.Receipt,
				};
				soLine = PXCache<SOLine>.CreateCopy(graph.Transactions.Insert(soLine));
				soLine.InventoryID = line.InventoryID;
				soLine.SubItemID = line.SubItemID;
				soLine.TaxCategoryID = tran?.TaxCategoryID;
				if (tran?.ProjectID != null)
				{
					soLine.ProjectID = tran.ProjectID;
					soLine.TaskID = tran.TaskID;
					soLine.CostCodeID = tran.CostCodeID;
				}
				else
				{
					soLine.ProjectID = ProjectDefaultAttribute.NonProject();
				}
				soLine.IntercompanyPOLineNbr = line.LineNbr;
				soLine = PXCache<SOLine>.CreateCopy(graph.Transactions.Update(soLine));
				if (tran?.SiteID != null)
					soLine.SiteID = tran.SiteID;
				if (tran?.AvalaraCustomerUsageType != null)
					soLine.AvalaraCustomerUsageType = tran.AvalaraCustomerUsageType;
				soLine.TranDesc = line.TranDesc;
				soLine.UOM = line.UOM;
				soLine.ManualPrice = true;
				soLine.ManualDisc = true;
				soLine = PXCache<SOLine>.CreateCopy(graph.Transactions.Update(soLine));
				soLine.OrderQty = line.ReceiptQty;
				soLine.CuryUnitPrice = line.CuryUnitCost;
				soLine = PXCache<SOLine>.CreateCopy(graph.Transactions.Update(soLine));
				soLine.CuryExtPrice = line.CuryExtCost;
				soLine = PXCache<SOLine>.CreateCopy(graph.Transactions.Update(soLine));
				soLine.DiscPct = tran?.DiscPct ?? line.DiscPct;
				if (tran.RefNbr != null)
				{
					soLine = PXCache<SOLine>.CreateCopy(graph.Transactions.Update(soLine));
					soLine.InvoiceType = tran.TranType;
					soLine.InvoiceNbr = tran.RefNbr;
					soLine.InvoiceLineNbr = tran.LineNbr;
					soLine.InvoiceDate = tran.TranDate;
					soLine.OrigOrderType = tran.SOOrderType;
					soLine.OrigOrderNbr = tran.SOOrderNbr;
					soLine.OrigLineNbr = tran.SOOrderLineNbr;
					soLine.SalesPersonID = tran.SalesPersonID;
					soLine.Commissionable = tran.Commissionable;
				}
				soLine = graph.Transactions.Update(soLine);

				if (splits.Count > 1 || splits.Any(s => !string.IsNullOrEmpty(s.LotSerialNbr)))
				{
					graph.lsselect.RaiseRowDeleted(graph.Transactions.Cache, soLine);
					foreach (POReceiptLineSplit split in splits)
					{
						SOLineSplit soSplit = PXCache<SOLineSplit>.CreateCopy(graph.splits.Insert());
						soSplit.SubItemID = split.SubItemID;
						soSplit.LotSerialNbr = split.LotSerialNbr;
						soSplit.ExpireDate = split.ExpireDate;
						soSplit.UOM = split.UOM;
						soSplit = PXCache<SOLineSplit>.CreateCopy(graph.splits.Update(soSplit));
						soSplit.Qty = split.Qty;
						soSplit = graph.splits.Update(soSplit);
					}
				}
			}
		}

		protected virtual List<PXResult<POReceiptLine>> GetLinesForSOReturn(POReceipt poReturn)
		{
			PXView view = new PXView(Base, true,
				new SelectFrom<POReceiptLine>
				.LeftJoin<POReceiptLine2>.On<POReceiptLine2.receiptNbr.IsEqual<POReceiptLine.origReceiptNbr>
					.And<POReceiptLine2.lineNbr.IsEqual<POReceiptLine.origReceiptLineNbr>>>
				.LeftJoin<POReceipt>.On<POReceiptLine.FK.OriginalReceipt>
				.LeftJoin<SOShipLine>.On<SOShipLine.shipmentNbr.IsEqual<POReceipt.intercompanyShipmentNbr>
					.And<SOShipLine.lineNbr.IsEqual<POReceiptLine2.intercompanyShipmentLineNbr>>>
				.LeftJoin<ARTran>.On<ARTran.sOShipmentType.IsEqual<SOShipLine.shipmentType>
					.And<ARTran.sOShipmentNbr.IsEqual<SOShipLine.shipmentNbr>>
					.And<ARTran.sOShipmentLineNbr.IsEqual<SOShipLine.lineNbr>>
					.And<ARTran.released.IsEqual<True>>>
				.Where<POReceiptLine.FK.Receipt.SameAsCurrent>());

			using (new PXFieldScope(view, typeof(POReceiptLine), typeof(ARTran)))
			{
				return view.SelectMultiBound(new[] { poReturn })
					.Cast<PXResult<POReceiptLine>>().ToList();
			}
		}

		protected virtual void _(Events.RowSelecting<POReceipt> eventArgs)
		{
			if (eventArgs.Row == null
				|| eventArgs.Row.ReceiptType != POReceiptType.POReturn)
				return;

			if (eventArgs.Row.IsIntercompany == true)
			{
				using (new PXConnectionScope())
				using (new PXReadBranchRestrictedScope())
				{
					SOOrder intercompanySOReturn =
						SelectFrom<SOOrder>
							.Where<SOOrder.FK.IntercompanyPOReturn.SameAsCurrent>
						.View.SelectSingleBound(Base, new[] { eventArgs.Row });
					eventArgs.Row.IntercompanySOType = intercompanySOReturn?.OrderType;
					eventArgs.Row.IntercompanySONbr = intercompanySOReturn?.OrderNbr;
					eventArgs.Row.IntercompanySOCancelled = intercompanySOReturn?.Cancelled;
				}
			}
		}

		protected virtual void _(Events.RowSelected<POReceipt> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			eventArgs.Cache.Adjust<PXUIFieldAttribute>(eventArgs.Row)
				.For<POReceipt.intercompanyShipmentNbr>(a =>
				{
					a.Visible = (eventArgs.Row.ReceiptType == POReceiptType.POReceipt && eventArgs.Row.IsIntercompany == true);
					a.Enabled = false;
				})
				.For<POReceipt.intercompanySOType>(a =>
				{
					a.Visible = (eventArgs.Row.ReceiptType == POReceiptType.POReturn && eventArgs.Row.IsIntercompany == true);
					a.Enabled = false;
				})
				.SameFor<POReceipt.intercompanySONbr>()
				.For<POReceipt.excludeFromIntercompanyProc>(a =>
				{
					a.Visible = (eventArgs.Row.ReceiptType == POReceiptType.POReturn && eventArgs.Row.IsIntercompany == true);
					a.Enabled = true;
				});

			if (eventArgs.Row.IsIntercompany == true && eventArgs.Row.IntercompanySONbr != null && eventArgs.Row.IntercompanySOCancelled == true)
			{
				Base.Document.Cache.RaiseExceptionHandling<POReceipt.intercompanySONbr>(eventArgs.Row, eventArgs.Row.IntercompanySONbr,
					new PXSetPropertyException(Messages.IntercompanySOReturnCancelled, PXErrorLevel.Warning, eventArgs.Row.IntercompanySONbr));
			}
			else if (eventArgs.Row.IsIntercompanySOCreated == true && eventArgs.Row.IntercompanySONbr == null)
			{
				Base.Document.Cache.RaiseExceptionHandling<POReceipt.intercompanySONbr>(eventArgs.Row, eventArgs.Row.IntercompanySONbr,
					new PXSetPropertyException(Messages.RelatedSalesOrderDeleted, PXErrorLevel.Warning));
			}
		}

		[PXOverride]
		public virtual int? GetNonStockExpenseAccount(POReceiptLine row, InventoryItem item,
			Func<POReceiptLine, InventoryItem, int?> baseFunc)
		{
			if (row != null && row.AccrueCost != true && row.PONbr == null && 
				item.StkItem != true && Base.vendor.Current?.IsBranch == true &&
				Base.apsetup.Current?.IntercompanyExpenseAccountDefault == APAcctSubDefault.MaskLocation)
			{
				return Base.location.Current?.VExpenseAcctID;
			}
			else
			{
				return baseFunc(row, item);
			}
		}
	}
}
