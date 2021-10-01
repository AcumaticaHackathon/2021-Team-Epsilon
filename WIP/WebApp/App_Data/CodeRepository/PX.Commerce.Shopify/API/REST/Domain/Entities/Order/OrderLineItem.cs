using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using PX.Commerce.Core;

namespace PX.Commerce.Shopify.API.REST
{
	/// <summary>
	/// A list of line item objects, each containing information about an item in the order.
	/// </summary>
	[JsonObject(Description = "Order Line Item")]
	[CommerceDescription(ShopifyCaptions.LineItem)]
	public class OrderLineItem : BCAPIEntity
	{
		public OrderLineItem Clone()
		{
			return (OrderLineItem)this.MemberwiseClone();
		}

		[JsonIgnore]
		public virtual Guid? PackageId { get; set; }

		/// <summary>
		/// The amount available to fulfill, calculated as follows:
		/// quantity - max(refunded_quantity, fulfilled_quantity) - pending_fulfilled_quantity - open_fulfilled_quantity
		/// </summary>
		[JsonProperty("fulfillable_quantity", NullValueHandling = NullValueHandling.Ignore)]
		public int? FulfillableQuantity { get; set; }

		/// <summary>
		/// The service provider that's fulfilling the item. Valid values: manual, or the name of the provider, such as amazon or shipwire. 
		/// </summary>
		[JsonProperty("fulfillment_service", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.FulfillmentService, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public string FulfillmentService { get; set; }

		/// <summary>
		/// How far along an order is in terms line items fulfilled. Valid values: null, fulfilled, partial, and not_eligible.
		/// </summary>
		[JsonProperty("fulfillment_status", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.FulfillmentStatus, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public OrderFulfillmentStatus? FulfillmentStatus { get; set; }

		/// <summary>
		/// The weight of the item in grams.
		/// </summary>
		[JsonProperty("grams", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Weight, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public decimal? WeightInGrams { get; set; }

		/// <summary>
		/// The ID of the line item.
		[JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Id, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public long? Id { get; set; }

		/// <summary>
		/// The ID of the Order.
		[JsonIgnore]
		public long? OrderId { get; set; }

		/// <summary>
		/// The name of the Order.
		[JsonIgnore]
		public string OrderName { get; set; }

		/// <summary>
		/// The price of the item before discounts have been applied in the shop currency.
		/// </summary>
		[JsonProperty("price", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Price, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public decimal? Price { get; set; }

		/// <summary>
		/// The price of the line item in shop and presentment currencies.
		/// </summary>
		[JsonProperty("price_set", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.PriceSet)]
		public PriceSet PriceSet { get; set; }

		/// <summary>
		/// Whether the product exists.
		/// </summary>
		[JsonProperty("product_exists", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.ProductExists)]
		public bool? ProductExists { get; set; }

		/// <summary>
		///The ID of the product that the line item belongs to. Can be null if the original product associated with the order is deleted at a later date.
		/// </summary>
		[JsonProperty("product_id", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.ProductId, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public long? ProductId { get; set; }

		/// <summary>
		/// The number of items that were purchased.
		/// </summary>
		[JsonProperty("quantity")]
		[CommerceDescription(ShopifyCaptions.Quantity, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public int? Quantity { get; set; }

		/// <summary>
		/// Whether the item requires shipping.
		/// </summary>
		[JsonProperty("requires_shipping", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.RequiresShipping, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public bool? RequiresShipping { get; set; }

		/// <summary>
		/// A unique identifier for the product variant in the shop. Required in order to connect to a FulfillmentService.
		/// </summary>
		[JsonProperty("sku", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.SKU, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public String Sku { get; set; }

		/// <summary>
		/// The title of the product.
		/// </summary>
		[JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Title, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public String Title { get; set; }

		/// <summary>
		/// The ID of the product variant.
		/// </summary>
		[JsonProperty("variant_id", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.VariantId, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public long? VariantId { get; set; }

		/// <summary>
		/// The title of the product variant.
		/// </summary>
		[JsonProperty("variant_title", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.VariantTitle, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public String VariantTitle { get; set; }

		/// <summary>
		/// The name of the inventory management system.
		/// </summary>
		[JsonProperty("variant_inventory_management", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.InventoryManagement)]
		public String VariantInventoryManagement { get; set; }

		/// <summary>
		/// The name of the item's supplier.
		/// </summary>
		[JsonProperty("vendor", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Vendor, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public String Vendor { get; set; }

		/// <summary>
		/// The name of the product variant.
		/// </summary>
		[JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Name, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public String Name { get; set; }

		/// <summary>
		/// Whether the item is a gift card. If true, then the item is not taxed or considered for shipping charges.
		/// </summary>
		[JsonProperty("gift_card", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.GiftCard, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public bool? IsGiftCard { get; set; }

		/// <summary>
		/// An array of custom information for the item that has been added to the cart. Often used to provide product customization options.
		/// </summary>
		[JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Properties)]
		public List<NameValuePair> Properties { get; set; }

		/// <summary>
		/// Whether the item was taxable.
		/// </summary>
		[JsonProperty("taxable", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.Taxable, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public bool? Taxable { get; set; }

		/// <summary>
		/// A list of tax line objects, each of which details a tax applied to the item.
		/// </summary>
		[JsonProperty("tax_lines", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TaxLine)]
		public List<OrderTaxLine> TaxLines { get; set; }

		/// <summary>
		/// The payment gateway used to tender the tip, such as shopify_payments. Present only on tips.
		/// </summary>
		[JsonProperty("tip_payment_gateway", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TipPaymentGateway, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public String TipPaymentGateway { get; set; }

		/// <summary>
		/// The payment method used to tender the tip, such as Visa. Present only on tips.
		/// </summary>
		[JsonProperty("tip_payment_method", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TipPaymentMethod, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public String TipPaymentMethod { get; set; }

		/// <summary>
		/// [Note] This field value is always 0, cannot use this to get the line item discount.
		/// The total discount amount applied to this line item in the shop currency. This value is not subtracted in the line item price.
		/// </summary>
		[JsonProperty("total_discount", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TotalDiscount, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public decimal? TotalDiscount { get; set; }

		/// <summary>
		/// The total discount applied to the line item in shop and presentment currencies.
		/// </summary>
		[JsonProperty("total_discount_set", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.TotalDiscountSet)]
		public PriceSet TotalDiscountSet { get; set; }

		/// <summary>
		/// An ordered list of amounts allocated by discount applications. Each discount allocation is associated to a particular discount application.
		/// </summary>
		[JsonProperty("discount_allocations", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.DiscountAllocation)]
		public List<OrderDiscountAllocation> DiscountAllocations { get; set; }

		/// <summary>
		/// A list of duty objects, each containing information about a duty on the line item.
		/// </summary>
		[JsonProperty("duties", NullValueHandling = NullValueHandling.Ignore)]
		public List<OrderItemDuty> Duties { get; set; }

		/// <summary>
		/// The location of the line item’s fulfillment origin.
		/// </summary>
		[JsonProperty("origin_location", NullValueHandling = NullValueHandling.Ignore)]
		public OrderItemLocation OriginLocation { get; set; }
	}
}
