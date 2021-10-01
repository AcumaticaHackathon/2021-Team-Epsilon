using PX.Api.ContractBased.Models;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Common;
using PX.Data;
using PX.Objects.CA;
using PX.Objects.GL;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.BigCommerce
{
	public abstract class BCOrderBaseProcessor<TGraph, TEntityBucket, TPrimaryMapped> : BCProcessorSingleBase<TGraph, TEntityBucket, TPrimaryMapped>,IProcessor
		where TGraph : PXGraph
		where TEntityBucket : class, IEntityBucket, new()
		where TPrimaryMapped : class, IMappedEntity, new()
	{
		public BCHelper helper = PXGraph.CreateInstance<BCHelper>();

		protected InventoryItem refundItem;
		protected BCRestClient client;
		protected OrderRestDataProvider orderDataProvider;
		protected OrderRefundsRestDataProvider orderRefundsRestDataProvider;
		protected IChildRestDataProvider<OrdersProductData> orderProductsRestDataProvider;
		protected IChildReadOnlyRestDataProvider<OrdersTaxData> orderTaxesRestDataProvider;
		protected IChildReadOnlyRestDataProvider<OrdersTransactionData> orderTransactionsRestDataProvider;
		protected IChildRestDataProvider<OrdersCouponData> orderCouponsRestDataProvider;

		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);

			client = BCConnector.GetRestClient(GetBindingExt<BCBindingBigCommerce>());
			orderDataProvider = new OrderRestDataProvider(client);
			orderProductsRestDataProvider = new OrderProductsRestDataProvider(client);
			orderTaxesRestDataProvider = new OrderTaxesRestDataProvider(client);
			orderTransactionsRestDataProvider = new OrderTransactionsRestDataProvider(client);
			orderRefundsRestDataProvider = new OrderRefundsRestDataProvider(client);
			orderCouponsRestDataProvider = new OrderCouponsRestDataProvider(client);

			var bindingExt = GetBindingExt<BCBindingExt>();
			refundItem = bindingExt?.RefundAmountItemID != null ? PX.Objects.IN.InventoryItem.PK.Find((PXGraph)this, bindingExt?.RefundAmountItemID) : throw new PXException(BigCommerceMessages.NoRefundItem);
			
			helper.Initialize(this);
		}

		public virtual SalesOrderDetail InsertRefundAmountItem(decimal amount, StringValue branch)
		{
			decimal quantity = 1;
			if (string.IsNullOrWhiteSpace(GetBindingExt<BCBindingExt>().ReasonCode)) 
				throw new PXException(BigCommerceMessages.ReasonCodeRequired);

			SalesOrderDetail detail = new SalesOrderDetail();
			detail.InventoryID = refundItem.InventoryCD?.TrimEnd().ValueField();
			detail.OrderQty = quantity.ValueField();
			detail.UOM = refundItem.BaseUnit.ValueField();
			detail.Branch = branch;
			detail.UnitPrice = amount.ValueField();
			detail.ManualPrice = true.ValueField();
			detail.ReasonCode = GetBindingExt<BCBindingExt>()?.ReasonCode?.ValueField();
			return detail;

		}
	
		public virtual decimal CalculateTaxableRefundAmount(OrdersProductData originalOrderProduct, IEnumerable<RefundedItem> shippingRefundItems, int quantity, string lineItemType)
		{
			var discountPerItem = (originalOrderProduct?.AppliedDiscounts?.Sum(x => x.DiscountAmount) ?? 0) > 0 ? originalOrderProduct?.AppliedDiscounts?.Sum(x => x.DiscountAmount) / originalOrderProduct?.Quantity : 0;
			decimal taxableAmountForRefundItem = (((originalOrderProduct?.PriceExcludingTax ?? 0) - discountPerItem) * quantity) ?? 0;
			decimal excluderefundAmount = taxableAmountForRefundItem;
			if (lineItemType.Equals(BCConstants.Shipping, StringComparison.InvariantCultureIgnoreCase))
			{
				excluderefundAmount += shippingRefundItems?.Sum(x => x.RequestedAmount ?? 0) ?? 0;
			}

			return excluderefundAmount;
		}
	}
}
