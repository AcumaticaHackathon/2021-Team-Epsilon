using Newtonsoft.Json;
using PX.Commerce.Core;
using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class GiftCertificate
    {
        [JsonProperty("code")]
		[CommerceDescription(BigCommerceCaptions.Code, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public string Code { get; set; }

        [JsonProperty("original_balance")]
		[CommerceDescription(BigCommerceCaptions.OriginalBalance, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public double OriginalBalance { get; set; }

        [JsonProperty("starting_balance")]
		[CommerceDescription(BigCommerceCaptions.StartingBalance, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public double StartingBalance { get; set; }

        [JsonProperty("remaining_balance")]
		[CommerceDescription(BigCommerceCaptions.RemainingBalance, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public double RemainingBalance { get; set; }

        [JsonProperty("status")]
		[CommerceDescription(BigCommerceCaptions.GiftCertificateStatus, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public GiftCertificateStatus Status { get; set; }
    }
}
