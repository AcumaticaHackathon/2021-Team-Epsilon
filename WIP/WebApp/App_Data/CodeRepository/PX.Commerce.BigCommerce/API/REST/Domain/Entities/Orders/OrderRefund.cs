using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PX.Commerce.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace PX.Commerce.BigCommerce.API.REST
{
	public class OrderRefund : BCAPIEntity
	{
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("order_id")]
		[Description(BigCommerceCaptions.OrderId)]
		public string OrderId { get; set; }

		[JsonProperty("reason")]
		[Description(BigCommerceCaptions.RefundReason)]
		public string Reason { get; set; }

		[JsonProperty("total_amount")]
		[Description(BigCommerceCaptions.TotalAmount)]
		public decimal? TotalAmount { get; set; }

		[JsonProperty("total_tax")]
		[Description(BigCommerceCaptions.TotalTax)]
		public decimal? TotalTax { get; set; }

		[JsonProperty("created")]
		[Description(BigCommerceCaptions.DateCreatedUT)]
		public string  DateCreated { get; set; }

		[JsonProperty("items")]
		[Description(BigCommerceCaptions.RefundedItems)]
		public List<RefundedItem> RefundedItems { get; set; }

		[JsonProperty("payments")]
		[Description(BigCommerceCaptions.Payments)]
		public List<RefundPayment> RefundPayments { get; set; }
	}
	public class RefundPayment
	{
		[JsonProperty("id")]
		[Description(BigCommerceCaptions.OrderId)]
		public int Id { get; set; }

		[JsonProperty("provider_id")]
		[Description(BigCommerceCaptions.OrderId)]
		public string ProviderId { get; set; }

		[JsonProperty("amount")]
		[Description(BigCommerceCaptions.OrderId)]
		public decimal? Amount { get; set; }

		[JsonProperty("offline")]
		[Description(BigCommerceCaptions.OrderId)]
		public bool Offline { get; set; }

		[JsonProperty("is_declined")]
		[Description(BigCommerceCaptions.OrderId)]
		public bool IsDeclined { get; set; }

		[JsonProperty("declined_message")]
		[Description(BigCommerceCaptions.OrderId)]
		public string DeclinedMessage { get; set; }
	}
	public class RefundedItem
	{
		[JsonProperty("item_id")]
		[Description(BigCommerceCaptions.ItemId)]
		public int ItemId { get; set; }

		[JsonProperty("item_type")]
		[Description(BigCommerceCaptions.ItemType)]
		public RefundItemType ItemType { get; set; }

		[JsonProperty("reason")]
		[Description(BigCommerceCaptions.RefundReason)]
		public string Reason { get; set; }

		[JsonProperty("quantity")]
		[Description(BigCommerceCaptions.Quantity)]
		public int? Quantity { get; set; }

		[JsonProperty("requested_amount")]
		public decimal? RequestedAmount { get; set; }
	}
	public class OrderRefundsList : IEntitiesResponse<OrderRefund>
	{
		[JsonProperty("data")]
		public List<OrderRefund> Data { get; set; }

		[JsonProperty("meta")]
		public Meta Meta { get; set; }
	}

	[JsonConverter(typeof(StringEnumConverter))]
	public enum RefundItemType
	{
		[EnumMember(Value = "PRODUCT")]
		Product,

		[EnumMember(Value = "GIFT_WRAPPING")]
		GiftWrapping,

		[EnumMember(Value = "SHIPPING")]
		Shipping,

		[EnumMember(Value = "HANDLING")]
		Handling,

		[EnumMember(Value = "ORDER")]
		Order
	}
}
