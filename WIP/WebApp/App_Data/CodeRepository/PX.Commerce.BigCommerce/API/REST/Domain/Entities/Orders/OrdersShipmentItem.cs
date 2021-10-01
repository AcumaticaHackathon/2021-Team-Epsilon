using Newtonsoft.Json;
using PX.Commerce.Core;
using System;
using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
    [JsonObject(Description = "Order -> Shipment -> ShipmentItem")]
    public class OrdersShipmentItem
    {
        public OrdersShipmentItem Clone()
        {
            return (OrdersShipmentItem)this.MemberwiseClone();
        }
        /// <summary>
        /// The ID of the Order Product the item is associated with.
        /// </summary>
        [JsonProperty("order_product_id")]
        public virtual int? OrderProductId { get; set; }

        /// <summary>
        /// The ID of the Product the item is associated with.
        /// </summary>
        [JsonProperty("product_id")]
        public virtual int ProductId { get; set; }

        /// <summary>
        /// The quantity of the item in the shipment.
        /// </summary>
        [JsonProperty("quantity")]
		[CommerceDescription(BigCommerceCaptions.Quantity, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public virtual int Quantity { get; set; }

		[JsonIgnore]
		public virtual string OrderID { get; set; }

        [JsonIgnore]
        public virtual Guid? PackageId { get; set; }
    }
}
