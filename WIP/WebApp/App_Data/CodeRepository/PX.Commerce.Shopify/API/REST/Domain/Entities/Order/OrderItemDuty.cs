using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using PX.Commerce.Core;

namespace PX.Commerce.Shopify.API.REST
{
	[JsonObject(Description = "Duties")]
	[Description(ShopifyCaptions.Duties)]
	public class OrderItemDuty : BCAPIEntity
	{
		[JsonProperty("id")]
		[Description(ShopifyCaptions.Id)]
		public long? Id { get; set; }

		[JsonProperty("harmonized_system_code")]
		public String HarmonizedSystemCode { get; set; }

		[JsonProperty("country_code_of_origin")]
		public String CountryCodeOfOrigin { get; set; }

		[JsonProperty("shop_money", NullValueHandling = NullValueHandling.Ignore)]
		public PresentmentPrice ShopMoney { get; set; }

		[JsonProperty("presentment_money", NullValueHandling = NullValueHandling.Ignore)]
		public PresentmentPrice PresentmentMoney { get; set; }

		[JsonProperty("tax_lines")]
		[Description(ShopifyCaptions.TaxLine)]
		public List<OrderTaxLine> TaxLines { get; set; }
	}

}