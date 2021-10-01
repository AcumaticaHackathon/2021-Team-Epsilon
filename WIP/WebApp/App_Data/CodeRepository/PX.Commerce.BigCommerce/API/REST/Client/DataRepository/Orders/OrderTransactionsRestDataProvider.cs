using PX.Commerce.Core;
using PX.Commerce.Core.Model;
using System;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class OrderTransactionsRestDataProvider : RestDataProviderV3, IChildReadOnlyRestDataProvider<OrdersTransactionData>
    {
		private const string id_string = "id";
		private const string parent_id_string = "parent_id";

		protected override string GetListUrl { get; } = "v3/orders/{parent_id}/transactions";
        protected override string GetSingleUrl => string.Empty; //Not implemented on Big Commerce
        protected override string GetCountUrl => string.Empty; //Not implemented on Big Commerce

		public OrderTransactionsRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
            _restClient = restClient;
		}

        public int Count(string parentId)
        {
			var segments = MakeUrlSegments(parentId);
			var result = GetCount<OrdersTransactionData, OrderTransactionsOptionList>(segments);

			return result.Count;
		}

        public List<OrdersTransactionData> Get(string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return base.Get<OrdersTransactionData, OrderTransactionsOptionList>(null, segments)?.Data;
        }

        public OrdersTransactionData GetByID(string id, string parentId)
        {
			foreach(OrdersTransactionData tran in Get(parentId))
			{
				if (tran.Id == id.ToInt()) return tran;
			}
			return null;
        }

        public IEnumerable<OrdersTransactionData> GetAll(string parentId)
        {
            var segments = MakeParentUrlSegments(parentId);
            return GetAll<OrdersTransactionData, OrderTransactionsOptionList>(null, segments);
        }
    }
}
