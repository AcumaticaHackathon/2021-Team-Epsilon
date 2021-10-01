using System;
using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
	class FilterAddresses : Filter
	{
		[Description("id:in")]
		public string Id { get; set; }

		[Description("customer_id:in")]
		public string CustomerId { get; set; }

		[Description("include")]
		public string Include { get; set; }
	}
}
