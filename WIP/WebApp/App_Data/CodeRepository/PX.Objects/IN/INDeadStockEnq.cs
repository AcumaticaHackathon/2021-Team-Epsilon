using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN.DAC.Unbound;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN
{
	[TableAndChartDashboardType]
	public class INDeadStockEnq : PXGraph<INDeadStockEnq>
	{
		public PXCancel<INDeadStockEnqFilter> Cancel;
		
		public PXFilter<INDeadStockEnqFilter> Filter;

		[PXFilterable]
		public PXSelect<INDeadStockEnqResult> Result;

		public PXSetup<Company> Company;

		public INDeadStockEnq()
		{
			Result.AllowDelete = false;
			Result.AllowInsert = false;
			Result.AllowUpdate = false;
		}

		protected virtual IEnumerable result()
		{
			var filter = Filter.Current;

			if (!ValidateFilter(filter))
				return new INDeadStockEnqResult[0];

			GetStartDates(filter, out DateTime? inStockSince, out DateTime? noSalesSince);
			PXSelectBase<INSiteStatus> command = CreateCommand();
			var parameters = AddFilters(filter, command, inStockSince, noSalesSince);

			var singleRow = GetRowByPrimaryKeys(command, filter, inStockSince, noSalesSince);
			if (singleRow != null)
				return new INDeadStockEnqResult[] { singleRow };

			bool userSortsFilters = ValidateViewSortsFilters();

			var result = new PXDelegateResult();
			result.IsResultFiltered = !userSortsFilters;
			result.IsResultSorted = !userSortsFilters;
			int resultCounter = 0;

			foreach (PXResult<INSiteStatus> row in command.Select(parameters.ToArray()))
			{
				INDeadStockEnqResult newResult = MakeResult(row, inStockSince, noSalesSince);
				if (newResult == null)
					continue;

				result.Add(new PXResult<INDeadStockEnqResult, InventoryItem>(newResult, row.GetItem<InventoryItem>()));
				resultCounter++;

				if (!userSortsFilters && (PXView.StartRow + PXView.MaximumRows) <= resultCounter)
					break;
			}

			return result;
		}

		protected virtual bool ValidateFilter(INDeadStockEnqFilter filter)
		{
			if (filter == null || filter.SiteID == null || filter.SelectBy == null)
				return false;

			if (filter.SelectBy == INDeadStockEnqFilter.selectBy.Days &&
				filter.InStockDays == null &&
				filter.NoSalesDays == null)
				return false;

			if (filter.SelectBy == INDeadStockEnqFilter.selectBy.Date &&
				filter.InStockSince == null &&
				filter.NoSalesSince == null)
				return false;

			return true;
		}

		protected virtual void GetStartDates(INDeadStockEnqFilter filter, out DateTime? inStockSince, out DateTime? noSalesSince)
		{
			switch (filter.SelectBy)
			{
				case INDeadStockEnqFilter.selectBy.Days:
					inStockSince = filter.InStockDays == null ? (DateTime?)null :
						GetCurrentDate().AddDays(-1 * (int)filter.InStockDays);

					noSalesSince = filter.NoSalesDays == null ? (DateTime?)null :
						GetCurrentDate().AddDays(-1 * (int)filter.NoSalesDays);
					break;
				case INDeadStockEnqFilter.selectBy.Date:
					inStockSince = filter.InStockSince;
					noSalesSince = filter.NoSalesSince;
					break;
				default:
					throw new NotImplementedException();
			}
		}

		protected virtual DateTime GetCurrentDate()
			=> Accessinfo.BusinessDate.Value.Date;

		protected virtual bool ValidateViewSortsFilters()
		{
			if ((PXView.Filters?.Length ?? 0) != 0)
				return true;

			if ((PXView.SortColumns?.Length ?? 0) != 0 &&
					(PXView.SortColumns.Length != Result.Cache.Keys.Count ||
						!PXView.SortColumns.SequenceEqual(Result.Cache.Keys, StringComparer.OrdinalIgnoreCase) ||
						PXView.Descendings?.Any(d => d != false) == true))
				return true;

			if (PXView.ReverseOrder)
				return true;

			if (PXView.Searches?.Any(v => v != null) == true)
				return true;

			return false;
		}

		protected virtual PXSelectBase<INSiteStatus> CreateCommand()
		{
			return new SelectFrom<INSiteStatus>
				.InnerJoin<InventoryItem>.On<INSiteStatus.FK.InventoryItem>
				.OrderBy<InventoryItem.inventoryCD.Asc>.View.ReadOnly(this);
		}

		protected virtual List<object> AddFilters(INDeadStockEnqFilter filter, PXSelectBase<INSiteStatus> command,
			DateTime? inStockSince, DateTime? noSalesSince)
		{
			var parameters = new List<object>();

			AddQtyOnHandFilter(command);
			AddSiteFilter(command, filter);
			AddInventoryFilter(command, filter);
			AddItemClassFilter(command, filter);
			AddNoSalesSinceFilter(command, parameters, noSalesSince);

			return parameters;
		}

		protected virtual void AddQtyOnHandFilter(PXSelectBase<INSiteStatus> command)
		{
			command.WhereAnd<Where<INSiteStatus.qtyOnHand.IsGreater<decimal0>>>();

			var fields = GetNegativePlanFields();

			// QtySOBackOrdered + QtyPOPrepared + QtySOBooked + ... < qtyOnHand
			var lastField = fields.Last();
			var whereTypes = new List<Type>() { typeof(Where<,>) };
			whereTypes.AddRange(
				fields.Where(field => field != lastField)
				.SelectMany(field => new[] { typeof(Add<,>), field }));
			whereTypes.Add(lastField);
			whereTypes.Add(typeof(Less<INSiteStatus.qtyOnHand>));

			var whereNegativePlansLessOnHand = BqlCommand.Compose(whereTypes.ToArray());
			command.WhereAnd(whereNegativePlansLessOnHand);
		}

		protected virtual void AddSiteFilter(PXSelectBase<INSiteStatus> command, INDeadStockEnqFilter filter)
		{
			if (filter.SiteID != null)
			{
				command.WhereAnd<Where<INSiteStatus.siteID.IsEqual<INDeadStockEnqFilter.siteID.FromCurrent>>>();
			}
		}

		protected virtual void AddInventoryFilter(PXSelectBase<INSiteStatus> command, INDeadStockEnqFilter filter)
		{
			if (filter.InventoryID != null)
			{
				command.WhereAnd<Where<INSiteStatus.inventoryID
					.IsEqual<INDeadStockEnqFilter.inventoryID.FromCurrent>>>();
			}

			command.WhereAnd<Where<InventoryItem.itemStatus.IsNotIn<
				InventoryItemStatus.markedForDeletion, InventoryItemStatus.inactive>>>();
		}

		protected virtual void AddItemClassFilter(PXSelectBase<INSiteStatus> command, INDeadStockEnqFilter filter)
		{
			if (filter.ItemClassID != null)
			{
				command.WhereAnd<Where<InventoryItem.itemClassID.
					IsEqual<INDeadStockEnqFilter.itemClassID.FromCurrent>>>();
			}
		}

		protected virtual void AddNoSalesSinceFilter(PXSelectBase<INSiteStatus> command,
			List<object> parameters, DateTime? noSalesSince)
		{
			if (noSalesSince != null)
			{
				command.WhereAnd<Where<NotExists<SelectFrom<INItemSiteHistD>
					.Where<INItemSiteHistD.siteID.IsEqual<INSiteStatus.siteID>
						.And<INItemSiteHistD.inventoryID.IsEqual<INSiteStatus.inventoryID>>
						.And<INItemSiteHistD.subItemID.IsEqual<INSiteStatus.subItemID>>
						.And<INItemSiteHistD.sDate.IsGreaterEqual<@P.AsDateTime>>
						.And<INItemSiteHistD.qtySales.IsGreater<decimal0>>>>>>();

				parameters.Add(noSalesSince);
			}
		}

		protected virtual INDeadStockEnqResult GetRowByPrimaryKeys(PXSelectBase<INSiteStatus> command,
			INDeadStockEnqFilter filter, DateTime? inStockSince, DateTime? noSalesSince)
		{
			if (PXView.MaximumRows == 1 && PXView.StartRow == 0 &&
				PXView.Searches?.Length == Result.Cache.Keys.Count &&
				PXView.SearchColumns.Select(sc => sc.Column)
					.SequenceEqual(Result.Cache.Keys, StringComparer.OrdinalIgnoreCase) &&
				PXView.Searches.All(k => k != null))
			{
				int startRow = 0;
				int totalRows = 0;
				var rows = command.View.Select(new object[] { filter },
					PXView.Parameters, PXView.Searches, PXView.SortColumns, PXView.Descendings,
					PXView.Filters, ref startRow, PXView.MaximumRows, ref totalRows);

				foreach (var row in rows)
				{
					if (row is PXResult)
						return MakeResult(row as PXResult<INSiteStatus>, inStockSince, noSalesSince);

					return MakeResult(new PXResult<INSiteStatus>(row as INSiteStatus), inStockSince, noSalesSince);
				}
			}

			return null;
		}

		protected virtual INDeadStockEnqResult MakeResult(PXResult<INSiteStatus> selectResult,
			DateTime? inStockSince, DateTime? noSalesSince)
		{
			INSiteStatus siteStatus = selectResult;
			INItemSiteHistD currentRow = GetCurrentINItemSiteHistD(siteStatus, inStockSince, noSalesSince);

			decimal deadStockQty = currentRow?.EndQty ?? 0m;
			if (deadStockQty <= 0m)
				return null;

			decimal? negativeQty = GetNegativeQty(siteStatus, inStockSince, noSalesSince);
			deadStockQty -= negativeQty ?? 0m;
			if (deadStockQty <= 0m)
				return null;

			var result = new INDeadStockEnqResult()
			{
				BaseCuryID = Company.Current.BaseCuryID,
				DeadStockQty = deadStockQty,
				InStockQty = siteStatus.QtyOnHand,
				SiteID = siteStatus.SiteID,
				LastCost = GetLastCost(siteStatus),
				LastSaleDate = GetLastSaleDate(siteStatus),
				InventoryID = siteStatus.InventoryID,
				SubItemID = siteStatus.SubItemID
			};

			CalculateDeadStockValues(result, siteStatus, currentRow, deadStockQty);

			return result;
		}

		protected virtual INItemSiteHistD GetCurrentINItemSiteHistD(INSiteStatus siteStatus,
			DateTime? inStockSince, DateTime? noSalesSince)
		{
			return SelectFrom<INItemSiteHistD>
				.Where<INItemSiteHistD.siteID.IsEqual<INSiteStatus.siteID.FromCurrent>
					.And<INItemSiteHistD.inventoryID.IsEqual<INSiteStatus.inventoryID.FromCurrent>>
					.And<INItemSiteHistD.subItemID.IsEqual<INSiteStatus.subItemID.FromCurrent>>
					.And<INItemSiteHistD.sDate.IsLessEqual<@P.AsDateTime>>>
				.OrderBy<INItemSiteHistD.sDate.Desc>
				.View.ReadOnly.SelectSingleBound(this, new object[] { siteStatus }, inStockSince ?? noSalesSince);
		}

		protected virtual decimal? GetNegativeQty(INSiteStatus siteStatus, DateTime? inStockSince, DateTime? noSalesSince)
		{
			var siteStatusCache = Caches[typeof(INSiteStatus)];

			decimal negativeQty = 
				GetNegativePlanFields()
				.Sum(field => 
					(decimal?)siteStatusCache.GetValue(siteStatus, field.Name)) ?? 0m;

			INItemSiteHistD aggregatedLastRows = SelectFrom<INItemSiteHistD>
				.Where<INItemSiteHistD.siteID.IsEqual<INSiteStatus.siteID.FromCurrent>
					.And<INItemSiteHistD.inventoryID.IsEqual<INSiteStatus.inventoryID.FromCurrent>>
					.And<INItemSiteHistD.subItemID.IsEqual<INSiteStatus.subItemID.FromCurrent>>
					.And<INItemSiteHistD.sDate.IsGreater<@P.AsDateTime>>>
				.AggregateTo<Sum<INItemSiteHistD.qtyCredit>>
				.View.ReadOnly.SelectSingleBound(this, new object[] { siteStatus }, inStockSince ?? noSalesSince);

			negativeQty += aggregatedLastRows?.QtyCredit ?? 0m;

			return negativeQty;
		}

		protected virtual Type[] GetNegativePlanFields()
		{
			return new Type[]
			{
				typeof(INSiteStatus.qtySOBackOrdered),
				typeof(INSiteStatus.qtySOPrepared),
				typeof(INSiteStatus.qtySOBooked),
				typeof(INSiteStatus.qtySOShipping),
				typeof(INSiteStatus.qtySOShipped),
				typeof(INSiteStatus.qtyINIssues),
				typeof(INSiteStatus.qtyFSSrvOrdPrepared),
				typeof(INSiteStatus.qtyFSSrvOrdBooked),
				typeof(INSiteStatus.qtyFSSrvOrdAllocated),
				typeof(INSiteStatus.qtyINAssemblyDemand),
				typeof(INSiteStatus.qtyProductionDemand)
			};
		}

		protected virtual decimal? GetLastCost(INSiteStatus siteStatus)
		{
			INItemStats itemStats = SelectFrom<INItemStats>
				.Where<INItemStats.inventoryID.IsEqual<INSiteStatus.inventoryID.FromCurrent>
					.And<INItemStats.siteID.IsEqual<INSiteStatus.siteID.FromCurrent>>>
				.OrderBy<INItemStats.lastCostDate.Desc>
				.View.ReadOnly.SelectSingleBound(this, new object[] { siteStatus });

			return itemStats?.LastCost;
		}

		protected virtual DateTime? GetLastSaleDate(INSiteStatus siteStatus)
		{
			INItemSiteHistD lastSaleRow = SelectFrom<INItemSiteHistD>
				.Where<INItemSiteHistD.siteID.IsEqual<INSiteStatus.siteID.FromCurrent>
					.And<INItemSiteHistD.inventoryID.IsEqual<INSiteStatus.inventoryID.FromCurrent>>
					.And<INItemSiteHistD.subItemID.IsEqual<INSiteStatus.subItemID.FromCurrent>>
					.And<INItemSiteHistD.qtySales.IsGreater<decimal0>>>
				.AggregateTo<Max<INItemSiteHistD.sDate>>
				.View.ReadOnly.SelectSingleBound(this, new object[] { siteStatus });

			return lastSaleRow?.SDate;
		}

		protected virtual void CalculateDeadStockValues(INDeadStockEnqResult result,
			INSiteStatus siteStatus, INItemSiteHistD currentRow, decimal deadStockQty)
		{
			decimal deadStockQtyCounter = deadStockQty;
			result.InDeadStockDays = 0m;
			result.TotalDeadStockCost = 0m;

			IEnumerable<INItemSiteHistD> lastRows = GetLastRows(siteStatus, deadStockQty, currentRow);
			foreach (INItemSiteHistD lastRow in lastRows)
			{
				if ((lastRow.QtyDebit ?? 0m) == 0m)
					continue;

				if (CalculateDeadStockValues(ref deadStockQtyCounter, result, lastRow))
					return;
			}

			OnNotEnoughINItemSiteHistDRecords(siteStatus, currentRow, deadStockQty, deadStockQtyCounter);
		}

		protected virtual IEnumerable<INItemSiteHistD> GetLastRows(INSiteStatus siteStatus, decimal deadStockQty, INItemSiteHistD currentRow)
		{
			const int MaxRows = 1000;

			var getRows = new SelectFrom<INItemSiteHistD>
				.Where<INItemSiteHistD.siteID.IsEqual<@P.AsInt>
					.And<INItemSiteHistD.inventoryID.IsEqual<@P.AsInt>>
					.And<INItemSiteHistD.subItemID.IsEqual<@P.AsInt>>
					.And<INItemSiteHistD.sDate.IsLess<@P.AsDateTime>>
					.And<INItemSiteHistD.qtyDebit.IsGreater<decimal0>>>
				.OrderBy<INItemSiteHistD.sDate.Desc>.View.ReadOnly(this);

			DateTime? lastDate = currentRow.SDate;
			decimal deadStockQtyCounter = deadStockQty;

			yield return currentRow;

			while (lastDate != null && deadStockQtyCounter > 0m)
			{
				// Acuminator disable once PX1015 IncorrectNumberOfSelectParameters It's acuminator issue: see jira ATR-600
				PXResultset<INItemSiteHistD> rows = getRows.SelectWindowed(0, MaxRows,
					siteStatus.SiteID, siteStatus.InventoryID, siteStatus.SubItemID, lastDate);

				lastDate = null;

				foreach (var row in rows)
				{
					INItemSiteHistD newRow = row;
					yield return newRow;
					
					lastDate = newRow.SDate;
					
					deadStockQtyCounter -= newRow.QtyDebit ?? 0m;
					if (deadStockQtyCounter <= 0m)
						break;
				}
			}
		}

		protected virtual bool CalculateDeadStockValues(ref decimal deadStockQtyCounter,
			INDeadStockEnqResult result, INItemSiteHistD lastRow)
		{
			decimal qtyDebit = (decimal)lastRow.QtyDebit;
			decimal mult = (deadStockQtyCounter >= qtyDebit) ? 1m : (deadStockQtyCounter / qtyDebit);

			result.TotalDeadStockCost += (lastRow.CostDebit ?? 0m) * mult;

			decimal days = (decimal)GetCurrentDate().Subtract(lastRow.SDate.Value.Date).TotalDays;
			result.InDeadStockDays += days * qtyDebit * mult;

			deadStockQtyCounter -= qtyDebit;

			if (deadStockQtyCounter <= 0m)
			{
				result.AverageItemCost = result.TotalDeadStockCost / result.DeadStockQty;
				result.InDeadStockDays /= result.DeadStockQty;
				return true;
			}

			return false;
		}

		protected virtual void OnNotEnoughINItemSiteHistDRecords(INSiteStatus siteStatus, INItemSiteHistD currentRow, decimal deadStockQty, decimal deadStockQtyCounter)
		{
			PXTrace.WriteError(
				new Common.Exceptions.RowNotFoundException(Caches[typeof(INItemSiteHist)],
					siteStatus.SiteID,
					siteStatus.InventoryID,
					siteStatus.SubItemID,
					currentRow.SDate,
					deadStockQty,
					deadStockQtyCounter));
		}
	}
}



