using PX.Common;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.Common.DAC;
using PX.Objects.CS;
using PX.Objects.IN;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PO.GraphExtensions.POReceiptEntryExt
{
	public class DropShipLinksExt : PXGraphExtension<POReceiptEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.dropShipments>();
		}

		[PXCopyPasteHiddenView()]
		public PXSelect<DropShipLink,
			Where<DropShipLink.pOOrderType, Equal<Required<POLine.orderType>>,
				And<DropShipLink.pOOrderNbr, Equal<Required<POLine.orderNbr>>,
				And<DropShipLink.pOLineNbr, Equal<Required<POLine.lineNbr>>>>>> DropShipLinks;

		public virtual IEnumerable dropShipLinks()
		{
			var parameters = PXView.Parameters;

			if (parameters.Length >= 3)
			{
				var key = new DropShipLink();
				key.POOrderType = (string)parameters[0];
				key.POOrderNbr = (string)parameters[1];
				key.POLineNbr = (int?)parameters[2];

				DropShipLink cached = (DropShipLink)DropShipLinks.Cache.Locate(key);
				if (cached != null && DropShipLinks.Cache.GetStatus(cached).IsNotIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted))
				{
					yield return cached;
					yield break;
				}

				cached = DropShipLink.UK.ByPOLine.FindDirty(Base, key.POOrderType, key.POOrderNbr, key.POLineNbr);
				yield return cached;
			}
			yield break;
		}

		#region CacheAttached
		[PXCustomizeBaseAttribute(typeof(PXDBStringAttribute), nameof(PXDBFieldAttribute.IsKey), false)]
		public virtual void _(Events.CacheAttached<DropShipLink.sOOrderType> e)
		{
		}

		[PXCustomizeBaseAttribute(typeof(PXDBStringAttribute), nameof(PXDBFieldAttribute.IsKey), false)]
		public virtual void _(Events.CacheAttached<DropShipLink.sOOrderNbr> e)
		{
		}

		[PXCustomizeBaseAttribute(typeof(PXDBIntAttribute), nameof(PXDBFieldAttribute.IsKey), false)]
		public virtual void _(Events.CacheAttached<DropShipLink.sOLineNbr> e)
		{
		}

		[PXCustomizeBaseAttribute(typeof(PXDBStringAttribute), nameof(PXDBFieldAttribute.IsKey), true)]
		public virtual void _(Events.CacheAttached<DropShipLink.pOOrderType> e)
		{
		}


		[PXCustomizeBaseAttribute(typeof(PXDBStringAttribute), nameof(PXDBFieldAttribute.IsKey), true)]
		public virtual void _(Events.CacheAttached<DropShipLink.pOOrderNbr> e)
		{
		}

		[PXCustomizeBaseAttribute(typeof(PXDBIntAttribute), nameof(PXDBFieldAttribute.IsKey), true)]
		public virtual void _(Events.CacheAttached<DropShipLink.pOLineNbr> e)
		{
		}
		#endregion

		#region CreateReceipt

		/// <summary>
		/// Overrides <see cref="POReceiptEntry.PrefetchDropShipLinks(POOrder)"/>
		/// </summary>
		[PXOverride] 
		public virtual void PrefetchDropShipLinks(POOrder order)
		{
			if (order == null || order.OrderType != POOrderType.DropShip || order.IsLegacyDropShip == true)
				return;

			var linesWithLinksQuery = new PXSelectReadonly2<POLine,
				LeftJoin<DropShipLink, On<DropShipLink.FK.POLine>>,
				Where<POLine.orderType, Equal<Required<POOrder.orderType>>,
					And<POLine.orderNbr, Equal<Required<POOrder.orderNbr>>>>>(Base);

			var fieldsAndTables = new[]
			{
				typeof(POLine.orderType), typeof(POLine.orderNbr), typeof(POLine.lineNbr), typeof(DropShipLink)
			};
			using (new PXFieldScope(linesWithLinksQuery.View, fieldsAndTables))
			{
				int startRow = PXView.StartRow;
				int totalRows = 0;
				foreach (PXResult<POLine, DropShipLink> record in linesWithLinksQuery.View.Select(
					PXView.Currents, PXView.Parameters, PXView.Searches, PXView.SortColumns,
					PXView.Descendings, PXView.Filters, ref startRow, PXView.MaximumRows, ref totalRows))
				{
					POLine line = record;
					var key = new PXCommandKey(new object[] { line.OrderType, line.OrderNbr, line.LineNbr });

					DropShipLinkStoreCached(record, record);
				}
			}
		}

		/// <summary>
		/// Overrides <see cref="POReceiptEntry.ValidatePOOrder(POOrder)"/>
		/// </summary>
		[PXOverride]
		public virtual void ValidatePOOrder(POOrder order)
		{
			if (order.OrderType != POOrderType.DropShip || order.IsLegacyDropShip == true)
				return;

			if (order.SOOrderNbr != null)
			{
				DemandSOOrder soOrder = POOrder.FK.DemandSOOrder.FindParent(Base, order);
				string soStatus = PXStringListAttribute.GetLocalizedLabel<DemandSOOrder.status>(Base.Caches<DemandSOOrder>(), soOrder);
				if (soOrder.Status.IsIn(SO.SOOrderStatus.Hold, SO.SOOrderStatus.PendingApproval, SO.SOOrderStatus.AwaitingPayment))
					throw new PXException(Messages.DropShipReceiptSOStatus, soStatus);
			}
		}

		/// <summary>
		/// Overrides <see cref="POReceiptEntry.ValidatePOLine(POLine, POReceipt)"/>
		/// </summary>
		[PXOverride]
		public virtual void ValidatePOLine(POLine poline, POReceipt receipt)
		{
			POOrder order = POLine.FK.Order.FindParent(Base, poline);
			if (order.OrderType != POOrderType.DropShip || order.IsLegacyDropShip == true || receipt.ReceiptType != POReceiptType.POReceipt)
				return;

			string status = PXStringListAttribute.GetLocalizedLabel<POOrder.status>(Base.Caches<POOrder>(), order);
			if (order.Status != POOrderStatus.Open)
				throw new PXException(Messages.DropShipPOHasStatus, poline.LineNbr, order.OrderNbr, status);

			if (poline.LineType.IsIn(POLineType.GoodsForDropShip, POLineType.NonStockForDropShip))
			{
				DemandSOOrder soOrder = POOrder.FK.DemandSOOrder.FindParent(Base, order);
				string soStatus = PXStringListAttribute.GetLocalizedLabel<DemandSOOrder.status>(Base.Caches<DemandSOOrder>(), soOrder);
				if (soOrder.Status.IsIn(SO.SOOrderStatus.Hold, SO.SOOrderStatus.PendingApproval, SO.SOOrderStatus.AwaitingPayment))
					throw new PXException(Messages.DropShipReceiptAddPOSOStatus, order.OrderNbr, soStatus);
			}
		}

		/// <summary>
		/// Overrides <see cref="POReceiptEntry.InsertReceiptLine(POReceiptLine, POReceipt, POLine)"/>
		/// </summary>
		[PXOverride]
		public virtual POReceiptLine InsertReceiptLine(POReceiptLine line, POReceipt receipt, POLine poline,
			Func<POReceiptLine, POReceipt, POLine, POReceiptLine> baseMethod)
		{
			POReceiptLine insertedLine = baseMethod(line, receipt, poline);
			if (line.ReceiptQty < Decimal.Zero || receipt.ReceiptType != POReceiptType.POReceipt)
				return insertedLine;

			POOrder order = POLine.FK.Order.FindParent(Base, poline);
			if (order.OrderType != POOrderType.DropShip || order.IsLegacyDropShip == true)
				return insertedLine;

			var link = GetDropShipLink(poline.OrderType, poline.OrderNbr, poline.LineNbr);
			if (link == null)
				return insertedLine;

			link.InReceipt = true;
			DropShipLinks.Update(link);
			return insertedLine;
		}

		#endregion CreateReceipt

		#region ReleaseReceipt

		/// <summary>
		/// Overrides <see cref="POReceiptEntry.GetLinesToReleaseQuery"/>
		/// </summary>
		[PXOverride]
		public virtual PXSelectBase<POReceiptLine> GetLinesToReleaseQuery(Func<PXSelectBase<POReceiptLine>> baseMethod)
		{
			PXSelectBase<POReceiptLine> query = baseMethod();

			query.Join<
				LeftJoin<DropShipLink,
					On<DropShipLink.pOOrderType, Equal<POReceiptLine.pOType>,
						And<DropShipLink.pOOrderNbr, Equal<POReceiptLine.pONbr>,
						And<DropShipLink.pOLineNbr, Equal<POReceiptLine.pOLineNbr>>>>,
				LeftJoin<SO.SOLineSplit,
					On<SO.SOLineSplit.pOType, Equal<POReceiptLine.pOType>,
						And<SO.SOLineSplit.pONbr, Equal<POReceiptLine.pONbr>,
						And<SO.SOLineSplit.pOLineNbr, Equal<POReceiptLine.pOLineNbr>,
						And<SO.SOLineSplit.pOSource, Equal<INReplenishmentSource.dropShipToOrder>>>>>>>>();

			return query;
		}

		/// <summary>
		/// Overrides <see cref="POReceiptEntry.ValidateReceiptLineOnRelease(PXResult{POReceiptLine})"/>
		/// </summary>
		[PXOverride]
		public virtual void ValidateReceiptLineOnRelease(PXResult<POReceiptLine> row)
		{
			POReceiptLine receiptLine = row;
			POOrder order = row.GetItem<POOrder>();
			DropShipLink link = row.GetItem<DropShipLink>();
			SO.SOLineSplit soLineSplit = row.GetItem<SO.SOLineSplit>();

			if (order == null || receiptLine.ReceiptType != POReceiptType.POReceipt
				|| receiptLine.LineType.IsNotIn(POLineType.GoodsForDropShip, POLineType.NonStockForDropShip))
				return;

			if (soLineSplit.OrderNbr == null || order.IsLegacyDropShip != true && link.Active != true)
				throw new PXException(Messages.DropShipReceiptLinesNotLinked, order.OrderNbr);
		}

		/// <summary>
		/// Overrides <see cref="POReceiptEntry.UpdateReceiptLineOnRelease(PXResult{POReceiptLine})"/>
		/// </summary>
		[PXOverride]
		public virtual void UpdateReceiptLineOnRelease(PXResult<POReceiptLine> row)
		{
			POReceiptLine receiptLine = row;
			POOrder order = row.GetItem<POOrder>();
			if (order == null || receiptLine.ReceiptType != POReceiptType.POReceipt
				|| receiptLine.LineType.IsNotIn(POLineType.GoodsForDropShip, POLineType.NonStockForDropShip))
				return;

			DropShipLink link = row.GetItem<DropShipLink>();
			if (link?.POOrderNbr == null)
				return;

			link = DropShipLinks.Locate(link) ?? link;
			link.BaseReceivedQty += receiptLine.BaseQty;
			link.InReceipt = HasOtherUnreleasedReceipts(receiptLine);
			DropShipLinks.Update(link);
		}

		public virtual bool HasOtherUnreleasedReceipts(POReceiptLine receiptLine)
		{
			int? otherLinesCount = PXSelectGroupBy<POReceiptLine,
				Where<POReceiptLine.released, NotEqual<True>,
					And<POReceiptLine.pOType, Equal<Required<POReceiptLine.pOType>>,
					And<POReceiptLine.pONbr, Equal<Required<POReceiptLine.pONbr>>,
					And<POReceiptLine.pOLineNbr, Equal<Required<POReceiptLine.pOLineNbr>>,
					And<Where<POReceiptLine.receiptType, NotEqual<Required<POReceiptLine.receiptType>>,
						Or<POReceiptLine.receiptNbr, NotEqual<Required<POReceiptLine.receiptNbr>>,
						Or<POReceiptLine.lineNbr, NotEqual<Required<POReceiptLine.lineNbr>>>>>>>>>>,
				Aggregate<Count>>.Select(Base, receiptLine.POType, receiptLine.PONbr, receiptLine.POLineNbr,
					receiptLine.ReceiptType, receiptLine.ReceiptNbr, receiptLine.LineNbr)
				.RowCount;
			return otherLinesCount > 0;
		}

		#endregion ReleaseReceipt

		/// <summary>
		/// Overrides <see cref="POReceiptEntry.PrefetchWithDetails"/>
		/// </summary>
		[PXOverride] 
		public virtual void PrefetchWithDetails()
		{
			POReceipt receipt = Base.Document.Current;
			if (receipt == null || receipt.ReceiptType != POReceiptType.POReceipt || receipt.LineCntr == 0)
				return;

			var linesWithLinksQuery = new PXSelectReadonly2<POReceiptLine,
				LeftJoin<DropShipLink,
					On<DropShipLink.pOOrderType, Equal<POReceiptLine.pOType>,
					And<DropShipLink.pOOrderNbr, Equal<POReceiptLine.pONbr>,
					And<DropShipLink.pOLineNbr, Equal<POReceiptLine.pOLineNbr>>>>>,
				Where<POReceiptLine.receiptType, Equal<Current<POReceipt.receiptType>>,
					And<POReceiptLine.receiptNbr, Equal<Current<POReceipt.receiptNbr>>,
					And<POReceiptLine.pOLineNbr, IsNotNull>>>>(Base);

			var fieldsAndTables = new[]
			{
				typeof(POReceiptLine.receiptType), typeof(POReceiptLine.receiptNbr), typeof(POReceiptLine.lineNbr),
				typeof(POReceiptLine.pOType), typeof(POReceiptLine.pONbr), typeof(POReceiptLine.pOLineNbr),
				typeof(DropShipLink)
			};
			using (new PXFieldScope(linesWithLinksQuery.View, fieldsAndTables))
			{
				int startRow = PXView.StartRow;
				int totalRows = 0;
				foreach (PXResult<POReceiptLine, DropShipLink> record in linesWithLinksQuery.View.Select(
					PXView.Currents, PXView.Parameters, PXView.Searches, PXView.SortColumns,
					PXView.Descendings, PXView.Filters, ref startRow, PXView.MaximumRows, ref totalRows)) 
				{
					DropShipLinkStoreCached(record, record);
				}
			}
		}


		public virtual DropShipLink GetDropShipLink(string orderType, string orderNbr, int? lineNbr)
		{
			if (orderType == null || orderNbr == null || lineNbr == null)
				return null;

			return DropShipLinks.SelectWindowed(0, 1, orderType, orderNbr, lineNbr);
		}

		public virtual void DropShipLinkStoreCached(DropShipLink link, POReceiptLine line)
		{
			if (!(DropShipLinks.Cache.Locate(link) is DropShipLink cached) || DropShipLinks.Cache.GetStatus(cached) == PXEntryStatus.Notchanged)
			{
				DropShipLinks.Cache.Hold(link);
			}
		}

		public virtual void DropShipLinkStoreCached(DropShipLink link, POLine line)
		{
			if (!(DropShipLinks.Cache.Locate(link) is DropShipLink cached) || DropShipLinks.Cache.GetStatus(cached) == PXEntryStatus.Notchanged)
			{
				DropShipLinks.Cache.Hold(link);
			}
		}

		protected virtual void _(Events.RowDeleted<POReceiptLine> e)
		{
			var link = GetDropShipLink(e.Row.POType, e.Row.PONbr, e.Row.POLineNbr);
			if (link != null)
			{
				link.InReceipt = HasOtherUnreleasedReceipts(e.Row);
				DropShipLinks.Update(link);
			}
		}

		protected virtual void _(Events.RowUpdated<POLineUOpen> e)
		{
			if (e.Row.Completed == e.OldRow.Completed)
				return;

			var link = GetDropShipLink(e.Row.OrderType, e.Row.OrderNbr, e.Row.LineNbr);
			if (link != null)
			{
				link.POCompleted = e.Row.Completed;
				DropShipLinks.Update(link);
			}
		}
	}
}
