using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.SO;

namespace PX.Objects.IN
{
	[GL.TableAndChartDashboardType]
	public class IntercompanyGoodsInTransitInq : PXGraph<IntercompanyGoodsInTransitInq>
	{
		public PXCancel<IntercompanyGoodsInTransitFilter> Cancel;

		public PXFilter<IntercompanyGoodsInTransitFilter> Filter;

		public SelectFrom<IntercompanyGoodsInTransitResult>
			.Where<IntercompanyGoodsInTransitResult.stkItem.IsEqual<True>
				.And<IntercompanyGoodsInTransitResult.operation.IsEqual<SOOperation.issue>>
				.And<IntercompanyGoodsInTransitResult.shipmentConfirmed.IsEqual<True>>
				.And<IntercompanyGoodsInTransitResult.pOReceiptNbr.IsNull
					.Or<IntercompanyGoodsInTransitResult.receiptReleased.IsEqual<False>>>
				.And<IntercompanyGoodsInTransitResult.excludeFromIntercompanyProc.IsNotEqual<True>>
				.And<IntercompanyGoodsInTransitFilter.inventoryID.FromCurrent.IsNull
					.Or<IntercompanyGoodsInTransitResult.inventoryID.IsEqual<IntercompanyGoodsInTransitFilter.inventoryID.FromCurrent>>>
				.And<IntercompanyGoodsInTransitResult.shipDate.IsLessEqual<IntercompanyGoodsInTransitFilter.shippedBefore.FromCurrent>>
				.And<IntercompanyGoodsInTransitFilter.showOverdueItems.FromCurrent.IsNotEqual<True>
					.Or<IntercompanyGoodsInTransitResult.daysOverdue.IsNotNull>>
				.And<IntercompanyGoodsInTransitFilter.showItemsWithoutReceipt.FromCurrent.IsNotEqual<True>
					.Or<IntercompanyGoodsInTransitResult.pOReceiptNbr.IsNull>>
				.And<IntercompanyGoodsInTransitFilter.sellingCompany.FromCurrent.IsNull
					.Or<IntercompanyGoodsInTransitResult.sellingBranchBAccountID.IsEqual<IntercompanyGoodsInTransitFilter.sellingCompany.FromCurrent>>>
				.And<IntercompanyGoodsInTransitFilter.sellingSiteID.FromCurrent.IsNull
					.Or<IntercompanyGoodsInTransitResult.sellingSiteID.IsEqual<IntercompanyGoodsInTransitFilter.sellingSiteID.FromCurrent>>>
				.And<IntercompanyGoodsInTransitFilter.purchasingCompany.FromCurrent.IsNull
					.Or<IntercompanyGoodsInTransitResult.purchasingBranchID.IsEqual<IntercompanyGoodsInTransitFilter.purchasingCompany.FromCurrent>>>
				.And<IntercompanyGoodsInTransitFilter.purchasingSiteID.FromCurrent.IsNull
					.Or<IntercompanyGoodsInTransitResult.purchasingSiteID.IsEqual<IntercompanyGoodsInTransitFilter.purchasingSiteID.FromCurrent>>>>
			.View.ReadOnly
			Results;

		protected virtual IEnumerable results()
		{
			using (new PXReadBranchRestrictedScope())
			{
				PXView query = new PXView(this, true, Results.View.BqlSelect);
				int startRow = PXView.StartRow;
				int totalRows = 0;

				var res = query.Select(PXView.Currents, PXView.Parameters,
					PXView.Searches, PXView.SortColumns, PXView.Descendings, PXView.Filters,
					ref startRow, PXView.MaximumRows, ref totalRows);

				PXView.StartRow = 0;

				return res;
			}
		}
	}
}
