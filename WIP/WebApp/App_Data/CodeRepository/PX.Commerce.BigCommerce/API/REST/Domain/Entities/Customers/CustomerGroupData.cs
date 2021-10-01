using Newtonsoft.Json;
using PX.Commerce.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Commerce.BigCommerce.API.REST
{
	[JsonObject(Description = "Customer Group")]
	[CommerceDescription(BigCommerceCaptions.CustomerGroup)]
	public class CustomerGroupData : BCAPIEntity
	{
		[JsonProperty("id")]
		[CommerceDescription(BigCommerceCaptions.ID, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public virtual int? Id { get; set; }

		[JsonProperty("name")]
		[CommerceDescription(BigCommerceCaptions.Name, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public virtual string Name { get; set; }

		[JsonProperty("is_default")]
		[CommerceDescription(BigCommerceCaptions.IsDefault, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public virtual bool IsDefault { get; set; }

		[JsonProperty("category_access")]
		[CommerceDescription(BigCommerceCaptions.CategoryAccess, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public virtual CategoryAccess CategoryAccess { get; set; }

		[JsonProperty("discount_rules")]
		[CommerceDescription(BigCommerceCaptions.DiscountRule, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public virtual List<DiscountRule> DiscountRule { get; set; }
	}

	public class DiscountRule
	{
		[JsonProperty("type")]
		[CommerceDescription(BigCommerceCaptions.Type, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public virtual string Type { get; set; }

		[JsonProperty("method")]
		[CommerceDescription(BigCommerceCaptions.Method, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public virtual string Method { get; set; }

		[JsonProperty("amount")]
		[CommerceDescription(BigCommerceCaptions.Amount, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public virtual string Amount { get; set; }

		[JsonProperty("price_list_id")]
		[CommerceDescription(BigCommerceCaptions.PriceListId, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public virtual int? PriceListId { get; set; }
	}

	public class CategoryAccess
	{
		[JsonProperty("type")]
		[CommerceDescription(BigCommerceCaptions.Type, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public virtual string Type { get; set; }

		[JsonProperty("categories")]
		[CommerceDescription(BigCommerceCaptions.Categories, FieldFilterStatus.Filterable, FieldMappingStatus.Skipped)]
		public virtual string[] Categories { get; set; }
	}
}
