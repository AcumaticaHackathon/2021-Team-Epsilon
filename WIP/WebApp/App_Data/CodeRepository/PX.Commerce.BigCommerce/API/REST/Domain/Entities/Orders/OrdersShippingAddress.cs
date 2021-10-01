using Newtonsoft.Json;
using PX.Commerce.Core;
using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
	[CommerceDescription(BigCommerceCaptions.OrdersShippingAddress)]
	public class OrdersShippingAddressData : BCAPIEntity
    {
        /// <summary>
        /// The base cost of the shipping for the items associated with the shipping address.
        /// decimal(20,4)
        /// </summary>
        [JsonProperty("base_cost")]
        [CommerceDescription(BigCommerceCaptions.BaseCost, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
        public virtual decimal BaseCost { get; set; }

        /// <summary>
        /// The base handling cost.
        /// decimal(20,4)
        /// </summary>
        [JsonProperty("base_handling_cost")]
        [CommerceDescription(BigCommerceCaptions.BaseHandlingCost, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
        public virtual decimal BaseHandlingCost { get; set; }

        /// <summary>
        /// The city or suburb of the address.
        /// [string(50)]
        /// </summary>
        [JsonProperty("city")]
        [CommerceDescription(BigCommerceCaptions.City, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
        public virtual string City { get; set; }

        /// <summary>
        /// The company of the addressee.
        /// [string(100)]
        /// </summary>
        [JsonProperty("company")]
        [CommerceDescription(BigCommerceCaptions.CompanyName, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
        public virtual string Company { get; set; }

        /// <summary>
        /// The cost of the shipping excluding tax. 
        /// decimal(20,4)
        /// </summary>
        [JsonProperty("cost_ex_tax")]
        [CommerceDescription(BigCommerceCaptions.CostExcludingTax, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
        public virtual decimal CostExcludingTax { get; set; }

        /// <summary>
        /// The cost of the shipping including tax. 
        /// 
        /// decimal(20,4)
        /// </summary>
        [JsonProperty("cost_inc_tax")]
        [CommerceDescription(BigCommerceCaptions.CostIncludingTax, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
        public virtual decimal CostIncludingTax { get; set; }

        /// <summary>
        /// The amount of tax on the shipping cost. 
        /// decimal(20,4)
        /// </summary>
        [JsonProperty("cost_tax")]
        [CommerceDescription(BigCommerceCaptions.CostTax, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
        public virtual decimal CostTax { get; set; }

        /// <summary>
        /// The ID of the tax class used to tax the shipping cost. 
        /// </summary>
        [JsonProperty("cost_tax_class_id")]
        [CommerceDescription(BigCommerceCaptions.CostTaxClassId, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
        public virtual int CostTaxClassId { get; set; }

        /// <summary>
        /// The country of the address.
        /// string(50)
        /// </summary>
        [JsonProperty("country")]
        [CommerceDescription(BigCommerceCaptions.Country, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
        public virtual string Country { get; set; }

        /// <summary>
        /// The country code of the country field.
        /// string(50)
        /// </summary>
        [JsonProperty("country_iso2")]
        [CommerceDescription(BigCommerceCaptions.CountryISOCode, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
        public virtual string CountryIso2 { get; set; }

        /// <summary>
        /// The email address of the addressee.
        /// string(255)
        /// </summary>
        [JsonProperty("email")]
        [CommerceDescription(BigCommerceCaptions.Email, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
        public virtual string Email { get; set; }

        /// <summary>
        /// The handling cost excluding tax. 
        /// 
        /// decimal(20,4)
        /// </summary>
        [JsonProperty("handling_cost_ex_tax")]
        [CommerceDescription(BigCommerceCaptions.HandlingCostExcludingTax, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
        public virtual decimal HandlingCostExcludingTax { get; set; }

        /// <summary>
        /// The handling cost including tax. 
        /// decimal(20,4)
        /// </summary>
        [JsonProperty("handling_cost_inc_tax")]
        [CommerceDescription(BigCommerceCaptions.HandlingCostIncludingTax, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
        public virtual decimal HandlingCostIncludingTax { get; set; }

        /// <summary>
        /// The amount of tax on the handling cost. 
        /// decimal(20,4)
        /// </summary>
        [JsonProperty("handling_cost_tax")]
        [CommerceDescription(BigCommerceCaptions.HandlingCostTax, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
        public virtual decimal HandlingCostTax { get; set; }

        /// <summary>
        /// The ID of the tax class used to tax the handling cost. 
        /// </summary>
        [JsonProperty("handling_cost_tax_class_id")]
        [CommerceDescription(BigCommerceCaptions.HandlingCostTaxClassId, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
        public virtual int HandlingCostTaxClassId { get; set; }

        /// <summary>
        /// The first name of the addressee
        /// [string(255)]
        /// </summary>
        [JsonProperty("first_name")]
        [CommerceDescription(BigCommerceCaptions.FirstName, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
        public virtual string FirstName { get; set; }

        /// <summary>
        /// The ID of the shipping address. 
        /// </summary>
        [JsonProperty("id")]
        public virtual int? Id { get; set; }

        /// <summary>
        /// The quantity of items that have been shipped. 
        /// </summary>
        [JsonIgnore]
        [CommerceDescription(BigCommerceCaptions.ItemShipped, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
        public virtual int ItemsShipped { get; set; }

        [JsonProperty("items_shipped")]
        public virtual string ItemsShipped_InString
        {
            get
            {
                return ItemsShipped.ToString();
            }
            set
            {
                int tmpInt;
                bool wasParsed = int.TryParse(value, out tmpInt);
                if (wasParsed)
                    ItemsShipped = tmpInt;
                else
                    ItemsShipped = 0;
            }
        }

        /// <summary>
        /// The total amount of items (sum of the products * quantity) assigned 
        /// to the shipping address. 
        /// </summary>
        [JsonProperty("items_total")]
        [CommerceDescription(BigCommerceCaptions.ItemsTotal, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
        public virtual int ItemsTotal { get; set; }

        /// <summary>
        /// The last name of the addressee.
        /// [string(255)]
        /// </summary>
        [JsonProperty("last_name")]
		[CommerceDescription(BigCommerceCaptions.LastName, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public virtual string LastName { get; set; }

        /// <summary>
        /// The ID of the order this shipping address is associated with.
        /// </summary>
        [JsonProperty("order_id")]
        public virtual int? OrderId { get; set; }

        /// <summary>
        /// The phone number of the addressee.
        /// string(50)
        /// </summary>
        [JsonProperty("phone")]
        [CommerceDescription(BigCommerceCaptions.Phone, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
        public virtual string Phone { get; set; }

        /// <summary>
        /// The name of the shipping method. 
        /// string(250) 
        /// </summary>
        [JsonProperty("shipping_method")]
        [CommerceDescription(BigCommerceCaptions.ShippingMethod, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
        public virtual string ShippingMethod { get; set; }

        /// <summary>
        /// The ID of the shipping zone the shipping address is associated with. 
        /// </summary>
        [JsonProperty("shipping_zone_id")]
        [CommerceDescription(BigCommerceCaptions.ShippingZoneId, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
        public virtual int ShippingZoneId { get; set; }

        /// <summary>
        /// The name of the shipping zone the shipping address is associated with. 
        /// string(250)
        /// </summary>
        [JsonProperty("shipping_zone_name")]
        [CommerceDescription(BigCommerceCaptions.ShippingZoneName, FieldFilterStatus.Skipped, FieldMappingStatus.Import)]
        public virtual string ShippingZoneName { get; set; }

        /// <summary>
        /// The state of the address.
        /// string(50)
        /// </summary>
        [JsonProperty("state")]
        [CommerceDescription(BigCommerceCaptions.State, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
        public virtual string State { get; set; }

        /// <summary>
        /// The first street line of the address.
        /// [string(255)]
        /// </summary>
        [JsonProperty("street_1")]
		[CommerceDescription(BigCommerceCaptions.Street1, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public virtual string Street1 { get; set; }

        /// <summary>
        /// The second street line of the address.
        /// [string(255)]
        /// </summary>
        [JsonProperty("street_2")]
		[CommerceDescription(BigCommerceCaptions.Street2, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public virtual string Street2 { get; set; }

        /// <summary>
        /// The zip or postcode of the address.
        /// [string(20)]
        /// </summary>
        [JsonProperty("zip")]
		[CommerceDescription(BigCommerceCaptions.Zip, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public virtual string ZipCode { get; set; }

		public bool ShouldSerializeItemsShipped_InString()
		{
			return false;
		}
	}
}
