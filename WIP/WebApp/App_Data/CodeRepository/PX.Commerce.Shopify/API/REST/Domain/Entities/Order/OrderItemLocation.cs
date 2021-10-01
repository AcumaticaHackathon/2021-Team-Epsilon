using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using PX.Commerce.Core;

namespace PX.Commerce.Shopify.API.REST
{
	[JsonObject(Description = "Order Item Location")]
	[Description(ShopifyCaptions.OrderItemLocation)]
	public class OrderItemLocation : BCAPIEntity
	{
		/// <summary>
		/// The first line of the address.
		/// </summary>
		[JsonProperty("address1")]
		public string Address1 { get; set; }

		/// <summary>
		/// The second line of the address.
		/// </summary>
		[JsonProperty("address2")]
		public string Address2 { get; set; }

		/// <summary>
		/// The city the location is in.
		/// </summary>
		[JsonProperty("city")]
		public virtual string City { get; set; }

		/// <summary>
		/// The zip or postal code.
		/// </summary>
		[JsonProperty("zip")]
		public virtual string PostalCode { get; set; }

		/// <summary>
		/// The two-letter code (ISO 3166-1 alpha-2 format) corresponding to country the location is in.
		/// </summary>
		[JsonProperty("country_code")]
		public string CountryCode { get; set; }

		/// <summary>
		/// The two-letter code corresponding to province or state the location is in.
		/// </summary>
		[JsonProperty("province_code")]
		public string ProvinceCode { get; set; }

		/// <summary>
		/// The name of the location.
		/// </summary>
		[JsonProperty("name")]
		public string Name { get; set; }

		/// <summary>
		/// The ID for the location.
		/// </summary>
		[JsonProperty("id")]
		public long? Id { get; set; }
	}
}
