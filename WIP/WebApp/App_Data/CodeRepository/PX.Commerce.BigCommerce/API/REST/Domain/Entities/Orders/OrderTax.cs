using System;
using PX.Commerce.BigCommerce.API.REST;
using Newtonsoft.Json;
using PX.Commerce.Core;
using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
	[CommerceDescription(BigCommerceCaptions.OrdersTax)]
	public class OrdersTaxData : BCAPIEntity
	{
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

		[JsonProperty("order_address_id")]
        public int OrderAddressId { get; set; }

		[JsonProperty("order_product_id", NullValueHandling = NullValueHandling.Ignore)]
		public decimal OrderProductId { get; set; }

		[JsonProperty("tax_rate_id")]
        public int TaxRateId { get; set; }

		[JsonProperty("tax_class_id")]
		public int TaxClassId { get; set; }

		[JsonProperty("class")]
		public string Class { get; set; }

		[JsonProperty("name")]
		[CommerceDescription(BigCommerceCaptions.TaxName, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public string Name { get; set; }

        [JsonProperty("rate")]
		[CommerceDescription(BigCommerceCaptions.TaxRate, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public decimal Rate { get; set; }

        [JsonProperty("priority")]
		[CommerceDescription(BigCommerceCaptions.TaxPriority, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public int Priority { get; set; }

		[JsonProperty("priority_amount")]
		[CommerceDescription(BigCommerceCaptions.TaxPriorityAmount, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public decimal PriorityAmount { get; set; }

		[JsonProperty("line_amount")]
		[CommerceDescription(BigCommerceCaptions.TaxLineAmount, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public decimal LineAmount { get; set; }

		[JsonProperty("line_item_type", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(BigCommerceCaptions.TaxLineItemType, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public string LineItemType { get; set; }
	}
}
