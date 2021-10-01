using Newtonsoft.Json;
using PX.Commerce.Core;
using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class CvvResult
    {
        [JsonProperty("code")]
		[CommerceDescription(BigCommerceCaptions.Code, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public string Code { get; set; }

        [JsonProperty("message")]
		[CommerceDescription(BigCommerceCaptions.Message, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public string Message { get; set; }
    }
}
