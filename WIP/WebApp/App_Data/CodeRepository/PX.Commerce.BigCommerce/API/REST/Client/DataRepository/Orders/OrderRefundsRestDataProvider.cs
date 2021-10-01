using PX.Commerce.Core;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
	public class OrderRefundsRestDataProvider : RestDataProviderV3, IChildReadOnlyRestDataProvider<OrderRefund>
	{
		private const string id_string = "id";
		private const string parent_id_string = "parent_id";

		protected override string GetListUrl { get; } = "v3/orders/{parent_id}/payment_actions/refunds";
		protected override string GetSingleUrl => string.Empty; //Not implemented on Big Commerce
		protected override string GetCountUrl => string.Empty; //Not implemented on Big Commerce

		public OrderRefundsRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
			_restClient = restClient;
		}

		public int Count(string parentId)
		{
			var segments = MakeUrlSegments(parentId);
			var result = GetCount<ProductsVariantData, ProductVariantList>(segments);

			return result.Count;
		}

		public List<OrderRefund> Get(string parentId)
		{
			var segments = MakeParentUrlSegments(parentId);
			return base.Get<OrderRefund, OrderRefundsList>(null, segments)?.Data;
		}

		public OrderRefund GetByID(string id, string parentId)
		{
			foreach (OrderRefund refund in Get(parentId))
			{
				if (refund.Id == id.ToInt()) return refund;
			}
			return null;
		}

		public new IEnumerable<OrderRefund> GetAll(string externID)
		{
			throw new System.NotImplementedException();
		}
	}
}
