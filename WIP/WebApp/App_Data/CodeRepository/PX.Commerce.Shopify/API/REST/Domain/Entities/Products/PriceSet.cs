using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using PX.Commerce.Core;

namespace PX.Commerce.Shopify.API.REST
{
	[CommerceDescription(ShopifyCaptions.PriceSet)]
	public class PriceSet
	{
		[JsonProperty("shop_money", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.ShopMoney, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public PresentmentPrice ShopMoney { get; set; }

		[JsonProperty("presentment_money", NullValueHandling = NullValueHandling.Ignore)]
		[CommerceDescription(ShopifyCaptions.PresentmentMoney, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
		public PresentmentPrice PresentmentMoney { get; set; }
	}
}
