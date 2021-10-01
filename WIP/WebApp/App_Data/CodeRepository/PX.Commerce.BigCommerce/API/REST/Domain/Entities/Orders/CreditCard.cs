using Newtonsoft.Json;
using PX.Commerce.Core;
using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class CreditCard
    {
        [JsonProperty("card_type")]
		[CommerceDescription(BigCommerceCaptions.CardType, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public string CardType { get; set; }

        [JsonProperty("card_iin")]
		[CommerceDescription(BigCommerceCaptions.CardIin, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public string CardIin { get; set; }

        [JsonProperty("card_last4")]
		[CommerceDescription(BigCommerceCaptions.CardLast4, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public string CardLast4 { get; set; }

		[JsonProperty("card_expiry_month")]
		[CommerceDescription(BigCommerceCaptions.CardExpiryMonth, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public int CardExpiryMonth { get; set; }

		[JsonProperty("card_expiry_year")]
		[CommerceDescription(BigCommerceCaptions.CardExpiryYear, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public int CardExpiryYear { get; set; }
	}
}
