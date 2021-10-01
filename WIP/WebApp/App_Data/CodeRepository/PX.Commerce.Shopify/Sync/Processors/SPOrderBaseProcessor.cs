using PX.Api.ContractBased.Models;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CA;
using PX.Objects.GL;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Commerce.Shopify
{
	public abstract class SPOrderBaseProcessor<TGraph, TEntityBucket, TPrimaryMapped> : BCProcessorSingleBase<TGraph, TEntityBucket, TPrimaryMapped>, IProcessor
		where TGraph : PXGraph
		where TEntityBucket : class, IEntityBucket, new()
		where TPrimaryMapped : class, IMappedEntity, new()
	{
		public SPHelper helper = PXGraph.CreateInstance<SPHelper>();

		protected BCBinding currentBinding;
		protected InventoryItem refundItem;

		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);
			currentBinding = GetBinding();
			BCBindingExt bindingExt = GetBindingExt<BCBindingExt>();
			refundItem = bindingExt.RefundAmountItemID != null ? PX.Objects.IN.InventoryItem.PK.Find((PXGraph)this, bindingExt.RefundAmountItemID) : throw new PXException(ShopifyMessages.NoRefundItem);

			helper.Initialize(this);
		}

		#region Refunds
		public virtual SalesOrderDetail InsertRefundAmountItem(decimal amount, StringValue branch)
		{
			decimal quantity = 1;
			BCBindingExt bindingExt = GetBindingExt<BCBindingExt>();
			if (string.IsNullOrWhiteSpace(bindingExt.ReasonCode)) 
				throw new PXException(ShopifyMessages.ReasonCodeRequired);

			SalesOrderDetail detail = new SalesOrderDetail();
			detail.InventoryID = refundItem.InventoryCD?.TrimEnd().ValueField();
			detail.OrderQty = quantity.ValueField();
			detail.UOM = refundItem.BaseUnit.ValueField();
			detail.Branch = branch;
			detail.UnitPrice = amount.ValueField();
			detail.ManualPrice = true.ValueField();
			detail.ReasonCode = bindingExt?.ReasonCode?.ValueField();
			return detail;

		}
		#endregion
	}
}
