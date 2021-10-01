using Newtonsoft.Json;

namespace PX.Commerce.BigCommerce.API.REST
{
	public class Countries
	{
		[JsonProperty("id")]
		public int ID { get; set; }

		[JsonProperty("country")]
		public string Country { get; set; }

		[JsonProperty("country_iso2")]
		public string CountryCode { get; set; }

	}
}
