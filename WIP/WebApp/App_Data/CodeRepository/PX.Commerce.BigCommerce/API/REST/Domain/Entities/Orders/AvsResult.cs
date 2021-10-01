using Newtonsoft.Json;
using PX.Commerce.Core;
using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class AvsResult
    {
        [JsonProperty("code")]
		[CommerceDescription(BigCommerceCaptions.Code, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public string Code { get; set; }

        [JsonProperty("message")]
		[CommerceDescription(BigCommerceCaptions.Message, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public string Message { get; set; }

        [JsonProperty("street_match")]
		[CommerceDescription(BigCommerceCaptions.StreetMatch, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public string StreetMatch { get; set; }

		[JsonProperty("postal_match")]
		[CommerceDescription(BigCommerceCaptions.PostalMatch, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public string PostalMatch { get; set; }
    }
}
