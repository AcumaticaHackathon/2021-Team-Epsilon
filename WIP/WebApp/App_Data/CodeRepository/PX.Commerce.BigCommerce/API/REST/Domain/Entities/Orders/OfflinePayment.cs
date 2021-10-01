using Newtonsoft.Json;
using PX.Commerce.Core;
using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class OfflinePayment
    {
        [JsonProperty("display_name")]
		[CommerceDescription(BigCommerceCaptions.DisplayName, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
        public string DisplayName { get; set; }
    }
}
