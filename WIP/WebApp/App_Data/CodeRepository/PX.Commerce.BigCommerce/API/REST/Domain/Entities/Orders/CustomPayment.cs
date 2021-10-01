using Newtonsoft.Json;
using PX.Commerce.Core;
using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class CustomPayment
    {
        [JsonProperty("payment_method")]
		[CommerceDescription(BigCommerceCaptions.PaymentMethod, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public string PaymentMethod { get; set; }
    }
}
